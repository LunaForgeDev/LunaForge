using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace LunaForge.Tools;

public class BannedToStrikeThrough : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (bool.TryParse(value?.ToString(), out bool b))
        {
            if (b)
            {
                return TextDecorations.Strikethrough;
            }
            else
            {
                return null;
            }
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == TextDecorations.Strikethrough;
    }
}

public class BoolToOpacity : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (bool.TryParse(value?.ToString(), out bool b))
        {
            if (b)
            {
                return 0.4;
            }
            else
            {
                return 1;
            }
        }
        return 1;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (float.TryParse(value?.ToString(), out float f))
        {
            return f > 0.4;
        }
        return true;
    }
}
