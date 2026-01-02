using LunaForge.Backend.Attributes.TreeNodesAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

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

    public static TreeNodeMetaData Process(TreeNode node)
    {
        Type t = node.GetType();
        TreeNodeMetaData meta = new()
        {
            Leaf = t.IsDefined(typeof(LeafNodeAttribute), false),
            IsFolder = t.IsDefined(typeof(IsFolderAttribute), false),
            CannotBeDeleted = !t.IsDefined(typeof(CannotDeleteAttribute), false),
            CannotBeBanned = t.IsDefined(typeof(CannotBanAttribute), false),
            Icon = $"/{t.Assembly.GetName().Name};component/Nodes/Images/{t.GetCustomAttribute<NodeIconAttribute>()?.Path}",
        };

        return meta;
    }
}
