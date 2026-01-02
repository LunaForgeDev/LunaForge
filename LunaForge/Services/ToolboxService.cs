using LunaForge.Models.TreeNodes;
using LunaForge.Plugins;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Services;

public class ToolboxService
{
    private static readonly ILogger Logger = CoreLogger.Create("ToolboxService");

    private readonly PluginManager _pluginManager;
    private readonly ObservableCollection<ToolboxCategory> _toolboxCategories = [];

    public IReadOnlyCollection<ToolboxCategory> ToolboxCategories => _toolboxCategories;

    public ToolboxService(PluginManager pluginManager)
    {
        _pluginManager = pluginManager;
        _pluginManager.OnLibraryLoaded += OnLibraryLoaded;
        _pluginManager.OnLibraryUnloaded += OnLibraryUnloaded;
    }

    public void RebuildToolbox()
    {
        _toolboxCategories.Clear();

        foreach (var (libraryName, category) in _pluginManager.GetAllNodeCategories())
        {
            var library = _pluginManager.GetLibrary(libraryName);
            if (library == null) continue;

            var toolboxCategory = new ToolboxCategory
            {
                LibraryName = libraryName,
                LibraryDisplayName = library.DisplayName,
                CategoryName = category.Name,
                DisplayName = $"{library.DisplayName} - {category.Name}",
                Items = []
            };

            foreach (var node in category.Nodes)
            {
                if (node.IsSeparator)
                {
                    toolboxCategory.Items.Add(ToolboxItem.CreateSeparator());
                }
                else
                {
                    toolboxCategory.Items.Add(new ToolboxItem
                    {
                        LibraryName = libraryName,
                        CategoryName = category.Name,
                        NodeType = node.NodeType,
                        DisplayName = node.DisplayName,
                        IconPath = node.IconPath,
                        Tag = $"{libraryName}.{category.Name}.{node.NodeType?.Name}",
                        Factory = node.Factory
                    });
                }
            }

            _toolboxCategories.Add(toolboxCategory);
        }

        Logger.Information($"Rebuilt toolbox with {_toolboxCategories.Count} categories");
    }

    public TreeNode? CreateNodeByTag(string tag)
    {
        var item = FindItemByTag(tag);
        return item?.CreateInstance();
    }

    public TreeNode? CreateNode(ToolboxItem item)
    {
        return item?.CreateInstance();
    }

    public ToolboxItem? FindItemByTag(string tag)
    {
        foreach (var category in _toolboxCategories)
        {
            var item = category.Items.FirstOrDefault(i => i.Tag == tag);
            if (item != null)
                return item;
        }
        return null;
    }

    public IEnumerable<ToolboxItem> GetAllItems()
    {
        return _toolboxCategories.SelectMany(c => c.Items.Where(i => !i.IsSeparator));
    }

    private void OnLibraryLoaded(INodeLibrary library)
    {
        Logger.Information($"Library loaded: {library.DisplayName}, rebuilding toolbox");
        RebuildToolbox();
        foreach (var category in library.Categories)
            Logger.Information($"Loaded Category: {category.Name} (from {library.DisplayName}) with {category.Nodes.Count} nodes");
    }

    private void OnLibraryUnloaded(string libraryName)
    {
        Logger.Information($"Library unloaded: {libraryName}, rebuilding toolbox");
        RebuildToolbox();
    }
}

public class ToolboxCategory
{
    public string LibraryName { get; set; } = string.Empty;
    public string LibraryDisplayName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<ToolboxItem> Items { get; set; } = [];
}

public class ToolboxItem
{
    public string LibraryName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public Type? NodeType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public bool IsSeparator { get; set; }
    public Func<TreeNode>? Factory { get; set; }

    public static ToolboxItem CreateSeparator()
    {
        return new ToolboxItem
        {
            IsSeparator = true
        };
    }

    public TreeNode? CreateInstance()
    {
        if (IsSeparator || Factory == null)
            return null;

        try
        {
            var node = Factory();
            Logger.Information($"Created node: {DisplayName} ({NodeType?.Name})");
            return node;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to create node {DisplayName}: {ex.Message}");
            return null;
        }
    }

    private static readonly ILogger Logger = CoreLogger.Create("ToolboxItem");
}
