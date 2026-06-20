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
using System.Windows;
using System.Xml.Linq;
using MessageBox = System.Windows.MessageBox;

namespace LunaForge.ViewModels;

public partial class NewFileWindowModel : ObservableObject
{
    private static ILogger Logger = CoreLogger.Create("New File Window");

    private readonly OnlineResourceService onlineResourceService = new();
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
    public NewFileTemplateViewModel? selectedTemplate = null;

    [ObservableProperty]
    private FileType selectedFileType = FileType.Lfd;

    [ObservableProperty]
    private bool isLoadingTemplates = false;

    [ObservableProperty]
    private ObservableCollection<string> luaSTGInstances = [];

    public ObservableCollection<NewFileTemplateViewModel> Templates { get; } = [];
    public ObservableCollection<NewFileTemplateViewModel> FilteredTemplates { get; } = [];

    private List<NewFileTemplateViewModel> allTemplates = [];

    public NewFileWindowModel() { }

    public NewFileWindowModel(NewFileWindow owner)
    {
        this.owner = owner;
        _ = LoadTemplatesAsync();
        FilePath = MainWindowModel.Project?.ProjectRoot ?? "";
    }

    public NewFileWindowModel(NewFileWindow owner, string preFilledPath)
    {
        this.owner = owner;
        _ = LoadTemplatesAsync();
        FilePath = string.IsNullOrEmpty(preFilledPath) ? MainWindowModel.Project?.ProjectRoot ?? "" : preFilledPath;
    }

    private async Task LoadTemplatesAsync()
    {
        IsLoadingTemplates = true;
        allTemplates.Clear();
        Templates.Clear();

        try
        {
            var localTemplates = LoadLocalTemplates();
            foreach (var template in localTemplates)
                allTemplates.Add(template);

            var onlineTemplates = await onlineResourceService.GetAvailableFileTemplatesAsync();
            foreach (var onlineTemplate in onlineTemplates)
            {
                string ext = "." + onlineTemplate.FileType.TrimStart('.');
                allTemplates.Add(new NewFileTemplateViewModel
                {
                    Text = onlineTemplate.Name,
                    FullPath = string.Empty,
                    Icon = GetIconForFileType(ext),
                    Description = onlineTemplate.Description,
                    FileExtension = ext,
                    IsOnline = true,
                    OnlineTemplate = onlineTemplate
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error loading templates: {ex.Message}");
        }
        finally
        {
            IsLoadingTemplates = false;
        }

        // Add default "Empty" templates for each type
        allTemplates.Add(new NewFileTemplateViewModel
        {
            Text = "Empty",
            FullPath = string.Empty,
            Icon = GetIconForFileType(".lfd"),
            Description = "Empty LFD Definition File",
            FileExtension = ".lfd"
        });
        allTemplates.Add(new NewFileTemplateViewModel
        {
            Text = "Empty",
            FullPath = string.Empty,
            Icon = GetIconForFileType(".lua"),
            Description = "Empty Lua Script",
            FileExtension = ".lua"
        });
        allTemplates.Add(new NewFileTemplateViewModel
        {
            Text = "Empty",
            FullPath = string.Empty,
            Icon = GetIconForFileType(".lfs"),
            Description = "Empty Shader File",
            FileExtension = ".lfs"
        });

        UpdateFilteredTemplates();
    }

    private List<NewFileTemplateViewModel> LoadLocalTemplates()
    {
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string LunaForgePath = Path.Combine(documentsPath, "LunaForge");
        string templatesPath = Path.Combine(LunaForgePath, ".templates");
        Directory.CreateDirectory(templatesPath).Attributes |= FileAttributes.Directory | FileAttributes.Hidden;
        string templatesFilesPath = Path.Combine(templatesPath, "files");
        DirectoryInfo dir = Directory.CreateDirectory(templatesFilesPath);

        List<FileInfo> fis = [.. dir.GetFiles("*.lua"), .. dir.GetFiles("*.lfd"), .. dir.GetFiles("*.lfs")];

        return [.. from FileInfo fi in fis
            where fi.Exists
            select new NewFileTemplateViewModel
            {
                Text = Path.GetFileNameWithoutExtension(fi.Name),
                FullPath = fi.FullName,
                Icon = GetIconForFileType(fi.Extension),
                Description = GetDescriptionForTemplate(fi.FullName),
                FileExtension = fi.Extension,
                IsOnline = false
            }
        ];
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
    public async Task Ok()
    {
        if (SelectedTemplate?.IsOnline == true && SelectedTemplate.OnlineTemplate != null)
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string cachePath = Path.Combine(documentsPath, "LunaForge", ".templates", "cache", "files");
            Directory.CreateDirectory(cachePath);

            string ext = SelectedTemplate.FileExtension;
            string localPath = Path.Combine(cachePath, SelectedTemplate.Text + ext);

            Logger.Information($"Downloading online file template: {SelectedTemplate.Text}");
            string? downloaded = await onlineResourceService.DownloadTemplateAsync(
                SelectedTemplate.OnlineTemplate.DownloadUrl, localPath);

            if (downloaded == null)
            {
                MessageBox.Show("Failed to download the template.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SelectedTemplate.FullPath = downloaded;
        }

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

    [RelayCommand]
    public void ManageLuaSTGInstances()
    {

    }
}

public partial class NewFileTemplateViewModel : ObservableObject
{
    public string Text { get; set; }
    public string FullPath { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public string FileExtension { get; set; }
    public bool IsOnline { get; set; } = false;
    public OnlineFileTemplate? OnlineTemplate { get; set; }
}