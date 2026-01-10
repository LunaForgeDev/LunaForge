using LunaForge.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LunaForge.Services;

public class RecentFilesService
{
    private static readonly ILogger Logger = CoreLogger.Create("RecentFiles");
    private const int MaxRecentFiles = 10;
    
    public RecentFilesService()
    {
    }
    
    public List<string> GetRecentFiles()
    {
        try
        {
            var recentFilesEntry = MainWindowModel.Project.ProjectConfig.Get<string>("ProjectFilesOpenedRecently", ConfigSystemCategory.CurrentProject);
            
            if (string.IsNullOrWhiteSpace(recentFilesEntry.Value))
                return [];
            
            var files = recentFilesEntry.Value
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Distinct()
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .ToList();
            
            return files;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load recent files. Reason:\n{ex}");
            return [];
        }
    }
    
    public void AddRecentFile(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return;
            
            string relativePath = GetRelativePath(MainWindowModel.Project.ProjectRoot, filePath);
            
            var recentFiles = GetRecentFiles();
            recentFiles.Remove(relativePath);
            recentFiles.Insert(0, relativePath);

            if (recentFiles.Count > MaxRecentFiles)
                recentFiles = [.. recentFiles.Take(MaxRecentFiles)];

            string recentFilesString = string.Join(";", recentFiles);
            MainWindowModel.Project.ProjectConfig.SetOrCreate("ProjectFilesOpenedRecently", recentFilesString, ConfigSystemCategory.CurrentProject);
            MainWindowModel.Project.ProjectConfig.CommitAllAndSave();
            
            Logger.Debug($"Added recent file: {relativePath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to add recent file. Reason:\n{ex}");
        }
    }
    
    public void ClearRecentFiles()
    {
        try
        {
            MainWindowModel.Project.ProjectConfig.SetOrCreate("ProjectFilesOpenedRecently", string.Empty, ConfigSystemCategory.CurrentProject);
            MainWindowModel.Project.ProjectConfig.CommitAllAndSave();
            Logger.Information("Cleared recent files");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to clear recent files. Reason:\n{ex}");
        }
    }

    public void SaveRecentFiles()
    {
        try
        {
            MainWindowModel.Project.ProjectConfig.CommitAllAndSave();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save recent files. Reason:\n{ex}");
        }
    }
    
    private static string GetRelativePath(string relativeTo, string path)
    {
        try
        {
            Uri fromUri = new(AppendDirectorySeparator(relativeTo));
            Uri toUri = new(path);
            
            if (fromUri.Scheme != toUri.Scheme)
                return path;
            
            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());
            
            if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }
            
            return relativePath;
        }
        catch
        {
            return path;
        }
    }
    
    private static string AppendDirectorySeparator(string path)
    {
        if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            return path + Path.DirectorySeparatorChar;
        return path;
    }
}
