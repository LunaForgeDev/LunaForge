using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaForge.Models;
using LunaForge.Services;
using Microsoft.Win32;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace LunaForge.ViewModels;

public partial class CreateProjectViewModel : ObservableObject
{
    private static ILogger Logger = CoreLogger.Create("Create Project");
    private readonly OnlineTemplateService onlineTemplateService = new();

    [ObservableProperty]
    private int currentPage = 0; // 0: Template; 1: Details
    [ObservableProperty]
    private ObservableCollection<ProjectTemplateViewModel> templates = [];
    [ObservableProperty]
    private ProjectTemplateViewModel? selectedTemplate;
    [ObservableProperty]
    private string projectName = "";
    [ObservableProperty]
    private string projectAuthor = "";
    [ObservableProperty]
    private string projectPath = "";
    [ObservableProperty]
    private bool isCreating = false;
    [ObservableProperty]
    private bool isLoadingTemplates = false;

    public event Action<string>? ProjectCreated;
    public event Action? RequestClose;

    public CreateProjectViewModel()
    {
        ProjectPath = Path.Combine(
            EditorConfig.Default.Get<string>("ProjectsFolder").Value,
            EditorConfig.Default.Get<string>("ProjectName").Value
        );
        ProjectAuthor = EditorConfig.Default.Get<string>("ProjectAuthor").Value;

        _ = LoadTemplatesAsync();
    }

    private async Task LoadTemplatesAsync()
    {
        IsLoadingTemplates = true;
        Templates.Clear();

        try
        {
            Templates.Add(new ProjectTemplateViewModel
            {
                Name = "Empty",
                Description = "Empty Project",
                IsOnline = false,
            });

            var localTemplates = LoadLocalTemplates();
            foreach (var template in localTemplates)
            {
                Templates.Add(template);
            }

            var onlineTemplates = await onlineTemplateService.GetAvailableProjectTemplatesAsync();
            foreach (var onlineTemplate in onlineTemplates)
            {
                Templates.Add(new ProjectTemplateViewModel
                {
                    Name = onlineTemplate.Name,
                    Description = onlineTemplate.Description,
                    Version = onlineTemplate.Version,
                    Author = onlineTemplate.Author,
                    IsOnline = true,
                    OnlineTemplate = onlineTemplate
                });
            }

            if (Templates.Count > 0)
                SelectedTemplate = Templates[0];
        }
        catch (Exception ex)
        {
            Logger.Error($"Error loading templates: {ex.Message}");
        }
        finally
        {
            IsLoadingTemplates = false;
        }
    }

    private List<ProjectTemplateViewModel> LoadLocalTemplates()
    {
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string lunaForgePath = Path.Combine(documentsPath, "LunaForge");
        string templatesPath = Path.Combine(lunaForgePath, ".templates");
        Directory.CreateDirectory(templatesPath).Attributes |= FileAttributes.Directory | FileAttributes.Hidden;
        string templatesProjectsPath = Path.Combine(templatesPath, "projects");
        DirectoryInfo dir = Directory.CreateDirectory(templatesProjectsPath);

        List<FileInfo> fis = [.. dir.GetFiles("*.json")];

        return [.. from FileInfo fi in fis
            where File.Exists(Path.Combine(templatesProjectsPath, Path.ChangeExtension(fi.Name, ".zip")))
            select GetLocalTemplateInfo(templatesProjectsPath, fi)
        ];
    }

    private ProjectTemplateViewModel GetLocalTemplateInfo(string templateDir, FileInfo fi)
    {
        try
        {
            using StreamReader sr = fi.OpenText();
            ProjectTemplate def = JsonConvert.DeserializeObject<ProjectTemplate>(sr.ReadToEnd()) ?? new();

            return new ProjectTemplateViewModel
            {
                Name = def.Name,
                Description = def.Description,
                Version = def.Version,
                ZipPath = Path.Combine(templateDir, Path.ChangeExtension(fi.Name, ".zip")),
                IsOnline = false
            };
        }
        catch (Exception ex)
        {
            Logger.Error($"Cannot find template info for template '{fi.Name}'. Reason:\n{ex}");
            return new() { Name = "Unknown", Description = "Error loading template" };
        }
    }

    [RelayCommand]
    private void SelectTemplate(ProjectTemplateViewModel? template)
    {
        SelectedTemplate = template;
    }

    [RelayCommand]
    private void ConfirmTemplate()
    {
        if (SelectedTemplate == null)
        {
            MessageBox.Show("Please select a template", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CurrentPage = 1;
    }

    [RelayCommand]
    private void ChangePath()
    {
        OpenFolderDialog dialog = new()
        {
            Title = "Select parent folder for project creation",
            InitialDirectory = Path.GetDirectoryName(ProjectPath),
        };

        if (dialog.ShowDialog() == true)
        {
            ProjectPath = Path.Combine(dialog.FolderName, ProjectName);
        }
    }

    [RelayCommand]
    private async Task CreateProject()
    {
        if (string.IsNullOrEmpty(ProjectName))
        {
            MessageBox.Show("Project name cannot be empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (string.IsNullOrEmpty(ProjectPath))
        {
            MessageBox.Show("Project path cannot be empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            IsCreating = true;

            string fullProjectPath = ProjectPath;

            DirectoryInfo dirInfo = new(fullProjectPath);
            if (dirInfo.Exists && (dirInfo.GetFiles().Length > 0 || dirInfo.GetDirectories().Length > 0))
            {
                MessageBox.Show("The selected folder is not empty. Please choose an empty folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                IsCreating = false;
                return;
            }

            string? zipPath = null;

            // Download if online
            if (SelectedTemplate?.IsOnline == true && SelectedTemplate.OnlineTemplate != null)
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string cachePath = Path.Combine(documentsPath, "LunaForge", ".templates", "cache");
                Directory.CreateDirectory(cachePath);

                string fileName = Path.GetFileName(new Uri(SelectedTemplate.OnlineTemplate.DownloadUrl).LocalPath);
                string localPath = Path.Combine(cachePath, fileName);

                Logger.Information($"Downloading template: {SelectedTemplate.Name}");
                MessageBox.Show($"Downloading Remote Template", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                zipPath = await onlineTemplateService.DownloadTemplateAsync(
                    SelectedTemplate.OnlineTemplate.DownloadUrl,
                    localPath);

                if (zipPath == null)
                {
                    MessageBox.Show($"Failed to download template", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    IsCreating = false;
                    return;
                }
            }
            else
            {
                zipPath = SelectedTemplate?.ZipPath;
            }

            Project proj = Project.CreateNew(fullProjectPath, ProjectAuthor, zipPath);

            Logger.Information($"Project created successfully at: {fullProjectPath}");
            Logger.Information($"Project file: {proj.ProjectFile}");

            ProjectCreated?.Invoke(proj.ProjectFile);
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error creating project: {ex.Message}\n{ex.StackTrace}");
            MessageBox.Show($"Error creating project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            IsCreating = false;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        if (CurrentPage > 0)
            CurrentPage--;
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }

    partial void OnProjectNameChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            string parentFolder = EditorConfig.Default.Get<string>("ProjectsFolder").Value;
            ProjectPath = Path.Combine(parentFolder, value);
        }
    }
}

public class ProjectTemplate
{
    public string Name { get; set; } = "Empty";
    public string Description { get; set; } = "Default Project";
    public string Version { get; set; } = "1.0.0";
    public string? ZipPath { get; set; }
}

public partial class ProjectTemplateViewModel : ObservableObject
{
    [ObservableProperty]
    private string name = "Empty";
    [ObservableProperty]
    private string description = "Default Project";
    [ObservableProperty]
    private string version = "1.0.0";
    [ObservableProperty]
    private string? author;
    [ObservableProperty]
    private string? zipPath;
    [ObservableProperty]
    private bool isOnline = false;
    [ObservableProperty]
    private OnlineProjectTemplate? onlineTemplate;

    public string DisplaySource => IsOnline ? "Remote" : "Local";
}