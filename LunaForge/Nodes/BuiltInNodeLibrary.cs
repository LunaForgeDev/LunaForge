using LunaForge.Nodes.General;
using LunaForge.Nodes.Advanced;
using LunaForge.Nodes.Data;

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

        general.AddSeparator();

        general.AddNode<ModuleDefine>("Define Module");
        #endregion
        #region Advanced
        var advanced = CreateCategory("Advanced");

        advanced.AddNode<AdvancedRepeat>("Advanced Repeat", () =>
        {
            var advancedRepeat = new AdvancedRepeat();
            var variableCollection = new VariableCollection();
            advancedRepeat.AddChild(variableCollection);
            return advancedRepeat;
        });

        advanced.AddSeparator();

        advanced.AddNode<IncrementVariable>("Increment Variable");

        advanced.AddSeparator();

        advanced.AddNode<LinearVariable>("Linear Variable");
        advanced.AddNode<SinusoidalInterpolationVariable>("Sinusoidal Interpolation Variable");
        advanced.AddNode<SinusoidalMovementVariable>("Sinusoidal Movement Variable");
        advanced.AddNode<CustomInterpolationVariable>("Custom Interpolation Variable");

        advanced.AddSeparator();

        advanced.AddNode<ReboundingVariable>("Rebounding Variable");
        advanced.AddNode<SinusoidalOscillationVariable>("Sinusoidal Oscillation Variable");
        #endregion
        #region Data
        var data = CreateCategory("Data");

        data.AddNode<LocalVar>("Local Variable");
        data.AddNode<Assignment>("Assignment");
        data.AddNode<DefineFunction>("Define Function");
        data.AddNode<CallFunction>("Call Function");
        data.AddNode<ReturnNode>("Return");

        data.AddSeparator();

        data.AddNode<RecordPos>("Record Position");
        #endregion
    }

    public override void Shutdown()
    {
        Logger.Information("Built-in library shutting down");
        base.Shutdown();
    }
}
