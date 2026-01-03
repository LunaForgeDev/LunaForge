using LunaForge.Backend.Attributes.TreeNodesAttributes;
using LunaForge.Models.Documents;
using LunaForge.Models.TreeNodes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.THlib.Nodes.Task;

[NodeIcon("task.png")]
[RequireAncestor(typeof(CodeAlikeTypes))]
public class TaskNode : TreeNode
{
    public override string NodeName { get; set; } = "Create task";

    [JsonConstructor]
    public TaskNode() : base()
    { }

    public TaskNode(DocumentFileLFD workspace)
        : base(workspace)
    { }

    public override IEnumerable<string> ToLua(int spacing)
    {
        string sp = Indent(spacing);
        yield return sp + "lasttask = task.New(self, function()\n";
        foreach (var a in base.ToLua(spacing + 1))
            yield return a;
        yield return sp + "end)\n";
    }

    public override IEnumerable<Tuple<int, TreeNode>> GetLines()
    {
        yield return new Tuple<int, TreeNode>(1, this);
        foreach (Tuple<int, TreeNode> t in GetChildLines())
            yield return t;
        yield return new Tuple<int, TreeNode>(1, this);
    }

    public override string ToString()
    {
        return "Create task";
    }

    public override object Clone()
    {
        TaskNode n = new(ParentTree);
        n.DeepCopyFrom(this);
        return n;
    }
}
