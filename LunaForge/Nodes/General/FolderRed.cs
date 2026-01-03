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

[Serializable, NodeIcon("folderred.png")]
[IsFolder]
public class FolderRed : TreeNode
{
    public override string NodeName { get; set; } = "Folder Red";

    [JsonConstructor]
    public FolderRed() : base() { }

    public FolderRed(DocumentFileLFD workspace)
        : this(workspace, "Folder")
    { }

    public FolderRed(DocumentFileLFD workspace, string name)
        : base(workspace)
    { }

    [JsonIgnore]
    [NodeAttribute("Folder")]
    public string Name { get; set; }

    public override string ToString()
    {
        return $"==[ {Name} ]==";
    }

    public override object Clone()
    {
        FolderRed n = new();
        n.DeepCopyFrom(this);
        return n;
    }

    public override IEnumerable<Tuple<int, TreeNode>> GetLines()
    {
        foreach (Tuple<int, TreeNode> t in GetChildLines())
            yield return t;
    }
}