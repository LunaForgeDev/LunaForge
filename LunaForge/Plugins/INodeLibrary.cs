using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        var (iconPath, icon) = GetIconFromAttribute(nodeType);

        Nodes.Add(new NodeDescriptor
        {
            NodeType = nodeType,
            DisplayName = displayName,
            IconPath = iconPath,
            Icon = icon,
            Factory = factory ?? (() => Activator.CreateInstance<TNode>()!)
        });
    }

    public void AddNode(Type nodeType, string displayName, Func<TreeNode>? factory = null)
    {
        if (!typeof(TreeNode).IsAssignableFrom(nodeType))
            throw new ArgumentException($"Type {nodeType.Name} does not inherit from TreeNode", nameof(nodeType));

        var (iconPath, icon) = GetIconFromAttribute(nodeType);

        Nodes.Add(new NodeDescriptor
        {
            NodeType = nodeType,
            DisplayName = displayName,
            IconPath = iconPath,
            Icon = icon,
            Factory = factory ?? (() => (TreeNode)Activator.CreateInstance(nodeType)!)
        });
    }

    public void AddSeparator()
    {
        Nodes.Add(NodeDescriptor.CreateSeparator());
    }

    private static (string path, ImageSource? icon) GetIconFromAttribute(Type nodeType)
    {
        var iconAttribute = nodeType.GetCustomAttribute<NodeIconAttribute>();
        if (iconAttribute == null || string.IsNullOrEmpty(iconAttribute.Path))
            return (string.Empty, null);

        var assembly = nodeType.Assembly;
        var assemblyName = assembly.GetName().Name;
        var resourcePath = $"{assemblyName}.Nodes.Images.{iconAttribute.Path}";
        var icon = LoadImageFromEmbeddedResource(assembly, resourcePath);
        
        if (icon == null)
        {
            icon = LoadImageFromWpfResource(assembly, iconAttribute.Path);
        }

        var packUri = $"pack://application:,,,/{assemblyName};component/Nodes/Images/{iconAttribute.Path}";
        return (packUri, icon);
    }

    private static ImageSource? LoadImageFromEmbeddedResource(Assembly assembly, string resourcePath)
    {
        try
        {
            using var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
                return null;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static ImageSource? LoadImageFromWpfResource(Assembly assembly, string imageName)
    {
        try
        {
            var assemblyName = assembly.GetName().Name;
            var resourceName = $"/{assemblyName};component/Nodes/Images/{imageName}";
            
            var resourceStream = System.Windows.Application.GetResourceStream(
                new Uri(resourceName, UriKind.Relative));
            
            if (resourceStream?.Stream == null)
                return null;

            using var stream = resourceStream.Stream;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}

public class NodeDescriptor
{
    public Type? NodeType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public ImageSource? Icon { get; set; }
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
