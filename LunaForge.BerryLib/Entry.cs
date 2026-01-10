using LunaForge.Models;
using LunaForge.Plugins;
using LunaForge.Services;
using Serilog;
using System.Numerics;
using System.Windows.Forms;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.BerryLib;

public class Plugin : NodeLibraryBase
{
    public override string LibraryName => "LunaForge.BerryLib";
    public override string DisplayName => "BerryLib";
    public override string Version => "1.0.0";
    public override string Description => "BerryLib node library for LunaForge";

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void RegisterCategories()
    {
        var data = CreateCategory("Data");
        var stage = CreateCategory("Stage");
        #region Task
        var task = CreateCategory("Task");
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
        #endregion
        var render = CreateCategory("Render");
        var background = CreateCategory("Background");
        var player = CreateCategory("Player");
        var gamedata = CreateCategory("Game Data");
    }

    public override void Shutdown()
    {
        Logger.Information("BerryLib library shutting down");
        base.Shutdown();
    }
}

public class BerryLibTarget : ICompilationTarget
{
    public string TargetName { get; } = "BerryLib";
    public SupportedBranches SupportedBranches { get; } = SupportedBranches.Sub | SupportedBranches.Flux;
    public string BuildDirectory { get; } = "game/";
    public bool SupportStageDebug { get; } = false;
    public bool SupportSCDebug { get; } = false;
    public string RootBaseContents { get; } = "";

    public void BeforeRun()
    {

    }
}