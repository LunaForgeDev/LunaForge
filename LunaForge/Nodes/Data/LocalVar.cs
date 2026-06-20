using LunaForge.Backend.Attributes.TreeNodesAttributes;
using LunaForge.Models;
using LunaForge.Models.Documents;
using LunaForge.Models.TreeNodes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Nodes.Data;

[Serializable, NodeIcon("variable.png")]
[LeafNode]
public class LocalVar : TreeNode
{
    public override string NodeName { get; set; } = "Local Var";

    [JsonConstructor]
    public LocalVar() : base() { }

    public LocalVar(DocumentFileLFD workspace)
        : base(workspace)
    {
    }

    [JsonIgnore]
    [NodeAttribute("1", "Number of Vars", IsDependency = true)]
    public string NumOfVar { get; set; }

    protected override void OnInitializeDefaultDynamicAttributes()
    {
        EnsureVariableAttributes(1);
    }

    public override string ToString()
    {
        string bres = "Local:";
        foreach (var g in IterateDynamicGroups(2))
            if (!string.IsNullOrEmpty(g[0].Value))
                bres += $"\n{g[0].Value}{(string.IsNullOrEmpty(g[1].Value) ? "" : " = " + g[1].Value)}";
        return bres;
    }

    public override IEnumerable<string> ToLua(int spacing)
    {
        string sp = Indent(spacing);
        foreach (var g in IterateDynamicGroups(2))
            if (!string.IsNullOrEmpty(g[0].Value))
                yield return string.IsNullOrEmpty(g[1].Value)
                    ? sp + $"local {g[0].Value}\n"
                    : sp + $"local {g[0].Value} = ({g[1].Value})\n";
    }

    public override object Clone()
    {
        LocalVar n = new();
        n.DeepCopyFrom(this);
        return n;
    }

    private void EnsureVariableAttributes(int count)
    {
        EnsureDynamicGroups(count, 2, i =>
        [
            ($"Var {i} name", ""),
            ($"Var {i} init value", "0")
        ]);
    }

    public override void OnDependencyAttributeChangedImpl(NodeAttribute attr, NodeAttributeChangedEventArgs args)
    {
        if (attr.Name == "Number of Vars")
            EnsureVariableAttributes(ParseAndClampCount(args.NewValue, App.MaxVariablesCount));
    }
}