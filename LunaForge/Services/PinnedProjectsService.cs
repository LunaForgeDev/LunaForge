using LunaForge.Backend.Enums;
using LunaForge.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunaForge.Services;

public static class PinnedProjectsService
{
    public static List<string> LoadPinnedProjects()
    {
        try
        {
            var pinnedProjectsEntry = EditorConfig.Default.Get<string>(
                nameof(BaseConfigEnum.PinnedProjects));

            if (string.IsNullOrWhiteSpace(pinnedProjectsEntry.Value))
                return [];

            return [.. pinnedProjectsEntry.Value.Split(';', StringSplitOptions.RemoveEmptyEntries)];
        }
        catch { return []; }
    }

    public static async Task TogglePinProject(string projectPath)
    {
        try
        {
            var pinnedProjects = LoadPinnedProjects();

            if (pinnedProjects.Contains(projectPath))
                pinnedProjects.Remove(projectPath);
            else
                pinnedProjects.Add(projectPath);

            string pinnedProjectsStr = string.Join(';', pinnedProjects);
            EditorConfig.Default.Set(
                nameof(BaseConfigEnum.PinnedProjects),
                pinnedProjectsStr);

            EditorConfig.Default.CommitAllAndSave();

            await Task.CompletedTask;
        }
        catch { }
    }

    public static void UpdateProjectAccessTime(string projectPath)
    {
        try
        {
            if (File.Exists(projectPath))
                File.SetLastAccessTime(projectPath, DateTime.Now);
        }
        catch { }
    }

    public static async Task PinProject(string projectPath)
    {
        var pinnedProjects = LoadPinnedProjects();
        if (!pinnedProjects.Contains(projectPath))
        {
            pinnedProjects.Add(projectPath);
            string pinnedProjectsStr = string.Join(';', pinnedProjects);
            EditorConfig.Default.Set(
                nameof(BaseConfigEnum.PinnedProjects),
                pinnedProjectsStr);
            EditorConfig.Default.CommitAllAndSave();
        }
        await Task.CompletedTask;
    }

    public static async Task UnpinProject(string projectPath)
    {
        var pinnedProjects = LoadPinnedProjects();
        if (pinnedProjects.Contains(projectPath))
        {
            pinnedProjects.Remove(projectPath);
            string pinnedProjectsStr = string.Join(";", pinnedProjects);
            EditorConfig.Default.Set(
                nameof(BaseConfigEnum.PinnedProjects),
                pinnedProjectsStr);
            EditorConfig.Default.CommitAllAndSave();
        }
        await Task.CompletedTask;
    }
}
