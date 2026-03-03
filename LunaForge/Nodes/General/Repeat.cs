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
        string bres = "";
        bool first = true;
        
        if (!int.TryParse(NumOfVar, out int nVars))
            nVars = 0;
        nVars = Math.Clamp(nVars, 0, App.MaxVariablesCount);

        for (int i = 2; i <= nVars * 3 + 1; i += 3)
        {
            if (i < Attributes.Count && !string.IsNullOrEmpty(Attributes[i].Value))
            {
                if (!first)
                    bres += ", ";

                bres += $"({Attributes[i].Value} = {Attributes[i + 1].Value}, increment {Attributes[i + 2].Value})";
                first = false;
            }
        }

        if (first)
            return $"Repeat {RepeatTimes} times";
        else
            return $"Repeat {RepeatTimes} times, {bres}";
    }

    public override IEnumerable<string> ToLua(int spacing)
    {
        string sp = Indent(spacing);

        if (!int.TryParse(NumOfVar, out int nVars))
            nVars = 0;

        if (nVars == 0)
        {
            yield return sp + $"for _ = 1, {RepeatTimes} do\n";
            foreach (var a in base.ToLua(spacing + 1))
                yield return a;
            yield return sp + $"end\n";
        }
        else
        {
            List<string> varNames = [];
            List<string> initValues = [];
            List<string> increments = [];

            for (int i = 0; i < nVars; i++)
            {
                int baseIdx = 2 + (i * 3);
                if (baseIdx + 2 < Attributes.Count)
                {
                    varNames.Add(Attributes[baseIdx].Value);
                    initValues.Add(Attributes[baseIdx + 1].Value);
                    increments.Add(Attributes[baseIdx + 2].Value);
                }
            }

            string varDecl = string.Join(", ", varNames.Select((v, i) => $"{v}, _d_{v}"));
            string initDecl = string.Join(", ", varNames.Select((v, i) => $"({initValues[i]}), ({increments[i]})"));
            string incrDecl = string.Join(" ", varNames.Select((v, i) => $"{v} = {v} + _d_{v}"));

            yield return $"{sp}do local {varDecl} = {initDecl} for _ = 1, {RepeatTimes} do\n";
            foreach (var a in base.ToLua(spacing + 1))
                yield return a;
            yield return $"{sp}{incrDecl} end end\n";
        }
    }

    public override IEnumerable<Tuple<int, TreeNode>> GetLines()
    {
        foreach (Tuple<int, TreeNode> t in GetChildLines())
            yield return t;
    }

    public override object Clone()
    {
        Repeat n = new();
        n.DeepCopyFrom(this);
        return n;
    }

    private void EnsureVariableAttributes(int count)
    {
        int currentVarCount = DynamicAttributeCount / 3;

        if (currentVarCount < count)
        {
            for (int i = currentVarCount + 1; i <= count; i++)
            {
                AddDynamicAttribute($"Var {i} name", "");
                AddDynamicAttribute($"Var {i} init value", "0");
                AddDynamicAttribute($"Var {i} increment", "1");
            }
        }
        else if (currentVarCount > count)
        {
            int baseIndex = 2 + (count * 3);
            RemoveDynamicAttributesFromIndex(baseIndex);
        }

        OnPropertyChanged(nameof(ScreenString));
    }

    public override void OnDependencyAttributeChangedImpl(NodeAttribute attr, NodeAttributeChangedEventArgs args)
    {
        if (attr.Name == "Number of Vars")
        {
            if (!int.TryParse(args.NewValue, out int newCount))
                newCount = 0;

            newCount = Math.Clamp(newCount, 0, App.MaxVariablesCount);
            EnsureVariableAttributes(newCount);
        }
    }
}