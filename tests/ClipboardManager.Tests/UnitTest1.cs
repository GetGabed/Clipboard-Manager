using System.IO;
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

        Assert.Equal(2, svc.Items.Count);           // still only A and B
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
}

// ── TextTransformHelper ───────────────────────────────────────────────────────

public class TextTransformHelperTests
{
    [Theory]
    [InlineData("hello world", "HELLO WORLD")]
    [InlineData("",            ""             )]
    public void ToUpperCase_Correct(string input, string expected)
        => Assert.Equal(expected, TextTransformHelper.ToUpperCase(input));

    [Theory]
    [InlineData("HELLO WORLD", "hello world")]
    public void ToLowerCase_Correct(string input, string expected)
        => Assert.Equal(expected, TextTransformHelper.ToLowerCase(input));

    [Fact]
    public void ToTitleCase_Correct()
        => Assert.Equal("Hello World", TextTransformHelper.ToTitleCase("hello world"));

    [Fact]
    public void ToSentenceCase_CapitalisesFirstChar()
    {
        var result = TextTransformHelper.ToSentenceCase("hello world");
        Assert.StartsWith("H", result);
    }

    [Fact]
    public void TrimWhitespace_RemovesLeadingTrailing()
        => Assert.Equal("hi", TextTransformHelper.TrimWhitespace("  hi  "));

    [Fact]
    public void RemoveExtraSpaces_CollapsesInternalSpaces()
        => Assert.Equal("a b c", TextTransformHelper.RemoveExtraSpaces("a   b   c"));

    [Fact]
    public void Base64_RoundTrip()
    {
        var original = "Hello, Base64!";
        var encoded  = TextTransformHelper.EncodeBase64(original);
        var decoded  = TextTransformHelper.DecodeBase64(encoded);
        Assert.Equal(original, decoded);
    }

    [Fact]
    public void DecodeBase64_InvalidInput_ReturnsErrorString()
        => Assert.Equal("[Invalid Base64]", TextTransformHelper.DecodeBase64("not valid!!!"));

    [Fact]
    public void Url_RoundTrip()
    {
        var original = "hello world & more=1";
        var encoded  = TextTransformHelper.UrlEncode(original);
        var decoded  = TextTransformHelper.UrlDecode(encoded);
        Assert.Equal(original, decoded);
    }

    [Fact]
    public void Html_RoundTrip()
    {
        var original = "<div class=\"x\">&amp;</div>";
        var encoded  = TextTransformHelper.HtmlEncode(original);
        var decoded  = TextTransformHelper.HtmlDecode(encoded);
        Assert.Equal(original, decoded);
    }

    [Fact]
    public void ReverseText_Correct()
        => Assert.Equal("olleh", TextTransformHelper.ReverseText("hello"));

    [Fact]
    public void CountCharacters_ReturnsExpectedFormat()
    {
        var result = TextTransformHelper.CountCharacters("hello world");
        Assert.Contains("Characters:", result);
        Assert.Contains("Words:",      result);
        Assert.Contains("Lines:",      result);
    }
}

// ── ClipboardItem helpers ─────────────────────────────────────────────────────

public class ClipboardItemHelperTests
{
    [Fact]
    public void ImageDimensions_NonZero_ReturnsFormattedString()
    {
        var item = new ClipboardItem
        {
            ContentType = ClipboardContentType.Image,
            ImageWidth  = 1920,
            ImageHeight = 1080
        };
        Assert.Equal("1920 × 1080", item.ImageDimensions);
    }

    [Fact]
    public void ImageDimensions_ZeroValues_ReturnsEmpty()
    {
        var item = new ClipboardItem { ContentType = ClipboardContentType.Text };
        Assert.Equal(string.Empty, item.ImageDimensions);
    }

    [Fact]
    public void FilePathsTooltip_MultipleFiles_ReturnsJoinedPaths()
    {
        var item = new ClipboardItem
        {
            ContentType = ClipboardContentType.Files,
            FilePaths   = new[] { @"C:\a.txt", @"C:\b.txt" }
        };
        Assert.Contains("\n", item.FilePathsTooltip);
        Assert.Contains(@"C:\a.txt", item.FilePathsTooltip);
    }

    [Fact]
    public void FilePathsTooltip_TextItem_ReturnsNull()
    {
        var item = new ClipboardItem
        {
            ContentType = ClipboardContentType.Text,
            TextContent = "hello"
        };
        Assert.Null(item.FilePathsTooltip);
    }
}

// ── HistoryPersistenceService ─────────────────────────────────────────────────

public class HistoryPersistenceServiceTests : IDisposable
{
    // Redirect persistence to a temp path so tests are isolated
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"cbm_test_{Guid.NewGuid():N}");

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* ignore */ }
    }

    private static ClipboardItem TextItem(string text, bool pinned = false) =>
        new() { ContentType = ClipboardContentType.Text, TextContent = text, IsPinned = pinned };

    [Fact]
    public void SaveAndLoad_TextItems_RoundTrip()
    {
        var svc   = new ClipboardStorageService(10);
        var items = new[]
        {
            TextItem("first"),
            TextItem("second", pinned: true),
            TextItem("third")
        };
        foreach (var i in items) svc.Add(i);

        var persistence = new HistoryPersistenceService(_tempDir);
        persistence.Save(svc.Items, 100);

        var loaded = persistence.Load(100);

        Assert.Equal(3, loaded.Count);
        Assert.Contains(loaded, i => i.TextContent == "first");
        Assert.Contains(loaded, i => i.TextContent == "second" && i.IsPinned);
        Assert.Contains(loaded, i => i.TextContent == "third");
    }

    [Fact]
    public void Save_ImageItems_SkippedDuringSerialization()
    {
        var svc = new ClipboardStorageService(10);
        svc.Add(TextItem("text only"));
        // Image items have no TextContent — they should be excluded
        var imageItem = new ClipboardItem { ContentType = ClipboardContentType.Image };
        svc.Add(imageItem);

        var persistence = new HistoryPersistenceService(_tempDir);
        persistence.Save(svc.Items, 100);
        var loaded = persistence.Load(100);

        Assert.All(loaded, i => Assert.Equal(ClipboardContentType.Text, i.ContentType));
    }

    [Fact]
    public void Load_WhenNoFileExists_ReturnsEmptyList()
    {
        var persistence = new HistoryPersistenceService(_tempDir);
        // _tempDir is a fresh unique temp directory with no history.json — guaranteed empty
        var loaded = persistence.Load(100);
        Assert.NotNull(loaded);
        Assert.Empty(loaded);
    }}