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
[RequireParent(typeof(VariableCollection))]
[NodeIcon("LinearVariable.png")]
[LeafNode]
public class IncrementVariable : VariableTransformation
{
    public override string NodeName { get; set; } = "Increment Variable";

    [JsonConstructor]
    public IncrementVariable() : base() { }

    public IncrementVariable(DocumentFileLFD workspace)
        : this(workspace, "", "0", "0")
    { }

    public IncrementVariable(DocumentFileLFD workspace, string name, string init, string inc)
        : base(workspace)
    {
        Name = name;
    }

    [JsonIgnore]
    [NodeAttribute("Name")]
    public string Name { get; set; }

    [JsonIgnore]
    [NodeAttribute("0", "Initial value")]
    public string Init { get; set; }

    [JsonIgnore]
    [NodeAttribute("0")]
    public string Increment { get; set; }

    public override string ToString()
    {
        return $"{Name}: {Init}, +({Increment}) each loop";
    }

    public override object Clone()
    {
        IncrementVariable n = new(ParentTree);
        n.DeepCopyFrom(this);
        return n;
    }

    public override Tuple<string, string> GetInformation(string sp, string times)
    {
        string begin = $"{sp}local {Name} = {Init}\n"
            + $"{sp}local _d_{Name} = ({Increment})\n";
        string repeat = $"{sp}{sp}{Name} = {Name} + _d_{Name}\n";
        return new Tuple<string, string>(begin, repeat);
    }
}
