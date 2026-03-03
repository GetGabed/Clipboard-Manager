using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ClipboardManager.Helpers;
using ClipboardManager.Models;
using ClipboardManager.Services;

namespace ClipboardManager.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly SettingsService _settingsService;
    private readonly IClipboardStorageService _storage;
    private AppSettings _settings;

    public int MaxHistoryItems
    {
        get => _settings.MaxHistoryItems;
        set
        {
            if (_settings.MaxHistoryItems != value)
            {
                _settings.MaxHistoryItems = value;
                // Live-resize the in-memory buffer immediately
                _storage.Resize(value);
                OnPropertyChanged();
            }
        }
    }

    public bool StartWithWindows
    {
        get => _settings.StartWithWindows;
        set
        {
            if (_settings.StartWithWindows != value)
            {
                _settings.StartWithWindows = value;
                OnPropertyChanged();
            }
        }
    }

    public bool DarkMode
    {
        get => _settings.DarkMode;
        set
        {
            if (_settings.DarkMode != value)
            {
                _settings.DarkMode = value;
                OnPropertyChanged();
                // Apply live — no restart required
                if (Application.Current is App app)
                    app.ApplyTheme(value);
            }
        }
    }

    public bool PersistToDisk
    {
        get => _settings.PersistToDisk;
        set
        {
            if (_settings.PersistToDisk != value)
            {
                _settings.PersistToDisk = value;
                OnPropertyChanged();
            }
        }
    }

    public string HotkeyDisplay => _settings.Hotkey.ToString();

    /// <summary>
    /// Comma-separated list of process names to exclude from clipboard capture.
    /// Bound to a TextBox in SettingsWindow.
    /// </summary>
    public string ExcludedApps
    {
        get => string.Join(", ", _settings.ExcludedApps);
        set
        {
            var list = value
                .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToLowerInvariant())
                .Where(s => s.Length > 0)
                .ToList();
            _settings.ExcludedApps = list;
            OnPropertyChanged();
        }
    }

    public int AutoExpireDays
    {
        get => _settings.AutoExpireDays;
        set
        {
            if (_settings.AutoExpireDays != value)
            {
                _settings.AutoExpireDays = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Called by <see cref="Views.SettingsWindow"/> when the user presses a new
    /// key combination in the hotkey capture box.
    /// </summary>
    public void UpdateHotkey(int modifiers, int key)
    {
        _settings.Hotkey = new HotkeyConfig { Modifiers = modifiers, Key = key };
        OnPropertyChanged(nameof(HotkeyDisplay));
    }

    public ICommand SaveCommand          { get; }
    public ICommand ResetCommand         { get; }
    public ICommand ExportHistoryCommand { get; }

    public SettingsViewModel(SettingsService settingsService, IClipboardStorageService storage)
    {
        _settingsService = settingsService;
        _storage         = storage;
        _settings        = _settingsService.Current;

        SaveCommand          = new RelayCommand(_ => Save());
        ResetCommand         = new RelayCommand(_ => Reset());
        ExportHistoryCommand = new RelayCommand(_ => ExportHistory());
    }

    private void Save()
    {
        _settingsService.Save();

        // Apply startup registry change
        StartupHelper.SetStartWithWindows(_settings.StartWithWindows);
    }

    private void Reset()
    {
        _settingsService.Reset();
        _settings = _settingsService.Current;
        OnPropertyChanged(string.Empty); // Refresh all bindings
    }

    private void ExportHistory()
    {
        try
        {
            var exportPath = Path.Combine(Path.GetTempPath(), "clipboard_export.txt");
            var lines = _storage.Items
                .Where(i => i.ContentType == ClipboardContentType.Text)
                .Select(i => $"[{i.CapturedAt:yyyy-MM-dd HH:mm}]{(i.IsPinned ? " ★" : "")}\n{i.TextContent}");

            File.WriteAllText(exportPath, string.Join("\n\n---\n\n", lines));

            Process.Start(new ProcessStartInfo("notepad.exe", $"\"{exportPath}\"")
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "[SettingsViewModel] ExportHistory failed");
            MessageBox.Show($"Export failed: {ex.Message}", "Clipboard Manager",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

