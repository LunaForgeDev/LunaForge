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

[Serializable, NodeIcon("folder.png")]
[CannotDelete, CannotBan]
[IsFolder]
public class RootFolder : TreeNode
{
    public override string NodeName { get; set; } = "Root";

    [JsonConstructor]
    public RootFolder() : base() { }

    public RootFolder(DocumentFileLFD workspace)
        : base(workspace)
    { }

    public override string ToString()
    {
        return "Root";
    }

    public override object Clone()
    {
        RootFolder n = new();
        n.DeepCopyFrom(this);
        return n;
    }

    public override IEnumerable<Tuple<int, TreeNode>> GetLines()
    {
        yield return new(1, this);
    }
}