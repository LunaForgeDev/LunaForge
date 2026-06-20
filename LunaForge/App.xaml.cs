using System.Windows;
using LunaForge.Models;
using LunaForge.Services;
using LunaForge.Views;
using Serilog;
using Velopack;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace LunaForge;

public partial class App : Application
{
    public const int MaxVariablesCount = 10;

    public const string AppVersion = "v0.10-alpha";

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        LFUpdateManager.OnUpdateFound += async (newVersion, mgr) =>
        {
            var result = MessageBox.Show(
                $"A new version ({newVersion.TargetFullRelease.Version}) is available.\nWould you like to download and install it now?",
                "Update Available",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
                await LFUpdateManager.DownloadAndInstallUpdateAsync(newVersion, mgr);
        };

        CoreLogger.Initialize();

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

        _ = LFUpdateManager.CheckForUpdatesAsync();
    }
}