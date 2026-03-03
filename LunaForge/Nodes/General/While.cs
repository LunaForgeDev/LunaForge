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

[Serializable, NodeIcon("while.png")]
public class While : TreeNode
{
    public override string NodeName { get; set; } = "While";

    [JsonConstructor]
    public While() : base() { }

    public While(DocumentFileLFD workspace)
        : this(workspace, "While")
    { }

    public While(DocumentFileLFD workspace, string name)
        : base(workspace)
    { }

    [JsonIgnore]
    [NodeAttribute("")]
    public string Condition { get; set; }

    public override string ToString()
    {
        return $"While ({Condition})";
    }

    public override IEnumerable<string> ToLua(int spacing)
    {
        string sp = Indent(spacing);
        yield return sp + $"while {Condition} do\n";
        foreach (var a in base.ToLua(spacing + 1))
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
        While n = new();
        n.DeepCopyFrom(this);
        return n;
    }
}