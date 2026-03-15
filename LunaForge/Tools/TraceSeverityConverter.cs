using LunaForge.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace LunaForge.Tools;

public class TraceSeverityConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not TraceSeverity severity)
            return null;

        string path = severity switch
        {
            TraceSeverity.Error => "pack://application:,,,/Images/Error.png",
            TraceSeverity.Warning => "pack://application:,,,/Images/Warning.png",
            TraceSeverity.Information => "pack://application:,,,/Images/Info.png",
            _ => null!
        };

        try
        {
            return new BitmapImage(new Uri(path));
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
