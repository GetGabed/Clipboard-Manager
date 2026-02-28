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

    public event EventHandler<ClipboardItem>? ItemAdded;

    public IReadOnlyList<ClipboardItem> Items
    {
        get { lock (_lock) { return _buffer.ToList(); } }
    }

    public ClipboardStorageService(int capacity = 200)
    {
        _buffer = new CircularBuffer<ClipboardItem>(capacity);
    }

    public void Add(ClipboardItem item)
    {
        lock (_lock)
        {
            // Skip duplicates (compare against the most-recent item)
            if (_buffer.Count > 0 && item.IsDuplicateOf(_buffer.Last()))
                return;

            _buffer.PushBack(item);
        }

        ItemAdded?.Invoke(this, item);
    }

    public void Remove(ClipboardItem item)
    {
        lock (_lock) { _buffer.Remove(item); }
    }

    public void Clear()
    {
        lock (_lock) { _buffer.Clear(); }
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
    }
}
