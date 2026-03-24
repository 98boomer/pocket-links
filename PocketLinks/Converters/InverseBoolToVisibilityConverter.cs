using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PocketLinks.Converters;

/// <summary>
/// Inverts a boolean value before converting to Visibility.
/// True → Collapsed, False → Visible.
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
