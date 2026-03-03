using LunaForge.Backend.Attributes.TreeNodesAttributes;
using LunaForge.Models;
using LunaForge.Models.Documents;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Xml.Serialization;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Nodes.General;

[Serializable, NodeIcon("elseif.png")]
[RequireParent(typeof(IfNode))]
public class IfElseIf : TreeNode, IIfChild
{
    public override string NodeName { get; set; } = "Else If";

    [JsonConstructor]
    public IfElseIf() : base() { }

    public IfElseIf(DocumentFileLFD workspace)
        : this(workspace, "")
    { }

    public IfElseIf(DocumentFileLFD workspace, string name)
        : base(workspace)
    { }

    [JsonIgnore]
    [NodeAttribute("")]
    public string Condition { get; set; }

    public override string ToString()
    {
        return $"Else if {Condition}";
    }

    public override IEnumerable<string> ToLua(int spacing)
    {
        string sp = Indent(spacing);
        yield return sp + $"elseif {Condition} then\n";
        foreach (var a in base.ToLua(spacing + 1))
            yield return a;
    }

    public override IEnumerable<Tuple<int, TreeNode>> GetLines()
    {
        yield return new Tuple<int, TreeNode>(1, this);
        foreach (Tuple<int, TreeNode> t in GetChildLines())
            yield return t;
    }

    public override object Clone()
    {
        IfElseIf n = new();
        n.DeepCopyFrom(this);
        return n;
    }

    [JsonIgnore]
    public int Priority => 0;
}