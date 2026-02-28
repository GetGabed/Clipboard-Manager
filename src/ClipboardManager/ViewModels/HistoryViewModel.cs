using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows.Input;
using ClipboardManager.Helpers;
using ClipboardManager.Models;
using ClipboardManager.Services;

namespace ClipboardManager.ViewModels;

public class HistoryViewModel : BaseViewModel
{
    private readonly IClipboardStorageService _storage;
    private readonly ClipboardMonitorService _monitor;

    private string _searchText = string.Empty;
    private ClipboardItem? _selectedItem;
    private bool _isWindowVisible;

    // ── Observable collections ────────────────────────────────────────────
    public ObservableCollection<ClipboardItem> FilteredItems { get; } = new();

    // ── Properties ───────────────────────────────────────────────────────
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value))
                RefreshFilter();
        }
    }

    public ClipboardItem? SelectedItem
    {
        get => _selectedItem;
        set => SetField(ref _selectedItem, value);
    }

    public bool IsWindowVisible
    {
        get => _isWindowVisible;
        set => SetField(ref _isWindowVisible, value);
    }

    public int TotalCount => _storage.Items.Count;

    // ── Commands ─────────────────────────────────────────────────────────
    public ICommand PasteSelectedCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand PinSelectedCommand { get; }
    public ICommand ClearHistoryCommand { get; }
    public ICommand ClearSearchCommand { get; }

    public HistoryViewModel(IClipboardStorageService storage, ClipboardMonitorService monitor)
    {
        _storage = storage;
        _monitor = monitor;

        // Propagate new items → filtered list
        _storage.ItemAdded += OnItemAdded;

        PasteSelectedCommand  = new RelayCommand(_ => PasteSelected(),  _ => SelectedItem is not null);
        DeleteSelectedCommand = new RelayCommand(_ => DeleteSelected(), _ => SelectedItem is not null);
        PinSelectedCommand    = new RelayCommand(_ => PinSelected(),    _ => SelectedItem is not null);
        ClearHistoryCommand   = new RelayCommand(_ => ClearHistory(),   _ => _storage.Items.Count > 0);
        ClearSearchCommand    = new RelayCommand(_ => SearchText = string.Empty);

        RefreshFilter();
    }

    // ── Command implementations ───────────────────────────────────────────
    private void PasteSelected()
    {
        if (SelectedItem is null) return;
        _monitor.SuppressNextCapture();
        _storage.SetAsCurrentClipboard(SelectedItem);
        IsWindowVisible = false;

        // Small delay so the window hides before the paste keystroke
        Task.Delay(150).ContinueWith(_ =>
        {
            // Window is already hidden; the item is on the clipboard — nothing else needed.
        });
    }

    private void DeleteSelected()
    {
        if (SelectedItem is null) return;
        _storage.Remove(SelectedItem);
        FilteredItems.Remove(SelectedItem);
        SelectedItem = null;
        OnPropertyChanged(nameof(TotalCount));
    }

    private void PinSelected()
    {
        if (SelectedItem is null) return;
        SelectedItem.IsPinned = !SelectedItem.IsPinned;
        OnPropertyChanged(nameof(SelectedItem));
    }

    private void ClearHistory()
    {
        _storage.Clear();
        FilteredItems.Clear();
        SelectedItem = null;
        OnPropertyChanged(nameof(TotalCount));
    }

    // ── Filtering ─────────────────────────────────────────────────────────
    public void RefreshFilter()
    {
        FilteredItems.Clear();
        var query = _searchText.Trim();

        var source = _storage.Items
            .OrderByDescending(i => i.IsPinned)
            .ThenByDescending(i => i.CapturedAt);

        foreach (var item in source)
        {
            if (string.IsNullOrEmpty(query) ||
                (item.TextContent?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                item.Preview.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                FilteredItems.Add(item);
            }
        }
    }

    private void OnItemAdded(object? sender, ClipboardItem item)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            RefreshFilter();
            OnPropertyChanged(nameof(TotalCount));
        });
    }
}
