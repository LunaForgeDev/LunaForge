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

[Serializable, NodeIcon("code.png")]
[LeafNode]
public partial class Code : TreeNode
{
    public override string NodeName { get; set; } = "Code";

    [JsonConstructor]
    public Code() : base() { }

    public Code(DocumentFileLFD workspace)
        : this(workspace, "")
    { }

    public Code(DocumentFileLFD workspace, string code)
        : base(workspace)
    {
        CodeContent = code;
    }

    [JsonIgnore]
    [NodeAttribute("", "Code")]
    public string CodeContent { get; set; }

    public override IEnumerable<string> ToLua(int spacing)
    {
        Regex r = CodeRegex();
        string sp = Indent(spacing);
        string nsp = "\n" + sp;
        yield return sp + r.Replace(CodeContent, nsp) + "\n";
    }

    public override IEnumerable<Tuple<int, TreeNode>> GetLines()
    {
        string s = CodeContent;
        int i = 1;
        foreach (char c in s)
            if (c == '\n')
                i++;
        yield return new Tuple<int, TreeNode>(i, this);
    }

    public override string ToString()
    {
        return CodeContent;
    }

    public override object Clone()
    {
        Code n = new();
        n.DeepCopyFrom(this);
        return n;
    }

    [GeneratedRegex("\\n")]
    private static partial Regex CodeRegex();
}