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

    [JsonProperty("monitorInterval")]
    public int MonitorIntervalMs { get; set; } = 200;
}

public class HotkeyConfig
{
    [JsonProperty("modifiers")]
    public int Modifiers { get; set; } = 0x0002 | 0x0004; // CTRL + SHIFT

    [JsonProperty("key")]
    public int Key { get; set; } = 0x56; // V

    public override string ToString() => "Ctrl+Shift+V";
}
