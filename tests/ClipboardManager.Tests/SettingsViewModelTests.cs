using System.IO;
using ClipboardManager.Models;
using ClipboardManager.Services;
using ClipboardManager.ViewModels;

namespace ClipboardManager.Tests;

/// <summary>
/// Unit tests for SettingsViewModel: ExcludedApps parsing, AutoExpireDays,
/// MaxHistoryItems live-resize, hotkey rebind, and DarkMode toggle.
/// Uses a temporary directory so real AppData is never touched.
/// </summary>
public class SettingsViewModelTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), $"cbm_svm_test_{Guid.NewGuid():N}");

    // ── Minimal IClipboardStorageService fake ─────────────────────────────

    private sealed class FakeStorage : IClipboardStorageService
    {
        public IReadOnlyList<ClipboardItem> Items { get; } = new List<ClipboardItem>();
        public event EventHandler<ClipboardItem>? ItemAdded;
        public void Add(ClipboardItem item) { }
        public void Remove(ClipboardItem item) { }
        public void Clear() { }
        public void ClearUnpinned() { }
        public void SetAsCurrentClipboard(ClipboardItem item) { }
        public void Promote(ClipboardItem item) { }
        public void SetPinned(ClipboardItem item, bool pinned) => item.IsPinned = pinned;
        public void IncrementCopyCount(ClipboardItem item) => item.CopyCount++;

        public int LastResizeCapacity;
        public void Resize(int newCapacity) => LastResizeCapacity = newCapacity;
    }

    private (SettingsService settings, SettingsViewModel vm, FakeStorage storage) Build()
    {
        var settings = new SettingsService(_tempDir);
        var storage  = new FakeStorage();
        var vm       = new SettingsViewModel(settings, storage);
        return (settings, vm, storage);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* ignore */ }
    }

    // ── ExcludedApps ──────────────────────────────────────────────────────

    [Fact]
    public void ExcludedApps_Get_ReturnsEmptyStringByDefault()
    {
        var (_, vm, _) = Build();
        Assert.Equal(string.Empty, vm.ExcludedApps);
    }

    [Fact]
    public void ExcludedApps_SetCommaSeparated_ParsesIntoList()
    {
        var (settings, vm, _) = Build();
        vm.ExcludedApps = "keepass, 1Password, bitwarden";

        Assert.Equal(3, settings.Current.ExcludedApps.Count);
        Assert.Contains("keepass",   settings.Current.ExcludedApps);
        Assert.Contains("1password", settings.Current.ExcludedApps);
        Assert.Contains("bitwarden", settings.Current.ExcludedApps);
    }

    [Fact]
    public void ExcludedApps_SetSemicolonSeparated_ParsesIntoList()
    {
        var (settings, vm, _) = Build();
        vm.ExcludedApps = "keepass; notepad";

        Assert.Equal(2, settings.Current.ExcludedApps.Count);
    }

    [Fact]
    public void ExcludedApps_SetEmptyString_ResultsInEmptyList()
    {
        var (settings, vm, _) = Build();
        vm.ExcludedApps = "keepass";
        vm.ExcludedApps = "";

        Assert.Empty(settings.Current.ExcludedApps);
    }

    [Fact]
    public void ExcludedApps_Get_ReturnsPreviouslySetApps()
    {
        var (_, vm, _) = Build();
        vm.ExcludedApps = "keepass, bitwarden";

        Assert.Equal("keepass, bitwarden", vm.ExcludedApps);
    }

    [Fact]
    public void ExcludedApps_LowercasesEntries()
    {
        var (settings, vm, _) = Build();
        vm.ExcludedApps = "KeePass, BITWARDEN";

        Assert.All(settings.Current.ExcludedApps, e => Assert.Equal(e.ToLower(), e));
    }

    // ── AutoExpireDays ────────────────────────────────────────────────────

    [Fact]
    public void AutoExpireDays_DefaultIsZero()
    {
        var (_, vm, _) = Build();
        Assert.Equal(0, vm.AutoExpireDays);
    }

    [Fact]
    public void AutoExpireDays_Set_UpdatesSettings()
    {
        var (settings, vm, _) = Build();
        vm.AutoExpireDays = 30;

        Assert.Equal(30, settings.Current.AutoExpireDays);
    }

    [Fact]
    public void AutoExpireDays_Set_RaisesPropertyChanged()
    {
        var (_, vm, _) = Build();
        var raised = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(vm.AutoExpireDays)) raised = true;
        };
        vm.AutoExpireDays = 7;

        Assert.True(raised);
    }

    [Fact]
    public void AutoExpireDays_SetSameValue_DoesNotRaisePropertyChanged()
    {
        var (_, vm, _) = Build();
        vm.AutoExpireDays = 7;
        var count = 0;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(vm.AutoExpireDays)) count++;
        };
        vm.AutoExpireDays = 7; // same value — should not fire again

        Assert.Equal(0, count);
    }

    // ── MaxHistoryItems (live resize) ─────────────────────────────────────

    [Fact]
    public void MaxHistoryItems_Set_CallsResizeOnStorage()
    {
        var (_, vm, storage) = Build();
        vm.MaxHistoryItems = 50;

        Assert.Equal(50, storage.LastResizeCapacity);
    }

    [Fact]
    public void MaxHistoryItems_Set_UpdatesSettings()
    {
        var (settings, vm, _) = Build();
        vm.MaxHistoryItems = 75;

        Assert.Equal(75, settings.Current.MaxHistoryItems);
    }

    // ── Hotkey rebind ─────────────────────────────────────────────────────

    [Fact]
    public void UpdateHotkey_ChangesHotkeyDisplay()
    {
        var (_, vm, _) = Build();
        var before = vm.HotkeyDisplay;

        // Ctrl+Alt+H  (modifiers: 0x0002 | 0x0001 = 3,  key: H = 0x48)
        vm.UpdateHotkey(0x0003, 0x48);

        Assert.NotEqual(before, vm.HotkeyDisplay);
        Assert.Contains("H", vm.HotkeyDisplay);
    }

    [Fact]
    public void UpdateHotkey_RaisesPropertyChangedForHotkeyDisplay()
    {
        var (_, vm, _) = Build();
        var raised = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(vm.HotkeyDisplay)) raised = true;
        };

        vm.UpdateHotkey(0x0002, 0x48);

        Assert.True(raised);
    }

    // ── DarkMode ──────────────────────────────────────────────────────────

    [Fact]
    public void DarkMode_Set_UpdatesSettings()
    {
        var (settings, vm, _) = Build();
        vm.DarkMode = true;

        Assert.True(settings.Current.DarkMode);
    }

    [Fact]
    public void DarkMode_Toggle_RaisesPropertyChanged()
    {
        var (_, vm, _) = Build();
        var raised = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(vm.DarkMode)) raised = true;
        };
        vm.DarkMode = true;

        Assert.True(raised);
    }
}

/// <summary>
/// Additional tests for ClipboardStorageService — IncrementCopyCount and Items caching.
/// </summary>
public class ClipboardStorageServiceCopyCountTests
{
    private static ClipboardItem Text(string value) =>
        new() { ContentType = ClipboardContentType.Text, TextContent = value };

    [Fact]
    public void IncrementCopyCount_IncreasesCountByOne()
    {
        var svc  = new ClipboardStorageService(10);
        var item = Text("hello");
        svc.Add(item);

        svc.IncrementCopyCount(item);

        Assert.Equal(1, svc.Items[0].CopyCount);
    }

    [Fact]
    public void IncrementCopyCount_CalledMultipleTimes_AccumulatesCount()
    {
        var svc  = new ClipboardStorageService(10);
        var item = Text("repeat");
        svc.Add(item);

        svc.IncrementCopyCount(item);
        svc.IncrementCopyCount(item);
        svc.IncrementCopyCount(item);

        Assert.Equal(3, svc.Items[0].CopyCount);
    }

    [Fact]
    public void Items_AfterAdd_ReturnsCachedList_SameReference()
    {
        var svc = new ClipboardStorageService(10);
        svc.Add(Text("a"));

        var first  = svc.Items;
        var second = svc.Items;

        // Both calls should return the same cached IReadOnlyList instance
        Assert.Same(first, second);
    }

    [Fact]
    public void Items_AfterSecondAdd_ReturnsFreshList()
    {
        var svc = new ClipboardStorageService(10);
        svc.Add(Text("a"));
        var before = svc.Items;

        svc.Add(Text("b"));
        var after = svc.Items;

        Assert.NotSame(before, after);
        Assert.Equal(2, after.Count);
    }
}

/// <summary>
/// Tests for SweepExpired in HistoryViewModel.
/// </summary>
public class HistoryViewModelSweepExpiredTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), $"cbm_sweep_test_{Guid.NewGuid():N}");

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* ignore */ }
    }

    private static ClipboardItem OldText(string value, int daysAgo) =>
        new() { ContentType = ClipboardContentType.Text, TextContent = value,
                CapturedAt = DateTime.Now.AddDays(-daysAgo) };

    [Fact]
    public void SweepExpired_RemovesUnpinnedItemsOlderThanThreshold()
    {
        var settings = new SettingsService(_tempDir);
        settings.Current.AutoExpireDays = 7;

        var storage = new ClipboardStorageService(50);
        storage.Add(OldText("old",    10)); // 10 days old — should be removed
        storage.Add(OldText("recent",  3)); // 3 days old  — should stay

        var monitor = new ClipboardMonitorService(storage);
        var vm = new HistoryViewModel(storage, monitor, settings);
        vm.RefreshFilter();

        Assert.Single(vm.FilteredItems);
        Assert.Equal("recent", vm.FilteredItems[0].TextContent);
    }

    [Fact]
    public void SweepExpired_PreservesPinnedItemsEvenIfOld()
    {
        var settings = new SettingsService(_tempDir);
        settings.Current.AutoExpireDays = 7;

        var storage = new ClipboardStorageService(50);
        var pinned = OldText("important", 30);
        pinned.IsPinned = true;
        storage.Add(pinned);
        storage.Add(OldText("old", 10));

        var monitor = new ClipboardMonitorService(storage);
        var vm = new HistoryViewModel(storage, monitor, settings);
        vm.RefreshFilter();

        Assert.Single(vm.FilteredItems);
        Assert.True(vm.FilteredItems[0].IsPinned);
    }

    [Fact]
    public void SweepExpired_WhenAutoExpireDaysIsZero_RemovesNothing()
    {
        var settings = new SettingsService(_tempDir);
        settings.Current.AutoExpireDays = 0; // disabled

        var storage = new ClipboardStorageService(50);
        storage.Add(OldText("ancient", 9999)); // very old
        storage.Add(OldText("recent",     1));

        var monitor = new ClipboardMonitorService(storage);
        var vm = new HistoryViewModel(storage, monitor, settings);
        vm.RefreshFilter();

        Assert.Equal(2, vm.FilteredItems.Count);
    }
}
