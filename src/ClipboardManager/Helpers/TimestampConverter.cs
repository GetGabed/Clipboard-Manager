using System;
using System.Globalization;
using System.Windows.Data;

namespace ClipboardManager.Helpers;

/// <summary>
/// Formats a <see cref="DateTime"/>: today → "HH:mm", older → "MMM d".
/// </summary>
[ValueConversion(typeof(DateTime), typeof(string))]
public class TimestampConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
            return dt.Date == DateTime.Today ? dt.ToString("HH:mm") : dt.ToString("MMM d");
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Returns <see cref="System.Windows.Visibility.Visible"/> when int == 0, else Collapsed.</summary>
[ValueConversion(typeof(int), typeof(System.Windows.Visibility))]
public class ZeroToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int n && n == 0
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
