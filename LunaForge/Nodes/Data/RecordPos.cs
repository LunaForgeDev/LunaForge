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

[Serializable, NodeIcon("positionVar.png")]
[LeafNode]
public class RecordPos : TreeNode
{
    public override string NodeName { get; set; } = "Record Pos";

    [JsonConstructor]
    public RecordPos() : base() { }

    public RecordPos(DocumentFileLFD workspace)
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
        string bres = "Record position:";
        int nVars = ParseAndClampCount(NumOfVar, App.MaxVariablesCount);
        if (nVars == 0) return bres;

        foreach (var g in IterateDynamicGroups(2))
            if (!string.IsNullOrEmpty(g[0].Value))
                bres += $"\nUse {g[0].Value} to record position of {g[1].Value}";

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
                vals.Add($"{{ x = {g[1].Value}.x, y = {g[1].Value}.y }}");
            }

        if (vars.Count > 0)
            yield return sp + "local " + string.Join(", ", vars) + " = " + string.Join(", ", vals) + "\n";
        else
            yield return sp + "\n";
    }

    public override object Clone()
    {
        RecordPos n = new();
        n.DeepCopyFrom(this);
        return n;
    }

    private void EnsureVariableAttributes(int count)
    {
        EnsureDynamicGroups(count, 2, i =>
        [
            ($"Use var", ""),
            ($"To record", "0")
        ]);
    }

    public override void OnDependencyAttributeChangedImpl(NodeAttribute attr, NodeAttributeChangedEventArgs args)
    {
        if (attr.Name == "Number of Vars")
            EnsureVariableAttributes(ParseAndClampCount(args.NewValue, App.MaxVariablesCount));
    }
}