using System;
using System.Collections.Generic;
using System.Text;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Backend.EditorCommands;

public class InsertBeforeCommand : InsertCommand
{
    public InsertBeforeCommand(TreeNode source, TreeNode toInsert)
        : base(source, toInsert)
    { }

    public override void Execute()
    {
        TreeNode parent = Source.ParentNode;
        ToInsert.RaiseCreate(parent);
        parent?.InsertChild(ToInsert, parent.Children.IndexOf(Source));
    }

    public override void Undo()
    {
        ToInsert.RaiseRemove(ToInsert.ParentNode);
        Source.ParentNode?.RemoveChild(ToInsert);
    }

    public override string ToString()
    {
        return $"Insert node {Source.NodeName} before";
    }
}
