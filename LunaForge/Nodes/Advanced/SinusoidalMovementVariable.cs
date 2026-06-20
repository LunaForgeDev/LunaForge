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
[NodeIcon("SinusoidalMovementVariable.png")]
[LeafNode]
public class SinusoidalMovementVariable : VariableTransformation
{
    public override string NodeName { get; set; } = "Sinusoidal Movement Variable";

    [JsonConstructor]
    public SinusoidalMovementVariable() : base() { }

    public SinusoidalMovementVariable(DocumentFileLFD workspace)
        : this(workspace, "", "-90", "0", "1", "1", "true")
    { }

    public SinusoidalMovementVariable(DocumentFileLFD workspace, string name, string init, string min, string max, string period, string precise)
        : base(workspace)
    {
        Name = name;
        InitPhase = init;
        Min = min;
        Max = max;
        Period = period;
        Precisely = precise;
    }

    [JsonIgnore]
    [NodeAttribute("")]
    public string Name { get; set; }

    [JsonIgnore]
    [NodeAttribute("-90", "Initial phase")]
    public string InitPhase { get; set; }

    [JsonIgnore]
    [NodeAttribute("0")]
    public string Min { get; set; }

    [JsonIgnore]
    [NodeAttribute("1")]
    public string Max { get; set; }

    [JsonIgnore]
    [NodeAttribute("1", "Num of periods")]
    public string Period { get; set; }

    [JsonIgnore]
    [NodeAttribute("true")]
    public string Precisely { get; set; }

    public override string ToString()
    {
        string offchar = Precisely == "true" ? "(Precisely)" : "(Expect next value IS)";
        return $"{Name} : sinusoidal movement between {Min} <=> {Max}"
            + $" with initial phase {InitPhase}, for {Period} period(s) {offchar}";
    }

    public override object Clone()
    {
        SinusoidalMovementVariable n = new(ParentTree);
        n.DeepCopyFrom(this);
        return n;
    }

    public override Tuple<string, string> GetInformation(string sp, string times)
    {
        string offchar = Precisely == "true" ? " - 1" : "";
        string sineTokenHead = $"_h_{Name} * sin";
        string sineTokenTail = $"+ _t_{Name}";
        string begin = $"{sp}local _h_{Name} = ({Max} - ({Min})) / 2\n"
            + $"{sp}local _t_{Name} = ({Max} + ({Min})) / 2\n"
            + $"{sp}local {Name} = {sineTokenHead}({InitPhase}) {sineTokenTail}\n"
            + $"{sp}local _w_{Name} = {InitPhase}\n"
            + $"{sp}local _d_w_{Name} = {Period} * 360 / ({times}{offchar})\n";
        string repeat = $"{sp}{sp}_w_{Name} = _w_{Name} + _d_w_{Name}\n"
            + $"{sp}{sp}{Name} = {sineTokenHead}(_w_{Name}) {sineTokenTail}\n";
        return new Tuple<string, string>(begin, repeat);
    }
}
