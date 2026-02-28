using System.Windows;
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
}
