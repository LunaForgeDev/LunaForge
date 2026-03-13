using System.Windows;
using LunaForge.Models;
using LunaForge.Services;
using LunaForge.Views;
using Serilog;
using Velopack;
using Application = System.Windows.Application;

namespace LunaForge;

public partial class App : Application
{
    public const int MaxVariablesCount = 10;

    public const string AppVersion = "v0.10-alpha";

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        CoreLogger.Initialize();

        await CheckForUpdatesAsync();

        bool setupDone = EditorConfig.Default.Get<bool>("SetupDone").Value;
        if (!setupDone)
        {
            var setupWindow = new SetupWindow();
            bool? result = setupWindow.ShowDialog();

            if (result != true)
            {
                Shutdown();
                return;
            }
        }

        var launcher = new LauncherWindow();
        launcher.Closed += (s, args) =>
        {
            bool hasMainWindow = false;
            foreach (Window window in Current.Windows)
            {
                if (window is MainWindow)
                {
                    hasMainWindow = true;
                    break;
                }
            }

            if (!hasMainWindow)
            {
                Shutdown();
            }
        };
        
        launcher.Show();
    }

    /// <summary>
    /// TODO: Move that into some class, like "UpdaterService" + UI.
    /// </summary>
    /// <returns></returns>
    private static async Task CheckForUpdatesAsync()
    {
        ILogger logger = CoreLogger.Create("Updater");
        try
        {
            logger.Information("Checking updates...");
            var mgr = new UpdateManager("https://github.com/LunaForgeDev/LunaForge/releases");
            if (!mgr.IsInstalled)
            {
                logger.Information("Not installed or running dev, skipping update check.");
                return; // Dev
            }

            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion is null)
            {
                logger.Information("No update found.");
                return;
            }

            // TODO: UI for update confirmation.
            logger.Information("New update found: {0}", newVersion.TargetFullRelease.Version);
            await mgr.DownloadUpdatesAsync(newVersion);
            mgr.ApplyUpdatesAndRestart(newVersion);
        }
        catch (Exception ex)
        {
            logger.Warning("Impossible to check for updates: {0}", ex);
        }
    }
}