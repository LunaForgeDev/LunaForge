using System;
using System.Collections.Generic;
using System.Text;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Backend.EditorCommands;

public class SwitchBanCommand : Command
{
    private TreeNode target;
    private bool targetValue;
    private bool originalValue;

    public SwitchBanCommand(TreeNode node, bool value)
    {
        target = node;
        targetValue = value;
        originalValue = target.IsBanned;
    }

    public override void Execute()
    {
        target.IsBanned = targetValue;
    }

    public override void Undo()
    {
        target.IsBanned = originalValue;
    }

    public override string ToString() => $"Toggle ban of '{target.NodeName}' to {(targetValue ? "banned" : "unbanned")}";
}
