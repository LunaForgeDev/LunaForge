using CommunityToolkit.Mvvm.DependencyInjection;
using LibGit2Sharp;
using LunaForge.Backend.Attributes.TreeNodesAttributes;
using LunaForge.Models;
using LunaForge.Models.Documents;
using LunaForge.Models.TreeNodes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace LunaForge.Nodes.Advanced;

[Serializable]
[RequireParent(typeof(VariableCollection))]
[NodeIcon("ReboundingVariable.png")]
[LeafNode]
public class ReboundingVariable : VariableTransformation
{
    public override string NodeName { get; set; } = "Rebounding Variable";

    [JsonConstructor]
    public ReboundingVariable() : base() { }

    public ReboundingVariable(DocumentFileLFD workspace)
        : this(workspace, "", "-1", "1")
    { }

    public ReboundingVariable(DocumentFileLFD workspace, string name, string init, string inc)
        : base(workspace)
    {
        Name = name;
        Init = init;
        Another = inc;
    }

    [JsonIgnore]
    [NodeAttribute("")]
    public string Name { get; set; }

    [JsonIgnore]
    [NodeAttribute("-1", "Initial value")]
    public string Init { get; set; }

    [JsonIgnore]
    [NodeAttribute("1", "Another value")]
    public string Another { get; set; }

    public override string ToString()
    {
        return $"{Name} : {Init}(Initial) <=> {Another}";
    }

    public override object Clone()
    {
        ReboundingVariable n = new(ParentTree);
        n.DeepCopyFrom(this);
        return n;
    }

    public override Tuple<string, string> GetInformation(string sp, string times)
    {
        string begin = $"{sp}local {Name} = {Init}\n"
                + $"{sp}local _n_{Name} = ({Init}) + ({Another})\n";
        string repeat = $"{sp}{sp}{Name} = -({Name}) + _n_{Name}\n";
        return new Tuple<string, string>(begin, repeat);
    }
}
