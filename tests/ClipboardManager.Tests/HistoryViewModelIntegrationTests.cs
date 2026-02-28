using System.IO;
using ClipboardManager.Models;
using ClipboardManager.Services;
using ClipboardManager.ViewModels;

namespace ClipboardManager.Tests;

/// <summary>
/// Integration / smoke tests for HistoryViewModel.
/// Items are pre-populated into storage before the ViewModel is constructed
/// so no ItemAdded events fire (which would require a WPF Dispatcher).
/// RefreshFilter is called directly.
/// </summary>
public class HistoryViewModelIntegrationTests
{
    private static ClipboardItem Text(string value) =>
        new() { ContentType = ClipboardContentType.Text, TextContent = value };

    private static (ClipboardStorageService storage, ClipboardMonitorService monitor, HistoryViewModel vm)
        BuildVm(int capacity = 200)
    {
        var storage = new ClipboardStorageService(capacity);
        var monitor = new ClipboardMonitorService(storage);
        var vm = new HistoryViewModel(storage, monitor);
        return (storage, monitor, vm);
    }

    // ── Populate-then-construct helpers ───────────────────────────────────

    private static HistoryViewModel BuildVmWithPrePopulatedStorage(
        IEnumerable<ClipboardItem> items, int capacity = 200)
    {
        var storage = new ClipboardStorageService(capacity);
        foreach (var item in items)
            storage.Add(item);

        var monitor = new ClipboardMonitorService(storage);
        return new HistoryViewModel(storage, monitor);
    }

    // ── Filter: empty query returns all items ─────────────────────────────

    [Fact]
    public void RefreshFilter_EmptyQuery_Returns_AllItems()
    {
        var items = Enumerable.Range(1, 50)
                              .Select(i => Text($"item-{i}"))
                              .ToList();

        var vm = BuildVmWithPrePopulatedStorage(items, capacity: 100);

        vm.SearchText = string.Empty; // triggers RefreshFilter internally
        vm.RefreshFilter();

        Assert.Equal(50, vm.FilteredItems.Count);
    }

    [Fact]
    public void RefreshFilter_EmptyQuery_IncludesAllContentTypes()
    {
        var storage = new ClipboardStorageService(10);
        storage.Add(Text("text item"));
        storage.Add(new ClipboardItem
        {
            ContentType    = ClipboardContentType.Files,
            FilePaths      = new[] { @"C:\test.txt" }
        });

        var vm = new HistoryViewModel(storage, new ClipboardMonitorService(storage));
        vm.RefreshFilter();

        Assert.Equal(2, vm.FilteredItems.Count);
    }

    // ── Filter: specific query returns only matching items ────────────────

    [Fact]
    public void RefreshFilter_SearchHello_ReturnsOnlyMatchingItems()
    {
        var storage = new ClipboardStorageService(20);
        storage.Add(Text("hello world"));
        storage.Add(Text("HELLO COPILOT"));
        storage.Add(Text("foo bar"));
        storage.Add(Text("baz qux"));
        storage.Add(Text("say hello again"));

        var vm = new HistoryViewModel(storage, new ClipboardMonitorService(storage));
        vm.SearchText = "hello"; // triggers RefreshFilter internally

        // "hello world", "HELLO COPILOT" (case-insensitive), "say hello again" → 3 matches
        Assert.Equal(3, vm.FilteredItems.Count);
        Assert.All(vm.FilteredItems, item =>
            Assert.True(
                item.TextContent?.Contains("hello", StringComparison.OrdinalIgnoreCase) ?? false));
    }

    [Fact]
    public void RefreshFilter_NoMatch_ReturnsEmptyCollection()
    {
        var storage = new ClipboardStorageService(10);
        storage.Add(Text("apple"));
        storage.Add(Text("banana"));

        var vm = new HistoryViewModel(storage, new ClipboardMonitorService(storage));
        vm.SearchText = "zzznomatch";

        Assert.Empty(vm.FilteredItems);
    }

    // ── ShowPinnedOnly filter ─────────────────────────────────────────────

    [Fact]
    public void RefreshFilter_ShowPinnedOnly_ReturnsOnlyPinnedItems()
    {
        var storage = new ClipboardStorageService(10);
        var pinned = Text("pinned");
        storage.Add(Text("unpinned-a"));
        storage.Add(pinned);
        storage.Add(Text("unpinned-b"));
        pinned.IsPinned = true;

        var vm = new HistoryViewModel(storage, new ClipboardMonitorService(storage));
        vm.ShowPinnedOnly = true; // triggers RefreshFilter internally

        Assert.Single(vm.FilteredItems);
        Assert.Equal("pinned", vm.FilteredItems[0].TextContent);
    }

    // ── TotalCount property ───────────────────────────────────────────────

    [Fact]
    public void TotalCount_ReflectsStorageItemCount()
    {
        var storage = new ClipboardStorageService(10);
        storage.Add(Text("a"));
        storage.Add(Text("b"));
        storage.Add(Text("c"));

        var vm = new HistoryViewModel(storage, new ClipboardMonitorService(storage));

        Assert.Equal(3, vm.TotalCount);
    }

    // ── IDisposable  ──────────────────────────────────────────────────────

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var (_, _, vm) = BuildVm();
        var ex = Record.Exception(() => vm.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var (_, _, vm) = BuildVm();
        vm.Dispose();
        var ex = Record.Exception(() => vm.Dispose());
        Assert.Null(ex);
    }
}
