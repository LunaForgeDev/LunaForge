using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaForge.Models;
using LunaForge.Services;
using LunaForge.Views;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace LunaForge.ViewModels;

public partial class LauncherViewModel : ObservableObject
{
    [ObservableProperty]
    public ObservableCollection<ProjectCategory> projectCategories = [];

    public event Action<string>? ProjectSelected;
    public event Action? RequestClose;

    public LauncherViewModel()
    {
        LoadRecentProjects();
    }

    private void LoadRecentProjects()
    {
        ProjectCategories.Clear();

        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string LunaForgePath = Path.Combine(documentsPath, "LunaForge");

        Directory.CreateDirectory(LunaForgePath);

        List<FileInfo> projectFiles = [.. Directory.GetFiles(LunaForgePath, "*.lfp", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .OrderByDescending(f => f.LastAccessTime)];

        List<string> pinnedProjects = PinnedProjectsService.LoadPinnedProjects();

        DateTime now = DateTime.Now;
        ProjectCategory pinned = new("📍 Pinned");
        ProjectCategory today = new("🕐 Today");
        ProjectCategory yesterday = new("⏰ Yesterday");
        ProjectCategory aWeekAgo = new("📅 A Week Ago");
        ProjectCategory aMonthAgo = new("📆 A Month Ago");
        ProjectCategory older = new("🗂 Older");

        foreach (var file in projectFiles)
        {
            string projectName = Path.GetFileNameWithoutExtension(file.Name);
            bool isPinned = pinnedProjects.Contains(file.FullName);
            ProjectItem projectItem = new(file.FullName, projectName, file.LastAccessTime, isPinned);

            double daysAgo = (now - file.LastAccessTime).TotalDays;

            if (isPinned)
                pinned.Projects.Add(projectItem);
            else if (daysAgo < 1)
                today.Projects.Add(projectItem);
            else if (daysAgo < 2)
                yesterday.Projects.Add(projectItem);
            else if (daysAgo < 7)
                aWeekAgo.Projects.Add(projectItem);
            else if (daysAgo < 30)
                aMonthAgo.Projects.Add(projectItem);
            else
                older.Projects.Add(projectItem);
        }

        if (pinned.Projects.Count > 0)
            ProjectCategories.Add(pinned);
        if (today.Projects.Count > 0)
            ProjectCategories.Add(today);
        if (yesterday.Projects.Count > 0)
            ProjectCategories.Add(yesterday);
        if (aWeekAgo.Projects.Count > 0)
            ProjectCategories.Add(aWeekAgo);
        if (aMonthAgo.Projects.Count > 0)
            ProjectCategories.Add(aMonthAgo);
        if (older.Projects.Count > 0)
            ProjectCategories.Add(older);
    }

    [RelayCommand]
    private void OpenProject(ProjectItem project)
    {
        try
        {
            if (!File.Exists(project.Path))
            {
                MessageBox.Show("The selected project file does not exist anymore.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadRecentProjects();
                return;
            }

            PinnedProjectsService.UpdateProjectAccessTime(project.Path);
            ProjectSelected?.Invoke(project.Path);
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error while opening project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task TogglePin(ProjectItem project)
    {
        project.IsPinned = !project.IsPinned;
        await PinnedProjectsService.TogglePinProject(project.Path);
        LoadRecentProjects();
    }

    [RelayCommand]
    private void CreateNew()
    {
        CreateProjectWindow createWindow = new();
        CreateProjectViewModel? viewModel = createWindow.DataContext as CreateProjectViewModel;

        if (viewModel != null)
        {
            viewModel.ProjectCreated += (projectPath) =>
            {
                ProjectSelected?.Invoke(projectPath);
                RequestClose?.Invoke();
            };
            viewModel.RequestClose += () => createWindow.Close();
        }

        createWindow.ShowDialog();
    }

    [RelayCommand]
    private void OpenExisting()
    {

    }

    [RelayCommand]
    private void CloneRepository()
    {

    }

    [RelayCommand]
    private void GoToMain()
    {
        ProjectSelected?.Invoke(string.Empty);
        RequestClose?.Invoke();
    }
}
