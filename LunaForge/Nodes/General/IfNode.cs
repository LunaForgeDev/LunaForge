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

[Serializable, NodeIcon("if.png")]
public class IfNode : TreeNode
{
    public override string NodeName { get; set; } = "IfNode";

    [JsonConstructor]
    public IfNode() : base() { }

    public IfNode(DocumentFileLFD workspace)
        : this(workspace, "")
    { }

    public IfNode(DocumentFileLFD workspace, string name)
        : base(workspace)
    { }

    [JsonIgnore]
    [NodeAttribute("")]
    public string Condition { get; set; }

    public override string ToString()
    {
        return $"If ({Condition})";
    }

    public override IEnumerable<string> ToLua(int spacing)
    {
        string sp = Indent(spacing);
        var i = GetRealChildren().OrderBy((s) => (s as IIfChild)?.Priority ?? 0);
        List<TreeNode> t = [.. i];

        yield return sp + "if " + Condition;
        foreach (var a in ToLua(spacing, i))
            yield return a;
        yield return sp + "end\n";
    }

    public override IEnumerable<Tuple<int, TreeNode>> GetLines()
    {
        yield return new Tuple<int, TreeNode>(1, this);
        foreach (Tuple<int, TreeNode> t in GetChildLines())
            yield return t;
        yield return new Tuple<int, TreeNode>(1, this);
    }

    public override object Clone()
    {
        IfNode n = new();
        n.DeepCopyFrom(this);
        return n;
    }
}