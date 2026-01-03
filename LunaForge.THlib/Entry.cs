using LunaForge.Plugins;
using System.Windows.Forms;
using LunaForge.Services;
using Serilog;
using LunaForge.Models;
using LunaForge.THlib.Nodes;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;
using LunaForge.THlib.Nodes.Task;

namespace LunaForge.THlib;

public class Plugin : NodeLibraryBase
{
    public override string LibraryName => "LunaForge.THlib";
    public override string DisplayName => "THlib";
    public override string Version => "1.0.0";
    public override string Description => "THlib node library for LunaForge";

    public override void Initialize()
    {
        base.Initialize();
        Logger.Information("THlib library initialized successfully");
    }

    protected override void RegisterCategories()
    {
        var data = CreateCategory("Data");
        var stage = CreateCategory("Stage");
        #region Task
        var task = CreateCategory("Task");
        task.AddNode<TaskNode>("Task");
        #endregion
        var enemy = CreateCategory("Enemy");
        var boss = CreateCategory("Boss");
        var bullet = CreateCategory("Bullet");
        var laser = CreateCategory("Laser");
        var @object = CreateCategory("Object");
        var control = CreateCategory("Control");
        var graphics = CreateCategory("Graphics");
        #region Audio
        var audio = CreateCategory("Audio");
        audio.AddNode<PlaySE>("Play Sound");
        #endregion
        var render = CreateCategory("Render");
        var background = CreateCategory("Background");
        var player = CreateCategory("Player");
        var gamedata = CreateCategory("Game Data");
    }

    public override void Shutdown()
    {
        Logger.Information("THlib library shutting down");
        base.Shutdown();
    }
}

public class THlibTarget : ICompilationTarget
{
    public string TargetName { get; } = "THlib";
    public string[] SupportedBranches { get; } = ["ExPlus", "Sub", "Flux", "Evo"];
    public string BuildDirectory { get; } = "mod/";
    public bool SupportStageDebug { get; } = true;
    public bool SupportSCDebug { get; } = true;
    public string RootBaseContents { get; } = "Include('THlib.lua')";

    public void BeforeRun()
    {

    }
}