using LunaForge.Backend.Attributes.TreeNodesAttributes;
using LunaForge.Models;
using LunaForge.Models.Documents;
using LunaForge.Models.TreeNodes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Xml.Serialization;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Nodes.General;

[Serializable, NodeIcon("repeat.png")]
public class Repeat : TreeNode
{
    public override string NodeName { get; set; } = "Repeat";

    [JsonConstructor]
    public Repeat() : base() { }

    public Repeat(DocumentFileLFD workspace)
        : base(workspace)
    {
    }

    [JsonIgnore]
    [NodeAttribute("_infinity", "Times")]
    public string RepeatTimes { get; set; }

    [JsonIgnore]
    [NodeAttribute("1", "Number of Vars", IsDependency = true)]
    public string NumOfVar { get; set; }

    protected override void OnInitializeDefaultDynamicAttributes()
    {
        EnsureVariableAttributes(1);
    }

    public override string ToString()
    {
        var parts = new List<string>();
        foreach (var g in IterateDynamicGroups(3))
            if (!string.IsNullOrEmpty(g[0].Value))
                parts.Add($"({g[0].Value} = {g[1].Value}, increment {g[2].Value})");

        return parts.Count == 0
            ? $"Repeat {RepeatTimes} times"
            : $"Repeat {RepeatTimes} times, {string.Join(", ", parts)}";
    }

    public override IEnumerable<string> ToLua(int spacing)
    {
        string sp = Indent(spacing);
        int nVars = ParseAndClampCount(NumOfVar, App.MaxVariablesCount);

        if (nVars == 0)
        {
            yield return sp + $"for _ = 1, {RepeatTimes} do\n";
            foreach (var a in base.ToLua(spacing + 1))
                yield return a;
            yield return sp + $"end\n";
        }
        else
        {
            var varNames  = new List<string>();
            var initValues = new List<string>();
            var increments = new List<string>();

            foreach (var g in IterateDynamicGroups(3))
            {
                varNames.Add(g[0].Value);
                initValues.Add(g[1].Value);
                increments.Add(g[2].Value);
            }

            string varDecl  = string.Join(", ", varNames.Select(v => $"{v}, _d_{v}"));
            string initDecl = string.Join(", ", varNames.Select((v, i) => $"({initValues[i]}), ({increments[i]})"));
            string incrDecl = string.Join(" ", varNames.Select(v => $"{v} = {v} + _d_{v}"));

            yield return $"{sp}do local {varDecl} = {initDecl} for _ = 1, {RepeatTimes} do\n";
            foreach (var a in base.ToLua(spacing + 1))
                yield return a;
            yield return $"{sp}{incrDecl} end end\n";
        }
    }

    public override object Clone()
    {
        Repeat n = new();
        n.DeepCopyFrom(this);
        return n;
    }

    private void EnsureVariableAttributes(int count)
    {
        EnsureDynamicGroups(count, 3, i =>
        [
            ($"Var {i} name", ""),
            ($"Var {i} init value", "0"),
            ($"Var {i} increment", "1")
        ]);
    }

    public override void OnDependencyAttributeChangedImpl(NodeAttribute attr, NodeAttributeChangedEventArgs args)
    {
        if (attr.Name == "Number of Vars")
            EnsureVariableAttributes(ParseAndClampCount(args.NewValue, App.MaxVariablesCount));
    }
}