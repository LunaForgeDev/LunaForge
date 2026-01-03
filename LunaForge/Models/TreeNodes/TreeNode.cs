using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaForge.Backend.EditorCommands;
using LunaForge.Helpers;
using LunaForge.Models.Documents;
using LunaForge.Services;
using LunaForge.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LunaForge.Models.TreeNodes;

public abstract partial class TreeNode : ObservableObject, ICloneable
{
    protected TreeNode()
    {
        IsExpanded = true;
        MetaData = TreeNodeMetaData.Process(this);
        
        InitializeAttributes();
    }

    public TreeNode(DocumentFileLFD workspace)
        : this()
    {
        ParentTree = workspace;
    }

    public string ScreenString => ToString();

    public override string ToString()
    {
        return NodeName;
    }

    public abstract object Clone();

    public void DeepCopyFrom(TreeNode source)
    {
        // Copy attributes
        Attributes.Clear();
        foreach (var sourceAttr in source.Attributes)
        {
            var attr = new NodeAttribute(sourceAttr.Name, sourceAttr.DefaultValue, this)
            {
                Value = sourceAttr.Value,
                EditorWindow = sourceAttr.EditorWindow
            };
            Attributes.Add(attr);
        }
        
        SyncAttributesToProperties();

        // Copy children
        var children = from TreeNode t in source.Children select (TreeNode)t.Clone();
        Children.Clear();
        foreach (TreeNode treeNode in children)
        {
            treeNode.ParentNode = this;
            Children.Add(treeNode);
        }

        ParentNode = source.ParentNode;
        IsExpanded = source.IsExpanded;
        IsBanned = source.IsBanned;
    }

    public void SerializeFile(StreamWriter fs, int level)
    {
        fs.WriteLine($"{level},{EditorSerializer.SerializeTreeNode(this)}");
        foreach (TreeNode t in Children)
            t.SerializeFile(fs, level + 1);
    }

    #region API

    public virtual IEnumerable<string> ToLua(int spacing)
    {
        return ToLua(spacing, Children);
    }

    protected IEnumerable<string> ToLua(int spacing, IEnumerable<TreeNode> children)
    {
        bool childof = false;
        TreeNode temp;

        bool firstC = false;
        bool folderFound = false;
        bool equalFound = false;

        if (false)
        {
            foreach (TreeNode t in GetRealChildren())
            {
                if (!t.IsBanned)
                {
                    foreach (var a in t.ToLua(spacing))
                        yield return a;
                    if (true)
                    {
                        // TODO: Implement SCDebugger
                    }
                }
            }
        }
        else
        {
            foreach (TreeNode t in children)
            {
                foreach (var a in t.ToLua(spacing))
                    yield return a;
            }
        }
    }

    public abstract IEnumerable<Tuple<int, TreeNode>> GetLines();

    protected IEnumerable<Tuple<int, TreeNode>> GetChildLines()
    {
        foreach (TreeNode t in Children)
            if (!t.IsBanned)
                foreach (Tuple<int, TreeNode> ti in t.GetLines())
                    yield return ti;
    }

    public static string Indent(int spacing)
    {
        int repeatTimes = EditorConfig.Default.Get<int>("CodeIndentSpaces").Value;
        return new string(' ', spacing * repeatTimes);
    }

    #endregion
}
