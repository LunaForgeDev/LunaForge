using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using LunaForge.Backend.Attributes.TreeNodesAttributes;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Plugins;

public interface INodeLibrary
{
    string LibraryName { get; }
    string DisplayName { get; }

    string Version { get; }

    string Description { get; }

    IReadOnlyList<NodeCategory> Categories { get; }

    void Initialize();

    void Shutdown();

    UserControl? GetAttributeEditor(string attributeType);
}

public class NodeCategory
{
    public string Name { get; set; } = string.Empty;
    public List<NodeDescriptor> Nodes { get; set; } = [];

    public NodeCategory() { }

    public NodeCategory(string name)
    {
        Name = name;
    }

    public void AddNode<TNode>(string displayName, Func<TreeNode>? factory = null)
        where TNode : TreeNode
    {
        var nodeType = typeof(TNode);
        var resolvedIconPath = GetIconPathFromAttribute(nodeType);

        Nodes.Add(new NodeDescriptor
        {
            NodeType = nodeType,
            DisplayName = displayName,
            IconPath = resolvedIconPath,
            Factory = factory ?? (() => Activator.CreateInstance<TNode>()!)
        });
    }

    public void AddNode(Type nodeType, string displayName, Func<TreeNode>? factory = null)
    {
        if (!typeof(TreeNode).IsAssignableFrom(nodeType))
            throw new ArgumentException($"Type {nodeType.Name} does not inherit from TreeNode", nameof(nodeType));

        var resolvedIconPath = GetIconPathFromAttribute(nodeType);

        Nodes.Add(new NodeDescriptor
        {
            NodeType = nodeType,
            DisplayName = displayName,
            IconPath = resolvedIconPath,
            Factory = factory ?? (() => (TreeNode)Activator.CreateInstance(nodeType)!)
        });
    }

    public void AddSeparator()
    {
        Nodes.Add(NodeDescriptor.CreateSeparator());
    }

    private static string GetIconPathFromAttribute(Type nodeType)
    {
        var iconAttribute = nodeType.GetCustomAttribute<NodeIconAttribute>();
        if (iconAttribute == null || string.IsNullOrEmpty(iconAttribute.Path))
            return string.Empty;
        
        return $"/{nodeType.Assembly.GetName().Name};component/Nodes/Images/{iconAttribute.Path}";
    }
}

public class NodeDescriptor
{
    public Type? NodeType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public bool IsSeparator { get; set; }
    public Func<TreeNode>? Factory { get; set; }

    public static NodeDescriptor CreateSeparator()
    {
        return new NodeDescriptor
        {
            IsSeparator = true,
            DisplayName = string.Empty,
            IconPath = string.Empty
        };
    }

    public TreeNode? CreateInstance()
    {
        if (IsSeparator || Factory == null)
            return null;

        try
        {
            return Factory();
        }
        catch
        {
            return null;
        }
    }
}
