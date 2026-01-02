using System.Windows;
using LunaForge.Models;
using LunaForge.Services;
using LunaForge.Views;
using Application = System.Windows.Application;

namespace LunaForge;

public partial class App : Application
{
    public const string AppVersion = "v0.10-alpha";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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
    }
}