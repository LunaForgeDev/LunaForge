using LunaForge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LunaForge.Tools;

public class FileEditorTemplateSelector : DataTemplateSelector
{
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item is DocumentFile documentFile)
        {
            var element = container as FrameworkElement;

            return documentFile.FileExtension.ToLower() switch
            {
                ".lua" => element?.FindResource("LuaEditorTemplate") as DataTemplate ?? base.SelectTemplate(item, container),
                ".lfd" => element?.FindResource("LfdEditorTemplate") as DataTemplate ?? base.SelectTemplate(item, container),
                ".lfs" => element?.FindResource("LfsEditorTemplate") as DataTemplate ?? base.SelectTemplate(item, container),
                _ => element?.FindResource("LfdEditorTemplate") as DataTemplate ?? base.SelectTemplate(item, container)
            };
        }

        return base.SelectTemplate(item, container);
    }
}
