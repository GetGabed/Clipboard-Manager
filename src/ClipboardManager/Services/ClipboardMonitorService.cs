using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using ClipboardManager.Models;
using System.Drawing;

namespace ClipboardManager.Services;

/// <summary>
/// Monitors the system clipboard using the modern WM_CLIPBOARDUPDATE message.
/// Requires a hidden HWND — call <see cref="Attach"/> once the application
/// dispatcher is ready.
/// </summary>
public class ClipboardMonitorService : IDisposable
{
    private const int WM_CLIPBOARDUPDATE = 0x031D;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    private readonly IClipboardStorageService _storage;
    private HwndSource? _hwndSource;
    private bool _suppress;
    private bool _disposed;

    public ClipboardMonitorService(IClipboardStorageService storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// Creates a hidden Win32 window and registers for clipboard notifications.
    /// Must be called on the UI thread after the dispatcher starts.
    /// </summary>
    public void Attach()
    {
        var param = new HwndSourceParameters("ClipboardMonitor")
        {
            Width  = 0,
            Height = 0,
            WindowStyle = 0x800000, // WS_OVERLAPPED (no visual)
        };
        _hwndSource = new HwndSource(param);
        _hwndSource.AddHook(WndProc);
        AddClipboardFormatListener(_hwndSource.Handle);
    }

    /// <summary>
    /// Call before programmatically setting the clipboard to avoid
    /// re-capturing the item we just put there.
    /// </summary>
    public void SuppressNextCapture() => _suppress = true;

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            handled = true;
            if (_suppress)
            {
                _suppress = false;
                return IntPtr.Zero;
            }
            OnClipboardChanged();
        }
        return IntPtr.Zero;
    }

    private void OnClipboardChanged()
    {
        try
        {
            // WM_CLIPBOARDUPDATE arrives on the UI thread — read directly.
            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();
                if (!string.IsNullOrEmpty(text))
                {
                    _storage.Add(new ClipboardItem
                    {
                        ContentType = ClipboardContentType.Text,
                        TextContent = text
                    });
                }
            }
            else if (Clipboard.ContainsImage())
            {
                var img = Clipboard.GetImage();
                if (img is not null)
                {
                    _storage.Add(new ClipboardItem
                    {
                        ContentType    = ClipboardContentType.Image,
                        ImageThumbnail = CreateThumbnail(img),
                        ImageWidth     = img.PixelWidth,
                        ImageHeight    = img.PixelHeight
                    });
                }
            }
            else if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                var paths = new string[files.Count];
                files.CopyTo(paths, 0);

                _storage.Add(new ClipboardItem
                {
                    ContentType = ClipboardContentType.Files,
                    FilePaths   = paths,
                    FileIcon    = paths.Length > 0 ? ExtractFileIcon(paths[0]) : null
                });
            }
        }
        catch (Exception ex)
        {
            // Clipboard can be locked by other processes — log and continue
            System.Diagnostics.Debug.WriteLine($"[ClipboardMonitor] {ex.Message}");
        }
    }

    private static BitmapSource CreateThumbnail(BitmapSource source)
    {
        const int maxSize = 256;
        if (source.PixelWidth <= maxSize && source.PixelHeight <= maxSize)
            return source;

        double scale = Math.Min((double)maxSize / source.PixelWidth,
                                (double)maxSize / source.PixelHeight);
        return new TransformedBitmap(source,
            new System.Windows.Media.ScaleTransform(scale, scale));
    }

    /// <summary>
    /// Extracts the Shell-associated icon for a file path and returns a WPF BitmapSource.
    /// Returns null if the file doesn't exist or extraction fails.
    /// </summary>
    private BitmapSource? ExtractFileIcon(string path)
    {
        try
        {
            if (!System.IO.File.Exists(path)) return null;
            using var icon = Icon.ExtractAssociatedIcon(path);
            if (icon is null) return null;
            using var bmp = icon.ToBitmap();
            var hBitmap = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_hwndSource is not null)
        {
            RemoveClipboardFormatListener(_hwndSource.Handle);
            _hwndSource.RemoveHook(WndProc);
            _hwndSource.Dispose();
        }
    }
}
