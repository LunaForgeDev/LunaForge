using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using Velopack;

namespace LunaForge.Services; 

public static class LFUpdateManager
{
    private static readonly ILogger Logger = CoreLogger.Create("UpdateManager");
    private const string UpdateCheckUrl = "https://github.com/LunaForgeDev/LunaForge/releases";

    public delegate void OnUpdateFoundHandler(UpdateInfo newVersion, UpdateManager mgr);

    public static event OnUpdateFoundHandler OnUpdateFound;

    public static async Task<bool> CheckForUpdatesAsync()
    {
        try
        {
            Logger.Information("Checking for updates...");
            var mgr = new UpdateManager(UpdateCheckUrl);
            if (!mgr.IsInstalled)
            {
                Logger.Information("No installation found. Skipping update check.");
                return false;
            }

            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion is null)
            {
                Logger.Information("No update found.");
                return false;
            }

            Logger.Information("Update found: {version}", newVersion.TargetFullRelease.Version);
            OnUpdateFound.Invoke(newVersion, mgr);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to check for updates. {ex}", ex);
            return false;
        }
    }

    public static async Task DownloadAndInstallUpdateAsync(UpdateInfo newVersion, UpdateManager mgr)
    {
        await mgr.DownloadUpdatesAsync(newVersion);
        mgr.ApplyUpdatesAndRestart(newVersion);
    }
}
