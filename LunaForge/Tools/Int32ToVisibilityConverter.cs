using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LunaForge.Tools;

public class Int32ToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int intValue || parameter is not string paramString)
            return Visibility.Collapsed;

        if (int.TryParse(paramString, out int expectedValue))
        {
            return intValue == expectedValue ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}