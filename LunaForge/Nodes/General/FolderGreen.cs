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

[Serializable, NodeIcon("foldergreen.png")]
[IsFolder]
public class FolderGreen : TreeNode
{
    public override string NodeName { get; set; } = "Folder Green";

    [JsonConstructor]
    public FolderGreen() : base() { }

    public FolderGreen(DocumentFileLFD workspace)
        : this(workspace, "Folder")
    { }

    public FolderGreen(DocumentFileLFD workspace, string name)
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
        FolderGreen n = new();
        n.DeepCopyFrom(this);
        return n;
    }

    public override IEnumerable<Tuple<int, TreeNode>> GetLines()
    {
        foreach (Tuple<int, TreeNode> t in GetChildLines())
            yield return t;
    }
}