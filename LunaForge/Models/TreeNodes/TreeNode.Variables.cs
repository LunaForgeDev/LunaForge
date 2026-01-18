using CommunityToolkit.Mvvm.ComponentModel;
using LunaForge.Models.Documents;
using LunaForge.Services;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunaForge.Models.TreeNodes;

public abstract partial class TreeNode : ObservableObject
{
    private static ILogger Logger = CoreLogger.Create("TreeNode"); // Generic Lua Node

    [JsonIgnore]
    public abstract string NodeName { get; set; }
    [JsonIgnore]
    public TreeNodeMetaData MetaData { get; private set; }
    [JsonIgnore]
    public TreeNode ParentNode { get; set; }
    [JsonIgnore]
    public DocumentFileLFD ParentTree { get; set; }

    [JsonIgnore]
    public string NodeHash { get; set; } = Guid.NewGuid().ToString();

    [JsonIgnore]
    public ObservableCollection<TreeNode> Children { get; private set; } = [];

    [JsonIgnore]
    public bool HasChildren => Children.Count > 0;

    [ObservableProperty, JsonIgnore]
    public bool isSelected;
    
    [ObservableProperty, JsonIgnore]
    public bool isExpanded;
    
    [ObservableProperty, JsonIgnore]
    public bool isBanned;

    partial void OnIsSelectedChanged(bool value)
    {
        if (value && ParentTree != null)
        {
            ParentTree.SelectedNode = this;
        }
    }
}
