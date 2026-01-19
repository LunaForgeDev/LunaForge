using LunaForge.Backend.Attributes.TreeNodesAttributes;
using LunaForge.Models;
using LunaForge.Models.Documents;
using LunaForge.Models.TreeNodes;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LunaForge.Services;

public class ExportedSymbol
{
    public string Name { get; set; }
    public string SourceFile { get; set; }
    public SymbolType Type { get; set; }
    public List<string> Parameters { get; set; } = [];
    public DateTime LastModified { get; set; }
}

/// <summary>
/// TODO: Replace this with all the available class types from plugins.
/// </summary>
public enum SymbolType
{
    Object,
    Function,
}

public struct SymbolIndexChangedEventArgs
{
    public string? FilePath { get; set; }
    public ChangeType ChangeType { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum ChangeType
{
    Modified,
    Removed,
    FullRebuild,
}

public class ProjectSymbolIndexService : IDisposable
{
    private static readonly ILogger Logger = CoreLogger.Create("SymbolIndexing");

    private readonly Project project;
    private readonly string indexFilePath;

    private readonly ConcurrentDictionary<string, List<ExportedSymbol>> fileExports = [];
    private readonly ConcurrentDictionary<string, List<string>> fileDependencies = [];
    private readonly ConcurrentDictionary<string, DateTime> fileModificationTimes = [];

    private FileSystemWatcher? fileWatcher;
    private readonly HashSet<string> pendingUpdates = [];
    private readonly object updateLock = new();
    private readonly SemaphoreSlim saveLock = new(1, 1);
    private bool saveScheduled;

    public event EventHandler<SymbolIndexChangedEventArgs>? SymbolIndexChanged;

    public ProjectSymbolIndexService(Project project)
    {
        this.project = project;
        indexFilePath = Path.Combine(project.DotFolder, "symbol_index.json");

        InitializeFileWatcher();
    }

    private void InitializeFileWatcher()
    {
        fileWatcher = new(project.ProjectRoot)
        {
            Filter = ".lfd",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
        };

        fileWatcher.Changed += OnFileChanged;
        fileWatcher.Created += OnFileChanged;
        fileWatcher.Deleted += OnFileDeleted;
        fileWatcher.Renamed += OnFileRenamed;

        Logger.Information($"File system watcher initialized for project {project.Name}");
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        lock (updateLock)
            pendingUpdates.Add(e.FullPath);

        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            await ProcessPendingUpdatesAsync();
        });
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        string relativePath = Path.GetRelativePath(project.ProjectRoot, e.FullPath);

        fileExports.TryRemove(relativePath, out _);
        fileDependencies.TryRemove(relativePath, out _);
        fileModificationTimes.TryRemove(relativePath, out _);

        SaveIndex();
        NotifySymbolIndexChanged(relativePath, ChangeType.Removed);

        Logger.Information($"File removed from index: {relativePath}");
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        var oldRelativePath = Path.GetRelativePath(project.ProjectRoot, e.OldFullPath);
        var newRelativePath = Path.GetRelativePath(project.ProjectRoot, e.FullPath);

        if (fileExports.TryRemove(oldRelativePath, out var exports))
            fileExports[newRelativePath] = exports;

        if (fileDependencies.TryRemove(oldRelativePath, out var deps))
            fileDependencies[newRelativePath] = deps;

        if (fileModificationTimes.TryRemove(oldRelativePath, out var modTime))
            fileModificationTimes[newRelativePath] = modTime;

        SaveIndex();
        NotifySymbolIndexChanged(newRelativePath, ChangeType.Modified);

        Logger.Information($"File renamed in index: {oldRelativePath} -> {newRelativePath}");
    }

    private async Task ProcessPendingUpdatesAsync()
    {
        List<string> toProcess;

        lock (updateLock)
        {
            toProcess = [.. pendingUpdates];
            pendingUpdates.Clear();
        }

        foreach (string filePath in toProcess)
            if (File.Exists(filePath))
                await IndexFileAsync(filePath, true);
    }

    public async Task RebuildIndexAsync()
    {
        Logger.Information("Rebuilding project symbol index...");

        fileExports.Clear();
        fileDependencies.Clear();
        fileModificationTimes.Clear();

        var ldfFiles = Directory.GetFiles(project.ProjectRoot, "*.lfd", SearchOption.AllDirectories);
        foreach (var filePath in ldfFiles)
            await IndexFileAsync(filePath);

        SaveIndex();
        NotifySymbolIndexChanged(null, ChangeType.FullRebuild);

        Logger.Information($"Index rebuilt: {fileExports.Count} files indexed.");
    }

    public async Task IndexFileAsync(string filePath, bool fromWatcher = false)
    {
        try
        {
            string relativePath = Path.GetRelativePath(project.ProjectRoot, filePath);
            DateTime lastWriteTime = File.GetLastWriteTime(filePath);

            if (fileModificationTimes.TryGetValue(relativePath, out var cachedTime))
            {
                if (lastWriteTime <= cachedTime && !fromWatcher)
                {
                    Logger.Debug($"File not modified. Skipping '{relativePath}'");
                    return;
                }
            }

            var doc = GetOpenedDocument(filePath) ?? DocumentFileLFD.Load(filePath);
            if (doc == null)
                return;

            var exports = ExtractExportsFromDocument(doc);

            fileExports[relativePath] = exports;
            fileDependencies[relativePath] = doc.Dependencies ?? [];
            fileModificationTimes[relativePath] = lastWriteTime;

            ScheduleDebouncedSave();
            NotifySymbolIndexChanged(relativePath, ChangeType.Modified);

            Logger.Debug($"Index file: {relativePath} ({exports.Count} exports)");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to index file '{filePath}': {ex.Message}");
        }

        await Task.CompletedTask;
    }

    public async Task IndexOpenedDocumentAsync(DocumentFileLFD doc)
    {
        try
        {
            if (string.IsNullOrEmpty(doc.FilePath))
                return;

            var relativePath = Path.GetRelativePath(project.ProjectRoot, doc.FilePath);
            var exports = ExtractExportsFromDocument(doc);

            fileExports[relativePath] = exports;
            fileDependencies[relativePath] = doc.Dependencies ?? [];
            fileModificationTimes[relativePath] = DateTime.Now;

            ScheduleDebouncedSave();
            NotifySymbolIndexChanged(relativePath, ChangeType.Modified);

            Logger.Debug($"Indexed opened document: {relativePath} ({exports.Count} exports)");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to index opened document: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private void ScheduleDebouncedSave()
    {
        lock (updateLock)
        {
            if (saveScheduled)
                return;
            saveScheduled = true;
        }

        _ = Task.Run(async () =>
        {
            await Task.Delay(300); // Debounce delay

            lock (updateLock)
                saveScheduled = false;

            await SaveIndexAsync();
        });
    }

    private async Task SaveIndexAsync()
    {
        await saveLock.WaitAsync();
        try
        {
            var index = new
            {
                LastUpdated = DateTime.Now,
                FileExports = fileExports.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                FileDependencies = fileDependencies.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                FileModificationTimes = fileModificationTimes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            };

            string json = JsonConvert.SerializeObject(index, Formatting.Indented);
            await File.WriteAllTextAsync(indexFilePath, json);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save symbol index: {ex.Message}");
        }
        finally
        {
            saveLock.Release();
        }
    }

    private DocumentFileLFD? GetOpenedDocument(string filePath)
    {
        if (project.Files == null)
            return null;

        foreach (var file in project.Files)
            if (file is DocumentFileLFD lfdFile && Path.GetFullPath(file.FilePath) == Path.GetFullPath(filePath))
                return lfdFile;

        return null;
    }

    private List<ExportedSymbol> ExtractExportsFromDocument(DocumentFileLFD doc)
    {
        var exports = new List<ExportedSymbol>();

        if (doc.Exports != null && doc.Exports.Count > 0)
        {
            foreach (var exportName in doc.Exports)
            {
                exports.Add(new()
                {
                    Name = exportName,
                    SourceFile = doc.FilePath,
                    Type = GuessSymbolType(exportName),
                    LastModified = DateTime.Now,
                });
            }
            return exports;
        }

        foreach (var rootNode in doc.TreeNodes)
            ExtractExportsFromNode(rootNode, ref exports, doc.FilePath);

        return exports;
    }

    private void ExtractExportsFromNode(TreeNode node, ref List<ExportedSymbol> exports, string sourceFile)
    {
        var nodeName = node.NodeName;

        if (IsExportableNode(node))
        {
            exports.Add(new()
            {
                Name = nodeName,
                SourceFile = sourceFile,
                Type = DetermineSymbolTypeFromNode(node),
                LastModified = DateTime.Now
            });
        }

        foreach (var child in node.Children)
            ExtractExportsFromNode(child, ref exports, sourceFile);
    }

    private bool IsExportableNode(TreeNode node)
    {
        Type nodeType = node.GetType();
        return nodeType.IsDefined(typeof(ClassAttribute), false);
    }

    private SymbolType DetermineSymbolTypeFromNode(TreeNode node)
    {
        // TODO: Get from attribute
        return SymbolType.Object;
    }

    private SymbolType GuessSymbolType(string name)
    {
        // TODO: Rework this
        if (char.IsUpper(name[0]))
            return SymbolType.Object;
        return SymbolType.Function;
    }

    public List<ExportedSymbol> GetAvailableSymbols(string filePath)
    {
        return null;
    }


    public List<ExportedSymbol> GetLibrarySymbols()
    {
        return null;
    }

    private void SaveIndex()
    {
        saveLock.Wait();
        try
        {
            var index = new
            {
                LastUpdated = DateTime.Now,
                FileExports = fileExports.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                FileDependencies = fileDependencies.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                FileModificationTimes = fileModificationTimes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            };

            string json = JsonConvert.SerializeObject(index, Formatting.Indented);
            File.WriteAllText(indexFilePath, json);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save symbol index: {ex.Message}");
        }
        finally
        {
            saveLock.Release();
        }
    }

    public void LoadIndex()
    {
        try
        {
            if (!File.Exists(indexFilePath))
                return;

            var json = File.ReadAllText(indexFilePath);
            var index = JsonConvert.DeserializeAnonymousType(json, new
            {
                LastUpdated = DateTime.Now,
                FileExports = new Dictionary<string, List<ExportedSymbol>>(),
                FileDependencies = new Dictionary<string, List<string>>(),
                FileModificationTimes = new Dictionary<string, DateTime>()
            });

            if (index != null)
            {
                foreach (var kvp in index.FileExports)
                    fileExports[kvp.Key] = kvp.Value;

                foreach (var kvp in index.FileDependencies)
                    fileDependencies[kvp.Key] = kvp.Value;

                foreach (var kvp in index.FileModificationTimes)
                    fileModificationTimes[kvp.Key] = kvp.Value;

                Logger.Information($"Symbol index loaded: {fileExports.Count} files");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load symbol index: {ex.Message}");
        }
    }

    public List<ExportedSymbol> SearchSymbols(string query)
    {
        var results = new List<ExportedSymbol>();

        foreach (var exports in fileExports.Values)
            results.AddRange(exports.Where(e => e.Name.Contains(query, StringComparison.OrdinalIgnoreCase)));

        return results;
    }

    private void NotifySymbolIndexChanged(string? filePath, ChangeType changeType)
    {
        SymbolIndexChanged?.Invoke(this, new()
        {
            FilePath = filePath,
            ChangeType = changeType,
            Timestamp = DateTime.Now,
        });
    }

    public void Dispose()
    {
        fileWatcher?.Dispose();
    }
}
