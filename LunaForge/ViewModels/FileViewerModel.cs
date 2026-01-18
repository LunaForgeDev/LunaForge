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
using System.Windows;
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

    public FileViewerModel(MainWindowModel window)
        : this()
    {
        mainWindowModel = window;
    }

    private void InitializeFileTree()
    {
        if (MainWindowModel.Project == null)
            return;

        try
        {
            string projectRoot = MainWindowModel.Project.ProjectRoot;
            FileSystemItem rootItem = new(projectRoot, true)
            {
                Name = "Root",
                IsExpanded = true
            };

            rootItem.LoadChildren();
            RootItems.Add(rootItem);
        }
        catch (Exception ex)
        {
            Logger.Error($"Cannot initialize the file tree. Reason:\n{ex}");
        }

        EnableWatcher();
    }

    private void EnableWatcher()
    {
        watcher = new(MainWindowModel.Project.ProjectRoot)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName
            | NotifyFilters.DirectoryName
            | NotifyFilters.Size
            | NotifyFilters.LastWrite
            | NotifyFilters.LastAccess,
            EnableRaisingEvents = true,
        };
        watcher.Created += OnCreated;
        watcher.Deleted += OnDeleted;
        watcher.Changed += OnChanged;
        watcher.Renamed += OnRenamed;
        watcher.Error += (s, e) =>
        {
            Logger.Error($"File system watcher error: {e.GetException()}");
        };
    }

    #region FileSystemWatcher events

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                string parentPath = Path.GetDirectoryName(e.FullPath);
                FileSystemItem parentItem = FindItemByPath(parentPath);

                if (parentItem != null && parentItem.HasLoadedChildren)
                {
                    if (parentItem.Children.Any(x => x.FullPath == e.FullPath))
                        return;

                    bool isDirectory = Directory.Exists(e.FullPath);
                    var newItem = new FileSystemItem(e.FullPath, isDirectory) { Parent = parentItem };

                    int insertIndex = 0;
                    foreach (var child in parentItem.Children)
                    {
                        if (isDirectory && !child.IsFolder)
                            break;
                        if (isDirectory == child.IsFolder && string.Compare(newItem.Name, child.Name, StringComparison.OrdinalIgnoreCase) < 0)
                            break;
                        insertIndex++;
                    }

                    parentItem.Children.Insert(insertIndex, newItem);
                    Logger.Information($"File system item created: {e.FullPath}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error handling file creation: {ex.Message}");
            }
        });
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                string parentPath = Path.GetDirectoryName(e.FullPath);
                FileSystemItem parentItem = FindItemByPath(parentPath);

                if (parentItem != null && parentItem.HasLoadedChildren)
                {
                    var itemToRemove = parentItem.Children.FirstOrDefault(x => x.FullPath == e.FullPath);
                    if (itemToRemove != null)
                    {
                        parentItem.Children.Remove(itemToRemove);
                        Logger.Information($"File system item deleted: {e.FullPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error handling file deletion: {ex.Message}");
            }
        });
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                //TODO: Change metadata
                Logger.Debug($"File system item changed: {e.FullPath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error handling file change: {ex.Message}");
            }
        });
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                string parentPath = Path.GetDirectoryName(e.FullPath);
                FileSystemItem parentItem = FindItemByPath(parentPath);

                if (parentItem != null && parentItem.HasLoadedChildren)
                {
                    var itemToRename = parentItem.Children.FirstOrDefault(x => x.FullPath == e.OldFullPath);
                    if (itemToRename != null)
                    {
                        itemToRename.FullPath = e.FullPath;
                        itemToRename.Name = Path.GetFileName(e.FullPath);

                        var sorted = parentItem.Children.OrderBy(x => !x.IsFolder).ThenBy(x => x.Name).ToList();
                        parentItem.Children.Clear();
                        foreach (var item in sorted)
                            parentItem.Children.Add(item);

                        Logger.Information($"File system item renamed: {e.OldFullPath} -> {e.FullPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error handling file rename: {ex.Message}");
            }
        });
    }

    private FileSystemItem FindItemByPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        foreach (var rootItem in RootItems)
        {
            if (rootItem.FullPath == path)
                return rootItem;

            var found = FindItemByPathRecursive(rootItem, path);
            if (found != null)
                return found;
        }

        return null;
    }

    private FileSystemItem FindItemByPathRecursive(FileSystemItem parent, string path)
    {
        if (!parent.HasLoadedChildren)
            return null;

        foreach (var child in parent.Children)
        {
            if (child.FullPath == path)
                return child;

            if (child.IsFolder && path.StartsWith(child.FullPath + Path.DirectorySeparatorChar))
            {
                var found = FindItemByPathRecursive(child, path);
                if (found != null)
                    return found;
            }
        }

        return null;
    }

    #endregion

    #region Commands

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
            if (parentItem == null || !parentItem.IsFolder)
                return;

            var dialog = new InputDialog("Create Folder", "Enter folder name:", "NewFolder")
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() != true)
                return;

            string folderName = dialog.InputText?.Trim();
            if (string.IsNullOrWhiteSpace(folderName))
            {
                MessageBox.Show("Folder name cannot be empty.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate folder name for invalid characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (folderName.IndexOfAny(invalidChars) >= 0)
            {
                MessageBox.Show("Folder name contains invalid characters.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newFolderPath = Path.Combine(parentItem.FullPath, folderName);

            if (Directory.Exists(newFolderPath))
            {
                MessageBox.Show($"A folder named '{folderName}' already exists.", "Folder Exists", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Directory.CreateDirectory(newFolderPath);
            
            if (!parentItem.IsExpanded)
            {
                parentItem.IsExpanded = true;
                if (!parentItem.HasLoadedChildren)
                    parentItem.LoadChildren();
            }
            
            Logger.Information($"Folder created: {newFolderPath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error trying to create folder. Reason:\n{ex}");
            MessageBox.Show($"Error creating folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public void CreateFile(FileSystemItem parentItem)
    {
        try
        {
            if (parentItem == null || !parentItem.IsFolder)
                return;

            var newFileWindow = new NewFileWindow(parentItem.FullPath)
            {
                Owner = Application.Current.MainWindow
            };

            if (newFileWindow.ShowDialog() == true)
            {
                var ctx = (NewFileWindowModel)newFileWindow.DataContext;
                string fullFilePath = Path.Combine(ctx.FilePath, ctx.FileName);

                DocumentFile file = null;

                if (string.IsNullOrEmpty(ctx.SelectedTemplate?.FullPath))
                {
                    file = MainWindowModel.Project.CreateFile(fullFilePath, ctx.SelectedFileType);
                }
                else
                {
                    File.Copy(ctx.SelectedTemplate.FullPath, fullFilePath, overwrite: false);
                    file = MainWindowModel.Project.OpenFile(fullFilePath);
                }

                if (file != null)
                {
                    MainWindowModel.Instance.SelectedFile = file;
                }

                if (!parentItem.IsExpanded)
                {
                    parentItem.IsExpanded = true;
                    if (!parentItem.HasLoadedChildren)
                        parentItem.LoadChildren();
                }

                Logger.Information($"File created: {fullFilePath}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error trying to create file. Reason:\n{ex}");
            MessageBox.Show($"Error creating file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public void DeleteItem(FileSystemItem item)
    {
        try
        {
            if (item == null)
                return;

            if (item.Parent == null || RootItems.Contains(item))
            {
                MessageBox.Show("Cannot delete the root folder.", "Delete Not Allowed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string itemType = item.IsFolder ? "folder" : "file";
            string message;

            if (item.IsFolder)
            {
                int fileCount = 0;
                int folderCount = 0;
                CountContents(item.FullPath, ref fileCount, ref folderCount);

                if (fileCount > 0 || folderCount > 0)
                {
                    message = $"Are you sure you want to delete the folder '{item.Name}'?\n\n" +
                              $"This will permanently delete:\n" +
                              $"- {folderCount} folder(s)\n" +
                              $"- {fileCount} file(s)\n\n" +
                              "This action cannot be undone.";
                }
                else
                {
                    message = $"Are you sure you want to delete the empty folder '{item.Name}'?";
                }
            }
            else
            {
                message = $"Are you sure you want to delete the file '{item.Name}'?\n\nThis action cannot be undone.";
            }

            var result = MessageBox.Show(message, $"Delete {itemType}?", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            if (item.IsFolder)
            {
                CloseFilesInFolder(item.FullPath);
                Directory.Delete(item.FullPath, true);
            }
            else
            {
                CloseFileIfOpen(item.FullPath);
                File.Delete(item.FullPath);
            }

            item.Parent?.Children.Remove(item);

            Logger.Information($"{itemType} deleted: {item.FullPath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error trying to delete item. Reason:\n{ex}");
            MessageBox.Show($"Error deleting item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public void RenameItem(FileSystemItem item)
    {
        try
        {
            if (item == null)
                return;

            if (item.Parent == null || RootItems.Contains(item))
            {
                MessageBox.Show("Cannot rename the root folder.", "Rename Not Allowed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string itemType = item.IsFolder ? "folder" : "file";
            var dialog = new InputDialog($"Rename {itemType}", $"Enter new name for '{item.Name}':", item.Name)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() != true)
                return;

            string newName = dialog.InputText?.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Name cannot be empty.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (newName.IndexOfAny(invalidChars) >= 0)
            {
                MessageBox.Show("Name contains invalid characters.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string parentPath = Path.GetDirectoryName(item.FullPath);
            string newPath = Path.Combine(parentPath, newName);

            if (item.IsFolder)
            {
                if (Directory.Exists(newPath))
                {
                    MessageBox.Show($"A folder named '{newName}' already exists.", "Folder Exists", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Directory.Move(item.FullPath, newPath);
            }
            else
            {
                if (File.Exists(newPath))
                {
                    MessageBox.Show($"A file named '{newName}' already exists.", "File Exists", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                File.Move(item.FullPath, newPath);
            }

            item.FullPath = newPath;
            item.Name = newName;

            Logger.Information($"{itemType} renamed to: {newPath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error trying to rename item. Reason:\n{ex}");
            MessageBox.Show($"Error renaming item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Helper Methods

    private void CountContents(string folderPath, ref int fileCount, ref int folderCount)
    {
        try
        {
            var dirs = Directory.GetDirectories(folderPath);
            folderCount += dirs.Length;

            foreach (var dir in dirs)
            {
                CountContents(dir, ref fileCount, ref folderCount);
            }

            fileCount += Directory.GetFiles(folderPath).Length;
        }
        catch
        {
            // Not an issue lmao cry about it.
        }
    }

    private void CloseFilesInFolder(string folderPath)
    {
        if (MainWindowModel.Project?.Files == null)
            return;

        var filesToClose = MainWindowModel.Project.Files
            .Where(f => f.FilePath?.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        foreach (var file in filesToClose)
        {
            MainWindowModel.Project.Files.Remove(file);
        }
    }

    private void CloseFileIfOpen(string filePath)
    {
        if (MainWindowModel.Project?.Files == null)
            return;

        var fileToClose = MainWindowModel.Project.Files
            .FirstOrDefault(f => f.FilePath?.Equals(filePath, StringComparison.OrdinalIgnoreCase) == true);

        if (fileToClose != null)
        {
            MainWindowModel.Project.Files.Remove(fileToClose);
        }
    }

    private void OpenFile(FileSystemItem item)
    {
        if (item == null || item.IsFolder)
            return;

        MainWindowModel.Instance.OpenFile(item.FullPath);
    }

    #endregion
}
