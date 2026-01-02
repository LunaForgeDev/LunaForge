using System;
using System.Collections.Generic;
using System.Text;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Backend.EditorCommands;

public class InsertAncestorCommand : InsertCommand
{
    public InsertAncestorCommand(TreeNode source, TreeNode toInsert)
        : base(source, toInsert)
    { }

    public override void Execute()
    {
        TreeNode parent = Source.ParentNode;
        parent.InsertChild(ToInsert, parent.Children.IndexOf(Source));
        parent.RemoveChild(Source);
        ToInsert.AddChild(Source);
    }

    public override void Undo()
    {
        TreeNode parent = ToInsert.ParentNode;
        parent.InsertChild(Source, parent.Children.IndexOf(ToInsert));
        parent.RemoveChild(ToInsert);
        ToInsert.RemoveChild(Source);
    }

    public override string ToString()
    {
        return $"Insert node {Source.NodeName} as ancestor";
    }
}
