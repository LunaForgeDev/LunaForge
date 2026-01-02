using LunaForge.Backend.Attributes.TreeNodesAttributes;
using LunaForge.Models;
using LunaForge.Models.Documents;
using LunaForge.Models.TreeNodes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.THlib.Nodes;

[Serializable]
[LeafNode, NodeIcon("playsound.png")]
public class PlaySE : TreeNode
{
    public override string NodeName { get; set; } = "Play Sound";

    [JsonConstructor]
    public PlaySE() : base() { }

    public PlaySE(DocumentFileLFD workspace)
        : this(workspace, "\"tan00\"", "0.1", "self.x / 256", "false")
    { }

    public PlaySE(DocumentFileLFD workspace, string name, string vol, string pan, string ignoredef)
        : base(workspace)
    {
        Name = name;
        Volume = vol;
        Pan = pan;
        IgnoreDef = ignoredef;
    }

    [JsonIgnore]
    [NodeAttribute("\"tan00\"")]
    public string Name { get; set; }

    [JsonIgnore]
    [NodeAttribute("0.1")]
    public string Volume { get; set; }

    [JsonIgnore]
    [NodeAttribute("self.x / 256")]
    public string Pan { get; set; }

    [JsonIgnore]
    [NodeAttribute("false")]
    public string IgnoreDef { get; set; }

    public override string ToString()
    {
        return $"Play Sound {Name}";
    }

    public override object Clone()
    {
        PlaySE n = new();
        n.DeepCopyFrom(this);
        return n;
    }

    public override IEnumerable<Tuple<int, TreeNode>> GetLines()
    {
        yield return new(1, this);
    }

    public override IEnumerable<string> ToLua(int spacing)
    {
        string sp = Indent(spacing);
        yield return sp + $"PlaySound({Name}, {Volume}, {Pan})";
    }
}
