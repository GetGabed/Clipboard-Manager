using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using ClipboardManager.Services;
using ClipboardManager.ViewModels;
using ClipboardManager.Views;
using Hardcodet.Wpf.TaskbarNotification;
using Serilog;

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
    private System.Windows.Controls.MenuItem? _historyMenuItem;

    // ── Startup ───────────────────────────────────────────────────────────
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var sw = Stopwatch.StartNew();

        // Enforce single instance
        if (!EnsureSingleInstance()) { Shutdown(); return; }

        // Initialise structured logger — writes to %AppData%\ClipboardManager\logs\app-.log
        // Clipboard content is NEVER logged; only metadata (counts, elapsed ms, errors).
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClipboardManager", "logs");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: Path.Combine(logDir, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 5_000_000)
            .CreateLogger();

        // Boot services
        _settingsService = new SettingsService();
        _storageService  = new ClipboardStorageService(_settingsService.Current.MaxHistoryItems);
        _monitor         = new ClipboardMonitorService(_storageService, _settingsService);
        _hotkeyService   = new HotkeyService(_settingsService.Current.Hotkey);
        _persistence     = new HistoryPersistenceService();

        // Restore history from disk (text items only, if enabled)
        if (_settingsService.Current.PersistToDisk)
        {
            var loaded = _persistence.Load(_settingsService.Current.MaxHistoryItems);
            foreach (var item in loaded)
                _storageService.Add(item);
        }

        // Apply dark mode (explicit setting OR system high-contrast)
        if (_settingsService.Current.DarkMode || SystemParameters.HighContrast)
            ApplyDarkMode();

        // Build tray icon
        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        _trayIcon.Icon        = LoadAppIcon();
        _trayIcon.ContextMenu = BuildContextMenu();
        _trayIcon.TrayLeftMouseUp += (_, _) => ToggleHistoryWindow();
        UpdateTrayTooltip();

        // Keep tray tooltip in sync with clipboard count
        _storageService.ItemAdded += (_, _) => UpdateTrayTooltip();

        // Start clipboard monitoring (requires UI thread dispatcher)
        _monitor.Attach();

        // Register global hotkey via a dedicated hidden HWND
        var hotkeyHwnd = CreateHiddenHwndSource("HotkeyHost");
        _hotkeyService.Attach(hotkeyHwnd);
        _hotkeyService.HotkeyPressed += (_, _) => ToggleHistoryWindow();

        sw.Stop();
        Log.Information("[Startup] complete in {ElapsedMs} ms", sw.ElapsedMilliseconds);
    }

    // ── Theme (dark / light) ──────────────────────────────────────────────
    private static readonly Uri DarkColorsUri =
        new("Resources/Styles/DarkColors.xaml", UriKind.Relative);

    /// <summary>
    /// Applies or removes the dark colour overrides at runtime without requiring
    /// a restart.  Safe to call from any thread that has dispatcher access.
    /// </summary>
    public void ApplyTheme(bool isDark)
    {
        var dicts = Resources.MergedDictionaries;

        // Remove any previously merged DarkColors dictionary
        var existing = dicts.FirstOrDefault(
            d => d.Source?.OriginalString?.Contains("DarkColors") == true ||
                 d.Source == DarkColorsUri);
        if (existing is not null) dicts.Remove(existing);

        if (isDark)
        {
            dicts.Add(new ResourceDictionary { Source = DarkColorsUri });
        }
    }

    // Keep old internal name for the startup call
    private void ApplyDarkMode() => ApplyTheme(true);

    // ── Tray tooltip ──────────────────────────────────────────────────────
    private void UpdateTrayTooltip()
    {
        var count = _storageService.Items.Count;
        _trayIcon.ToolTipText = $"Clipboard Manager \u2014 {count} item{(count == 1 ? "" : "s")}";
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
            var vm = new HistoryViewModel(_storageService, _monitor, _settingsService);
            _historyWindow = new HistoryWindow(vm, _settingsService);
        }

        _historyWindow.ViewModel.RefreshFilter();
        _historyWindow.ShowAtCursor();
    }

    // ── Context menu ─────────────────────────────────────────────────────
    private System.Windows.Controls.ContextMenu BuildContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        _historyMenuItem = AddMenuItem(
            $"\uD83D\uDCCB  Open History  ({_settingsService.Current.Hotkey})",
            () => ToggleHistoryWindow(), menu);
        menu.Items.Add(new System.Windows.Controls.Separator());
        AddMenuItem("\u2699\uFE0F  Settings",  OpenSettings, menu);
        menu.Items.Add(new System.Windows.Controls.Separator());
        AddMenuItem("\u2715  Exit",  () => Shutdown(), menu);

        return menu;
    }

    private static System.Windows.Controls.MenuItem AddMenuItem(
        string header, Action action, System.Windows.Controls.ContextMenu menu)
    {
        var item = new System.Windows.Controls.MenuItem { Header = header };
        item.Click += (_, _) => action();
        menu.Items.Add(item);
        return item;
    }

    // ── Settings window ───────────────────────────────────────────────────
    private void OpenSettings()
    {
        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            return;
        }

        // Snapshot the hotkey before showing settings so we can detect changes.
        var prevModifiers = _settingsService.Current.Hotkey.Modifiers;
        var prevKey       = _settingsService.Current.Hotkey.Key;

        var vm = new SettingsViewModel(_settingsService, _storageService);
        _settingsWindow = new SettingsWindow(vm);
        _settingsWindow.ShowDialog();

        // If hotkey changed, update the running service without restart.
        var cur = _settingsService.Current.Hotkey;
        if (cur.Modifiers != prevModifiers || cur.Key != prevKey)
        {
            _hotkeyService.UpdateConfig(cur);
            // Sync the tray menu label so it always shows the current shortcut
            if (_historyMenuItem is not null)
                _historyMenuItem.Header = $"\uD83D\uDCCB  Open History  ({cur})";
        }
    }

    // ── Shutdown ──────────────────────────────────────────────────────────
    protected override void OnExit(ExitEventArgs e)
    {        // Persist history to disk before shutting down
        if (_settingsService.Current.PersistToDisk)
            _persistence.Save(_storageService.Items, _settingsService.Current.MaxHistoryItems);
        _settingsService.Save();
        _historyWindow?.ViewModel.Dispose();
        _hotkeyService.Dispose();
        _monitor.Dispose();
        _trayIcon.Dispose();
        _mutex?.ReleaseMutex();
        Log.Information("[App] exiting");
        Log.CloseAndFlush();
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

