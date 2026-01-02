using System;
using System.Collections.Generic;
using System.Text;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Backend.EditorCommands;

public class DeleteNodeCommand : Command
{
    private readonly int index;
    private TreeNode toOperate;

    public DeleteNodeCommand(TreeNode node)
    {
        toOperate = node;
        index = toOperate.ParentNode.Children.IndexOf(toOperate);
    }

    public override void Execute()
    {
        toOperate.RaiseRemove(toOperate.ParentNode ?? toOperate);
        toOperate.ParentNode?.RemoveChild(toOperate);
    }

    public override void Undo()
    {
        toOperate.RaiseCreate(toOperate.ParentNode ?? toOperate);
        toOperate.ParentNode?.InsertChild(toOperate, index);
        toOperate.IsSelected = true;
    }

    public override string ToString() => $"Delete node {toOperate.NodeName}";
}
