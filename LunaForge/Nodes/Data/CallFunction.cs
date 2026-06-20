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

[Serializable, NodeIcon("callfunc.png")]
[LeafNode]
public partial class CallFunction : TreeNode
{
    public override string NodeName { get; set; } = "Call Function";

    [JsonConstructor]
    public CallFunction() : base() { }

    public CallFunction(DocumentFileLFD workspace)
        : this(workspace, "", "")
    { }

    public CallFunction(DocumentFileLFD workspace, string name, string param)
        : base(workspace)
    {
        FuncName = name;
        Parameters = param;
    }

    [JsonIgnore, NodeAttribute("", "Name")]
    public string FuncName { get; set; }

    [JsonIgnore, NodeAttribute("", "Parameters")]
    public string Parameters { get; set; }

    public override IEnumerable<string> ToLua(int spacing)
    {
        string sp = Indent(spacing);
        yield return sp + $"{FuncName}({Parameters})\n";
    }

    public override string ToString()
    {
        return $"Call function {FuncName} with parameters: ({Parameters})";
    }

    public override object Clone()
    {
        CallFunction n = new();
        n.DeepCopyFrom(this);
        return n;
    }
}