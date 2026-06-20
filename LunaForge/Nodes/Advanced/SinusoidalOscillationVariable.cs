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
[NodeIcon("SinusoidalOscillationVariable.png")]
[LeafNode]
public class SinusoidalOscillationVariable : VariableTransformation
{
    public override string NodeName { get; set; } = "Sinusoidal Oscillation Variable";

    [JsonConstructor]
    public SinusoidalOscillationVariable() : base() { }

    public SinusoidalOscillationVariable(DocumentFileLFD workspace)
        : this(workspace, "", "0", "-1", "1", "1.5")
    { }

    public SinusoidalOscillationVariable(DocumentFileLFD workspace, string name, string init, string min, string max, string omega)
        : base(workspace)
    {
        Name = name;
        InitPhase = init;
        Min = min;
        Max = max;
        Omega = omega;
    }

    [JsonIgnore]
    [NodeAttribute("")]
    public string Name { get; set; }

    [JsonIgnore]
    [NodeAttribute("0", "Initial phase")]
    public string InitPhase { get; set; }

    [JsonIgnore]
    [NodeAttribute("-1")]
    public string Min { get; set; }

    [JsonIgnore]
    [NodeAttribute("1")]
    public string Max { get; set; }

    [JsonIgnore]
    [NodeAttribute("1.5")]
    public string Omega { get; set; }

    public override string ToString()
    {
        return $"{Name} : sinusoidal oscillation between {Min} <=> {Max}"
                + $" with initial phase {InitPhase} and omega {Omega}";
    }

    public override object Clone()
    {
        SinusoidalOscillationVariable n = new(ParentTree);
        n.DeepCopyFrom(this);
        return n;
    }

    public override Tuple<string, string> GetInformation(string sp, string times)
    {
        string sineTokenHead = $"_h_{Name} * sin";
        string sineTokenTail = $"+ _t_{Name}";
        string begin = $"{sp}local _h_{Name} = ({Max} - ({Min})) / 2\n"
            + $"{sp}local _t_{Name} = ({Max} + ({Min})) / 2\n"
            + $"{sp}local {Name} = {sineTokenHead}({InitPhase}) {sineTokenTail}\n"
            + $"{sp}local _w_{Name} = {InitPhase}\n"
            + $"{sp}local _d_w_{Name} = {Omega}\n";
        string repeat = $"{sp}{sp}_w_{Name} = _w_{Name} + _d_w_{Name}\n"
            + $"{sp}{sp}{Name} = {sineTokenHead}(_w_{Name}) {sineTokenTail}\n";
        return new Tuple<string, string>(begin, repeat);
    }
}
