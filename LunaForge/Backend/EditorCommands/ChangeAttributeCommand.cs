using LunaForge.Models.TreeNodes;
using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Backend.EditorCommands;

public class ChangeAttributeCommand(NodeAttribute attribute, string newValue) : Command
{
    private readonly string oldValue = attribute.Value;

    public override void Execute()
    {
        attribute.Value = newValue;
    }

    public override void Undo()
    {
        attribute.Value = oldValue;
    }

    public override string ToString() => $"Change attribute '{attribute.Name}' from '{oldValue}' to '{newValue}'";
}
