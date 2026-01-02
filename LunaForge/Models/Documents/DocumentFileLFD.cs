using LunaForge.Helpers;
using LunaForge.Nodes.General;
using LunaForge.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Models.Documents;

[Serializable]
public partial class DocumentFileLFD : DocumentFile
{
    [JsonProperty]
    public List<string> Exports { get; set; } = [];

    [JsonProperty]
    public List<string> Dependencies { get; set; } = [];

    public DocumentFileLFD() : this(string.Empty)
    { }

    public DocumentFileLFD(string filePath) : base(filePath)
    {
        FilePath = filePath;
        FileExtension = ".lfd";
        TreeNodes.Add(new RootFolder(this));
    }

    /// <summary>
    /// Taken from Sharp-X because that works and why the fuck wouldn't I?
    /// </summary>
    /// <param name="filePath">File to load.</param>
    /// <returns>A working (hopefully) LDF file</returns>
    public static DocumentFileLFD Load(string filePath)
    {
        DocumentFileLFD doc = new(filePath);
        doc.TreeNodes.Clear();

        TreeNode root = null;
        TreeNode prev = null;
        TreeNode tempN;
        int prevLevel = -1;
        int i, levelgrad;
        char[] temp;
        string des;

        try
        {
            using StreamReader sr = new(filePath, Encoding.UTF8);
            while (!sr.EndOfStream)
            {
                temp = sr.ReadLine().ToCharArray();
                i = 0;
                while (temp[i] != ',')
                    i++;
                des = new string(temp, i + 1, temp.Length - i - 1);
                if (prevLevel != -1)
                {
                    levelgrad = Convert.ToInt32(new string(temp, 0, i)) - prevLevel;
                    if (levelgrad <= 0)
                    {
                        for (int j = 0; j >= levelgrad; j--)
                            prev = prev.ParentNode;
                    }
                    tempN = (TreeNode)EditorSerializer.DeserializeTreeNode(des);
                    tempN.ParentTree = doc;
                    prev.AddChild(tempN);
                    prev = tempN;
                    prevLevel += levelgrad;
                }
                else
                {
                    root = (TreeNode)EditorSerializer.DeserializeTreeNode(des);
                    root.ParentTree = doc;
                    prev = root;
                    prevLevel = 0;
                }
            }

            doc.TreeNodes.Add(root);
            return doc;
        }
        catch (Exception ex)
        {
            Logger.Error($"Couldn't create node tree from file. Reason:\n{ex}");
            MessageBox.Show(ex.ToString());
            return null;
        }
    }

    public new bool Save()
    {
        return Save(FilePath);
    }

    public override bool Save(string filePath)
    {
        PushSavedCommand();

        try
        {
            using FileStream s = new(filePath, FileMode.Create, FileAccess.Write);
            using StreamWriter sw = new(s, Encoding.UTF8);
            TreeNodes[0].SerializeFile(sw, 0);

            _ = Task.Run(async () =>
            {
                var project = MainWindowModel.Project;
                if (project?.SymbolIndex != null)
                {
                    await project.SymbolIndex.IndexOpenedDocumentAsync(this);
                }
            });

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Unable to write to file '{filePath}'. Reason:\n{ex}");
            return false;
        }
    }

    public void OnTreeNodesChanged(ObservableCollection<TreeNode> value)
    {
        UpdateSymbolIndex();
    }

    private void UpdateSymbolIndex()
    {
        _ = Task.Run(async () =>
        {
            var project = MainWindowModel.Project;
            if (project?.SymbolIndex != null && !string.IsNullOrEmpty(FilePath))
            {
                await project.SymbolIndex.IndexOpenedDocumentAsync(this);
            }
        });
    }
}
