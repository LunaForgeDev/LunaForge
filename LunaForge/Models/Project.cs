using LunaForge.Models.Documents;
using LunaForge.Plugins;
using LunaForge.Services;
using LunaForge.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tomlyn.Model;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge.Models;

public class Project : IDisposable
{
    public static ILogger Logger = CoreLogger.Create("Project");

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name => ProjectConfig.Get<string>("ProjectName").Value;
    public string Version { get; set; } = "1.0.0";
    public string Author => ProjectConfig.Get<string>("ProjectAuthor").Value;
    public FileCollection Files { get; set; } = [];

    public string ProjectRoot { get; private set; } = string.Empty;
    public string ProjectFile => Path.Combine(ProjectRoot, $"{Name}.lfp");
    public string DotFolder => Path.Combine(ProjectRoot, ".lunaforge");

    public PluginManager? PluginManager { get; private set; }
    public ToolboxService? ToolboxService { get; private set; }

    public ConfigSystem ProjectConfig = new();

    public Project()
    { }

    public Project(string folder)
    {
        ProjectRoot = folder;
    }

    public static Project CreateEmpty() => new();

    public static Project CreateNew(string folder, string author, string? templatePath = null)
    {
        Project proj = new(folder);

        Directory.CreateDirectory(folder);

        ConfigSystem.RegisterBaseConfigs(ref proj.ProjectConfig, ConfigSystemCategory.DefaultProject);
        proj.ProjectConfig.CopyCategory(ConfigSystemCategory.DefaultProject, ConfigSystemCategory.CurrentProject);
        
        string projectName = Path.GetFileName(folder) ?? "Untitled";
        proj.ProjectConfig.Set("ProjectName", projectName);
        proj.ProjectConfig.Set("ProjectAuthor", author);
        proj.ProjectConfig.CommitAll();
        proj.Save();

        // Empty template
        if (string.IsNullOrEmpty(templatePath))
        {
            Directory.CreateDirectory(Path.Combine(folder, "Assets"));
            Directory.CreateDirectory(Path.Combine(folder, "Definitions"));
            Directory.CreateDirectory(Path.Combine(folder, "Scripts"));
            Directory.CreateDirectory(proj.DotFolder).Attributes |= FileAttributes.Hidden | FileAttributes.Directory;
        }
        // Template from zip
        else
        {
            Directory.CreateDirectory(proj.DotFolder).Attributes |= FileAttributes.Hidden | FileAttributes.Directory;

            ZipFile.ExtractToDirectory(templatePath, folder, overwriteFiles: true);
        }

        return proj;
    }

    public ProjectSymbolIndexService? SymbolIndex { get; private set; }

    public async Task InitializePlugins()
    {
        try
        {
            PluginManager = new();

            await PluginManager.LoadAllPlugins();
            PluginManager.EnableHotReload();

            ToolboxService = new ToolboxService(PluginManager);
            ToolboxService.RebuildToolbox();

            SymbolIndex = new(this);
            SymbolIndex.LoadIndex();

            _ = Task.Run(async () => await SymbolIndex.RebuildIndexAsync());
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to initialize plugins. Reason:\n{ex}");
        }
    }

    #region Files

    public DocumentFile CreateFile(string filePath, FileType fileType)
    {
        DocumentFile file = fileType switch
        {
            FileType.Lua => new DocumentFileLua(Path.ChangeExtension(filePath, ".lua")),
            FileType.Lfd => new DocumentFileLFD(Path.ChangeExtension(filePath, ".lfd")),
            FileType.Lfs => new DocumentFileLFS(Path.ChangeExtension(filePath, ".lfs")),
            _ => new DocumentFile(filePath)
        };

        Files.Add(file);
        Logger.Information($"Created new file: {filePath}");

        file.Save();
        
        return file;
    }

    public DocumentFile OpenFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Project file doesn't exist: {filePath}");

        var existingFile = Files.FirstOrDefault(f => filePath.Contains(f.FilePath));
        if (existingFile != null)
        {
            Logger.Information($"File already opened: {filePath}");
            return existingFile;
        }

        DocumentFile documentFile = null;
        string extension = Path.GetExtension(filePath).ToLower();

        try
        {
            documentFile = extension switch
            {
                ".lua" => DocumentFileLua.Load(filePath),
                ".lfd" => DocumentFileLFD.Load(filePath),
                ".lfs" => DocumentFileLFS.Load(filePath),
                _ => new DocumentFile(filePath)
            };

            if (documentFile != null)
            {
                Files.Add(documentFile);
                Logger.Information($"Opened file: {filePath}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to open file '{filePath}'. Reason:\n{ex}");
        }

        return documentFile;
    }

    #endregion
    #region Serialization

    public bool Save()
    {
        try
        {
            SaveOpenedFiles();
            ProjectConfig.Save(ProjectFile);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save project. Reason:\n{ex}");
            return false;
        }
    }

    public static (Project, string) Load(string path)
    {
        try
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Project file doesn't exist: {path}");

            Project proj = new(Path.GetDirectoryName(path)!)
            {
                ProjectConfig = ConfigSystem.Load<ConfigSystem>(path)
            };

            proj.ProjectConfig.CommitAllAndSave();
            proj.LoadOpenedFiles();

            return (proj, "");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load project. Reason:\n{ex}");
            return (null!, ex.ToString());
        }
    }

    private void SaveOpenedFiles()
    {
        try
        {
            if (Files == null || Files.Count == 0)
                return;

            string fileNames = string.Join(";", Files.Select(f => f.FileName));
            ProjectConfig.Set("OpenedFiles", fileNames);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save opened files. Reason:\n{ex}");
        }
    }

    private void LoadOpenedFiles()
    {
        try
        {
            var openedFilesEntry = ProjectConfig.Get<string>("OpenedFiles");

            if (string.IsNullOrWhiteSpace(openedFilesEntry.Value))
                return;

            string[] fileNames = [.. openedFilesEntry.Value.Split(";", StringSplitOptions.RemoveEmptyEntries)];
            Files.Clear();

            foreach (var fileName in fileNames)
            {
                var file = Path.Combine(ProjectRoot, fileName);
                OpenFile(file);
            }

            Logger.Information($"Loaded {fileNames.Length} opened files");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load opened files. Reason:\n{ex}");
        }
    }

    #endregion

    public void Dispose()
    {
        PluginManager?.Dispose();
    }
}