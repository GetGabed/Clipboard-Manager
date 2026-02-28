using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ClipboardManager.ViewModels;

namespace ClipboardManager.Views;

public partial class HistoryWindow : Window
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT pt);

    [StructLayout(LayoutKind.Sequential)]
    private record struct POINT(int X, int Y);

    public HistoryViewModel ViewModel => (HistoryViewModel)DataContext;

    public HistoryWindow(HistoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Close on focus loss
        Deactivated  += (_, _) => Hide();
        Loaded       += (_, _) => FocusSearch();
        KeyDown      += OnWindowKeyDown;
    }

    private void FocusSearch()
    {
        SearchTextBox.Focus();
        SearchTextBox.SelectAll();
    }

    // ── Show / Position ───────────────────────────────────────────────────
    public void ShowAtCursor()
    {
        PositionNearCursor();
        Show();
        Activate();
        FocusSearch();
    }

    private void PositionNearCursor()
    {
        var screen = SystemParameters.WorkArea;
        var dpi    = VisualTreeHelper.GetDpi(this);
        double winW = Width;
        double winH = Height;

        GetCursorPos(out var pt);
        double left = pt.X / dpi.DpiScaleX - winW / 2;
        double top  = pt.Y / dpi.DpiScaleY - winH - 10;

        // Keep within screen bounds
        Left = Math.Clamp(left, screen.Left, screen.Right  - winW);
        Top  = Math.Clamp(top,  screen.Top,  screen.Bottom - winH);
    }

    // ── Event handlers ────────────────────────────────────────────────────
    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Hide();

    private void ItemsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.SelectedItem is not null)
            ViewModel.PasteSelectedCommand.Execute(null);
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                Hide();
                e.Handled = true;
                break;
            case Key.Enter:
                if (ViewModel.PasteSelectedCommand.CanExecute(null))
                    ViewModel.PasteSelectedCommand.Execute(null);
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
            case Key.Down:
                ItemsListBox.Focus();
                break;
        }
    }
}
