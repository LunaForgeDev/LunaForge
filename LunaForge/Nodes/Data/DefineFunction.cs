using LunaForge.Backend.Attributes.TreeNodesAttributes;
using LunaForge.Models;
using LunaForge.Models.Documents;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Nodes.General;

[Serializable, NodeIcon("func.png")]
public partial class DefineFunction : TreeNode
{
    public override string NodeName { get; set; } = "Define Function";

    [JsonConstructor]
    public DefineFunction() : base() { }

    public DefineFunction(DocumentFileLFD workspace)
        : this(workspace, "", "", "true")
    { }

    public DefineFunction(DocumentFileLFD workspace, string name, string param, string localized)
        : base(workspace)
    {
        FuncName = name;
        Parameters = param;
        Localized = localized;
    }

    [JsonIgnore, NodeAttribute("", "Name")]
    public string FuncName { get; set; }

    [JsonIgnore, NodeAttribute("", "Parameters")]
    public string Parameters { get; set; }

    [JsonIgnore, NodeAttribute("", "Localized", EditorWindow = "bool")]
    public string Localized { get; set; }

    public override IEnumerable<string> ToLua(int spacing)
    {
        string sp = Indent(spacing);
        yield return sp;
        if (bool.TryParse(Localized, out bool local))
            if (local) yield return "local ";
        yield return $"function {FuncName}({Parameters})\n";
        foreach (string s in base.ToLua(spacing + 1))
            yield return s;
        yield return sp + "end\n";
    }

    public override string ToString()
    {
        string s = "Define";
        if (bool.TryParse(Localized, out bool local))
            if (local) s += " local";
        return s + $" function {FuncName}({Parameters})";
    }

    public override object Clone()
    {
        DefineFunction n = new();
        n.DeepCopyFrom(this);
        return n;
    }
}