using LunaForge.Backend.Attributes.TreeNodesAttributes;
using LunaForge.Models;
using LunaForge.Models.Documents;
using LunaForge.Models.TreeNodes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Nodes.Advanced;

[Serializable]
[NodeIcon("advancedrepeat.png")]
public class AdvancedRepeat : TreeNode
{
    public override string NodeName { get; set; } = "Advanced Repeat";

    [JsonConstructor]
    public AdvancedRepeat() : base() { }

    public AdvancedRepeat(DocumentFileLFD workspace)
        : this(workspace, "_infinite")
    { }

    public AdvancedRepeat(DocumentFileLFD workspace, string times)
        : base(workspace)
    {
        Times = times;
    }

    [JsonIgnore]
    [NodeAttribute("_infinite")]
    public string Times { get; set; }

    public override string ToString()
    {
        return $"Repeat {Times} times";
    }

    public override object Clone()
    {
        AdvancedRepeat n = new(ParentTree);
        n.DeepCopyFrom(this);
        return n;
    }

    public override IEnumerable<string> ToLua(int spacing)
    {
        string sp = Indent(spacing);
        string sp1 = Indent(spacing + 1);
        VariableCollection vc = GetVariableCollection();
        List<Tuple<string, string>> info = [];
        if (vc != null)
            foreach (VariableTransformation vt in vc.GetVariableTransformations())
                info.Add(vt.GetInformation(sp1, Times)); // normal sp

        yield return sp + "do\n";
        foreach (var t in info)
            yield return t.Item1; // sp1: Without + 1 sp
        yield return $"{sp1}for _ = 1, {Times} do\n";
        foreach (var a in base.ToLua(spacing + 2))
            yield return a;
        foreach (var t in info)
            yield return t.Item2; // sp2: With + 2 sp
        yield return sp1 + "end\n";
        yield return sp + "end\n";
    }

    private VariableCollection GetVariableCollection()
    {
        foreach (TreeNode t in GetRealChildren())
            if (t is VariableCollection collection)
                return collection;
        return null;
    }
}
