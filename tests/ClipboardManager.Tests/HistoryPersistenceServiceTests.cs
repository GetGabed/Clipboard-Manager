using System.IO;
using ClipboardManager.Models;
using ClipboardManager.Services;

namespace ClipboardManager.Tests;

public class HistoryPersistenceServiceTests : IDisposable
{
    // Redirect persistence to a temp path so tests never touch the live AppData
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"cbm_hist_test_{Guid.NewGuid():N}");

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
        // _tempDir is a fresh unique directory with no history.json — guaranteed empty
        var loaded = persistence.Load(100);
        Assert.NotNull(loaded);
        Assert.Empty(loaded);
    }
}
