using LunaForge.Models.Documents;
using LunaForge.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Models.TreeNodes;

public abstract partial class TreeNode
{
    #region Validation

    public bool ValidateChild(TreeNode toValidate)
    {
        return ValidateChild(this, toValidate);
    }

    public bool ValidateChild(TreeNode parent, TreeNode toValidate)
    {
        if (MetaData.IsFolder)
            return GetRealParent()?.ValidateChild(toValidate, parent) ?? true;
        if (toValidate.MetaData.IsFolder)
        {
            foreach (TreeNode t in toValidate.GetRealChildren())
            {
                if (!ValidateChild(parent, t))
                    return false;
            }
            return true;
        }

        return true;
    }

    #endregion

    public void AddChild(TreeNode child)
    {
        child.ParentNode = this;
        Children.Add(child);
        OnPropertyChanged(nameof(HasChildren));

        NotifyTreeStructureChanged();
    }

    public void InsertChild(TreeNode child, int index)
    {
        child.ParentNode = this;
        Children.Insert(index, child);
        OnPropertyChanged(nameof(HasChildren));

        NotifyTreeStructureChanged();
    }

    public void RemoveChild(TreeNode node)
    {
        ParentTree.SelectedNode = node.GetNearestEdited();
        Children.Remove(node);
        OnPropertyChanged(nameof(HasChildren));

        NotifyTreeStructureChanged();
    }

    private void NotifyTreeStructureChanged()
    {
        if (ParentTree is DocumentFileLFD lfdFile)
        {
            _ = Task.Run(async () =>
            {
                var project = MainWindowModel.Project;
                if (project?.SymbolIndex != null)
                {
                    await project.SymbolIndex.IndexOpenedDocumentAsync(lfdFile);
                }
            });
        }
    }

    public TreeNode GetNearestEdited()
    {
        TreeNode node = ParentNode;
        if (node != null)
        {
            int id = node.Children.IndexOf(this) - 1;
            if (id >= 0)
                node = node.Children[id];
            return node;
        }
        else
        {
            return this;
        }
    }

    public bool CanLogicallyDelete()
    {
        if (MetaData.IsFolder)
        {
            foreach (TreeNode t in GetRealChildren())
                if (t.MetaData.CannotBeDeleted)
                    return false;
            return true;
        }
        else
            return !MetaData.CannotBeDeleted;
    }

    public TreeNode GetRealParent()
    {
        TreeNode p = ParentNode;
        while (p != null && p.MetaData.IsFolder)
            p = p.ParentNode;
        return p;
    }

    public IEnumerable<TreeNode> GetRealChildren()
    {
        foreach (TreeNode n in Children)
        {
            if (n.ParentNode == this)
            {
                if (n.MetaData.IsFolder)
                    foreach (TreeNode t in n.GetRealChildren())
                        yield return t;
                else
                    yield return n;
            }
        }
    }

    public void ClearChildSelection()
    {
        IsSelected = false;
        foreach (TreeNode child in Children)
            child.ClearChildSelection();
    }

    public void FixParentDoc(DocumentFileLFD doc)
    {
        ParentTree = doc;
        foreach (TreeNode child in Children)
            child.FixParentDoc(doc);
    }
}
