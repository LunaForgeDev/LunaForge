using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using LunaForge.Backend.Attributes.TreeNodesAttributes;
using LunaForge.Models.TreeNodes;
using LunaForge.Services;
using Serilog;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Plugins;

/// <summary>
/// Base class for implementing node libraries with automatic node discovery
/// </summary>
public abstract class NodeLibraryBase : INodeLibrary
{
    protected readonly ILogger Logger;
    private readonly List<NodeCategory> _categories = [];

    public abstract string LibraryName { get; }
    public abstract string DisplayName { get; }
    public abstract string Version { get; }
    public virtual string Description => string.Empty;

    public IReadOnlyList<NodeCategory> Categories => _categories;

    protected NodeLibraryBase()
    {
        Logger = CoreLogger.Create(LibraryName);
    }

    public virtual void Initialize()
    {
        Logger.Information($"Initializing {DisplayName} v{Version}");
        RegisterCategories();
    }

    public virtual void Shutdown()
    {
        Logger.Information($"Shutting down {DisplayName}");
        _categories.Clear();
    }

    public virtual UserControl? GetAttributeEditor(string attributeType) => null;

    /// <summary>
    /// Override this method to register your node categories
    /// </summary>
    protected abstract void RegisterCategories();

    /// <summary>
    /// Creates a new category and adds it to the library
    /// </summary>
    protected NodeCategory CreateCategory(string name)
    {
        var category = new NodeCategory(name);
        _categories.Add(category);
        return category;
    }
}
