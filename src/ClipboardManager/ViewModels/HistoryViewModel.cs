using System.Collections.ObjectModel;
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
            {
                RefreshFilter();
                OnPropertyChanged(nameof(HasSearchText));
            }
        }
    }

    /// <summary>True when the search box has text — used to show/hide the clear (✕) button.</summary>
    public bool HasSearchText => !string.IsNullOrEmpty(_searchText);

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
    /// <summary>Copy the currently selected item to the clipboard (window stays open).</summary>
    public ICommand CopySelectedCommand { get; }
    /// <summary>Copy a specific item directly (used by the per-item hover button).</summary>
    public ICommand CopyItemCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand PinSelectedCommand { get; }
    public ICommand ClearHistoryCommand { get; }
    public ICommand ClearSearchCommand { get; }

    // ── Events ────────────────────────────────────────────────────────────
    /// <summary>Raised after an item is copied so the view can trigger a brief flash/toast.</summary>
    public event EventHandler<ClipboardItem>? ItemCopied;

    public HistoryViewModel(IClipboardStorageService storage, ClipboardMonitorService monitor)
    {
        _storage = storage;
        _monitor = monitor;

        // Propagate new items → filtered list
        _storage.ItemAdded += OnItemAdded;

        CopySelectedCommand  = new RelayCommand(_ => CopySelected(),               _ => SelectedItem is not null);
        CopyItemCommand      = new RelayCommand<ClipboardItem>(item => { if (item is not null) CopyItem(item); }, item => item is not null);
        DeleteSelectedCommand = new RelayCommand(_ => DeleteSelected(),             _ => SelectedItem is not null);
        PinSelectedCommand    = new RelayCommand(_ => PinSelected(),               _ => SelectedItem is not null);
        ClearHistoryCommand   = new RelayCommand(_ => ClearHistory(),              _ => _storage.Items.Count > 0);
        ClearSearchCommand    = new RelayCommand(_ => SearchText = string.Empty);

        RefreshFilter();
    }

    // ── Command implementations ───────────────────────────────────────────

    /// <summary>
    /// Copies the currently selected item to the clipboard.
    /// Window stays open — only Esc dismisses it.
    /// </summary>
    private void CopySelected()
    {
        if (SelectedItem is null) return;
        CopyItem(SelectedItem);
    }

    /// <summary>
    /// Core copy logic: suppress the next monitor capture to avoid a duplicate,
    /// set the item on the system clipboard, promote it to the top, refresh the view.
    /// </summary>
    private void CopyItem(ClipboardItem item)
    {
        // SuppressNextCapture MUST be called before SetAsCurrentClipboard so the
        // WM_CLIPBOARDUPDATE event triggered by Clipboard.Set* is silently ignored.
        _monitor.SuppressNextCapture();
        _storage.SetAsCurrentClipboard(item);  // also promotes item internally
        RefreshFilter();                       // show promoted item at top immediately
        ItemCopied?.Invoke(this, item);        // notify the view to show a flash/toast
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
        RefreshFilter();  // re-sort so pinned items float to top immediately
    }

    private void ClearHistory()
    {
        _storage.ClearUnpinned();
        RefreshFilter();
        SelectedItem = null;
        OnPropertyChanged(nameof(TotalCount));
    }

    // ── Filtering ─────────────────────────────────────────────────────────
    public void RefreshFilter()
    {
        FilteredItems.Clear();
        var query = _searchText.Trim();

        // Items is already newest-first; stable sort floats pinned to top
        // while preserving insertion order (including post-promotion order).
        var source = _storage.Items
            .OrderByDescending(i => i.IsPinned);

        foreach (var item in source)
        {
            if (string.IsNullOrEmpty(query) ||
                (item.TextContent?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                item.Preview.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                FilteredItems.Add(item);
            }
        }

        OnPropertyChanged(nameof(TotalCount));
    }

    private void OnItemAdded(object? sender, ClipboardItem item)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            RefreshFilter();
        });
    }
}
