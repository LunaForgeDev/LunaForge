using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace LunaForge.Tools;

[ValueConversion(typeof(string), typeof(bool))]
public class IsNullOrEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType,
        object parameter, CultureInfo culture)
        => string.IsNullOrEmpty(value as string);

    public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
