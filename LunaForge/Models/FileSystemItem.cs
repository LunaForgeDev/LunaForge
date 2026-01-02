using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunaForge.Models;

public partial class FileSystemItem : ObservableObject
{
    [ObservableProperty]
    public string name = "";
    [ObservableProperty]
    public string fullPath = "";
    [ObservableProperty]
    public bool isFolder;
    [ObservableProperty]
    public bool isExpanded;
    [ObservableProperty]
    public ObservableCollection<FileSystemItem> children = [];
    [ObservableProperty]
    public bool isVisible = true;

    public FileSystemItem Parent { get; set; }
    public bool HasLoadedChildren { get; set; }

    public FileSystemItem() { }

    public FileSystemItem(string path, bool isFolder)
    {
        FullPath = path;
        IsFolder = isFolder;
        Name = Path.GetFileName(path);

        if (isFolder)
        {
            Children.Add(new() { Name = "Loading..." });
        }
    }

    public void LoadChildren()
    {
        if (HasLoadedChildren || !IsFolder)
            return;

        try
        {
            Children.Clear();
            DirectoryInfo dirInfo = new(FullPath);

            foreach (var dir in dirInfo.GetDirectories())
            {
                if ((dir.Attributes & FileAttributes.Hidden) == 0)
                    Children.Add(new(dir.FullName, true) { Parent = this });
            }

            foreach (var file in dirInfo.GetFiles())
            {
                if ((file.Attributes & FileAttributes.Hidden) == 0)
                    Children.Add(new(file.FullName, false) { Parent = this });
            }

            var sorted = new ObservableCollection<FileSystemItem>(
                Children.OrderBy(x => !x.IsFolder).ThenBy(x => x.Name)
            );
            Children.Clear();
            foreach (var item in sorted)
                Children.Add(item);

            HasLoadedChildren = true;
        }
        catch (Exception ex)
        {
            Children.Clear();
            Children.Add(new() { Name = $"FS Error" });
        }
    }
}
