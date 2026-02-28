using ClipboardManager.Helpers;
using ClipboardManager.Models;
using ClipboardManager.Services;

namespace ClipboardManager.Tests;

// ── CircularBuffer ────────────────────────────────────────────────────────────

public class CircularBufferTests
{
    private static ClipboardItem Text(string value) =>
        new() { ContentType = ClipboardContentType.Text, TextContent = value };

    [Fact]
    public void PushBack_BelowCapacity_AddsItem()
    {
        var buf = new CircularBuffer<ClipboardItem>(5);
        buf.PushBack(Text("a"));
        buf.PushBack(Text("b"));
        Assert.Equal(2, buf.Count);
    }

    [Fact]
    public void PushBack_AtCapacity_EvictsOldestNonPinned()
    {
        var buf = new CircularBuffer<ClipboardItem>(3);
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
        var buf = new CircularBuffer<ClipboardItem>(2);
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
        var buf = new CircularBuffer<ClipboardItem>(5);
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
        var buf = new CircularBuffer<ClipboardItem>(3);
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
        var buf = new CircularBuffer<ClipboardItem>(3);
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

// ── ClipboardStorageService ───────────────────────────────────────────────────

public class ClipboardStorageServiceTests
{
    private static ClipboardItem Text(string value) =>
        new() { ContentType = ClipboardContentType.Text, TextContent = value };

    [Fact]
    public void Add_SameTextTwice_OnlyOneItemStored()
    {
        var svc = new ClipboardStorageService(10);
        svc.Add(Text("hello"));
        svc.Add(Text("hello"));
        Assert.Single(svc.Items);
    }

    [Fact]
    public void Add_DifferentText_BothStored()
    {
        var svc = new ClipboardStorageService(10);
        svc.Add(Text("a"));
        svc.Add(Text("b"));
        Assert.Equal(2, svc.Items.Count);
    }

    [Fact]
    public void Items_ReturnsNewestFirst()
    {
        var svc = new ClipboardStorageService(10);
        svc.Add(Text("first"));
        svc.Add(Text("second"));
        svc.Add(Text("third"));

        Assert.Equal("third",  svc.Items[0].TextContent);
        Assert.Equal("second", svc.Items[1].TextContent);
        Assert.Equal("first",  svc.Items[2].TextContent);
    }

    [Fact]
    public void ClearUnpinned_RemovesOnlyNonPinnedItems()
    {
        var svc  = new ClipboardStorageService(10);
        var pin  = Text("keep-me");
        svc.Add(Text("a"));
        svc.Add(pin);
        pin.IsPinned = true;
        svc.Add(Text("b"));

        svc.ClearUnpinned();

        Assert.Single(svc.Items);
        Assert.True(svc.Items[0].IsPinned);
        Assert.Equal("keep-me", svc.Items[0].TextContent);
    }

    [Fact]
    public void Promote_MovesItemToTopOfItems()
    {
        var svc = new ClipboardStorageService(10);
        var a = Text("a");
        var b = Text("b");
        var c = Text("c");
        svc.Add(a);
        svc.Add(b);
        svc.Add(c);

        svc.Promote(a); // was at bottom (oldest) → should be newest

        Assert.Equal(3, svc.Items.Count);
        Assert.Same(a, svc.Items[0]); // Items[0] = newest
    }

    [Fact]
    public void Remove_DecreasesCount()
    {
        var svc  = new ClipboardStorageService(10);
        var item = Text("x");
        svc.Add(item);
        svc.Remove(item);
        Assert.Empty(svc.Items);
    }

    [Fact]
    public void Add_PastCapacity_EvictsOldestNonPinned()
    {
        var svc = new ClipboardStorageService(3);
        var a = Text("a");
        var b = Text("b"); b.IsPinned = true;
        var c = Text("c");
        var d = Text("d");

        svc.Add(a);
        svc.Add(b);
        svc.Add(c);
        svc.Add(d); // a is oldest non-pinned → evicted

        Assert.Equal(3, svc.Items.Count);
        Assert.DoesNotContain(svc.Items, i => i.TextContent == "a");
        Assert.Contains(svc.Items, i => i.TextContent == "b"); // pinned survives
    }
}
