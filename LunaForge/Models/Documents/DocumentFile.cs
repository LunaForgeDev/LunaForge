using CommunityToolkit.Mvvm.ComponentModel;
using LunaForge.Backend.EditorCommands;
using LunaForge.Models.TreeNodes;
using LunaForge.Services;
using LunaForge.ViewModels;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Models;

public enum FileType
{
    Lua, // Lua File
    Lfd, // Defintion File
    Lfs, // Shader File
}

[Serializable]
public partial class DocumentFile : ObservableObject
{
    [JsonIgnore] public static ILogger Logger = CoreLogger.Create("Document");

    [JsonIgnore] public FileCollection Parent = null;

    [ObservableProperty]
    public string fileName = "Untitled";
    [JsonIgnore] public string FileHash { get; set; } = Guid.NewGuid().ToString();
    [JsonIgnore] public string FilePath { get; set; } = string.Empty;

    [ObservableProperty, JsonIgnore]
    public string fileExtension = ".lfd";

    [ObservableProperty, JsonIgnore]
    public string fileContent = string.Empty;

    [ObservableProperty, JsonIgnore]
    public TreeNode? selectedNode;

    public bool IsUnsaved
    {
        get
        {
            try
            {
                return CommandFlow.Peek() != SavedCommand;
            }
            catch (InvalidOperationException)
            {
                return SavedCommand != null;
            }
        }
    }

    [JsonIgnore]
    public ObservableCollection<TreeNode> TreeNodes { get; set; } = [];

    public static T CreateNew<T>(string name = "Unnamed") where T : DocumentFile, new()
    {
        T projectFile = new()
        {
            FilePath = name,
        };
        return projectFile;
    }

    public DocumentFile()
    { }

    public DocumentFile(string filePath)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        FileExtension = Path.GetExtension(filePath);
    }

    #region Commands

    [JsonIgnore] public Stack<Command> CommandFlow = [];
    [JsonIgnore] public Stack<Command> UndoFlow = [];
    [JsonIgnore] public Command SavedCommand = null;

    public bool CanUndo() => CommandFlow.Count > 0;
    public bool CanRedo() => UndoFlow.Count > 0;

    public void Undo()
    {
        if (!CanUndo())
            return;

        CommandFlow.Peek().Undo();
        UndoFlow.Push(CommandFlow.Pop());

        OnPropertyChanged(nameof(IsUnsaved));
        UpdateFileNameDisplay();
    }

    public void Redo()
    {
        if (!CanRedo())
            return;

        UndoFlow.Peek().Execute();
        CommandFlow.Push(UndoFlow.Pop());

        OnPropertyChanged(nameof(IsUnsaved));
        UpdateFileNameDisplay();
    }

    public void PushSavedCommand()
    {
        SavedCommand = CommandFlow.Count > 0 ? CommandFlow.Peek() : null;
        OnPropertyChanged(nameof(IsUnsaved));
        UpdateFileNameDisplay();
    }

    public bool AddAndExecuteCommand(Command command)
    {
        if (command != null)
        {
            command.Execute();
            CommandFlow.Push(command);
            UndoFlow.Clear();

            OnPropertyChanged(nameof(IsUnsaved));
            UpdateFileNameDisplay();

            return true;
        }
        else
            return false;
    }

    private void UpdateFileNameDisplay()
    {
        string baseName = FileName.TrimEnd('*', ' ');

        if (IsUnsaved && !FileName.EndsWith('*'))
            FileName = $"{baseName} *";
        else if (!IsUnsaved && FileName.EndsWith('*'))
            FileName = baseName;
    }

    #endregion
    #region Files

    public static T Load<T>(string fileName) where T : DocumentFile, new()
    {
        if (!File.Exists(fileName))
        {
            Logger.Error($"Project file '{fileName}' doesn't exist. Can't load.");
            return default;
        }

        T projFile = JsonConvert.DeserializeObject<T>(fileName) ?? new T();
        projFile.FilePath = fileName;

        return projFile;
    }

    public bool Save()
    {
        return Save(FilePath);
    }

    public virtual bool Save(string filePath)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        return true;
    }

    #endregion
    #region TreeView

    public void RevealTreeNode(TreeNode node)
    {
        if (node == null) return;

        TreeNode temp = node.ParentNode;
        MainWindowModel.Instance.SelectedFile = this;
        TreeNodes[0].ClearChildSelection();
        Stack<TreeNode> sta = [];
        while (temp != null)
        {
            sta.Push(temp);
            temp = temp.ParentNode;
        }

        while (sta.Count > 0)
        {
            sta.Pop().IsExpanded = true;
        }
        node.IsSelected = true;
    }

    public bool Insert(TreeNode parent, TreeNode node, InsertMode insertMode, bool doInvoke = true)
    {
        try
        {
            if (parent == null)
                return false;
            if (node.Children.Count > 0)
                node.IsExpanded = true;
            Command cmd = null;
            node.ParentTree = this;
            if (parent.ParentNode == null && insertMode != InsertMode.Child)
                return false;
            switch (insertMode)
            {
                case InsertMode.Ancestor:
                    break;
                case InsertMode.Before:
                    if (!parent.ParentNode.ValidateChild(node))
                        return false;
                    cmd = new InsertBeforeCommand(parent, node);
                    break;
                case InsertMode.After:
                    if (!parent.ParentNode.ValidateChild(node))
                        return false;
                    cmd = new InsertAfterCommand(parent, node);
                    break;
                case InsertMode.Child:
                    if (!parent.ValidateChild(node))
                        return false;
                    cmd = new InsertChildCommand(parent, node);
                    break;
            }
            if (AddAndExecuteCommand(cmd))
            {
                RevealTreeNode(node);
                if (doInvoke)
                {
                    //CreateInvoke(node);
                }
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to insert node. Reason:\n{ex}");
            return false;
        }
    }

    private void SelectNode(TreeNode node)
    {
        if (SelectedNode != null)
            SelectedNode!.IsSelected = false;
        SelectedNode = node;
        node.IsSelected = true;
    }

    #endregion
}
