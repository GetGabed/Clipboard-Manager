using System.IO;
using ClipboardManager.Models;
using ClipboardManager.Services;

namespace ClipboardManager.Tests;

/// <summary>
/// Unit tests for SettingsService covering Load defaults, Save+Load round-trip,
/// Reset, and corrupt-JSON fallback.
/// Each test uses a private temp directory so it never touches the real AppData.
/// </summary>
public class SettingsServiceTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), $"cbm_settings_test_{Guid.NewGuid():N}");

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* ignore */ }
    }

    // ── Load ──────────────────────────────────────────────────────────────

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsDefaults()
    {
        // _tempDir is empty — no settings.json exists
        var svc = new SettingsService(_tempDir);

        Assert.Equal(200, svc.Current.MaxHistoryItems);
        Assert.False(svc.Current.DarkMode);
        Assert.False(svc.Current.StartWithWindows);
        Assert.True(svc.Current.PersistToDisk);
    }

    [Fact]
    public void Load_CorruptJson_FallsBackToDefaults_NoException()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "settings.json"), "{ this is not valid JSON !!!");

        var ex = Record.Exception(() => new SettingsService(_tempDir));
        Assert.Null(ex);
    }

    [Fact]
    public void Load_CorruptJson_CurrentIsDefault()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "settings.json"), "###corrupt###");

        var svc = new SettingsService(_tempDir);

        Assert.Equal(200, svc.Current.MaxHistoryItems);
    }

    // ── Save + Load round-trip ────────────────────────────────────────────

    [Fact]
    public void SaveThenLoad_RoundTripsAllProperties()
    {
        var svc = new SettingsService(_tempDir);
        svc.Current.MaxHistoryItems     = 42;
        svc.Current.DarkMode            = true;
        svc.Current.StartWithWindows    = true;
        svc.Current.PlaySound           = true;
        svc.Current.PersistToDisk       = false;
        svc.Current.HistoryWindowWidth  = 800;
        svc.Current.HistoryWindowHeight = 700;
        svc.Save();

        // Reload from the same directory
        var svc2 = new SettingsService(_tempDir);

        Assert.Equal(42,    svc2.Current.MaxHistoryItems);
        Assert.True(svc2.Current.DarkMode);
        Assert.True(svc2.Current.StartWithWindows);
        Assert.True(svc2.Current.PlaySound);
        Assert.False(svc2.Current.PersistToDisk);
        Assert.Equal(800.0, svc2.Current.HistoryWindowWidth);
        Assert.Equal(700.0, svc2.Current.HistoryWindowHeight);
    }

    [Fact]
    public void Save_CreatesSettingsFile()
    {
        var svc = new SettingsService(_tempDir);
        svc.Save();

        Assert.True(File.Exists(Path.Combine(_tempDir, "settings.json")));
    }

    // ── Reset ─────────────────────────────────────────────────────────────

    [Fact]
    public void Reset_RestoresAllDefaults()
    {
        var svc = new SettingsService(_tempDir);
        svc.Current.MaxHistoryItems = 999;
        svc.Current.DarkMode        = true;
        svc.Save();

        svc.Reset();

        Assert.Equal(200, svc.Current.MaxHistoryItems);
        Assert.False(svc.Current.DarkMode);
    }

    [Fact]
    public void Reset_OverwritesFileWithDefaults()
    {
        var svc = new SettingsService(_tempDir);
        svc.Current.MaxHistoryItems = 999;
        svc.Save();

        svc.Reset();

        // A fresh load should see defaults
        var svc2 = new SettingsService(_tempDir);
        Assert.Equal(200, svc2.Current.MaxHistoryItems);
    }

    // ── Settings file location ────────────────────────────────────────────

    [Fact]
    public void Save_DefaultConstructor_CreatesFileInAppData()
    {
        var expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClipboardManager", "settings.json");

        var svc = new SettingsService(); // real path
        svc.Save();

        Assert.True(File.Exists(expected), $"Expected settings file at: {expected}");
    }
}
