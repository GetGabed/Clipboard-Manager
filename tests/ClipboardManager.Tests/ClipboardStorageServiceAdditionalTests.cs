using ClipboardManager.Models;
using ClipboardManager.Services;

namespace ClipboardManager.Tests;

/// <summary>
/// Additional ClipboardStorageService tests covering Clear, event-firing
/// semantics, and thread-safety.
/// </summary>
public class ClipboardStorageServiceAdditionalTests
{
    private static ClipboardItem Text(string value) =>
        new() { ContentType = ClipboardContentType.Text, TextContent = value };

    // ── Clear ─────────────────────────────────────────────────────────────

    [Fact]
    public void Clear_AllItems_CountBecomesZero()
    {
        var svc = new ClipboardStorageService(10);
        svc.Add(Text("a"));
        svc.Add(Text("b"));
        svc.Add(Text("c"));

        svc.Clear();

        Assert.Equal(0, svc.Items.Count);
    }

    [Fact]
    public void Clear_AlreadyEmpty_NoException()
    {
        var svc = new ClipboardStorageService(10);
        var ex = Record.Exception(() => svc.Clear());
        Assert.Null(ex);
    }

    [Fact]
    public void Clear_PinnedItemsAlsoRemoved()
    {
        var svc    = new ClipboardStorageService(10);
        var pinned = Text("keep?"); pinned.IsPinned = true;
        svc.Add(pinned);
        svc.Add(Text("other"));

        svc.Clear(); // Clear (unlike ClearUnpinned) removes everything

        Assert.Equal(0, svc.Items.Count);
    }

    // ── ItemAdded event ───────────────────────────────────────────────────

    [Fact]
    public void ItemAdded_FiresExactlyOnce_PerNonDuplicateAdd()
    {
        var svc   = new ClipboardStorageService(10);
        int fired = 0;
        svc.ItemAdded += (_, _) => fired++;

        svc.Add(Text("a"));
        svc.Add(Text("b"));
        svc.Add(Text("c"));

        Assert.Equal(3, fired);
    }

    [Fact]
    public void ItemAdded_StillFires_ForDuplicateAdd()
    {
        // duplicate promotes the item but still fires so UI can refresh
        var svc   = new ClipboardStorageService(10);
        int fired = 0;
        svc.ItemAdded += (_, _) => fired++;

        svc.Add(Text("x"));
        svc.Add(Text("x")); // duplicate

        Assert.Equal(2, fired);
    }

    [Fact]
    public void ItemAdded_EventArg_IsTheStoredItem()
    {
        var svc  = new ClipboardStorageService(10);
        ClipboardItem? received = null;
        svc.ItemAdded += (_, item) => received = item;

        var original = Text("payload");
        svc.Add(original);

        Assert.NotNull(received);
        Assert.Equal("payload", received!.TextContent);
    }

    [Fact]
    public void ItemAdded_EventArg_ForDuplicate_IsExistingItem()
    {
        // When a duplicate is detected, the event fires with the *existing*
        // (promoted) item, not the newly constructed one.
        var svc  = new ClipboardStorageService(10);
        var first = Text("dup");
        svc.Add(first);

        ClipboardItem? received = null;
        svc.ItemAdded += (_, item) => received = item;

        svc.Add(Text("dup")); // new object, same content

        Assert.Same(first, received); // existing object promoted
    }

    // ── Thread-safety ─────────────────────────────────────────────────────

    [Fact]
    public void Add_Concurrent10Threads_NoExceptionAndCountWithinCapacity()
    {
        const int threadCount = 10;
        const int capacity    = 50;
        var svc = new ClipboardStorageService(capacity);

        var threads = Enumerable.Range(0, threadCount)
            .Select(i => new Thread(() =>
            {
                for (int j = 0; j < 20; j++)
                    svc.Add(Text($"t{i}-item{j}"));
            }))
            .ToList();

        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        foreach (var t in threads)
        {
            t.IsBackground = true;
            t.Start();
        }

        foreach (var t in threads)
            t.Join(TimeSpan.FromSeconds(10));

        // No exception should have been thrown; count must respect capacity
        Assert.True(exceptions.IsEmpty);
        Assert.True(svc.Items.Count <= capacity);
    }

    [Fact]
    public void Remove_Concurrent_DoesNotThrow()
    {
        var svc   = new ClipboardStorageService(200);
        var items = Enumerable.Range(0, 100).Select(i => Text($"item{i}")).ToList();
        foreach (var item in items) svc.Add(item);

        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        var threads = items.Select(item => new Thread(() =>
        {
            try { svc.Remove(item); }
            catch (Exception ex) { exceptions.Add(ex); }
        })).ToList();

        foreach (var t in threads) { t.IsBackground = true; t.Start(); }
        foreach (var t in threads) t.Join(TimeSpan.FromSeconds(10));

        Assert.True(exceptions.IsEmpty);
    }
}
