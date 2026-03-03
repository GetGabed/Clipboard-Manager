using System.Collections.Generic;
using System.Windows;
using ClipboardManager.Helpers;
using ClipboardManager.Models;

namespace ClipboardManager.Services;

/// <summary>
/// Stores clipboard history in a circular in-memory buffer.
/// Thread-safe: all public methods may be called from any thread.
/// </summary>
public class ClipboardStorageService : IClipboardStorageService
{
    private readonly CircularBuffer<ClipboardItem> _buffer;
    private readonly object _lock = new();
    private List<ClipboardItem>? _cachedItems;   // newest-first snapshot; null = dirty

    public event EventHandler<ClipboardItem>? ItemAdded;

    /// <summary>All stored items, <em>newest first</em> (buffer insertion order reversed).</summary>
    public IReadOnlyList<ClipboardItem> Items
    {
        get
        {
            lock (_lock)
            {
                if (_cachedItems is null)
                {
                    _cachedItems = _buffer.ToList();
                    _cachedItems.Reverse();
                }
                return _cachedItems;
            }
        }
    }

    public ClipboardStorageService(int capacity = 200)
    {
        _buffer = new CircularBuffer<ClipboardItem>(capacity, item => item.IsPinned);
    }

    public void Add(ClipboardItem item)
    {
        ClipboardItem toNotify;
        lock (_lock)
        {
            _cachedItems = null;  // invalidate
            // Check for a duplicate anywhere in history (not just the most-recent item).
            // If found, promote the existing entry to the top — e.g. copy A → B → A again
            // should move the original A to the top rather than creating a second A.
            var existing = _buffer.FirstOrDefault(i => item.IsDuplicateOf(i));
            if (existing is not null)
            {
                _buffer.Promote(existing);
                toNotify = existing;
            }
            else
            {
                _buffer.PushBack(item);
                toNotify = item;
            }
        }

        ItemAdded?.Invoke(this, toNotify);
    }

    public void Remove(ClipboardItem item)
    {
        lock (_lock) { _cachedItems = null; _buffer.Remove(item); }
    }

    public void Clear()
    {
        lock (_lock) { _cachedItems = null; _buffer.Clear(); }
    }

    public void ClearUnpinned()
    {
        lock (_lock)
        {
            _cachedItems = null;
            var toRemove = _buffer.Where(i => !i.IsPinned).ToList();
            foreach (var item in toRemove)
                _buffer.Remove(item);
        }
    }

    public void Promote(ClipboardItem item)
    {
        lock (_lock) { _cachedItems = null; _buffer.Promote(item); }
    }

    public void Resize(int newCapacity)
    {
        lock (_lock) { _cachedItems = null; _buffer.Resize(newCapacity); }
    }

    /// <inheritdoc/>
    public void SetPinned(ClipboardItem item, bool pinned)
    {
        lock (_lock) { item.IsPinned = pinned; }
    }

    /// <inheritdoc/>
    public void IncrementCopyCount(ClipboardItem item)
    {
        lock (_lock) { item.CopyCount++; }
    }

    public void SetAsCurrentClipboard(ClipboardItem item)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            switch (item.ContentType)
            {
                case ClipboardContentType.Text when item.TextContent is not null:
                    Clipboard.SetText(item.TextContent);
                    break;
                case ClipboardContentType.Image when item.ImageThumbnail is not null:
                    Clipboard.SetImage(item.ImageThumbnail);
                    break;
                case ClipboardContentType.Files when item.FilePaths is not null:
                    var col = new System.Collections.Specialized.StringCollection();
                    col.AddRange(item.FilePaths);
                    Clipboard.SetFileDropList(col);
                    break;
            }
        });

        // Promote the item to the newest slot so it appears at the top of
        // history when the window is next refreshed (no duplicate created
        // because SuppressNextCapture was called by the caller beforehand).
        Promote(item);
    }
}
