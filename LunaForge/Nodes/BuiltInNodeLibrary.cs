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

        general.AddNode<AddProjectFile>("Add File to Project");

        general.AddNode<Folder>("Folder");
        general.AddNode<FolderRed>("Red Folder");
        general.AddNode<FolderGreen>("Green Folder");
        general.AddNode<FolderBlue>("Blue Folder");
        general.AddNode<FolderYellow>("Yellow Folder");

        general.AddSeparator();

        general.AddNode<Code>("Code");
        general.AddNode<CodeSegment>("Code Segment");

        general.AddSeparator();

        general.AddNode<IfNode>("If", () => {
            var ifNode = new IfNode();
            var thenNode = new IfThen();
            var elseNode = new IfElse();
            ifNode.AddChild(thenNode);
            ifNode.AddChild(elseNode);
            return ifNode;
        });
        general.AddNode<IfElse>("Else");
        general.AddNode<IfElseIf>("Else If");
        general.AddNode<While>("While");
        general.AddNode<Repeat>("Repeat");

        #endregion
    }

    public override void Shutdown()
    {
        Logger.Information("Built-in library shutting down");
        base.Shutdown();
    }
}
