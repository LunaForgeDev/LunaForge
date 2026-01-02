using LunaForge.Plugins;
using LunaForge.ViewModels;
using System.Windows;

namespace LunaForge.Views;

public partial class PluginManagerWindow : Window
{
    public PluginManagerWindow(PluginManager pluginManager)
    {
        InitializeComponent();
        DataContext = new PluginManagerViewModel(this, pluginManager);
    }

    private void GridSplitter_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
    {

    }
}
