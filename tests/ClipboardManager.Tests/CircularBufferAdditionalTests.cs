using ClipboardManager.Helpers;
using ClipboardManager.Models;

namespace ClipboardManager.Tests;

/// <summary>
/// Additional CircularBuffer tests covering Last(), Remove(), Clear(),
/// ToList() ordering, and the invalid-capacity guard.
/// </summary>
public class CircularBufferAdditionalTests
{
    private static ClipboardItem Text(string value) =>
        new() { ContentType = ClipboardContentType.Text, TextContent = value };

    // ── Constructor guard ─────────────────────────────────────────────────

    [Fact]
    public void Constructor_ZeroCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CircularBuffer<ClipboardItem>(0));
    }

    [Fact]
    public void Constructor_NegativeCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CircularBuffer<ClipboardItem>(-5));
    }

    // ── Last() ────────────────────────────────────────────────────────────

    [Fact]
    public void Last_ReturnsMostRecentlyAddedItem()
    {
        var buf = new CircularBuffer<ClipboardItem>(5);
        buf.PushBack(Text("first"));
        buf.PushBack(Text("second"));
        buf.PushBack(Text("third"));

        Assert.Equal("third", buf.Last().TextContent);
    }

    [Fact]
    public void Last_AfterPushBack_UpdatesToNewest()
    {
        var buf = new CircularBuffer<ClipboardItem>(5);
        buf.PushBack(Text("a"));
        buf.PushBack(Text("b"));

        Assert.Equal("b", buf.Last().TextContent);

        buf.PushBack(Text("c"));
        Assert.Equal("c", buf.Last().TextContent);
    }

    // ── Remove() ─────────────────────────────────────────────────────────

    [Fact]
    public void Remove_SpecifiedItem_DecreasesCount()
    {
        var buf  = new CircularBuffer<ClipboardItem>(5);
        var item = Text("target");
        buf.PushBack(Text("a"));
        buf.PushBack(item);
        buf.PushBack(Text("b"));

        buf.Remove(item);

        Assert.Equal(2, buf.Count);
        Assert.DoesNotContain(item, buf.ToList());
    }

    [Fact]
    public void Remove_OnlyItem_BufferBecomesEmpty()
    {
        var buf  = new CircularBuffer<ClipboardItem>(3);
        var item = Text("solo");
        buf.PushBack(item);

        buf.Remove(item);

        Assert.Equal(0, buf.Count);
    }

    // ── Clear() ───────────────────────────────────────────────────────────

    [Fact]
    public void Clear_EmptiesBuffer_CountIsZero()
    {
        var buf = new CircularBuffer<ClipboardItem>(5);
        buf.PushBack(Text("a"));
        buf.PushBack(Text("b"));
        buf.PushBack(Text("c"));

        buf.Clear();

        Assert.Equal(0, buf.Count);
        Assert.Empty(buf.ToList());
    }

    [Fact]
    public void Clear_AlreadyEmpty_IsNoOp()
    {
        var buf = new CircularBuffer<ClipboardItem>(5);
        var ex = Record.Exception(() => buf.Clear());
        Assert.Null(ex);
        Assert.Equal(0, buf.Count);
    }

    // ── ToList() insertion order ──────────────────────────────────────────

    [Fact]
    public void ToList_ReturnsItems_OldestFirst()
    {
        var buf = new CircularBuffer<ClipboardItem>(5);
        var a = Text("a");
        var b = Text("b");
        var c = Text("c");
        buf.PushBack(a);
        buf.PushBack(b);
        buf.PushBack(c);

        var list = buf.ToList();

        Assert.Equal(3, list.Count);
        Assert.Same(a, list[0]); // oldest first
        Assert.Same(b, list[1]);
        Assert.Same(c, list[2]); // newest last
    }

    [Fact]
    public void ToList_AfterEviction_CorrectOrder()
    {
        var buf = new CircularBuffer<ClipboardItem>(3);
        var a = Text("a");
        var b = Text("b");
        var c = Text("c");
        var d = Text("d");
        buf.PushBack(a);
        buf.PushBack(b);
        buf.PushBack(c);
        buf.PushBack(d); // evicts a

        var list = buf.ToList();

        Assert.Equal(3, list.Count);
        Assert.Same(b, list[0]);
        Assert.Same(c, list[1]);
        Assert.Same(d, list[2]);
    }
}
