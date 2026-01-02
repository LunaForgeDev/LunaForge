using System;
using System.Collections.Generic;
using System.Text;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Backend.EditorCommands;

public abstract class InsertCommand(TreeNode source, TreeNode toInsert) : Command
{
    protected TreeNode Source = source;
    protected TreeNode ToInsert = toInsert;
}