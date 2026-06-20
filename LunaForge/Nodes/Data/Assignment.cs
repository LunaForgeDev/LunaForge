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
public class Assignment : TreeNode
{
    public override string NodeName { get; set; } = "Assignment";

    [JsonConstructor]
    public Assignment() : base() { }

    public Assignment(DocumentFileLFD workspace)
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
        string bres = "Assign:";
        int nVars = ParseAndClampCount(NumOfVar, App.MaxVariablesCount);
        if (nVars == 0) return bres;

        foreach (var g in IterateDynamicGroups(2))
            if (!string.IsNullOrEmpty(g[0].Value))
                bres += $"\n{g[0].Value} = {g[1].Value}";

        return bres;
    }

    public override IEnumerable<string> ToLua(int spacing)
    {
        string sp = Indent(spacing);
        int nVars = ParseAndClampCount(NumOfVar, App.MaxVariablesCount);
        if (nVars == 0) { yield return sp + "\n"; yield break; }

        var vars = new List<string>();
        var vals = new List<string>();

        foreach (var g in IterateDynamicGroups(2))
            if (!string.IsNullOrEmpty(g[0].Value))
            {
                vars.Add(g[0].Value);
                vals.Add($"({g[1].Value})");
            }

        if (vars.Count > 0)
            yield return sp + string.Join(", ", vars) + " = " + string.Join(", ", vals) + "\n";
        else
            yield return sp + "\n";
    }

    public override object Clone()
    {
        Assignment n = new();
        n.DeepCopyFrom(this);
        return n;
    }

    private void EnsureVariableAttributes(int count)
    {
        EnsureDynamicGroups(count, 2, i =>
        [
            ($"Var {i} name", ""),
            ($"Var {i} value", "0")
        ]);
    }

    public override void OnDependencyAttributeChangedImpl(NodeAttribute attr, NodeAttributeChangedEventArgs args)
    {
        if (attr.Name == "Number of Vars")
            EnsureVariableAttributes(ParseAndClampCount(args.NewValue, App.MaxVariablesCount));
    }
}