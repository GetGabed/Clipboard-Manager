using System.Collections;

namespace ClipboardManager.Helpers;

/// <summary>
/// A fixed-capacity circular buffer.  When the buffer is full the oldest
/// non-pinned item is evicted to make room for the newest one.
/// </summary>
public class CircularBuffer<T> : IEnumerable<T>
{
    private int _capacity;
    private readonly LinkedList<T> _list = new();
    private readonly Func<T, bool> _isPinned;

    public int Count    => _list.Count;
    public int Capacity => _capacity;

    /// <summary>
    /// Creates a buffer with the given capacity.
    /// <paramref name="isPinned"/> is called to determine whether an item
    /// should be skipped during eviction.  Defaults to <c>_ =&gt; false</c>
    /// (no item is ever pinned) for non-<see cref="ClipboardItem"/> types.
    /// </summary>
    public CircularBuffer(int capacity, Func<T, bool>? isPinned = null)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity  = capacity;
        _isPinned  = isPinned ?? (_ => false);
    }

    /// <summary>Adds an item to the back (newest end) of the buffer.</summary>
    public void PushBack(T item)
    {
        if (_list.Count >= _capacity)
            Evict();

        _list.AddLast(item);
    }

    /// <summary>Returns the most-recently added item without removing it.</summary>
    public T Last() => _list.Last!.Value;

    public void Remove(T item)        => _list.Remove(item);
    public void Clear()               => _list.Clear();
    public List<T> ToList()           => new(_list);
    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _list.GetEnumerator();

    /// <summary>
    /// Resizes the buffer capacity. If shrinking, oldest non-pinned items are
    /// removed first until the count fits; pinned items are evicted last.
    /// </summary>
    public void Resize(int newCapacity)
    {
        if (newCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(newCapacity));
        _capacity = newCapacity;

        // Trim excess items (oldest non-pinned first, then pinned if needed)
        while (_list.Count > _capacity)
        {
            // Find oldest non-pinned
            var node = _list.First;
            LinkedListNode<T>? candidate = null;
            while (node is not null)
            {
                if (!_isPinned(node.Value))
                {
                    candidate = node;
                    break;
                }
                node = node.Next;
            }
            if (candidate is not null)
                _list.Remove(candidate);
            else if (_list.First is not null)
                _list.RemoveFirst(); // all pinned — evict oldest
        }
    }

    /// <summary>
    /// Moves <paramref name="item"/> to the newest end of the buffer without
    /// changing the count or triggering eviction.  If the item is not present
    /// this is a no-op.
    /// </summary>
    public void Promote(T item)
    {
        var node = _list.Find(item);
        if (node is null) return;
        _list.Remove(node);
        _list.AddLast(item);
    }

    /// <summary>
    /// Evicts the oldest item in <see cref="_list"/> (front = oldest).
    /// Skips pinned items based on the <c>isPinned</c> delegate supplied at construction.
    /// </summary>
    private void Evict()
    {
        var node = _list.First;
        while (node is not null)
        {
            if (!_isPinned(node.Value))
            {
                _list.Remove(node);
                return;
            }
            node = node.Next;
        }
        // All items are pinned — remove the oldest anyway
        if (_list.First is not null)
            _list.RemoveFirst();
    }
}
