using Microsoft.Win32;

namespace ClipboardManager.Helpers;

/// <summary>Manages the Windows startup registry entry.</summary>
public static class StartupHelper
{
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "ClipboardManager";

    public static void SetStartWithWindows(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key is null) return;

            if (enable)
            {
                // Environment.ProcessPath is reliable in .NET 6+ and works in single-file apps.
                // Process.MainModule.FileName is the fallback for edge cases.
                var exePath = Environment.ProcessPath
                    ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                    ?? AppContext.BaseDirectory + AppName + ".exe";
                key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StartupHelper] {ex.Message}");
        }
    }

    public static bool IsStartWithWindows()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(AppName) is not null;
        }
        catch { return false; }
    }
}
