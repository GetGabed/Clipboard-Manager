using System.Windows.Input;
using ClipboardManager.Helpers;
using ClipboardManager.Models;
using ClipboardManager.Services;

namespace ClipboardManager.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly SettingsService _settingsService;
    private AppSettings _settings;

    public int MaxHistoryItems
    {
        get => _settings.MaxHistoryItems;
        set
        {
            if (_settings.MaxHistoryItems != value)
            {
                _settings.MaxHistoryItems = value;
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

    public ICommand SaveCommand { get; }
    public ICommand ResetCommand { get; }

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _settings = _settingsService.Current;

        SaveCommand  = new RelayCommand(_ => Save());
        ResetCommand = new RelayCommand(_ => Reset());
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
}
