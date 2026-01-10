using LunaForge.Models.TreeNodes;
using LunaForge.Services;
using McMaster.NETCore.Plugins;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Plugins;

public class PluginManager : IDisposable
{
    private static readonly ILogger Logger = CoreLogger.Create("PluginManager");
    private static readonly string PluginPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LunaForge", ".plugins");

    private readonly Dictionary<string, PluginLoadContext> _loadedPlugins = [];
    private readonly Dictionary<string, INodeLibrary> _libraries = [];
    private readonly Dictionary<string, string> _libraryToPluginMap = [];
    private readonly Dictionary<string, ICompilationTarget> _compilationTargets = [];
    private readonly Dictionary<string, string> _targetToPluginMap = [];
    private FileSystemWatcher? _watcher;
    private bool _disposed = false;
    private bool _hotReloadEnabled = false;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    private INodeLibrary? _builtInLibrary;
    private const string BuiltInLibraryKey = "[Built-In]";

    public event Action<INodeLibrary>? OnLibraryLoaded;
    public event Action<string>? OnLibraryUnloaded;
    public event Action<string, Exception>? OnLibraryLoadError;

    public IReadOnlyDictionary<string, INodeLibrary> LoadedLibraries => _libraries;
    public IReadOnlyDictionary<string, ICompilationTarget> CompilationTargets => _compilationTargets;
    public bool IsHotReloadEnabled => _hotReloadEnabled;
    public static string PluginsDirectory => PluginPath;

    public PluginManager()
    {
        Directory.CreateDirectory(PluginPath).Attributes |= FileAttributes.Directory | FileAttributes.Hidden;
        LoadBuiltInLibrary();
    }

    private void LoadBuiltInLibrary()
    {
        try
        {
            Logger.Information("Loading built-in node library...");
            
            _builtInLibrary = new Nodes.BuiltInNodeLibrary();
            _builtInLibrary.Initialize();
            
            _libraries[_builtInLibrary.LibraryName] = _builtInLibrary;
            _libraryToPluginMap[_builtInLibrary.LibraryName] = BuiltInLibraryKey;
            
            Logger.Information($"Built-in library loaded: {_builtInLibrary.DisplayName} (v{_builtInLibrary.Version})");
            OnLibraryLoaded?.Invoke(_builtInLibrary);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load built-in library: {ex.Message}");
            OnLibraryLoadError?.Invoke(BuiltInLibraryKey, ex);
        }
    }

    private HashSet<string> GetEnabledPlugins()
    {
        try
        {
            var project = ViewModels.MainWindowModel.Project;
            if (project == null)
                return [];

            var enabledPluginsStr = project.ProjectConfig.Get<string>("EnabledPlugins").Value;
            if (string.IsNullOrEmpty(enabledPluginsStr))
                return [];

            return enabledPluginsStr.Split(';', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        }
        catch
        {
            return [];
        }
    }

    private bool ShouldLoadLibrary(string libraryName)
    {
        if (libraryName == "LunaForge.BuiltIn")
            return true;

        var enabledPlugins = GetEnabledPlugins();
        
        // If no plugins are explicitly configured, don't load any external plugins by default
        if (enabledPlugins.Count == 0)
            return false;

        return enabledPlugins.Contains(libraryName);
    }

    private bool IsPluginEnabled(string pluginKey)
    {
        var enabledPlugins = GetEnabledPlugins();
        
        // If no plugins are explicitly configured, don't load any external plugins by default
        if (enabledPlugins.Count == 0)
            return false;

        return enabledPlugins.Any(ep => ep.Contains(pluginKey, StringComparison.OrdinalIgnoreCase));
    }

    public async Task LoadAllPlugins()
    {
        try
        {
            if (!Directory.Exists(PluginPath))
            {
                Logger.Warning($"Plugin directory does not exist: {PluginPath}");
                return;
            }

            var dllFiles = Directory.GetFiles(PluginPath, "*.dll", SearchOption.TopDirectoryOnly);

            if (dllFiles.Length == 0)
            {
                Logger.Information($"No plugin files found in {PluginPath}");
                return;
            }

            foreach (var dllPath in dllFiles)
                await LoadPlugin(dllPath);

            Logger.Information($"Loaded {_libraries.Count} node libraries ({_libraries.Count - 1} plugins + 1 built-in)");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error loading all plugins. Reason:\n{ex}");
        }
    }

    public async Task LoadPlugin(string dllPath)
    {
        PluginLoader? loader = null;

        try
        {
            if (!File.Exists(dllPath))
            {
                Logger.Warning($"Plugin DLL not found: {dllPath}");
                return;
            }

            string pluginKey = Path.GetFileNameWithoutExtension(dllPath);

            if (_loadedPlugins.ContainsKey(pluginKey))
            {
                Logger.Information($"Plugin {pluginKey} already loaded, reloading...");
                await UnloadPlugin(pluginKey);
            }

            Logger.Information($"Loading plugin: {pluginKey}");

            loader = PluginLoader.CreateFromAssemblyFile(
                dllPath,
                sharedTypes: Type.EmptyTypes,
                config =>
                {
                    config.IsUnloadable = true;
                    config.PreferSharedTypes = true;
                    config.LoadInMemory = false;
                    config.DefaultContext = System.Runtime.Loader.AssemblyLoadContext.Default;
                }
            );

            var assembly = loader.LoadDefaultAssembly();

            var libraryTypes = assembly.GetTypes()
                .Where(t => typeof(INodeLibrary).IsAssignableFrom(t)
                    && !t.IsInterface
                    && !t.IsAbstract)
                .ToList();

            var compilationTargetTypes = assembly.GetTypes()
                .Where(t => typeof(ICompilationTarget).IsAssignableFrom(t)
                    && !t.IsInterface
                    && !t.IsAbstract)
                .ToList();

            if (libraryTypes.Count == 0 && compilationTargetTypes.Count == 0)
            {
                Logger.Warning($"No INodeLibrary or ICompilationTarget implementations found in {pluginKey}");
                loader.Dispose();
                return;
            }

            var context = new PluginLoadContext(dllPath, loader);
            var loadedLibraries = new List<string>();
            var loadedTargets = new List<string>();
            var pluginIsEnabled = false;

            foreach (var libraryType in libraryTypes)
            {
                try
                {
                    var library = (INodeLibrary?)Activator.CreateInstance(libraryType);
                    if (library == null)
                    {
                        Logger.Error($"Failed to instantiate {libraryType.Name}: Activator returned null");
                        continue;
                    }

                    if (!ShouldLoadLibrary(library.LibraryName))
                    {
                        Logger.Information($"Plugin {library.DisplayName} is disabled, skipping load");
                        continue;
                    }

                    pluginIsEnabled = true;
                    library.Initialize();

                    _libraries[library.LibraryName] = library;
                    _libraryToPluginMap[library.LibraryName] = pluginKey;
                    loadedLibraries.Add(library.LibraryName);

                    Logger.Information($"Loaded library: {library.DisplayName} (v{library.Version})");
                    OnLibraryLoaded?.Invoke(library);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to instantiate {libraryType.Name}: {ex.Message}");
                    OnLibraryLoadError?.Invoke(pluginKey, ex);
                }
            }

            // Only load compilation targets if the plugin has at least one enabled library
            if (pluginIsEnabled)
            {
                foreach (var targetType in compilationTargetTypes)
                {
                    try
                    {
                        var target = (ICompilationTarget?)Activator.CreateInstance(targetType);
                        if (target == null)
                        {
                            Logger.Error($"Failed to instantiate compilation target {targetType.Name}: Activator returned null");
                            continue;
                        }

                        _compilationTargets[target.TargetName] = target;
                        _targetToPluginMap[target.TargetName] = pluginKey;
                        loadedTargets.Add(target.TargetName);

                        Logger.Information($"Loaded compilation target: {target.TargetName}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to instantiate compilation target {targetType.Name}: {ex.Message}");
                        OnLibraryLoadError?.Invoke(pluginKey, ex);
                    }
                }
            }
            else
            {
                Logger.Information($"Plugin {pluginKey} is disabled, skipping compilation targets");
            }

            if (loadedLibraries.Count > 0 || loadedTargets.Count > 0)
            {
                context.LibraryNames.AddRange(loadedLibraries);
                context.CompilationTargetNames.AddRange(loadedTargets);
                _loadedPlugins[pluginKey] = context;
            }
            else
            {
                Logger.Warning($"No libraries or targets loaded from {pluginKey}, disposing loader");
                context.Dispose();
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error loading plugin {dllPath}: {ex.Message}");
            OnLibraryLoadError?.Invoke(Path.GetFileNameWithoutExtension(dllPath), ex);
            
            loader?.Dispose();
        }
    }

    public async Task UnloadPlugin(string pluginKey)
    {
        try
        {
            // Don't allow unloading the built-in library
            if (pluginKey == BuiltInLibraryKey)
            {
                Logger.Warning("Cannot unload built-in library");
                return;
            }

            if (!_loadedPlugins.TryGetValue(pluginKey, out var context))
            {
                Logger.Warning($"Plugin {pluginKey} not found in loaded plugins");
                return;
            }

            foreach (var libraryName in context.LibraryNames.ToList())
            {
                if (_libraries.TryGetValue(libraryName, out var library))
                {
                    try
                    {
                        library.Shutdown();
                        _libraries.Remove(libraryName);
                        _libraryToPluginMap.Remove(libraryName);
                        OnLibraryUnloaded?.Invoke(libraryName);
                        Logger.Information($"Unloaded library: {libraryName}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error shutting down library {libraryName}: {ex.Message}");
                    }
                }
            }

            // Unload compilation targets
            foreach (var targetName in context.CompilationTargetNames.ToList())
            {
                if (_compilationTargets.Remove(targetName))
                {
                    _targetToPluginMap.Remove(targetName);
                    Logger.Information($"Unloaded compilation target: {targetName}");
                }
            }

            context.Dispose();
            _loadedPlugins.Remove(pluginKey);
            Logger.Information($"Unloaded plugin: {pluginKey}");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error unloading plugin {pluginKey}: {ex.Message}");
        }
    }

    public async Task ReloadPluginsWithSettings()
    {
        Logger.Information("Reloading plugins based on current settings...");

        var dllFiles = Directory.Exists(PluginPath) 
            ? Directory.GetFiles(PluginPath, "*.dll", SearchOption.TopDirectoryOnly)
            : [];

        var pluginKeys = _loadedPlugins.Keys.ToList();
        foreach (var pluginKey in pluginKeys)
        {
            await UnloadPlugin(pluginKey);
        }

        foreach (var dllPath in dllFiles)
        {
            await LoadPlugin(dllPath);
        }

        Logger.Information($"Plugin reload complete. {_libraries.Count} libraries loaded.");
    }

    public void EnableHotReload()
    {
        if (_hotReloadEnabled)
        {
            Logger.Warning("Hot reload is already enabled");
            return;
        }

        _watcher = new FileSystemWatcher(PluginPath, "*.dll")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };

        _watcher.Changed += OnPluginFileChanged;
        _watcher.Error += OnWatcherError;

        _hotReloadEnabled = true;
        Logger.Information("Hot reload enabled for plugin directory");
    }

    public void DisableHotReload()
    {
        if (_watcher != null)
        {
            _watcher.Changed -= OnPluginFileChanged;
            _watcher.Error -= OnWatcherError;
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }

        _hotReloadEnabled = false;
        Logger.Information("Hot reload disabled");
    }

    private async void OnPluginFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!await _reloadLock.WaitAsync(0))
        {
            Logger.Debug($"Skipping file change event for {e.Name}, reload already in progress");
            return;
        }

        try
        {
            await Task.Delay(1000);

            if (!File.Exists(e.FullPath))
            {
                Logger.Warning($"Plugin file {e.Name} no longer exists, skipping reload");
                return;
            }

            Logger.Information($"Plugin file changed: {e.Name}, reloading...");

            string pluginKey = Path.GetFileNameWithoutExtension(e.Name);
            await UnloadPlugin(pluginKey);
            await LoadPlugin(e.FullPath);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error during hot reload of {e.Name}: {ex.Message}");
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        Logger.Warning($"FileSystemWatcher error: {e.GetException()?.Message}");
    }

    public INodeLibrary? GetLibrary(string libraryName) =>
        _libraries.TryGetValue(libraryName, out var lib) ? lib : null;

    public ICompilationTarget? GetCompilationTarget(string targetName) =>
        _compilationTargets.TryGetValue(targetName, out var target) ? target : null;

    public TreeNode? CreateNode(Type nodeType)
    {
        try
        {
            if (!typeof(TreeNode).IsAssignableFrom(nodeType) || nodeType.IsAbstract)
            {
                Logger.Error($"Type {nodeType.Name} is not a valid TreeNode");
                return null;
            }

            var instance = (TreeNode?)Activator.CreateInstance(nodeType);
            return instance;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to create node of type {nodeType.Name}: {ex.Message}");
            return null;
        }
    }

    public IEnumerable<(string LibraryName, NodeCategory Category)> GetAllNodeCategories()
    {
        if (_builtInLibrary != null)
        {
            foreach (var category in _builtInLibrary.Categories)
            {
                yield return (_builtInLibrary.LibraryName, category);
            }
        }

        foreach (var (libraryName, library) in _libraries)
        {
            if (library == _builtInLibrary)
                continue;

            foreach (var category in library.Categories)
            {
                yield return (libraryName, category);
            }
        }
    }

    public TreeNode? CreateNodeByDescriptor(string libraryName, string categoryName, string nodeTypeName)
    {
        var library = GetLibrary(libraryName);
        if (library == null)
        {
            Logger.Error($"Library not found: {libraryName}");
            return null;
        }

        var category = library.Categories.FirstOrDefault(c => c.Name == categoryName);
        if (category == null)
        {
            Logger.Error($"Category not found: {categoryName} in library {libraryName}");
            return null;
        }

        var descriptor = category.Nodes.FirstOrDefault(n => n.NodeType?.Name == nodeTypeName);
        if (descriptor == null)
        {
            Logger.Error($"Node type not found: {nodeTypeName} in category {categoryName}");
            return null;
        }

        return descriptor.CreateInstance();
    }

    public IEnumerable<NodeDescriptor> GetAllNodeDescriptors()
    {
        foreach (var library in _libraries.Values)
        {
            foreach (var category in library.Categories)
            {
                foreach (var node in category.Nodes.Where(n => !n.IsSeparator))
                {
                    yield return node;
                }
            }
        }
    }

    public IEnumerable<string> GetAvailableCompilationTargets() =>
        _compilationTargets.Keys;

    public async Task ReloadAllPlugins()
    {
        await ReloadPluginsWithSettings();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        DisableHotReload();

        foreach (var lib in _libraries.Values)
        {
            // Don't shutdown built-in library during dispose
            if (lib == _builtInLibrary)
                continue;

            try
            {
                lib.Shutdown();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error shutting down library: {ex.Message}");
            }
        }

        foreach (var context in _loadedPlugins.Values)
        {
            try
            {
                context.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error disposing context: {ex.Message}");
            }
        }

        // Shutdown built-in library last
        if (_builtInLibrary != null)
        {
            try
            {
                _builtInLibrary.Shutdown();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error shutting down built-in library: {ex.Message}");
            }
        }

        _libraries.Clear();
        _loadedPlugins.Clear();
        _libraryToPluginMap.Clear();
        _compilationTargets.Clear();
        _targetToPluginMap.Clear();
        _reloadLock.Dispose();
        _disposed = true;

        Logger.Information("PluginManager disposed");
    }

    public List<DiscoveredPlugin> DiscoverAllPlugins()
    {
        var discoveredPlugins = new List<DiscoveredPlugin>();

        if (_builtInLibrary != null)
        {
            discoveredPlugins.Add(new DiscoveredPlugin
            {
                LibraryName = _builtInLibrary.LibraryName,
                DisplayName = _builtInLibrary.DisplayName,
                Version = _builtInLibrary.Version,
                Description = _builtInLibrary.Description,
                IsBuiltIn = true,
                IsLoaded = true,
                CategoryCount = _builtInLibrary.Categories.Count
            });
        }

        if (!Directory.Exists(PluginPath))
            return discoveredPlugins;

        var dllFiles = Directory.GetFiles(PluginPath, "*.dll", SearchOption.TopDirectoryOnly);
        var enabledPlugins = GetEnabledPlugins();

        foreach (var dllPath in dllFiles)
        {
            try
            {
                var pluginKey = Path.GetFileNameWithoutExtension(dllPath);
                
                // Check if plugin is already loaded
                if (_loadedPlugins.TryGetValue(pluginKey, out var context))
                {
                    // Plugin is loaded, get info from loaded libraries
                    foreach (var libraryName in context.LibraryNames)
                    {
                        if (_libraries.TryGetValue(libraryName, out var library))
                        {
                            discoveredPlugins.Add(new DiscoveredPlugin
                            {
                                LibraryName = library.LibraryName,
                                DisplayName = library.DisplayName,
                                Version = library.Version,
                                Description = library.Description,
                                IsBuiltIn = false,
                                IsLoaded = true,
                                CategoryCount = library.Categories.Count,
                                DllPath = dllPath
                            });
                        }
                    }
                }
                else
                {
                    // Plugin is not loaded, try to get info by temporarily loading the assembly
                    var pluginInfo = GetPluginInfoWithoutLoading(dllPath);
                    foreach (var info in pluginInfo)
                    {
                        info.DllPath = dllPath;
                        discoveredPlugins.Add(info);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to discover plugin {dllPath}: {ex.Message}");
            }
        }

        return discoveredPlugins;
    }

    private List<DiscoveredPlugin> GetPluginInfoWithoutLoading(string dllPath)
    {
        var plugins = new List<DiscoveredPlugin>();
        PluginLoader? tempLoader = null;

        try
        {
            tempLoader = PluginLoader.CreateFromAssemblyFile(
                dllPath,
                sharedTypes: Type.EmptyTypes,
                config =>
                {
                    config.IsUnloadable = true;
                    config.PreferSharedTypes = true;
                    config.LoadInMemory = true;
                    config.DefaultContext = System.Runtime.Loader.AssemblyLoadContext.Default;
                }
            );

            var assembly = tempLoader.LoadDefaultAssembly();

            var libraryTypes = assembly.GetTypes()
                .Where(t => typeof(INodeLibrary).IsAssignableFrom(t)
                    && !t.IsInterface
                    && !t.IsAbstract)
                .ToList();

            foreach (var libraryType in libraryTypes)
            {
                try
                {
                    var library = (INodeLibrary?)Activator.CreateInstance(libraryType);
                    if (library == null)
                        continue;

                    plugins.Add(new DiscoveredPlugin
                    {
                        LibraryName = library.LibraryName,
                        DisplayName = library.DisplayName,
                        Version = library.Version,
                        Description = library.Description,
                        IsBuiltIn = false,
                        IsLoaded = false,
                        CategoryCount = 0 // Can't get category count without initializing
                    });
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to instantiate {libraryType.Name} for discovery: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warning($"Failed to get plugin info from {dllPath}: {ex.Message}");
        }
        finally
        {
            tempLoader?.Dispose();
        }

        return plugins;
    }
}

public class DiscoveredPlugin
{
    public string LibraryName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsBuiltIn { get; set; }
    public bool IsLoaded { get; set; }
    public int CategoryCount { get; set; }
    public string? DllPath { get; set; }
}

public class PluginLoadContext(string pluginPath, PluginLoader? loader = null) : IDisposable
{
    private bool _disposed = false;

    public string PluginPath { get; } = pluginPath;
    public List<string> LibraryNames { get; } = [];
    public List<string> CompilationTargetNames { get; } = [];

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            loader?.Dispose();
        }
        catch (Exception ex)
        {
            CoreLogger.Create("PluginLoadContext").Error($"Error disposing loader: {ex.Message}");
        }

        _disposed = true;
    }
}