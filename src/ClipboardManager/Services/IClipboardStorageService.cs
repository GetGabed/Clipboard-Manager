using ClipboardManager.Models;

namespace ClipboardManager.Services;

/// <summary>Contract for clipboard history storage.</summary>
public interface IClipboardStorageService
{
    /// <summary>All currently stored clipboard items (newest first after add).</summary>
    IReadOnlyList<ClipboardItem> Items { get; }

    /// <summary>Fired on the background thread when a new item is captured.</summary>
    event EventHandler<ClipboardItem> ItemAdded;

    void Add(ClipboardItem item);
    void Remove(ClipboardItem item);
    void Clear();

    /// <summary>Puts the item back onto the system clipboard.</summary>
    void SetAsCurrentClipboard(ClipboardItem item);
}
