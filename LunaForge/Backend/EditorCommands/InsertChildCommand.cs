using System;
using System.Collections.Generic;
using System.Text;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Backend.EditorCommands;

public class InsertChildCommand : InsertCommand
{
    public InsertChildCommand(TreeNode source, TreeNode toInsert)
        : base(source, toInsert)
    { }

    public override void Execute()
    {
        TreeNode parent = Source.ParentNode;
        ToInsert.RaiseCreate(parent);
        Source.AddChild(ToInsert);
    }

    public override void Undo()
    {
        ToInsert.RaiseRemove(ToInsert.ParentNode);
        Source.RemoveChild(ToInsert);
    }

    public override string ToString()
    {
        return $"Insert node {Source.NodeName} as a child";
    }
}
