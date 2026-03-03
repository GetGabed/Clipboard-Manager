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
