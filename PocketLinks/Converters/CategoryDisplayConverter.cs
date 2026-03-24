using System;
using System.Globalization;
using System.Windows.Data;

namespace PocketLinks.Converters;

/// <summary>
/// Shows "(None)" for empty-string categories in the ComboBox dropdown.
/// </summary>
public class CategoryDisplayConverter : IValueConverter
{
    public static readonly CategoryDisplayConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = value as string;
        return string.IsNullOrWhiteSpace(s) ? "(None)" : s;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
