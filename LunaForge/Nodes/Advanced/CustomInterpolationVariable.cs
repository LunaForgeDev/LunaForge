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
[NodeIcon("CustomInterpolationVariable.png")]
[LeafNode]
public class CustomInterpolationVariable : VariableTransformation
{
    public override string NodeName { get; set; } = "Sinusoidal Movement Variable";

    [JsonConstructor]
    public CustomInterpolationVariable() : base() { }

    public CustomInterpolationVariable(DocumentFileLFD workspace)
        : this(workspace, "", "0", "0", "false", "function(x) return x end")
    { }

    public CustomInterpolationVariable(DocumentFileLFD workspace, string name, string from, string to, string precisely, string interp)
        : base(workspace)
    {
        Name = name;
        From = from;
        To = to;
        Precisely = precisely;
        InterpolationFunc = interp;
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
    [NodeAttribute("function(x) return x end", "Interpolation Function")]
    public string InterpolationFunc { get; set; }

    public override string ToString()
    {
        string offchar = Precisely == "true" ? "(Precisely)" : "(Expect next value IS)";
        string[] splited = InterpolationFunc.Split('\n');
        string shortTerm = splited.Length > 1 ? splited[0].Trim() + " ..." : splited[0].Trim();
        return $"{Name} : {From} => {To} {offchar}"
            + $", interpolate by: {shortTerm}";
    }

    public override object Clone()
    {
        CustomInterpolationVariable n = new(ParentTree);
        n.DeepCopyFrom(this);
        return n;
    }

    public override Tuple<string, string> GetInformation(string sp, string times)
    {
        string offchar = Precisely == "true" ? " - 1" : "";
        string beg = $"_beg_{Name}";
        string end = $"_end_{Name}";
        string func = $"_func_{Name}";
        string begin = $"{sp}local _beg_{Name} = {From}\n"
            + $"{sp}local _func_{Name} = {InterpolationFunc}\n"
            + $"{sp}local _w_{Name} = 0\n"
            + $"{sp}local _end_{Name} = {To}\n"
            + $"{sp}local _d_w_{Name} = 1 / ({times}{offchar})\n"
            + $"{sp}local {Name} = ({end} - {beg}) * {func}(0) + {beg}\n";
        string repeat = $"{sp}{sp}_w_{Name} = _w_{Name} + _d_w_{Name}\n"
            + $"{sp}{sp}{Name} = ({end} - {beg}) * {func}(_w_{Name}) + {beg}\n";
        return new Tuple<string, string>(begin, repeat);
    }
}
