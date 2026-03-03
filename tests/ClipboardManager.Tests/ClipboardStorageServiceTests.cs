using ClipboardManager.Models;
using ClipboardManager.Services;

namespace ClipboardManager.Tests;

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

    [Fact]
    public void Resize_Smaller_EvictsOldestItems()
    {
        var svc = new ClipboardStorageService(10);
        svc.Add(Text("a"));
        svc.Add(Text("b"));
        svc.Add(Text("c"));
        svc.Add(Text("d"));
        svc.Add(Text("e"));

        svc.Resize(3); // trim to 3

        Assert.Equal(3, svc.Items.Count);
        // newest items survive (e, d, c)
        Assert.Contains(svc.Items, i => i.TextContent == "e");
        Assert.Contains(svc.Items, i => i.TextContent == "d");
        Assert.Contains(svc.Items, i => i.TextContent == "c");
        Assert.DoesNotContain(svc.Items, i => i.TextContent == "a");
    }

    [Fact]
    public void Resize_PinnedItemsSurviveLonger()
    {
        var svc = new ClipboardStorageService(10);
        var pinned = Text("pinned"); pinned.IsPinned = true;
        svc.Add(pinned);
        svc.Add(Text("a"));
        svc.Add(Text("b"));
        svc.Add(Text("c"));

        svc.Resize(2); // trim to 2, non-pinned removed first

        // pinned should still be present even if it's older
        Assert.Contains(svc.Items, i => i.TextContent == "pinned");
    }

    [Fact]
    public void Add_NonConsecutiveDuplicate_PromotesToTopWithoutNewEntry()
    {
        // Reproduce: copy A → copy B → copy A again must not create two A entries.
        var svc = new ClipboardStorageService(10);
        svc.Add(Text("A"));
        svc.Add(Text("B"));
        svc.Add(Text("A")); // re-copy A

        Assert.Equal(2, svc.Items.Count);            // still only A and B
        Assert.Equal("A", svc.Items[0].TextContent); // A is now at the top
        Assert.Equal("B", svc.Items[1].TextContent);
    }

    [Fact]
    public void Add_NonConsecutiveDuplicate_FiringItemAddedAllowsUiRefresh()
    {
        var svc = new ClipboardStorageService(10);
        int fired = 0;
        svc.ItemAdded += (_, _) => fired++;

        svc.Add(Text("A"));
        svc.Add(Text("B"));
        svc.Add(Text("A")); // re-copy A — should still fire ItemAdded so the UI refreshes

        Assert.Equal(3, fired);
    }

    [Fact]
    public void SetPinned_TogglesUnderLock_WithoutRaceCondition()
    {
        var svc  = new ClipboardStorageService(10);
        var item = Text("x");
        svc.Add(item);

        svc.SetPinned(item, true);
        Assert.True(item.IsPinned);

        svc.SetPinned(item, false);
        Assert.False(item.IsPinned);
    }
}
