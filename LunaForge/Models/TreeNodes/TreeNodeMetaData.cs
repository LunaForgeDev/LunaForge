using LunaForge.Backend.Attributes.TreeNodesAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LunaForge.Plugins;

namespace LunaForge.Models.TreeNodes;

public partial class TreeNodeMetaData() : ObservableObject
{
    [ObservableProperty]
    public bool leaf = false;
    [ObservableProperty]
    public bool isFolder = false;
    [ObservableProperty]
    public bool cannotBeDeleted = false;
    [ObservableProperty]
    public bool cannotBeBanned = false;
    [ObservableProperty]
    public ImageSource? icon = null;
    //public bool CannotBeDragged = false;
    //public bool CannotBeDragTarget = false;
    public Type[] RequireParent = [];
    public Type[][] RequireAncestor = [];
    [ObservableProperty]
    public bool unique = false;
    [ObservableProperty]
    public bool cannotBeDragDropped = false;

    public static TreeNodeMetaData Process(TreeNode node)
    {
        Type t = node.GetType();
        var iconPath = t.GetCustomAttribute<NodeIconAttribute>()?.Path;
        
        TreeNodeMetaData meta = new()
        {
            Leaf = t.IsDefined(typeof(LeafNodeAttribute), false),
            IsFolder = t.IsDefined(typeof(IsFolderAttribute), false),
            CannotBeDeleted = t.IsDefined(typeof(CannotDeleteAttribute), false),
            CannotBeBanned = t.IsDefined(typeof(CannotBanAttribute), false),
            Icon = LoadIconFromAssembly(t.Assembly, iconPath),
            RequireParent = GetTypes(t.GetCustomAttribute<RequireParentAttribute>()?.ParentType ?? []),
            Unique = t.IsDefined(typeof(UniqueAttribute), false),
            CannotBeDragDropped = t.IsDefined(typeof(CannotDragDropAttribute), false)
        };
        var attrs = t.GetCustomAttributes<RequireAncestorAttribute>();
        meta.RequireAncestor = null;
        if (attrs.Any())
        {
            meta.RequireAncestor = [.. (from RequireAncestorAttribute at in attrs select GetTypes(at.RequiredTypes))];
        }

        return meta;
    }

    private static ImageSource? LoadIconFromAssembly(Assembly assembly, string? iconPath)
    {
        if (string.IsNullOrEmpty(iconPath))
            return null;

        var assemblyName = assembly.GetName().Name;
        var resourcePath = $"{assemblyName}.Nodes.Images.{iconPath}";
        var icon = LoadImageFromEmbeddedResource(assembly, resourcePath);
        
        if (icon != null)
            return icon;

        return LoadImageFromWpfResource(assemblyName, iconPath);
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

    private static ImageSource? LoadImageFromWpfResource(string? assemblyName, string imageName)
    {
        try
        {
            var uri = new Uri($"pack://application:,,,/{assemblyName};component/Nodes/Images/{imageName}", UriKind.Absolute);
            var bitmap = new BitmapImage(uri);
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public static Type[] GetTypes(Type[] src)
    {
        if (src != null)
        {
            LinkedList<Type> types = [];
            Type it = typeof(ITypeEnumerable);
            foreach (Type t in src)
            {
                if (it.IsAssignableFrom(t))
                {
                    ITypeEnumerable o = t.GetConstructor(Type.EmptyTypes).Invoke([]) as ITypeEnumerable;
                    foreach (Type ty in o)
                        types.AddLast(ty);
                }
                else
                {
                    types.AddLast(t);
                }
            }
            return [.. types];
        }
        return [];
    }
}
