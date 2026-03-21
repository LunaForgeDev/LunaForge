using LunaForge.Backend.Attributes.TreeNodesAttributes;
using LunaForge.Models;
using LunaForge.Models.Documents;
using LunaForge.Services;
using LunaForge.ViewModels;
using Newtonsoft.Json;
using System.IO;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Nodes.General;

[Serializable, NodeIcon("addprojectfile.png")]
[LeafNode]
public partial class AddProjectFile : TreeNode
{
    public override string NodeName { get; set; } = "AddProjectFile";

    [JsonConstructor]
    public AddProjectFile() : base() { }

    public AddProjectFile(DocumentFileLFD workspace)
        : this(workspace, "", "false", "")
    { }

    public AddProjectFile(DocumentFileLFD workspace, string file, string asModule, string moduleName)
        : base(workspace)
    {
        File = file;
        AsModule = asModule;
        ModuleName = moduleName;
    }

    [JsonIgnore]
    [NodeAttribute("", EditorWindow = "editorFileSelector")]
    public string File { get; set; } = "";

    [JsonIgnore]
    [NodeAttribute("false", "As Module ?")]
    public string AsModule { get; set; } = "false";

    [JsonIgnore]
    [NodeAttribute("", "Module Name")]
    public string ModuleName { get; set; } = "";

    private TraceHandle? moduleNameIsEmptyAndIsModule;
    private TraceHandle? addedFileIsInvalid;

    public override IEnumerable<string> ToLua(int spacing)
    {
        string modulePath = Path.GetRelativePath(MainWindowModel.Project!.ProjectRoot, File); 
        modulePath = Path.ChangeExtension(modulePath, null);
        modulePath = modulePath.Replace("\\\\", ".");
        modulePath = modulePath.Replace('\\', '.');
        modulePath = modulePath.Replace('/', '.');

        string sp = Indent(spacing);
        string code = sp;
        if (AsModule == "true")
            code += $"local {ModuleName} = ";

        if (Path.GetExtension(File) == ".lfd" || Path.GetExtension(File) == ".lua")
        {
            code += $"require(\"{modulePath}\")\n";
            yield return code;
        }
        else
        {
            yield return "-- Added Invalid File (must be .lfd or .lua)\n";
        }
    }

    public override string ToString()
    {
        if (AsModule == "true" && string.IsNullOrEmpty(ModuleName))
        {
            moduleNameIsEmptyAndIsModule ??= CommitTrace(TraceSeverity.Error, "Module name must not be empty when 'As Module' is true.");
        }
        else
        {
            moduleNameIsEmptyAndIsModule?.Resolve();
            moduleNameIsEmptyAndIsModule = null;
        }

        if (!string.IsNullOrEmpty(File) && !File.EndsWith(".lfd") && !File.EndsWith(".lua"))
        {
            addedFileIsInvalid ??= CommitTrace(TraceSeverity.Error, "Added file must have .lfd or .lua extension.");
        }
        else
        {
            addedFileIsInvalid?.Resolve();
            addedFileIsInvalid = null;
        }

        string str = $"Add file '{File}' to the project";
        if (AsModule == "true")
            str += " as a module";

        return str;
    }

    public override object Clone()
    {
        AddProjectFile n = new();
        n.DeepCopyFrom(this);
        return n;
    }
}