using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace LunaForge.Tools;

public class IsUnsavedToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool isUnsaved)
            return DependencyProperty.UnsetValue;

        if (isUnsaved)
        {
            return new SolidColorBrush(Color.FromRgb(255, 20, 20));
        }

        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return new NotImplementedException();
    }
}
