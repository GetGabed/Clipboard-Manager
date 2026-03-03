using Newtonsoft.Json;

namespace ClipboardManager.Models;

/// <summary>Persistent application settings serialised to JSON.</summary>
public class AppSettings
{
    [JsonProperty("maxHistoryItems")]
    public int MaxHistoryItems { get; set; } = 200;

    [JsonProperty("hotkey")]
    public HotkeyConfig Hotkey { get; set; } = new();

    [JsonProperty("startWithWindows")]
    public bool StartWithWindows { get; set; } = false;

    [JsonProperty("darkMode")]
    public bool DarkMode { get; set; } = false;

    [JsonProperty("playSound")]
    public bool PlaySound { get; set; } = false;

    [JsonProperty("persistToDisk")]
    public bool PersistToDisk { get; set; } = true;

    [JsonProperty("historyWindowWidth")]
    public double HistoryWindowWidth { get; set; } = 480;

    [JsonProperty("historyWindowHeight")]
    public double HistoryWindowHeight { get; set; } = 600;

    /// <summary>Last remembered X position of the history window. Null = position near cursor.</summary>
    [JsonProperty("historyWindowLeft")]
    public double? HistoryWindowLeft { get; set; }

    /// <summary>Last remembered Y position of the history window. Null = position near cursor.</summary>
    [JsonProperty("historyWindowTop")]
    public double? HistoryWindowTop { get; set; }

    /// <summary>
    /// Process names (without .exe) whose clipboard activity should be silently ignored.
    /// Case-insensitive partial match. Example: ["keepass", "1password", "bitwarden"]
    /// </summary>
    [JsonProperty("excludedApps")]
    public List<string> ExcludedApps { get; set; } = new();

    /// <summary>
    /// Automatically remove unpinned items older than this many days on startup.
    /// 0 = never auto-expire.
    /// </summary>
    [JsonProperty("autoExpireDays")]
    public int AutoExpireDays { get; set; } = 0;
}

public class HotkeyConfig
{
    [JsonProperty("modifiers")]
    public int Modifiers { get; set; } = 0x0002 | 0x0004; // CTRL + SHIFT

    [JsonProperty("key")]
    public int Key { get; set; } = 0x56; // V

    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string>();
        if ((Modifiers & 0x0001) != 0) parts.Add("Alt");
        if ((Modifiers & 0x0002) != 0) parts.Add("Ctrl");
        if ((Modifiers & 0x0004) != 0) parts.Add("Shift");
        if ((Modifiers & 0x0008) != 0) parts.Add("Win");
        try
        {
            var k = System.Windows.Input.KeyInterop.KeyFromVirtualKey(Key);
            parts.Add(k.ToString());
        }
        catch
        {
            parts.Add($"0x{Key:X2}");
        }
        return string.Join("+", parts);
    }
}
