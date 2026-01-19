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

[Serializable, NodeIcon("then.png")]
[CannotDelete, CannotBan]
[RequireParent(typeof(IfNode)), Unique]
public class IfThen : TreeNode, IIfChild
{
    public override string NodeName { get; set; } = "IfThen";

    [JsonConstructor]
    public IfThen() : base() { }

    public IfThen(DocumentFileLFD workspace)
        : this(workspace, "")
    { }

    public IfThen(DocumentFileLFD workspace, string name)
        : base(workspace)
    { }

    [JsonIgnore]
    [NodeAttribute("")]
    public string Condition { get; set; }

    public override string ToString()
    {
        return $"Then";
    }

    public override IEnumerable<string> ToLua(int spacing)
    {
        yield return " then\n";
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
        IfThen n = new();
        n.DeepCopyFrom(this);
        return n;
    }

    [JsonIgnore]
    public int Priority => -1;
}