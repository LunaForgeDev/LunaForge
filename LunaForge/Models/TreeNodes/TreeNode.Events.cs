using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunaForge.Models.TreeNodes;

public abstract partial class TreeNode
{
    public delegate void NodeCreatedEventHandler(TreeNode node);
    public delegate void NodeDeletedEventHandler(TreeNode node);
    public delegate void NodeAttributeChanged(NodeAttribute attr, NodeAttributeChangedEventArgs args);

    public event NodeCreatedEventHandler? OnCreate;
    public event NodeCreatedEventHandler? OnVirtualCreate;
    public event NodeDeletedEventHandler? OnRemove;
    public event NodeDeletedEventHandler? OnVirtualRemove;
    public event NodeAttributeChanged OnNodeAttributeChanged;
    public event NodeAttributeChanged OnDependencyAttributeChanged;

    public void RaiseCreate(TreeNode node)
    {
        if (!IsBanned)
            OnVirtualCreate?.Invoke(node);
        OnCreate?.Invoke(node);
        foreach (TreeNode n in Children)
            node.RaiseCreate(n);
    }

    public void RaiseVirtualCreate(TreeNode node)
    {
        if (!IsBanned)
        {
            OnVirtualCreate?.Invoke(node);
            foreach (TreeNode n in Children)
                node.RaiseVirtualCreate(n);
        }
    }

    public void RaiseRemove(TreeNode node)
    {
        if (!IsBanned)
            OnVirtualRemove?.Invoke(node);
        OnRemove?.Invoke(node);
        foreach (TreeNode n in Children)
            node.RaiseRemove(n);
    }

    public void RaiseVirtualRemove(TreeNode node)
    {
        if (!IsBanned)
        {
            OnVirtualRemove?.Invoke(node);
            foreach (TreeNode n in Children)
                node.RaiseVirtualRemove(n);
        }
    }

    public void RaiseAttributeChanged(NodeAttribute attr, NodeAttributeChangedEventArgs args)
    {
        OnNodeAttributeChanged?.Invoke(attr, args);
        OnAttributeChangedImpl(attr, args);
    }

    public virtual void OnAttributeChangedImpl(NodeAttribute attr, NodeAttributeChangedEventArgs args) { }

    public void RaiseDependencyAttributeChanged(NodeAttribute attr, NodeAttributeChangedEventArgs args)
    {
        OnDependencyAttributeChanged?.Invoke(attr, args);
    }
}
