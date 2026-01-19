using LunaForge.Backend.Attributes.TreeNodesAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
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
    public string icon = string.Empty;
    //public bool CannotBeDragged = false;
    //public bool CannotBeDragTarget = false;
    public Type[] RequireParent = [];
    public Type[][] RequireAncestor = [];
    [ObservableProperty]
    public bool unique = false;

    public static TreeNodeMetaData Process(TreeNode node)
    {
        Type t = node.GetType();
        TreeNodeMetaData meta = new()
        {
            Leaf = t.IsDefined(typeof(LeafNodeAttribute), false),
            IsFolder = t.IsDefined(typeof(IsFolderAttribute), false),
            CannotBeDeleted = t.IsDefined(typeof(CannotDeleteAttribute), false),
            CannotBeBanned = t.IsDefined(typeof(CannotBanAttribute), false),
            Icon = $"/{t.Assembly.GetName().Name};component/Nodes/Images/{t.GetCustomAttribute<NodeIconAttribute>()?.Path}",
            RequireParent = GetTypes(t.GetCustomAttribute<RequireParentAttribute>()?.ParentType ?? []),
            Unique = t.IsDefined(typeof(UniqueAttribute), false)
        };
        var attrs = t.GetCustomAttributes<RequireAncestorAttribute>();
        meta.RequireAncestor = null;
        if (attrs.Any())
        {
            meta.RequireAncestor = [.. (from RequireAncestorAttribute at in attrs select GetTypes(at.RequiredTypes))];
        }

        return meta;
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
