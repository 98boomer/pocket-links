using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PocketLinks.Converters;

/// <summary>
/// Returns Collapsed when the string value is null or empty, Visible otherwise.
/// Used to hide category headers for uncategorized (empty string) groups.
/// </summary>
public class EmptyToCollapsedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
