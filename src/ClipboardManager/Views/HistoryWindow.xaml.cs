using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ClipboardManager.Models;
using ClipboardManager.ViewModels;

namespace ClipboardManager.Views;

public partial class HistoryWindow : Window
{
    // ── P/Invoke ──────────────────────────────────────────────────────────
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT pt);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    private record struct POINT(int X, int Y);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int    cbSize;
        public RECT   rcMonitor;
        public RECT   rcWork;
        public uint   dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
        public double Width  => Right  - Left;
        public double Height => Bottom - Top;
    }

    // ── Toast timer ───────────────────────────────────────────────────────
    private readonly DispatcherTimer _toastTimer;

    // ── ViewModel accessor ────────────────────────────────────────────────
    public HistoryViewModel ViewModel => (HistoryViewModel)DataContext;

    public HistoryWindow(HistoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Toast dismiss timer (fires once after 700 ms)
        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
        _toastTimer.Tick += (_, _) => { _toastTimer.Stop(); FadeOutToast(); };

        // Subscribe to the ViewModel's ItemCopied event to flash the toast
        viewModel.ItemCopied += OnItemCopied;

        // Close on focus loss
        Deactivated += (_, _) => HideWithFade();
        Loaded      += (_, _) => FocusSearch();
        KeyDown     += OnWindowKeyDown;
    }

    // ── Focus helpers ─────────────────────────────────────────────────────
    private void FocusSearch()
    {
        SearchTextBox.Focus();
        SearchTextBox.SelectAll();
    }

    // ── Show / Hide with fade ──────────────────────────────────────────────
    public void ShowAtCursor()
    {
        PositionNearCursor();
        Show();
        Activate();
        FocusSearch();
        FadeIn();
    }

    private void FadeIn()
    {
        var anim = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(120)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(OpacityProperty, anim);
    }

    public void HideWithFade()
    {
        if (!IsVisible || Opacity <= 0) { Hide(); return; }
        var anim = new DoubleAnimation(Opacity, 0, new Duration(TimeSpan.FromMilliseconds(80)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        anim.Completed += (_, _) => Hide();
        BeginAnimation(OpacityProperty, anim);
    }

    // ── Multi-monitor aware positioning ────────────────────────────────────
    private void PositionNearCursor()
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        double winW = Width;
        double winH = Height;

        GetCursorPos(out var pt);

        // Get the work area of the monitor that contains the cursor (multi-monitor safe)
        var hMon = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
        var mi   = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfo(hMon, ref mi);

        double waLeft   = mi.rcWork.Left   / dpi.DpiScaleX;
        double waTop    = mi.rcWork.Top    / dpi.DpiScaleY;
        double waRight  = mi.rcWork.Right  / dpi.DpiScaleX;
        double waBottom = mi.rcWork.Bottom / dpi.DpiScaleY;

        double left = pt.X / dpi.DpiScaleX - winW / 2;
        double top  = pt.Y / dpi.DpiScaleY - winH - 10;

        Left = Math.Clamp(left, waLeft, waRight  - winW);
        Top  = Math.Clamp(top,  waTop,  waBottom - winH);
    }

    // ── Toast animation ───────────────────────────────────────────────────
    private void OnItemCopied(object? sender, ClipboardItem item)
    {
        // Flash the toast border in and start the dismiss timer
        _toastTimer.Stop();
        CopiedToast.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(150))));
        _toastTimer.Start();
    }

    private void FadeOutToast()
    {
        CopiedToast.BeginAnimation(OpacityProperty,
            new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(300))));
    }

    // ── Event handlers ────────────────────────────────────────────────────
    private void TransformButton_Click(object sender, RoutedEventArgs e)
        => TransformPopup.IsOpen = !TransformPopup.IsOpen;

    /// <summary>Close the transform popup after any transform item is clicked.</summary>
    private void TransformPopupItem_Click(object sender, RoutedEventArgs e)
        => TransformPopup.IsOpen = false;

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => HideWithFade();

    private void ItemsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Double-click copies to clipboard; window stays open
        if (ViewModel.SelectedItem is not null && ViewModel.CopySelectedCommand.CanExecute(null))
            ViewModel.CopySelectedCommand.Execute(null);
    }

    private void ItemsListBox_KeyDown(object sender, KeyEventArgs e)
    {
        // ↑ on the first item (or any item) returns focus to the search box
        if (e.Key == Key.Up && ItemsListBox.SelectedIndex <= 0)
        {
            FocusSearch();
            e.Handled = true;
        }
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                HideWithFade();
                e.Handled = true;
                break;

            case Key.Enter:
                if (ViewModel.CopySelectedCommand.CanExecute(null))
                    ViewModel.CopySelectedCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Delete:
                if (ViewModel.DeleteSelectedCommand.CanExecute(null))
                    ViewModel.DeleteSelectedCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.P when Keyboard.Modifiers == ModifierKeys.None:
                if (ViewModel.PinSelectedCommand.CanExecute(null))
                    ViewModel.PinSelectedCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Space:
                // Space key: show/hide the thumbnail tooltip for the selected image item
                if (ViewModel.SelectedItem?.ContentType == ClipboardContentType.Image &&
                    ViewModel.SelectedItem.ImageThumbnail is not null)
                {
                    OpenImagePreview(ViewModel.SelectedItem);
                    e.Handled = true;
                }
                break;

            case Key.Down:
                // Move focus from search box into the list
                if (!ItemsListBox.IsFocused)
                {
                    ItemsListBox.Focus();
                    if (ItemsListBox.SelectedIndex < 0 && ItemsListBox.Items.Count > 0)
                        ItemsListBox.SelectedIndex = 0;
                }
                e.Handled = true;
                break;
        }
    }

    // ── Image preview (Space key) ─────────────────────────────────────────
    private Popup? _imagePreviewPopup;

    private void OpenImagePreview(ClipboardItem item)
    {
        if (_imagePreviewPopup is { IsOpen: true })
        {
            _imagePreviewPopup.IsOpen = false;
            return;
        }

        _imagePreviewPopup = new Popup
        {
            Placement        = PlacementMode.Center,
            PlacementTarget  = ItemsListBox,
            StaysOpen        = false,
            AllowsTransparency = true,
            PopupAnimation   = PopupAnimation.Fade,
            Child            = new System.Windows.Controls.Border
            {
                Background      = new SolidColorBrush(Color.FromArgb(240, 20, 20, 20)),
                CornerRadius    = new CornerRadius(8),
                Padding         = new Thickness(8),
                MaxWidth        = 700,
                MaxHeight       = 550,
                Child           = new System.Windows.Controls.Image
                {
                    Source      = item.ImageThumbnail,
                    MaxWidth    = 680,
                    MaxHeight   = 530,
                    Stretch     = System.Windows.Media.Stretch.Uniform
                }
            }
        };

        _imagePreviewPopup.IsOpen = true;
    }
}
