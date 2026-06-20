using LunaForge.Backend.Attributes.TreeNodesAttributes;
using LunaForge.Models.Documents;
using LunaForge.Models.TreeNodes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Nodes.Advanced;

[Serializable, NodeIcon("callbackfunc.png")]
[RequireParent(typeof(AdvancedRepeat))]
[Unique]
[CannotDelete, CannotBan]
public class VariableCollection : TreeNode
{
    public override string NodeName { get; set; } = "VariableCollection";

    [JsonConstructor]
    public VariableCollection()
        : base() { }

    public VariableCollection(DocumentFileLFD doc) : base(doc) { }

    public override object Clone()
    {
        VariableCollection n = new(ParentTree);
        n.DeepCopyFrom(this);
        return n;
    }

    public override string ToString()
    {
        return "Variables";
    }

    public IEnumerable<VariableTransformation> GetVariableTransformations()
    {
        foreach (TreeNode t in GetRealChildren())
            if (t is VariableTransformation && !t.IsBanned)
                yield return (VariableTransformation)t;
    }
}
