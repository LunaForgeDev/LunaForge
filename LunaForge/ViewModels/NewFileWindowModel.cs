using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaForge.Models;
using LunaForge.Services;
using LunaForge.Views;
using Microsoft.Win32;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LunaForge.ViewModels;

public partial class NewFileWindowModel : ObservableObject
{
    private static ILogger Logger = CoreLogger.Create("New File Window");

    private NewFileWindow owner { get; }
    
    [ObservableProperty]
    public string fileName = "Untitled.lfd";
    
    [ObservableProperty]
    public string author = EditorConfig.Default.Get<string>("ProjectAuthor").Value;
    
    [ObservableProperty]
    public string filePath = "";

    [ObservableProperty]
    public string textDescription = "";

    [ObservableProperty]
    public DefS selectedTemplate = null;

    [ObservableProperty]
    private FileType selectedFileType = FileType.Lfd;

    public ObservableCollection<DefS> Templates { get; } = [];
    public ObservableCollection<DefS> FilteredTemplates { get; } = [];

    private List<DefS> allTemplates = [];

    public NewFileWindowModel() { }

    public NewFileWindowModel(NewFileWindow owner)
    {
        this.owner = owner;
        LoadTemplates();
        UpdateFilteredTemplates();
        FilePath = MainWindowModel.Project.ProjectRoot;
    }

    private void LoadTemplates()
    {
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string LunaForgePath = Path.Combine(documentsPath, "LunaForge");
        string templatesPath = Path.Combine(LunaForgePath, ".templates");
        Directory.CreateDirectory(templatesPath).Attributes |= FileAttributes.Directory | FileAttributes.Hidden;
        string templatesFilesPath = Path.Combine(templatesPath, "files");
        DirectoryInfo dir = Directory.CreateDirectory(templatesFilesPath);

        List<FileInfo> fis = [.. dir.GetFiles("*.lua"), .. dir.GetFiles("*.lfd"), .. dir.GetFiles("*.lfs")];

        foreach (FileInfo fi in fis)
        {
            Logger.Verbose($"Found template : {fi.FullName}");
            
            var template = new DefS
            {
                Text = Path.GetFileNameWithoutExtension(fi.Name),
                FullPath = fi.FullName,
                Icon = GetIconForFileType(fi.Extension),
                Description = GetDescriptionForTemplate(fi.FullName),
                FileExtension = fi.Extension
            };

            allTemplates.Add(template);
            Templates.Add(template);
        }

        // Add default "Empty" templates for each type
        allTemplates.Add(new DefS
        {
            Text = "Empty",
            FullPath = string.Empty,
            Icon = GetIconForFileType(".lfd"),
            Description = "Empty LFD Definition File",
            FileExtension = ".lfd"
        });
        allTemplates.Add(new DefS
        {
            Text = "Empty",
            FullPath = string.Empty,
            Icon = GetIconForFileType(".lua"),
            Description = "Empty Lua Script",
            FileExtension = ".lua"
        });
        allTemplates.Add(new DefS
        {
            Text = "Empty",
            FullPath = string.Empty,
            Icon = GetIconForFileType(".lfs"),
            Description = "Empty Shader File",
            FileExtension = ".lfs"
        });
    }

    private static string GetIconForFileType(string extension)
    {
        return extension.ToLower() switch
        {
            ".lua" => "/Images/lua.png",
            ".lfd" => "/Images/lfd.png",
            ".lfs" => "/Images/shader.png",
            _ => "/Images/Icon.png"
        };
    }

    private static string GetDescriptionForTemplate(string templatePath)
    {
        // I keep the extension because there can be multiple templates of different type but same name.
        string txt = templatePath + ".txt";

        if (File.Exists(txt))
        {
            return File.ReadAllText(txt);
        }
        Logger.Warning($"No description found for template '{Path.GetFileName(templatePath)}'");
        return "No Description";
    }

    private void UpdateFilteredTemplates()
    {
        FilteredTemplates.Clear();

        string targetExtension = SelectedFileType switch
        {
            FileType.Lua => ".lua",
            FileType.Lfd => ".lfd",
            FileType.Lfs => ".lfs",
            _ => ".lfd"
        };

        var filtered = allTemplates.Where(t => t.FileExtension.Equals(targetExtension, StringComparison.OrdinalIgnoreCase));

        foreach (var template in filtered)
        {
            FilteredTemplates.Add(template);
        }

        if (FilteredTemplates.Count > 0)
        {
            SelectedTemplate = FilteredTemplates[0];
        }

        Logger.Information($"Filtered {FilteredTemplates.Count} templates for {targetExtension}");
    }

    partial void OnSelectedFileTypeChanged(FileType value)
    {
        UpdateFilteredTemplates();
        UpdateFileExtension();
    }

    private void UpdateFileExtension()
    {
        string extension = SelectedFileType switch
        {
            FileType.Lua => ".lua",
            FileType.Lfd => ".lfd",
            FileType.Lfs => ".lfs",
            _ => ".lfd"
        };

        // Update filename extension if it doesn't already have the correct one
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(FileName);
        if (string.IsNullOrEmpty(fileNameWithoutExt))
        {
            fileNameWithoutExt = "Untitled";
        }
        FileName = fileNameWithoutExt + extension;
    }

    [RelayCommand]
    public void Ok()
    {
        owner.DialogResult = true;
        owner.Close();
    }

    [RelayCommand]
    public void Cancel()
    {
        owner.DialogResult = false;
        owner.Close();
    }

    [RelayCommand]
    public void BrowsePath()
    {
        OpenFolderDialog dialog = new()
        {
            Multiselect = false,
            InitialDirectory = MainWindowModel.Project.ProjectRoot,
            RootDirectory = MainWindowModel.Project.ProjectRoot,
            Title = "Select Folder for creating a new File"
        };

        if (dialog.ShowDialog() == true)
        {
            FilePath = dialog.FolderName;
        }
    }
}

public class DefS
{
    public string Text { get; set; }
    public string FullPath { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public string FileExtension { get; set; }
}