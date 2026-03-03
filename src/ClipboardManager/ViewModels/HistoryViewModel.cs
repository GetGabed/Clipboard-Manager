using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using ClipboardManager.Helpers;
using ClipboardManager.Models;
using ClipboardManager.Services;
using Serilog;

namespace ClipboardManager.ViewModels;

public class HistoryViewModel : BaseViewModel, IDisposable
{
    private readonly IClipboardStorageService _storage;
    private readonly ClipboardMonitorService _monitor;
    private readonly SettingsService? _settings;
    private readonly DispatcherTimer _searchDebounce;
    private bool _disposed;

    private string _searchText     = string.Empty;
    private ClipboardItem? _selectedItem;
    private bool _isWindowVisible;
    private bool _showPinnedOnly;

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
                // In the real app: restart the 130 ms debounce timer so rapid typing
                // doesn't flood RefreshFilter.  In unit-test context (no WPF Application)
                // fall through to an immediate call so assertions stay deterministic.
                if (System.Windows.Application.Current is not null)
                {
                    _searchDebounce.Stop();
                    _searchDebounce.Start();
                }
                else
                {
                    RefreshFilter();
                }
                OnPropertyChanged(nameof(HasSearchText));
            }
        }
    }

    /// <summary>True when the search box has text — used to show/hide the clear (✕) button.</summary>
    public bool HasSearchText => !string.IsNullOrEmpty(_searchText);

    /// <summary>Status bar label showing match count vs total, and keyboard hint.</summary>
    public string FilterStatusText
    {
        get
        {
            var total    = _storage.Items.Count;
            var filtered = FilteredItems.Count;
            if (string.IsNullOrEmpty(_searchText) && !_showPinnedOnly)
                return $"{total} item{(total == 1 ? "" : "s")} — Press Enter or ⧉ to copy";
            return $"{filtered} of {total} match{(filtered == 1 ? "" : "es")} — Press Enter or ⧉ to copy";
        }
    }

    public ClipboardItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetField(ref _selectedItem, value))
            {
                OnPropertyChanged(nameof(SelectedItemIsText));
                OnPropertyChanged(nameof(SelectedItemIsFiles));
            }
        }
    }

    public bool IsWindowVisible
    {
        get => _isWindowVisible;
        set => SetField(ref _isWindowVisible, value);
    }

    /// <summary>When true, only show pinned items.</summary>
    public bool ShowPinnedOnly
    {
        get => _showPinnedOnly;
        set
        {
            if (SetField(ref _showPinnedOnly, value))
                RefreshFilter();
        }
    }

    /// <summary>True when the selected item is plain text — drives Transform button visibility.</summary>
    public bool SelectedItemIsText  => _selectedItem?.ContentType == ClipboardContentType.Text;

    /// <summary>True when the selected item is a file drop — drives Open Folder button visibility.</summary>
    public bool SelectedItemIsFiles => _selectedItem?.ContentType == ClipboardContentType.Files;

    public int TotalCount => _storage.Items.Count;

    // ── Commands ─────────────────────────────────────────────────────────
    /// <summary>Copy the currently selected item to the clipboard (window stays open).</summary>
    public ICommand CopySelectedCommand { get; }
    /// <summary>Copy a specific item directly (used by the per-item hover button).</summary>
    public ICommand CopyItemCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand PinSelectedCommand    { get; }
    public ICommand ClearHistoryCommand   { get; }
    public ICommand ClearSearchCommand    { get; }
    /// <summary>Apply a named text transform to the selected item. Parameter = transform key string.</summary>
    public ICommand TransformCommand      { get; }
    /// <summary>Open the containing folder for the selected file item.</summary>
    public ICommand OpenFolderCommand     { get; }

    // ── Events ────────────────────────────────────────────────────────────
    /// <summary>Raised after an item is copied so the view can trigger a brief flash/toast.</summary>
    public event EventHandler<ClipboardItem>? ItemCopied;

    public HistoryViewModel(IClipboardStorageService storage, ClipboardMonitorService monitor,
                            SettingsService? settings = null)
    {
        _storage  = storage;
        _monitor  = monitor;
        _settings = settings;

        // Debounce search input — only created when running inside a WPF app (not in unit tests)
        _searchDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(130) };
        _searchDebounce.Tick += (_, _) => { _searchDebounce.Stop(); RefreshFilter(); };

        // Propagate new items → filtered list
        _storage.ItemAdded += OnItemAdded;

        CopySelectedCommand   = new RelayCommand(_ => CopySelected(),   _ => SelectedItem is not null);
        CopyItemCommand       = new RelayCommand<ClipboardItem>(item => { if (item is not null) CopyItem(item); }, item => item is not null);
        DeleteSelectedCommand = new RelayCommand(_ => DeleteSelected(), _ => SelectedItem is not null);
        PinSelectedCommand    = new RelayCommand(_ => PinSelected(),    _ => SelectedItem is not null);
        ClearHistoryCommand   = new RelayCommand(_ => ClearHistory(),   _ => _storage.Items.Count > 0);
        ClearSearchCommand    = new RelayCommand(_ => SearchText = string.Empty);
        TransformCommand      = new RelayCommand<string>(ApplyTransform, _ => SelectedItemIsText);
        OpenFolderCommand     = new RelayCommand(_ => OpenFolder(),      _ => SelectedItemIsFiles);

        RefreshFilter();
    }

    // ── Command implementations ───────────────────────────────────────────

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
        _monitor.SuppressNextCapture();
        _storage.IncrementCopyCount(item);
        _storage.SetAsCurrentClipboard(item);
        RefreshFilter();
        ItemCopied?.Invoke(this, item);
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
        _storage.SetPinned(SelectedItem, !SelectedItem.IsPinned);
        OnPropertyChanged(nameof(SelectedItem));
        RefreshFilter();
    }

    private void ClearHistory()
    {
        _storage.ClearUnpinned();
        RefreshFilter();
        SelectedItem = null;
        OnPropertyChanged(nameof(TotalCount));
    }

    // ── Text transforms ───────────────────────────────────────────────────

    private void ApplyTransform(string? transformName)
    {
        if (SelectedItem?.TextContent is null) return;
        var text = SelectedItem.TextContent;

        var result = transformName switch
        {
            "Uppercase"      => TextTransformHelper.ToUpperCase(text),
            "Lowercase"      => TextTransformHelper.ToLowerCase(text),
            "TitleCase"      => TextTransformHelper.ToTitleCase(text),
            "SentenceCase"   => TextTransformHelper.ToSentenceCase(text),
            "TrimWhitespace" => TextTransformHelper.TrimWhitespace(text),
            "RemoveSpaces"   => TextTransformHelper.RemoveExtraSpaces(text),
            "Base64Encode"   => TextTransformHelper.EncodeBase64(text),
            "Base64Decode"   => TextTransformHelper.DecodeBase64(text),
            "UrlEncode"      => TextTransformHelper.UrlEncode(text),
            "UrlDecode"      => TextTransformHelper.UrlDecode(text),
            "HtmlEncode"     => TextTransformHelper.HtmlEncode(text),
            "HtmlDecode"     => TextTransformHelper.HtmlDecode(text),
            "Reverse"        => TextTransformHelper.ReverseText(text),
            "Count"          => TextTransformHelper.CountCharacters(text),
            _                => null
        };

        if (result is null) return;

        // Add the transformed text as a new history entry and put it on the clipboard
        var newItem = new ClipboardItem { ContentType = ClipboardContentType.Text, TextContent = result };
        _monitor.SuppressNextCapture();
        _storage.Add(newItem);
        System.Windows.Clipboard.SetText(result);
        RefreshFilter();
        ItemCopied?.Invoke(this, newItem);
    }

    // ── File support ──────────────────────────────────────────────────────

    private void OpenFolder()
    {
        var path = SelectedItem?.FilePaths?.FirstOrDefault();
        if (path is null) return;
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"")
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[HistoryViewModel] OpenFolder failed");
        }
    }

    // ── Filtering ─────────────────────────────────────────────────────────
    public void RefreshFilter()
    {
        SweepExpired();

        FilteredItems.Clear();
        var query  = _searchText.Trim();
        var source = _storage.Items.OrderByDescending(i => i.IsPinned);

        foreach (var item in source)
        {
            if (_showPinnedOnly && !item.IsPinned) continue;

            if (string.IsNullOrEmpty(query) ||
                (item.TextContent?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                item.Preview.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                FilteredItems.Add(item);
            }
        }

        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(FilterStatusText));
    }

    /// <summary>Remove unpinned items older than the configured AutoExpireDays threshold.</summary>
    private void SweepExpired()
    {
        if (_settings is null) return;
        var days = _settings.Current.AutoExpireDays;
        if (days <= 0) return;
        var cutoff  = DateTime.Now.AddDays(-days);
        var expired = _storage.Items
            .Where(i => !i.IsPinned && i.CapturedAt < cutoff)
            .ToList();
        foreach (var item in expired)
            _storage.Remove(item);
    }

    private void OnItemAdded(object? sender, ClipboardItem item)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            RefreshFilter();
        });
    }

    // ── IDisposable ───────────────────────────────────────────────────────
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _storage.ItemAdded -= OnItemAdded;
    }
}

