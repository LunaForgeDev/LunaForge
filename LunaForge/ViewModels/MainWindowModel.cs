using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaForge.Backend.EditorCommands;
using LunaForge.Models;
using LunaForge.Models.Documents;
using LunaForge.Services;
using LunaForge.Views;
using Microsoft.Win32;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.ViewModels;

public enum InsertMode
{
    Ancestor,
    Before,
    After,
    Child,
}

public partial class MainWindowModel : ObservableObject
{
    private readonly ILogger Logger = CoreLogger.Create("MainWindow");

    public static Project Project { get; set; } // Only one Window opened per instance of this software. So this works.

    [ObservableProperty]
    public string debugString = "";

    [ObservableProperty]
    public string editorLog = "";

    [ObservableProperty]
    public FileViewerModel fileViewer;

    [ObservableProperty]
    public DocumentFile selectedFile;

    [ObservableProperty]
    private InsertMode currentInsertMode = InsertMode.Child;

    public bool CurrentFileIsLFD => SelectedFile != null && SelectedFile.FileExtension == ".lfd";

    public static MainWindowModel Instance { get; private set; }

    // Properties for RadioButton binding
    public bool IsAncestorState => CurrentInsertMode == InsertMode.Ancestor;
    public bool IsBeforeState => CurrentInsertMode == InsertMode.Before;
    public bool IsAfterState => CurrentInsertMode == InsertMode.After;
    public bool IsChildState => CurrentInsertMode == InsertMode.Child;

    public IReadOnlyCollection<ToolboxCategory>? ToolboxCategories => Project?.ToolboxService?.ToolboxCategories;

    public TreeNode TreeNodeClipboard { get; set; }

    public string WindowName
    {
        get
        {
            if (Project == null)
                return $"LunaForge {App.AppVersion}";
            else
                return $"LunaForge {App.AppVersion} - {Path.GetFileName(Project.Name)}";
        }
    }

    public MainWindowModel()
    {
        Instance = this;
    }

    public MainWindowModel(string projectPath)
    {
        Instance = this;
        Project = new(projectPath);
        InitializeProject(projectPath);
        FileViewer = new(this);
    }

    private async Task InitializeProject(string projectPath)
    { 
        try
        {
            var (project, error) = Project.Load(projectPath);

            if (project != null)
            {
                Project = project;
                await project.InitializePlugins();
                RefreshNodeLibraryUI();
                LoadOpenedFiles();
                
                DiscordRPCService.Default?.SetProjectPresence(Project.Name);
            }
        }
        catch (Exception ex)
        {
            // uhhhhhhhhhhhhhhhhhhhhhhhhhhhh... what.
            Logger.Fatal($"Project failed to initialize. Reason:\n{ex}");

            // If that happens, what the seven fucking hells did you do?
            // Help
        }

        StartLoggingUpdater();
    }

    private void RefreshNodeLibraryUI()
    {
        OnPropertyChanged(nameof(ToolboxCategories));
    }

    private void LoadOpenedFiles()
    {
        

    }

    private void StartLoggingUpdater()
    {
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        timer.Tick += (s, e) =>
        {
            EditorLog = CoreLogger.stringLog.GetStringBuilder().ToString();
        };

        timer.Start();
    }

    #region Commands

    [RelayCommand]
    private void NewFile()
    {
        NewFileWindow nw = new();
        if (nw.ShowDialog() == true)
        {
            NewFileWindowModel ctx = (NewFileWindowModel)nw.DataContext;
            string fullPathClone = ctx.SelectedTemplate.FullPath;
            DocumentFile file = null;

            string fullFilePath = Path.Combine(ctx.FilePath, ctx.FileName);

            if (string.IsNullOrEmpty(ctx.SelectedTemplate.FullPath))
            {
                file = Project.CreateFile(fullFilePath, ctx.SelectedFileType);
            }
            else
            {
                file = CreateFileFromTemplate(fullFilePath, ctx.SelectedFileType, fullPathClone);
            }

            if (file != null)
            {
                SelectedFile = file;
            }
        }
    }

    private DocumentFile CreateFileFromTemplate(string filePath, FileType fileType, string templatePath)
    {
        return null;
    }

    [RelayCommand]
    private void OpenFile(string filePath)
    {
        if (Path.GetExtension(filePath).Equals(".lfp", StringComparison.CurrentCultureIgnoreCase))
        {
            Settings settings = new();
            settings.Show();
            return;
        }

        var openedFile = Project.OpenFile(filePath);
        if (openedFile != null)
        {
            SelectedFile = openedFile;
        }
    }

    [RelayCommand]
    private void SaveFile()
    {
        if (SelectedFile == null)
        {
            Logger.Warning("No file selected to save.");
            return;
        }

        if (string.IsNullOrEmpty(SelectedFile.FilePath))
        {
            SaveFileAs();
            return;
        }

        bool success = SelectedFile.Save();
        if (success)
        {
            Logger.Information($"File saved successfully: {SelectedFile.FilePath}");
        }
        else
        {
            Logger.Error($"Failed to save file: {SelectedFile.FilePath}");
        }
    }

    [RelayCommand]
    private void SaveFileAs()
    {
        if (SelectedFile == null)
        {
            Logger.Warning("No file selected to save.");
            return;
        }

        SaveFileDialog dialog = new()
        {
            Title = "Save File As",
            InitialDirectory = string.IsNullOrEmpty(SelectedFile.FilePath) 
                ? Project?.ProjectRoot 
                : Path.GetDirectoryName(SelectedFile.FilePath),
            FileName = Path.GetFileNameWithoutExtension(SelectedFile.FileName),
            DefaultExt = SelectedFile.FileExtension,
            Filter = GetFileFilterForExtension(SelectedFile.FileExtension)
        };

        if (dialog.ShowDialog() == true)
        {
            string newFilePath = dialog.FileName;
            
            bool success = SelectedFile.Save(newFilePath);
            if (success)
            {
                SelectedFile.FilePath = newFilePath;
                SelectedFile.FileName = Path.GetFileName(newFilePath);
                Logger.Information($"File saved as: {newFilePath}");
            }
            else
            {
                Logger.Error($"Failed to save file as: {newFilePath}");
                MessageBox.Show($"Failed to save file to {newFilePath}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private static string GetFileFilterForExtension(string extension)
    {
        return extension.ToLower() switch
        {
            ".lua" => "Lua Script Files (*.lua)|*.lua|All Files (*.*)|*.*",
            ".lfd" => "LunaForge Definition Files (*.lfd)|*.lfd|All Files (*.*)|*.*",
            ".lfs" => "LunaForge Shader Files (*.lfs)|*.lfs|All Files (*.*)|*.*",
            _ => "All Files (*.*)|*.*"
        };
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var settings = new Settings();
        settings.Show();
    }

    [RelayCommand]
    private void OpenPluginManager()
    {
        if (Project?.PluginManager == null)
        {
            Logger.Warning("Plugin manager not available");
            return;
        }

        var pluginManagerWindow = new PluginManagerWindow(Project.PluginManager)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        pluginManagerWindow.ShowDialog();
        
        RefreshNodeLibraryUI();
    }

    [RelayCommand]
    private void OpenAbout()
    {

    }

    [RelayCommand]
    private void CloseFile(string fileHash)
    {
        DocumentFile toRemove = null;
        foreach (var file in Project.Files)
        {
            if (file.FileHash == fileHash)
            {
                toRemove = file;
                break;
            }
        }

        if (toRemove == null)
            return;

        if (toRemove.IsUnsaved)
        {
            switch (MessageBox.Show($"Do you want to save \"{toRemove.FileName}\"?", "LunaForge Editor", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
            {
                case MessageBoxResult.Yes:
                    bool saved = toRemove.Save();
                    if (saved)
                    {
                        Project.Files.Remove(toRemove);
                        Logger.Information($"File saved and closed: {toRemove.FileName}");
                    }
                    else
                    {
                        Logger.Warning($"Failed to save file: {toRemove.FileName}");
                    }
                    return;
                case MessageBoxResult.No:
                    break;
                default:
                    return;
            }
        }
        Logger.Information($"Closing file \"{toRemove.FileName}\"");
        Project.Files.Remove(toRemove);
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        SelectedFile?.Undo();
    }
    private bool CanUndo() => SelectedFile?.CanUndo() ?? false;

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        SelectedFile?.Redo();
    }
    private bool CanRedo() => SelectedFile?.CanRedo() ?? false;

    [RelayCommand]
    private void SwitchInsertMode(string mode)
    {
        if (Enum.TryParse<InsertMode>(mode, out var insertMode))
        {
            CurrentInsertMode = insertMode;
        }
    }

    [RelayCommand]
    private void InsertNode(ToolboxItem item)
    {
        if (item == null || Project?.ToolboxService == null)
            return;

        if (SelectedFile == null)
            return;
        if (SelectedFile.FileExtension != ".lfd" || SelectedFile.SelectedNode == null)
            return;

        TreeNode? node = Project.ToolboxService.CreateNode(item);

        InsertNode(node, item.DisplayName);
    }

    public void InsertNode(TreeNode node, string displayName)
    {
        if (SelectedFile.SelectedNode == null)
        {
            Logger.Warning("No node selected to insert into.");
            return;
        }

        TreeNode parent = SelectedFile.SelectedNode;
        if (node == null)
        {
            Logger.Warning($"Failed to create node from item: {displayName}");
            return;
        }
        if (SelectedFile is DocumentFileLFD ldfFile)
            ldfFile.Insert(parent, node, CurrentInsertMode);
    }

    #endregion

    partial void OnCurrentInsertModeChanged(InsertMode value)
    {
        OnPropertyChanged(nameof(IsAncestorState));
        OnPropertyChanged(nameof(IsBeforeState));
        OnPropertyChanged(nameof(IsAfterState));
        OnPropertyChanged(nameof(IsChildState));
    }

    partial void OnSelectedFileChanged(DocumentFile value)
    {
        OnPropertyChanged(nameof(CurrentFileIsLFD));
    }
}
