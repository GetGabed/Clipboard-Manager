using System.Runtime.InteropServices;
using System.Windows.Interop;
using ClipboardManager.Models;
using Serilog;

namespace ClipboardManager.Services;

/// <summary>
/// Registers and listens for a global hotkey using Win32
/// <c>RegisterHotKey</c> / <c>UnregisterHotKey</c>.
/// </summary>
public class HotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HotkeyId  = 9001;

    [DllImport("user32.dll")] private static extern bool RegisterHotKey  (IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private HwndSource? _hwndSource;
    private HotkeyConfig _config;
    private bool _disposed;

    /// <summary>Raised on the UI thread when the hotkey is pressed.</summary>
    public event EventHandler? HotkeyPressed;

    public HotkeyService(HotkeyConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Attaches to an existing HWND (e.g. the hidden tray window).
    /// </summary>
    public void Attach(HwndSource hwndSource)
    {
        _hwndSource = hwndSource;
        _hwndSource.AddHook(WndProc);
        Register();
    }

    public void UpdateConfig(HotkeyConfig config)
    {
        _config = config;
        if (_hwndSource is not null)
        {
            UnregisterHotKey(_hwndSource.Handle, HotkeyId);
            Register();
        }
    }

    private void Register()
    {
        if (_hwndSource is null) return;
        bool ok = RegisterHotKey(_hwndSource.Handle, HotkeyId,
                                 (uint)_config.Modifiers, (uint)_config.Key);
        if (!ok)
            Log.Warning("[HotkeyService] RegisterHotKey failed (Win32 error {Code})",
                        Marshal.GetLastWin32Error());
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_hwndSource is not null)
        {
            UnregisterHotKey(_hwndSource.Handle, HotkeyId);
            _hwndSource.RemoveHook(WndProc);
        }
    }
}
