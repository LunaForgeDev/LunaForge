using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaForge.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunaForge.ViewModels;

public partial class SetupViewModel : ObservableObject
{
    [ObservableProperty]
    public int currentPage = 0;

    [ObservableProperty]
    public string projectsFolder = DetermineDefaultProjectsPath();

    [ObservableProperty]
    public string projectAuthor = "";

    [ObservableProperty]
    public string pageTitle = "Welcome to LunaForge";

    private const int PageCount = 4;

    public event Action? SetupCompleted;

    public SetupViewModel()
    {
        string defaultAuthor = EditorConfig.Default.Get<string>("ProjectAuthor").Value;
        ProjectAuthor = string.IsNullOrEmpty(defaultAuthor) ? "John Dough" : defaultAuthor;
        UpdatePageTitle();
    }

    private static string DetermineDefaultProjectsPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "LunaForge",
            "Projects"
        );
    }

    [RelayCommand]
    public async Task NextPage()
    {
        if (CurrentPage < PageCount - 1)
        {
            CurrentPage++;
            UpdatePageTitle();
        }
        else
        {
            await FinishSetup();
        }
    }

    [RelayCommand]
    public void PreviousPage()
    {
        if (CurrentPage > 0)
        {
            CurrentPage--;
            UpdatePageTitle();
        }
    }

    [RelayCommand]
    public void BrowseProjectsFolder()
    {
        OpenFolderDialog dialog = new()
        {
            Title = "Select Projects Folder",
            InitialDirectory = ProjectsFolder,
        };

        if (dialog.ShowDialog() == true)
        {
            ProjectsFolder = dialog.FolderName;
        }
    }

    private async Task FinishSetup()
    {
        EditorConfig config = EditorConfig.Default;

        Directory.CreateDirectory(ProjectsFolder);

        config.SetOrCreate("ProjectsFolder", ProjectsFolder);
        config.SetOrCreate("ProjectAuthor", ProjectAuthor);
        config.SetOrCreate("SetupDone", true);

        config.CommitAll();
        config.Save();

        SetupCompleted?.Invoke();
        await Task.CompletedTask;
    }

    private void UpdatePageTitle()
    {
        PageTitle = CurrentPage switch
        {
            0 => "Welcome to LunaForge",
            1 => "Step 1: Setting a projects folder",
            2 => "Step 2: Setting up a project author",
            3 => "Done!",
            _ => "Getting Started"
        };
    }

    public bool IsLastPage => CurrentPage == PageCount - 1;
    public bool IsFirstPage => CurrentPage == 0;
    public string NextButtonText => IsLastPage ? "Finish" : "Next";
}
