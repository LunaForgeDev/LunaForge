using LunaForge.Nodes.General;
using LunaForge.Plugins;
using LunaForge.Services;
using Serilog;
using System.Windows.Forms;

namespace LunaForge.Nodes;

/// <summary>
/// Built-in node library that ships with LunaForge
/// </summary>
public class BuiltInNodeLibrary : NodeLibraryBase
{
    public override string LibraryName => "LunaForge.BuiltIn";
    public override string DisplayName => "Built-in Nodes";
    public override string Version => "1.0.0";
    public override string Description => "Built-in node library for LunaForge";

    public override void Initialize()
    {
        base.Initialize();
        
        foreach (var category in Categories)
        {
            Logger.Information($"  - Category: {category.Name} with {category.Nodes.Count} nodes");
        }
    }

    protected override void RegisterCategories()
    {
        var general = CreateCategory("General");
        general.AddNode<RootFolder>("Root Folder");
    }

    public override void Shutdown()
    {
        Logger.Information("Built-in library shutting down");
        base.Shutdown();
    }
}
