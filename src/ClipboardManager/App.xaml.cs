using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using ClipboardManager.Services;
using ClipboardManager.ViewModels;
using ClipboardManager.Views;
using Hardcodet.Wpf.TaskbarNotification;

namespace ClipboardManager;

/// <summary>
/// Application entry-point.  Wires up all services, the system-tray icon,
/// the global hotkey, and the clipboard monitor — with no main window.
/// </summary>
public partial class App : Application
{
    // ── Services ──────────────────────────────────────────────────────────
    private SettingsService            _settingsService  = null!;
    private ClipboardStorageService    _storageService   = null!;
    private ClipboardMonitorService    _monitor          = null!;
    private HotkeyService              _hotkeyService    = null!;
    private HistoryPersistenceService  _persistence      = null!;

    // ── Windows ───────────────────────────────────────────────────────────
    private HistoryWindow?  _historyWindow;
    private SettingsWindow? _settingsWindow;
    private TaskbarIcon     _trayIcon = null!;

    // ── Startup ───────────────────────────────────────────────────────────
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Enforce single instance
        if (!EnsureSingleInstance()) { Shutdown(); return; }

        // Boot services
        _settingsService = new SettingsService();
        _storageService  = new ClipboardStorageService(_settingsService.Current.MaxHistoryItems);
        _monitor         = new ClipboardMonitorService(_storageService);
        _hotkeyService   = new HotkeyService(_settingsService.Current.Hotkey);
        _persistence     = new HistoryPersistenceService();

        // Restore history from disk (text items only, if enabled)
        if (_settingsService.Current.PersistToDisk)
        {
            var loaded = _persistence.Load(_settingsService.Current.MaxHistoryItems);
            foreach (var item in loaded)
                _storageService.Add(item);
        }

        // Build tray icon
        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        _trayIcon.Icon        = LoadAppIcon();
        _trayIcon.ContextMenu = BuildContextMenu();
        _trayIcon.TrayLeftMouseUp += (_, _) => ToggleHistoryWindow();

        // Start clipboard monitoring (requires UI thread dispatcher)
        _monitor.Attach();

        // Register global hotkey via a dedicated hidden HWND
        var hotkeyHwnd = CreateHiddenHwndSource("HotkeyHost");
        _hotkeyService.Attach(hotkeyHwnd);
        _hotkeyService.HotkeyPressed += (_, _) => ToggleHistoryWindow();
    }

    // ── Single-instance guard ─────────────────────────────────────────────
    private static System.Threading.Mutex? _mutex;
    private static bool EnsureSingleInstance()
    {
        _mutex = new System.Threading.Mutex(true, "ClipboardManager_SingleInstance",
                                            out bool created);
        return created;
    }

    // ── History window ────────────────────────────────────────────────────
    private void ToggleHistoryWindow()
    {
        if (_historyWindow is { IsVisible: true })
        {
            _historyWindow.HideWithFade();
            return;
        }

        if (_historyWindow is null)
        {
            var vm = new HistoryViewModel(_storageService, _monitor);
            _historyWindow = new HistoryWindow(vm);
        }

        _historyWindow.ViewModel.RefreshFilter();
        _historyWindow.ShowAtCursor();
    }

    // ── Context menu ─────────────────────────────────────────────────────
    private System.Windows.Controls.ContextMenu BuildContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        MenuItem("📋  Open History  (Ctrl+Shift+V)", () => ToggleHistoryWindow(), menu);
        menu.Items.Add(new System.Windows.Controls.Separator());
        MenuItem("⚙️  Settings",  OpenSettings, menu);
        menu.Items.Add(new System.Windows.Controls.Separator());
        MenuItem("✕  Exit",  () => Shutdown(), menu);

        return menu;
    }

    private static void MenuItem(string header, Action action,
                                 System.Windows.Controls.ContextMenu menu)
    {
        var item = new System.Windows.Controls.MenuItem { Header = header };
        item.Click += (_, _) => action();
        menu.Items.Add(item);
    }

    // ── Settings window ───────────────────────────────────────────────────
    private void OpenSettings()
    {
        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            return;
        }

        var vm = new SettingsViewModel(_settingsService, _storageService);
        _settingsWindow = new SettingsWindow(vm);
        _settingsWindow.ShowDialog();
    }

    // ── Shutdown ──────────────────────────────────────────────────────────
    protected override void OnExit(ExitEventArgs e)
    {        // Persist history to disk before shutting down
        if (_settingsService.Current.PersistToDisk)
            _persistence.Save(_storageService.Items, _settingsService.Current.MaxHistoryItems);
        _settingsService.Save();
        _hotkeyService.Dispose();
        _monitor.Dispose();
        _trayIcon.Dispose();
        _mutex?.ReleaseMutex();
        base.OnExit(e);
    }

    // ── Icon loading ──────────────────────────────────────────────────────
    private static HwndSource CreateHiddenHwndSource(string name)
    {
        var p = new HwndSourceParameters(name)
        {
            Width = 0, Height = 0,
            WindowStyle = 0x800000 // WS_OVERLAPPED — no chrome
        };
        return new HwndSource(p);
    }

    private static Icon LoadAppIcon()
    {
        try
        {
            var uri    = new Uri("pack://application:,,,/Resources/Icons/app.ico");
            var stream = System.Windows.Application.GetResourceStream(uri)?.Stream;
            if (stream is not null) return new Icon(stream);
        }
        catch { /* fall through to generated icon */ }

        return GenerateFallbackIcon();
    }

    private static Icon GenerateFallbackIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(45, 125, 210));
        g.DrawString("C", new Font("Arial", 18, System.Drawing.FontStyle.Bold),
                     Brushes.White, 4, 2);
        return Icon.FromHandle(bmp.GetHicon());
    }
}

