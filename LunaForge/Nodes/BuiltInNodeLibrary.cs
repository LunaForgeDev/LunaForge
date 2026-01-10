using LunaForge.Nodes.General;
using LunaForge.Plugins;
using LunaForge.Services;
using Serilog;

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
    }

    protected override void RegisterCategories()
    {
        #region General
        var general = CreateCategory("General");
        general.AddNode<Folder>("Folder");
        general.AddNode<FolderRed>("Red Folder");
        general.AddNode<FolderGreen>("Green Folder");
        general.AddNode<FolderBlue>("Blue Folder");
        general.AddNode<FolderYellow>("Yellow Folder");
        general.AddSeparator();
        general.AddNode<Code>("Code");

        #endregion
    }

    public override void Shutdown()
    {
        Logger.Information("Built-in library shutting down");
        base.Shutdown();
    }
}
