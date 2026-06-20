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
[NodeIcon("SinusoidalInterpolationVariable.png")]
[LeafNode]
public class SinusoidalInterpolationVariable : VariableTransformation
{
    public override string NodeName { get; set; } = "Sinusoidal Interpolation Variable";

    [JsonConstructor]
    public SinusoidalInterpolationVariable() : base() { }

    public SinusoidalInterpolationVariable(DocumentFileLFD workspace)
        : this(workspace, "", "0", "0", "true", "SINE_ACC_DEC")
    { }

    public SinusoidalInterpolationVariable(DocumentFileLFD workspace, string name, string from, string to, string precisely, string mode)
        : base(workspace)
    {
        Name = name;
        From = from;
        To = to;
        Precisely = precisely;
        Mode = mode;
    }

    [JsonIgnore]
    [NodeAttribute("")]
    public string Name { get; set; }

    [JsonIgnore]
    [NodeAttribute("0")]
    public string From { get; set; }

    [JsonIgnore]
    [NodeAttribute("0")]
    public string To { get; set; }

    [JsonIgnore]
    [NodeAttribute("true")]
    public string Precisely { get; set; }

    [JsonIgnore]
    [NodeAttribute("MOVE_NORMAL")]
    public string Mode { get; set; }

    public override string ToString()
    {
        string offchar = Precisely == "true" ? "(Precisely)" : "(Expect next value IS)";
        string interp = "";
        switch (Mode)
        {
            case "SINE_ACCEL":
                interp = "accelerate";
                break;
            case "SINE_DECEL":
                interp = "decelerate";
                break;
            default:
                interp = "half period";
                break;
        }
        return $"{Name} : {From} => {To} {offchar}, following sine interpolation, {interp}";
    }

    public override object Clone()
    {
        SinusoidalInterpolationVariable n = new(ParentTree);
        n.DeepCopyFrom(this);
        return n;
    }

    public override Tuple<string, string> GetInformation(string sp, string times)
    {
        string offchar = Precisely == "true" ? " - 1" : "";
        string beg = $"_beg_{Name}";
        string end = $"_end_{Name}";
        string begPhase, phaseDiff, ampChar, center;
        switch (Mode)
        {
            case "SINE_ACCEL":
                begPhase = "-90";
                phaseDiff = "90";
                ampChar = "";
                center = end;
                break;
            case "SINE_DECEL":
                begPhase = "0";
                phaseDiff = "90";
                ampChar = "";
                center = beg;
                break;
            default:
                begPhase = "-90";
                phaseDiff = "180";
                ampChar = " / 2";
                center = $"({end} + {beg}) / 2";
                break;
        }
        string begin = $"{sp}local _beg_{Name} = {From}\n"
                + $"{sp}local {Name} = {beg}\n"
                + $"{sp}local _w_{Name} = {begPhase}\n"
                + $"{sp}local _end_{Name} = {To}\n"
                + $"{sp}local _d_w_{Name} = {phaseDiff} / ({times}{offchar})\n";
        string repeat = $"{sp}{sp}_w_{Name} = _w_{Name} + _d_w_{Name}\n"
            + $"{sp}{sp}{Name} = ({end} - {beg}){ampChar} * sin(_w_{Name}) + ({center})\n";
        return new(begin, repeat);
    }
}
