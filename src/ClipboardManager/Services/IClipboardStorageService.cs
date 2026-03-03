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

    /// <summary>Removes all non-pinned items from the history.</summary>
    void ClearUnpinned();

    /// <summary>Puts the item back onto the system clipboard and promotes it to newest position.</summary>
    void SetAsCurrentClipboard(ClipboardItem item);

    /// <summary>Moves <paramref name="item"/> to the top (newest slot) of the history buffer.</summary>
    void Promote(ClipboardItem item);

    /// <summary>Resizes the internal buffer to the new capacity, trimming oldest items if needed.</summary>
    void Resize(int newCapacity);

    /// <summary>
    /// Toggles the pinned state of <paramref name="item"/> while holding the storage lock,
    /// preventing races between the UI thread and any background buffer operations.
    /// </summary>
    void SetPinned(ClipboardItem item, bool pinned);

    /// <summary>Atomically increments the copy-count for <paramref name="item"/>.</summary>
    void IncrementCopyCount(ClipboardItem item);
}
