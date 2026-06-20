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
[NodeIcon("LinearVariable.png")]
[LeafNode]
public class LinearVariable : VariableTransformation
{
    public override string NodeName { get; set; } = "Linear Variable";

    [JsonConstructor]
    public LinearVariable() : base() { }

    public LinearVariable(DocumentFileLFD workspace)
        : this(workspace, "", "0", "0", "true", "MOVE_NORMAL")
    { }

    public LinearVariable(DocumentFileLFD workspace, string name, string from, string to, string precisely, string mode)
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
            case "MOVE_ACCEL":
                interp = ", accelerate";
                break;
            case "MOVE_DECEL":
                interp = ", decelerate";
                break;
            case "MOVE_ACC_DEC":
                interp = ", accelerate then decelerate";
                break;
            default:
                break;
        }
        return $"{Name} : {From} => {To} {offchar}{interp}";
    }

    public override object Clone()
    {
        LinearVariable n = new(ParentTree);
        n.DeepCopyFrom(this);
        return n;
    }

    public override Tuple<string, string> GetInformation(string sp, string times)
    {
        string offchar = Precisely == "true" ? " - 1" : "";
        string beg = $"_beg_{Name}";
        string end = $"_end_{Name}";
        string begin, repeat;
        switch (Mode)
        {
            case "MOVE_ACCEL":
                begin = $"{sp}local _beg_{Name} = {From}\n"
                    + $"{sp}local {Name} = {beg}\n"
                    + $"{sp}local _end_{Name} = {To}\n"
                    + $"{sp}local _w_{Name} = 0\n"
                    + $"{sp}local _d_w_{Name} = 1 / ({times}{offchar})\n";
                repeat = $"{sp}{sp}_w_{Name} = _w_{Name} + _d_w_{Name}\n"
                    + $"{sp}{sp}{Name} = ({end} - {beg}) * _w_{Name} ^ 2 + {beg}\n";
                break;
            case "MOVE_DECEL":
                begin = $"{sp}local _beg_{Name} = {From}\n"
                    + $"{sp}local {Name} = {beg}\n"
                    + $"{sp}local _end_{Name} = {To}\n"
                    + $"{sp}local _w_{Name} = 0\n"
                    + $"{sp}local _d_w_{Name} = 1 / ({times}{offchar})\n";
                repeat = $"{sp}{sp}_w_{Name} = _w_{Name} + _d_w_{Name}\n"
                    + $"{sp}{sp}{Name} = ({beg} - {end}) * (_w_{Name} - 1) ^ 2 + {end}\n";
                break;
            case "MOVE_ACC_DEC":
                begin = $"{sp}local _beg_{Name} = {From}\n"
                    + $"{sp}local {Name} = {beg}\n"
                    + $"{sp}local _end_{Name} = {To}\n"
                    + $"{sp}local _w_{Name} = 0\n"
                    + $"{sp}local _d_w_{Name} = 1 / ({times}{offchar})\n";
                repeat = $"{sp}{sp}_w_{Name} = _w_{Name} + _d_w_{Name}\n"
                    + $"{sp}{sp}if _w_{Name} < 0.5 then\n"
                    + $"{sp}{sp}{sp}{Name} = 2 * ({end} - {beg}) * _w_{Name} ^ 2 + {beg}\n"
                    + $"{sp}{sp}else\n"
                    + $"{sp}{sp}{sp}{Name} = ({end} - {beg}) * (-2 * _w_{Name} ^ 2 + 4 * _w_{Name} - 1) + {beg}\n"
                    + $"{sp}{sp}end\n";
                break;
            default:
                begin = $"{sp}local _beg_{Name} = {From}\n"
                    + $"{sp}local {Name} = {beg}\n"
                    + $"{sp}local _end_{Name} = {To}\n"
                    + $"{sp}local _d_{Name} = ({end} - {beg}) / ({times}{offchar})\n";
                repeat = $"{sp}{sp}{Name} = {Name} + _d_{Name}\n";
                break;
        }
        return new(begin, repeat);
    }
}
