using System.Windows;
using System.Windows.Input;
using ClipboardManager.ViewModels;

namespace ClipboardManager.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e) => Close();

    // ── Hotkey capture ──────────────────────────────────────────────────

    private void HotkeyBox_GotFocus(object sender, RoutedEventArgs e)
    {
        HotkeyBox.Text = "Press a key combo…";
    }

    private void HotkeyBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            HotkeyBox.Text = vm.HotkeyDisplay;
    }

    private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        // Resolve the real key (handles Alt combos reported via e.SystemKey)
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Ignore pure modifier keypresses — wait for a non-modifier key
        if (key is Key.LeftCtrl or Key.RightCtrl
                or Key.LeftShift or Key.RightShift
                or Key.LeftAlt or Key.RightAlt
                or Key.LWin or Key.RWin
                or Key.None)
            return;

        int modifiers = 0;
        if (Keyboard.IsKeyDown(Key.LeftCtrl)  || Keyboard.IsKeyDown(Key.RightCtrl))  modifiers |= 0x0002;
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) modifiers |= 0x0004;
        if (Keyboard.IsKeyDown(Key.LeftAlt)   || Keyboard.IsKeyDown(Key.RightAlt))   modifiers |= 0x0001;
        if (Keyboard.IsKeyDown(Key.LWin)      || Keyboard.IsKeyDown(Key.RWin))       modifiers |= 0x0008;

        // Require at least one modifier
        if (modifiers == 0) return;

        var vk = KeyInterop.VirtualKeyFromKey(key);
        if (DataContext is SettingsViewModel vm)
        {
            vm.UpdateHotkey(modifiers, vk);
            HotkeyBox.Text = vm.HotkeyDisplay;
        }
    }
}
