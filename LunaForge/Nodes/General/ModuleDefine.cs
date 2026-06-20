using LunaForge.Backend.Attributes.TreeNodesAttributes;
using LunaForge.Models.Documents;
using LunaForge.Services;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Nodes.General;

[Serializable, NodeIcon("moduledefine.png")]
[ExportSymbol("Module")]
public partial class ModuleDefine : TreeNode, ICanBeLocal
{
    public override string NodeName { get; set; } = "ModuleDefine";

    [JsonConstructor]
    public ModuleDefine() : base() { }

    public ModuleDefine(DocumentFileLFD workspace, string name, string isLocal)
        : base(workspace)
    {
        Name = name;
        IsLocal = isLocal;
    }

    [JsonIgnore]
    [NodeAttribute("")]
    public string Name { get; set; }
    private TraceHandle? moduleNameIsEmpty;

    [JsonIgnore]
    [NodeAttribute("true", "Is Local")]
    public string IsLocal { get; set; } = "true";

    [JsonIgnore]
    [NodeAttribute("true", "Return self")]
    public string ReturnSelf { get; set; } = "true";

    public override IEnumerable<string> ToLua(int spacing)
    {
        string sp = Indent(spacing);
        string code = sp;
        if (IsLocal == "true")
            code += $"local ";

        code += $"{Name} = {{}}\n";
        yield return code;

        foreach (var a in base.ToLua(spacing))
            yield return a;

        if (ReturnSelf == "true")
            yield return sp + $"return {Name}\n";
    }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Name))
        {
            moduleNameIsEmpty ??= CommitTrace(TraceSeverity.Error, "Module name must not be empty.");
        }
        else
        {
            moduleNameIsEmpty?.Resolve();
            moduleNameIsEmpty = null;
        }

        return $"Define {(IsLocal == "true" ? "local " : "")}module '{Name}'";
    }

    public override IEnumerable<Tuple<int, TreeNode>> GetLines()
    {
        foreach (Tuple<int, TreeNode> t in GetChildLines())
            yield return t;
    }

    public override object Clone()
    {
        ModuleDefine n = new();
        n.DeepCopyFrom(this);
        return n;
    }
}
