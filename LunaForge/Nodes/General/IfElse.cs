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

[Serializable, NodeIcon("else.png")]
[RequireParent(typeof(IfNode)), Unique]
public class IfElse : TreeNode, IIfChild
{
    public override string NodeName { get; set; } = "Else";

    [JsonConstructor]
    public IfElse() : base() { }

    public IfElse(DocumentFileLFD workspace)
        : base(workspace)
    { }

    public override string ToString()
    {
        return $"Else";
    }

    public override IEnumerable<string> ToLua(int spacing)
    {
        string sp = Indent(spacing);
        yield return sp + "else\n";
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
        IfElse n = new();
        n.DeepCopyFrom(this);
        return n;
    }

    [JsonIgnore]
    public int Priority => 1;
}