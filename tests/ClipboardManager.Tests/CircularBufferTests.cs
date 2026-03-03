using ClipboardManager.Helpers;
using ClipboardManager.Models;

namespace ClipboardManager.Tests;

public class CircularBufferTests
{
    private static ClipboardItem Text(string value) =>
        new() { ContentType = ClipboardContentType.Text, TextContent = value };

    [Fact]
    public void PushBack_BelowCapacity_AddsItem()
    {
        var buf = new CircularBuffer<ClipboardItem>(5, item => item.IsPinned);
        buf.PushBack(Text("a"));
        buf.PushBack(Text("b"));
        Assert.Equal(2, buf.Count);
    }

    [Fact]
    public void PushBack_AtCapacity_EvictsOldestNonPinned()
    {
        var buf = new CircularBuffer<ClipboardItem>(3, item => item.IsPinned);
        var a = Text("a");
        var b = Text("b");
        var c = Text("c");  // pinned — must survive
        c.IsPinned = true;
        var d = Text("d");

        buf.PushBack(a);
        buf.PushBack(b);
        buf.PushBack(c);
        buf.PushBack(d); // a is oldest non-pinned → evicted

        var list = buf.ToList();
        Assert.Equal(3, list.Count);
        Assert.DoesNotContain(a, list);
        Assert.Contains(b, list);
        Assert.Contains(c, list);
        Assert.Contains(d, list);
    }

    [Fact]
    public void PushBack_AllPinned_EvictsOldestAnyway()
    {
        var buf = new CircularBuffer<ClipboardItem>(2, item => item.IsPinned);
        var a = Text("a"); a.IsPinned = true;
        var b = Text("b"); b.IsPinned = true;
        var c = Text("c"); c.IsPinned = true;

        buf.PushBack(a);
        buf.PushBack(b);
        buf.PushBack(c); // a evicted even though pinned

        var list = buf.ToList();
        Assert.Equal(2, list.Count);
        Assert.DoesNotContain(a, list);
    }

    [Fact]
    public void Promote_MovesItemToNewestSlot_CountUnchanged()
    {
        var buf = new CircularBuffer<ClipboardItem>(5, item => item.IsPinned);
        var items = Enumerable.Range(0, 5)
                              .Select(i => Text(i.ToString()))
                              .ToArray();
        foreach (var it in items) buf.PushBack(it);

        buf.Promote(items[2]); // promote 3rd item

        var list = buf.ToList();
        Assert.Equal(5, list.Count);
        Assert.Same(items[2], list[^1]); // last slot = newest
    }

    [Fact]
    public void Promote_ItemNotInBuffer_IsNoOp()
    {
        var buf = new CircularBuffer<ClipboardItem>(3, item => item.IsPinned);
        buf.PushBack(Text("a"));
        var ghost = Text("ghost");

        var ex = Record.Exception(() => buf.Promote(ghost));
        Assert.Null(ex);
        Assert.Equal(1, buf.Count);
    }

    [Fact]
    public void Promote_DoesNotCauseExtraEviction()
    {
        // Buffer at full capacity: promoting an existing item should not
        // evict anything because Remove+AddLast keeps the count stable.
        var buf = new CircularBuffer<ClipboardItem>(3, item => item.IsPinned);
        var a = Text("a");
        var b = Text("b");
        var c = Text("c");
        buf.PushBack(a);
        buf.PushBack(b);
        buf.PushBack(c);

        buf.Promote(a); // count stays 3, no eviction

        Assert.Equal(3, buf.Count);
    }
}
