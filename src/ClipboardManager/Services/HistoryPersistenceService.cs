using System.Diagnostics;
using System.IO;
using ClipboardManager.Models;
using Newtonsoft.Json;

namespace ClipboardManager.Services;

/// <summary>
/// Serialises and deserialises text clipboard history to/from
/// <c>%AppData%\ClipboardManager\history.json</c>.
/// Image and file items are intentionally skipped (unsafe to persist raw data).
/// </summary>
public class HistoryPersistenceService
{
    private static readonly string DefaultDataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "ClipboardManager");

    private readonly string _dataDir;
    private readonly string _historyPath;

    /// <summary>Production constructor — uses %AppData%\ClipboardManager\history.json.</summary>
    public HistoryPersistenceService() : this(DefaultDataDir) { }

    /// <summary>
    /// Testable constructor — uses the supplied directory so tests can
    /// redirect reads/writes to a temp folder without touching AppData.
    /// </summary>
    internal HistoryPersistenceService(string dataDir)
    {
        _dataDir     = dataDir;
        _historyPath = Path.Combine(dataDir, "history.json");
    }

    /// <summary>
    /// Saves the text-only items from <paramref name="items"/>, capped at
    /// <paramref name="maxItems"/>, to disk.
    /// </summary>
    public void Save(IReadOnlyList<ClipboardItem> items, int maxItems)
    {
        try
        {
            var textItems = items
                .Where(i => i.ContentType == ClipboardContentType.Text)
                .Take(maxItems)
                .Select(i => new PersistedItem
                {
                    TextContent = i.TextContent!,
                    IsPinned    = i.IsPinned,
                    CapturedAt  = i.CapturedAt
                })
                .ToList();

            Directory.CreateDirectory(_dataDir);
            var json = JsonConvert.SerializeObject(textItems, Formatting.Indented);
            File.WriteAllText(_historyPath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HistoryPersistence] Save failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads up to <paramref name="maxItems"/> text clipboard entries from disk.
    /// Returns an empty list if the file doesn't exist or is unreadable.
    /// Items are returned oldest-first so the storage service builds newest-first order.
    /// </summary>
    public List<ClipboardItem> Load(int maxItems)
    {
        try
        {
            if (!File.Exists(_historyPath)) return new();

            var json      = File.ReadAllText(_historyPath);
            var persisted = JsonConvert.DeserializeObject<List<PersistedItem>>(json) ?? new();

            return persisted
                .Take(maxItems)
                .Select(p => new ClipboardItem
                {
                    ContentType = ClipboardContentType.Text,
                    TextContent = p.TextContent,
                    IsPinned    = p.IsPinned,
                    CapturedAt  = p.CapturedAt
                })
                .ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HistoryPersistence] Load failed: {ex.Message}");
            return new();
        }
    }

    // ── DTO ───────────────────────────────────────────────────────────────

    private class PersistedItem
    {
        [JsonProperty("text")]   public string   TextContent { get; set; } = string.Empty;
        [JsonProperty("pinned")] public bool     IsPinned    { get; set; }
        [JsonProperty("at")]     public DateTime CapturedAt  { get; set; }
    }
}
