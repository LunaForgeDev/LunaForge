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

[Serializable, NodeIcon("codesegment.png")]
public partial class CodeSegment : TreeNode
{
    public override string NodeName { get; set; } = "CodeSegment";

    [JsonConstructor]
    public CodeSegment() : base() { }

    public CodeSegment(DocumentFileLFD workspace)
        : this(workspace, "do", "end")
    { }

    public CodeSegment(DocumentFileLFD workspace, string head, string tail)
        : base(workspace)
    {
        Head = head;
        Tail = tail;
    }

    [JsonIgnore]
    [NodeAttribute("do")]
    public string Head { get; set; }

    [JsonIgnore]
    [NodeAttribute("end")]
    public string Tail { get; set; }

    public override IEnumerable<string> ToLua(int spacing)
    {
        Regex r = CodeRegex();
        string sp = Indent(spacing);
        string nsp = "\n" + sp;
        yield return sp + r.Replace(Head, nsp) + "\n";
        foreach (var a in base.ToLua(spacing + 1))
            yield return a;
        yield return sp + r.Replace(Tail, nsp) + "\n";
    }

    public override IEnumerable<Tuple<int, TreeNode>> GetLines()
    {
        string s = Head;
        int i = 1;
        foreach (char c in s)
            if (c == '\n')
                i++;
        yield return new Tuple<int, TreeNode>(i, this);
        foreach (Tuple<int, TreeNode> t in GetChildLines())
            yield return t;
        s = Tail;
        i = 1;
        foreach (char c in s)
            if (c == '\n')
                i++;
        yield return new Tuple<int, TreeNode>(i, this);
    }

    public override string ToString()
    {
        return $"{Head}\n...";
    }

    public override object Clone()
    {
        CodeSegment n = new();
        n.DeepCopyFrom(this);
        return n;
    }

    [GeneratedRegex("\\n\\b")]
    private static partial Regex CodeRegex();
}