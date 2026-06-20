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

[Serializable, NodeIcon("returnnode.png")]
[RequireAncestor(typeof(CodeAlikeTypes))]
[LeafNode]
public partial class ReturnNode : TreeNode
{
    public override string NodeName { get; set; } = "Return Node";

    [JsonConstructor]
    public ReturnNode() : base() { }

    public ReturnNode(DocumentFileLFD workspace)
        : this(workspace, "")
    { }

    public ReturnNode(DocumentFileLFD workspace, string code)
        : base(workspace)
    {
        Code = code;
    }

    [JsonIgnore, NodeAttribute("")]
    public string Code { get; set; }

    public override IEnumerable<string> ToLua(int spacing)
    {
        string sp = Indent(spacing);
        yield return sp + $"return{(!string.IsNullOrEmpty(Code) ? $" {Code}" : "")}\n";
    }

    public override string ToString()
    {
        return $"Return{(!string.IsNullOrEmpty(Code) ? $" {Code}" : "")}";
    }

    public override object Clone()
    {
        ReturnNode n = new();
        n.DeepCopyFrom(this);
        return n;
    }
}