using System.IO;
using System.Security.Cryptography;
using System.Text;
using ClipboardManager.Models;
using Newtonsoft.Json;
using Serilog;

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
            var json  = JsonConvert.SerializeObject(textItems, Formatting.Indented);
            var plain = Encoding.UTF8.GetBytes(json);
            var cipher = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_historyPath, cipher);
            Log.Debug("[HistoryPersistence] Saved {Count} text items", textItems.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[HistoryPersistence] Save failed");
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

            var fileBytes = File.ReadAllBytes(_historyPath);

            string json;
            try
            {
                // Attempt to decrypt (normal path for files saved after v0.9.0)
                var plain = ProtectedData.Unprotect(fileBytes, null, DataProtectionScope.CurrentUser);
                json = Encoding.UTF8.GetString(plain);
            }
            catch
            {
                // Migration path: file is still plain-text JSON from an older version
                Log.Warning("[HistoryPersistence] Decryption failed — loading as plain JSON (migration)");
                json = Encoding.UTF8.GetString(fileBytes);
            }

            var persisted = JsonConvert.DeserializeObject<List<PersistedItem>>(json) ?? new();

            var result = persisted
                .Take(maxItems)
                .Select(p => new ClipboardItem
                {
                    ContentType = ClipboardContentType.Text,
                    TextContent = p.TextContent,
                    IsPinned    = p.IsPinned,
                    CapturedAt  = p.CapturedAt
                })
                .ToList();

            Log.Debug("[HistoryPersistence] Loaded {Count} items", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[HistoryPersistence] Load failed");
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
