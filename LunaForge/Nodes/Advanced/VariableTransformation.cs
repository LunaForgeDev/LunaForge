using LunaForge.Models.Documents;
using LunaForge.Models.TreeNodes;
using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Nodes.Advanced;

public abstract class VariableTransformation : TreeNode
{
    public VariableTransformation() : base() { }
    public VariableTransformation(DocumentFileLFD workSpaceData) : base(workSpaceData) { }

    public abstract Tuple<string, string> GetInformation(string sp, string times);
}
