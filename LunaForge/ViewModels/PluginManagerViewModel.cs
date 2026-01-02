using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaForge.Plugins;
using LunaForge.Services;
using LunaForge.Views;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace LunaForge.ViewModels;

public partial class PluginManagerViewModel : ObservableObject
{
    private static readonly ILogger Logger = CoreLogger.Create("PluginManager");
    
    private readonly PluginManager _pluginManager;
    private readonly PluginManagerWindow _owner;

    [ObservableProperty]
    private ObservableCollection<PluginListItem> plugins = [];

    [ObservableProperty]
    private PluginListItem? selectedPlugin;

    [ObservableProperty]
    private bool isReloading = false;

    public PluginManagerViewModel() { }

    public PluginManagerViewModel(PluginManagerWindow owner, PluginManager pluginManager)
    {
        _owner = owner;
        _pluginManager = pluginManager;
        LoadPlugins();
    }

    private void LoadPlugins()
    {
        Plugins.Clear();

        var enabledPlugins = GetEnabledPlugins();

        foreach (var (libraryName, library) in _pluginManager.LoadedLibraries)
        {
            var isBuiltIn = libraryName == "LunaForge.BuiltIn";
            var isEnabled = isBuiltIn || enabledPlugins.Contains(libraryName);

            Plugins.Add(new PluginListItem
            {
                LibraryName = libraryName,
                DisplayName = library.DisplayName,
                Version = library.Version,
                Description = library.Description,
                IsEnabled = isEnabled,
                IsBuiltIn = isBuiltIn,
                CategoryCount = library.Categories.Count
            });
        }

        Logger.Information($"Loaded {Plugins.Count} plugins into manager");
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
    private bool isEnabled;

    [ObservableProperty]
    private bool isBuiltIn;

    [ObservableProperty]
    private int categoryCount;

    public string StatusText => IsBuiltIn ? "Built-in" : (IsEnabled ? "Enabled" : "Disabled");
    
    partial void OnIsEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(StatusText));
    }
}
