using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LunaForge.Tools;

public class FileExtensionToEditorTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string extension)
        {
            return extension.ToLower() switch
            {
                ".lua" => "LuaEditor",
                ".lfd" => "LfdEditor",
                ".lfs" => "LfsEditor",
                _ => "LfdEditor"
            };
        }
        return "LfdEditor";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
