using LunaForge.Models.TreeNodes;
using LunaForge.ViewModels;

namespace LunaForge.Backend.EditorCommands;

public class MoveNodeCommand : Command
{
    private readonly TreeNode node;
    private readonly TreeNode originalParent;
    private readonly int originalIndex;
    private readonly TreeNode target;
    private readonly InsertMode insertMode;

    public MoveNodeCommand(TreeNode node, TreeNode target, InsertMode insertMode)
    {
        this.node = node;
        originalParent = node.ParentNode;
        originalIndex = originalParent.Children.IndexOf(node);
        this.target = target;
        this.insertMode = insertMode;
    }

    public override void Execute()
    {
        node.RaiseRemove(originalParent);
        originalParent.Children.Remove(node);

        InsertAt(target, insertMode);
        node.IsSelected = true;
    }

    public override void Undo()
    {
        node.RaiseRemove(node.ParentNode ?? node);
        node.ParentNode?.Children.Remove(node);

        node.RaiseCreate(originalParent);
        originalParent.InsertChild(node, originalIndex);
        node.IsSelected = true;
    }

    private void InsertAt(TreeNode target, InsertMode mode)
    {
        switch (mode)
        {
            case InsertMode.Before:
                {
                    TreeNode parent = target.ParentNode;
                    node.RaiseCreate(parent);
                    parent.InsertChild(node, parent.Children.IndexOf(target));
                    break;
                }
            case InsertMode.After:
                {
                    TreeNode parent = target.ParentNode;
                    node.RaiseCreate(parent);
                    parent.InsertChild(node, parent.Children.IndexOf(target) + 1);
                    break;
                }
            case InsertMode.Child:
                {
                    node.RaiseCreate(target);
                    target.AddChild(node);
                    break;
                }
        }
    }

    public override string ToString() => $"Move node {node.NodeName}";
}
