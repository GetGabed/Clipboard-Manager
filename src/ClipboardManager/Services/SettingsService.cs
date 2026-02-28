using System.IO;
using Newtonsoft.Json;
using ClipboardManager.Models;

namespace ClipboardManager.Services;

/// <summary>Loads and persists <see cref="AppSettings"/> to a JSON file.</summary>
public class SettingsService
{
    private static readonly string DefaultSettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "ClipboardManager");

    private readonly string SettingsDir;
    private readonly string SettingsPath;

    public AppSettings Current { get; private set; } = new();

    /// <summary>Production constructor — uses %AppData%\ClipboardManager\settings.json.</summary>
    public SettingsService() : this(DefaultSettingsDir) { }

    /// <summary>
    /// Testable constructor — uses the supplied directory so tests can
    /// redirect reads/writes to a temp folder without touching AppData.
    /// </summary>
    internal SettingsService(string settingsDir)
    {
        SettingsDir  = settingsDir;
        SettingsPath = Path.Combine(settingsDir, "settings.json");
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                Current = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Load failed: {ex.Message}");
            Current = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonConvert.SerializeObject(Current, Formatting.Indented);
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Save failed: {ex.Message}");
        }
    }

    public void Reset()
    {
        Current = new AppSettings();
        Save();
    }
}
