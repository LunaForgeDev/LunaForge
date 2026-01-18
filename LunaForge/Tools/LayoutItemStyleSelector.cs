using AvalonDock.Controls;
using AvalonDock.Layout;
using LunaForge.Models;
using System.Windows;
using System.Windows.Controls;

namespace LunaForge.Tools;

public class LayoutItemStyleSelector : StyleSelector
{
    public Style DocumentStyle { get; set; }
    public Style AnchorableStyle { get; set; }

    public override Style SelectStyle(object item, DependencyObject container)
    {
        if (item is DocumentFile)
        {
            return DocumentStyle;
        }

        return AnchorableStyle ?? base.SelectStyle(item, container);
    }
}
