using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaForge.Plugins;
using LunaForge.Services;
using LunaForge.Views;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LunaForge.ViewModels;

public partial class PluginManagerViewModel : ObservableObject
{
    private static readonly ILogger Logger = CoreLogger.Create("PluginManager");
    
    private readonly PluginManager _pluginManager;
    private readonly PluginManagerWindow _owner;
    private readonly OnlineResourceService _onlineResourceService = new();

    [ObservableProperty]
    private ObservableCollection<PluginListItem> plugins = [];

    [ObservableProperty]
    private ObservableCollection<OnlinePluginItem> onlinePlugins = [];

    [ObservableProperty]
    private PluginListItem? selectedPlugin;

    [ObservableProperty]
    private OnlinePluginItem? selectedOnlinePlugin;

    [ObservableProperty]
    private bool isReloading = false;

    [ObservableProperty]
    private bool isLoadingOnlinePlugins = false;

    public PluginManagerViewModel() { }

    public PluginManagerViewModel(PluginManagerWindow owner, PluginManager pluginManager)
    {
        _owner = owner;
        _pluginManager = pluginManager;
        LoadPlugins();
        _ = LoadOnlinePluginsAsync();
    }

    private void LoadPlugins()
    {
        Plugins.Clear();

        var enabledPlugins = GetEnabledPlugins();
        var discoveredPlugins = _pluginManager.DiscoverAllPlugins();

        foreach (var plugin in discoveredPlugins)
        {
            var isEnabled = plugin.IsBuiltIn || enabledPlugins.Contains(plugin.LibraryName);

            Plugins.Add(new PluginListItem
            {
                LibraryName = plugin.LibraryName,
                DisplayName = plugin.DisplayName,
                Version = plugin.Version,
                Description = plugin.Description,
                IsNotDownloaded = false,
                IsEnabled = isEnabled,
                IsBuiltIn = plugin.IsBuiltIn,
                IsLoaded = plugin.IsLoaded,
                CategoryCount = plugin.CategoryCount
            });
        }

        Logger.Information($"Loaded {Plugins.Count} plugins into manager");
    }

    private async Task LoadOnlinePluginsAsync()
    {
        IsLoadingOnlinePlugins = true;
        OnlinePlugins.Clear();

        try
        {
            var onlinePlugins = await _onlineResourceService.GetAvailablePluginsAsync();
            var installedLibraryNames = Plugins.Select(p => p.LibraryName).ToHashSet();

            foreach (var plugin in onlinePlugins)
            {
                var isInstalled = installedLibraryNames.Contains(plugin.LibraryName);
                
                OnlinePlugins.Add(new OnlinePluginItem
                {
                    LibraryName = plugin.LibraryName,
                    DisplayName = plugin.Name,
                    Version = plugin.Version,
                    Description = plugin.Description,
                    Author = plugin.Author,
                    Size = plugin.Size,
                    IsInstalled = isInstalled,
                    OnlinePlugin = plugin
                });
            }

            Logger.Information($"Loaded {OnlinePlugins.Count} online plugins");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load online plugins: {ex.Message}");
        }
        finally
        {
            IsLoadingOnlinePlugins = false;
        }
    }

    private HashSet<string> GetEnabledPlugins()
    {
        try
        {
            var project = MainWindowModel.Project;
            if (project == null)
                return [];

            var enabledPluginsStr = project.ProjectConfig.Get<string>("EnabledPlugins").Value;
            if (string.IsNullOrEmpty(enabledPluginsStr))
                return [];

            return enabledPluginsStr.Split(';', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load enabled plugins: {ex.Message}");
            return [];
        }
    }

    private void SaveEnabledPlugins()
    {
        try
        {
            var project = MainWindowModel.Project;
            if (project == null)
                return;

            var enabledPlugins = Plugins
                .Where(p => p.IsEnabled && !p.IsBuiltIn)
                .Select(p => p.LibraryName)
                .ToList();

            var enabledPluginsStr = string.Join(";", enabledPlugins);
            project.ProjectConfig.SetOrCreate("EnabledPlugins", enabledPluginsStr);
            project.ProjectConfig.CommitAll();
            project.Save();

            Logger.Information($"Saved {enabledPlugins.Count} enabled plugins to project settings");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save enabled plugins: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task TogglePlugin(PluginListItem plugin)
    {
        if (plugin == null || plugin.IsBuiltIn)
            return;

        plugin.IsEnabled = !plugin.IsEnabled;
        SaveEnabledPlugins();
        
        Logger.Information($"Plugin {plugin.DisplayName} {(plugin.IsEnabled ? "enabled" : "disabled")}");
        
        await ReloadPlugins();
    }

    [RelayCommand]
    private async Task DownloadPlugin(OnlinePluginItem? pluginItem)
    {
        if (pluginItem == null || pluginItem.OnlinePlugin == null)
            return;

        if (pluginItem.IsInstalled)
        {
            Logger.Information($"Plugin {pluginItem.DisplayName} is already installed");
            return;
        }

        try
        {
            pluginItem.IsDownloading = true;
            var plugin = pluginItem.OnlinePlugin;

            Logger.Information($"Downloading plugin: {plugin.Name}");

            string fileName = Path.GetFileName(new Uri(plugin.DownloadUrl).LocalPath);
            if (string.IsNullOrEmpty(fileName) || !fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                fileName = $"{plugin.LibraryName}.dll";
            }

            string destinationPath = Path.Combine(PluginManager.PluginsDirectory, fileName);

            var progress = new Progress<double>(p => pluginItem.DownloadProgress = p);
            
            string? downloadedPath = await _onlineResourceService.DownloadPluginAsync(
                plugin.DownloadUrl,
                destinationPath,
                progress);

            if (downloadedPath != null)
            {
                Logger.Information($"Plugin {plugin.Name} downloaded successfully");
                
                // Reload plugins to show the newly downloaded plugin
                await _pluginManager.LoadPlugin(downloadedPath);
                LoadPlugins();
                await LoadOnlinePluginsAsync();
            }
            else
            {
                Logger.Error($"Failed to download plugin {plugin.Name}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error downloading plugin: {ex.Message}");
        }
        finally
        {
            pluginItem.IsDownloading = false;
            pluginItem.DownloadProgress = 0;
        }
    }

    [RelayCommand]
    private async Task ReloadPlugins()
    {
        try
        {
            IsReloading = true;
            Logger.Information("Reloading all plugins...");

            await _pluginManager.ReloadAllPlugins();
            LoadPlugins();

            Logger.Information("Plugin reload complete");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to reload plugins: {ex.Message}");
        }
        finally
        {
            IsReloading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshOnlinePlugins()
    {
        await LoadOnlinePluginsAsync();
    }

    [RelayCommand]
    private void Close()
    {
        _owner.Close();
    }
}

public partial class PluginListItem : ObservableObject
{
    [ObservableProperty]
    private string libraryName = string.Empty;

    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    private string version = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private bool isNotDownloaded = true;

    [ObservableProperty]
    private bool isEnabled;

    [ObservableProperty]
    private bool isBuiltIn;

    [ObservableProperty]
    private bool isLoaded;

    [ObservableProperty]
    private int categoryCount;

    public string StatusText => IsBuiltIn ? "Built-in" : (IsLoaded ? "Loaded" : (IsEnabled ? "Enabled" : "Disabled"));
    
    partial void OnIsEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(StatusText));
    }

    partial void OnIsLoadedChanged(bool value)
    {
        OnPropertyChanged(nameof(StatusText));
    }
}

public partial class OnlinePluginItem : ObservableObject
{
    [ObservableProperty]
    private string libraryName = string.Empty;

    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    private string version = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string? author;

    [ObservableProperty]
    private long size;

    [ObservableProperty]
    private bool isInstalled;

    [ObservableProperty]
    private bool isDownloading;

    [ObservableProperty]
    private double downloadProgress;

    [ObservableProperty]
    private OnlinePlugin? onlinePlugin;

    public string SizeText => Size > 1024 * 1024 
        ? $"{Size / (1024.0 * 1024.0):F2} MB" 
        : $"{Size / 1024.0:F2} KB";

    public string StatusText => IsInstalled ? "Installed" : "Available";
}
