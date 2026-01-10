using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaForge.Models;
using LunaForge.Services;
using LunaForge.Views;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessageBox = System.Windows.MessageBox;

namespace LunaForge.ViewModels;

public partial class FileViewerModel : ObservableObject
{
    private static ILogger Logger = CoreLogger.Create("FileViewer");

    [ObservableProperty]
    public ObservableCollection<FileSystemItem> rootItems = [];

    private FileSystemWatcher watcher;
    private MainWindowModel mainWindowModel;

    public FileViewerModel()
    {
        InitializeFileTree();
    }

    public FileViewerModel(MainWindowModel mainWindowModel) : this()
    {
        this.mainWindowModel = mainWindowModel;
    }

    private void InitializeFileTree()
    {
        if (MainWindowModel.Project == null)
            return;

        rootItems.Clear();

        try
        {
            string projectRoot = MainWindowModel.Project.ProjectRoot;
            var rootItem = new FileSystemItem(projectRoot, true)
            { Name = "Root", IsExpanded = true };

            rootItem.LoadChildren();
            rootItems.Add(rootItem);
        }
        catch (Exception ex)
        {
            Logger.Error($"Cannot initialize the file tree. Reason:\n{ex}");
        }

        watcher = new(MainWindowModel.Project.ProjectRoot);
    }

    [RelayCommand]
    public void ItemExpanded(FileSystemItem item)
    {
        if (!item.HasLoadedChildren)
            item.LoadChildren();
    }

    [RelayCommand]
    public void ItemDoubleClick(FileSystemItem item)
    {
        if (item.IsFolder)
        {
            item.IsExpanded = !item.IsExpanded;
            if (item.IsExpanded && !item.HasLoadedChildren)
                item.LoadChildren();
        }
        else
        {
            OpenFile(item);
        }
    }

    [RelayCommand]
    public void CreateFolder(FileSystemItem parentItem)
    {
        try
        {
            if (!parentItem.IsFolder)
                return;

            string newFolderPath = Path.Combine(parentItem.FullPath, "NewFolder");
            int counter = 1;

            while (Directory.Exists(newFolderPath))
            {
                newFolderPath = Path.Combine(parentItem.FullPath, $"NewFolder_{counter}");
                counter++;
            }

            Directory.CreateDirectory(newFolderPath);
            parentItem.LoadChildren();
            Logger.Information($"Folder created: {newFolderPath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error trying to create folder. Reason:\n{ex}");
            MessageBox.Show($"Error creating folder: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public void CreateFile(FileSystemItem parentItem)
    {

    }

    [RelayCommand]
    public void DeleteItem(FileSystemItem parentItem)
    {

    }

    private void OpenFile(FileSystemItem item)
    {
        if (item == null || item.IsFolder)
            return;

        MainWindowModel.Instance.OpenFile(item.FullPath);

        /*
        try
        {
            if (Path.GetExtension(item.FullPath).Equals(".lfp", StringComparison.CurrentCultureIgnoreCase))
            {
                Settings settings = new();
                settings.Show();
                return;
            }

            var openedFile = MainWindowModel.Project.OpenFile(item.FullPath);
            if (openedFile != null && mainWindowModel != null)
            {
                mainWindowModel.SelectedFile = openedFile;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error opening file '{item.FullPath}'. Reason:\n{ex}");
            System.Windows.MessageBox.Show($"Error opening file: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        */
    }
}
