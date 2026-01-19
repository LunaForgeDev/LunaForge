using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaForge.Models;
using LunaForge.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace LunaForge.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public event Action? RequestClose;

    [ObservableProperty]
    private string projectsFolder;
    [ObservableProperty]
    private int codeIndentSpaces;
    [ObservableProperty]
    private bool enableDiscordRPC;
    [ObservableProperty]
    private string marketplaceRepoServer;
    [ObservableProperty]
    private string defaultProjectAuthor;
    [ObservableProperty]
    private string defaultLuaSTGExecutablePath;
    [ObservableProperty]
    private string defaultCompilationTarget;
    [ObservableProperty]
    private string projectAuthor;
    [ObservableProperty]
    private string luaSTGExecutablePath;
    [ObservableProperty]
    private string compilationTarget;

    [ObservableProperty]
    private bool isProjectLoaded;

    public SettingsViewModel()
    {
        IsProjectLoaded = MainWindowModel.Project != null;
        LoadSettings();
    }

    private void LoadSettings()
    {
        ProjectsFolder = EditorConfig.Default.Get<string>("ProjectsFolder").Value;
        CodeIndentSpaces = EditorConfig.Default.Get<int>("CodeIndentSpaces").Value;
        EnableDiscordRPC = EditorConfig.Default.Get<bool>("UseDiscordRPC").Value;
        MarketplaceRepoServer = EditorConfig.Default.Get<string>("TemplateServerUrl").Value;
        DefaultProjectAuthor = EditorConfig.Default.Get<string>("ProjectAuthor").Value;
        DefaultLuaSTGExecutablePath = EditorConfig.Default.Get<string>("LuaSTGExecutablePath").Value;
        DefaultCompilationTarget = EditorConfig.Default.Get<string>("CompilationTarget").Value;

        if (IsProjectLoaded)
        {
            ProjectAuthor = MainWindowModel.Project!.ProjectConfig.Get<string>("ProjectAuthor", ConfigSystemCategory.CurrentProject).Value;
            LuaSTGExecutablePath = MainWindowModel.Project!.ProjectConfig.Get<string>("LuaSTGExecutablePath", ConfigSystemCategory.CurrentProject).Value;
            CompilationTarget = MainWindowModel.Project!.ProjectConfig.Get<string>("CompilationTarget", ConfigSystemCategory.CurrentProject).Value;
        }
    }

    private void SaveSettings()
    {
        EditorConfig.Default.Set("ProjectsFolder", ProjectsFolder);
        EditorConfig.Default.Set("CodeIndentSpaces", CodeIndentSpaces);
        EditorConfig.Default.Set("UseDiscordRPC", EnableDiscordRPC);
        EditorConfig.Default.Set("TemplateServerUrl", MarketplaceRepoServer);
        EditorConfig.Default.Set("ProjectAuthor", DefaultProjectAuthor);
        EditorConfig.Default.Set("LuaSTGExecutablePath", DefaultLuaSTGExecutablePath);
        EditorConfig.Default.Set("CompilationTarget", DefaultCompilationTarget);

        EditorConfig.Default.CommitAllAndSave();

        if (IsProjectLoaded)
        {
            MainWindowModel.Project!.ProjectConfig.Set("ProjectAuthor", ProjectAuthor);
            MainWindowModel.Project!.ProjectConfig.Set("LuaSTGExecutablePath", LuaSTGExecutablePath);
            MainWindowModel.Project!.ProjectConfig.Set("CompilationTarget", CompilationTarget);

            MainWindowModel.Project!.ProjectConfig.CommitAllAndSave();
        }
    }

    #region Commands

    [RelayCommand]
    private void ApplyAndClose()
    {
        SaveSettings();
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Apply()
    {
        SaveSettings();
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void OpenLSTGPath(bool setDefault)
    {
        OpenFileDialog dialog = new()
        {
            Filter = "LuaSTG Engine|*.exe|All Files|*.*",
            Multiselect = false,
        };

        if (dialog.ShowDialog() == true)
        {
            string result = dialog.FileName;
            if (setDefault)
                DefaultLuaSTGExecutablePath = result;
            else
                LuaSTGExecutablePath = result;
        }
    }

    [RelayCommand]
    private void OpenProjectsFolderPath()
    {
        OpenFolderDialog dialog = new()
        {
            Multiselect = false,
        };

        if (dialog.ShowDialog() == true)
        {
            string result = dialog.FolderName;
            ProjectsFolder = result;
        }
    }

    #endregion
}
