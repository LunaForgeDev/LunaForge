using LunaForge.Models;
using LunaForge.Models.Documents;
using LunaForge.Models.TreeNodes;
using LunaForge.Plugins;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;

namespace LunaForge.Services;

public class ProjectCompilerService
{
    private static readonly ILogger Logger = CoreLogger.Create("Compile");
    private Project proj { get; set; }

    private ConcurrentDictionary<string, string> computedMD5 = []; // If the md5 of a file changed or was created, this will be put inside the file.
    private string MD5HashesFilePath => Path.Combine(proj.DotFolder, "hashes_cache.json");
    private string CompileCache => Path.Combine(proj.DotFolder, "compilecache");

    public ProjectCompilerService(Project _proj)
    {
        proj = _proj;
    }

    public async Task<bool> Compile()
    {
        return await Compile(null);
    }

    /*
     * TODO:
     * - See for compiling images in a different way since it would use a lot of space to copy images into the compile cache.
     * - Allow selecting Compile Targets in the settings as well as project packing type (plain or zip)
     * - Compile LFS
     * - Gather all lua files as well as lfs and lfd
     */
    public async Task<bool> Compile(ICompilationTarget? target = null)
    {
        Logger.Information($"============================ Starting Compilation");
        if (target == null)
        {
            string targetName = proj.ProjectConfig.Get<string>("CompilationTarget").Value;
            target = proj.PluginManager?.GetCompilationTarget(targetName);
            if (target == null)
            {
                Logger.Error($"Cannot compile the project '{proj.Name}'. No target selected.");
                MessageBox.Show("No compiliation target selected. See your project settings.", "Compilation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        Logger.Information($"Compilation target: '{target.TargetName}' building in '{target.BuildDirectory}'");

        // -------------------------------------------- Step 1: Commit changes. Test for editor errors, and do other shit.
        // If "main.lua" or "main.lfd" doesn't exist somewhere in the project, that's a fatal compiler error.
        if (!CheckForEntryPoint())
            return false;
        Directory.CreateDirectory(CompileCache);

        // -------------------------------------------- Step 2: Call PreCompile on plugin target
        target.PreCompile(CompileCache);

        // -------------------------------------------- Step 3: Collect everything and separate lfd and lfs files
        List<string> files = [];
        if (!CollectFiles("*.*", ref files, proj.ProjectRoot))
            return false;

        // TODO: Collect all files, copy them except for lfd and lfs (and images?) and compile those in the cache.

        List<string> lfdFiles = [];
        List<string> lfsFiles = [];
        if (!CollectFiles("*.lfd", ref lfdFiles, proj.ProjectRoot))
            return false;
        if (!CollectFiles("*.lfs", ref lfsFiles, proj.ProjectRoot))
            return false;
        Logger.Information("Found {0} lfd files", lfdFiles.Count);
        Logger.Information("Found {0} lfs files", lfsFiles.Count);
        computedMD5 = ReadMD5Hashes();

        // -------------------------------------------- Step 4: Compile everything (compile dependencies before?)
        Directory.CreateDirectory(CompileCache);

        ConcurrentBag<bool> lfdResults = [];
        await Parallel.ForEachAsync(lfdFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (file, ct) =>
        {
            Logger.Verbose("Compiling {0}...", file);
            bool success = await CompileLFDFile(file);
            Logger.Verbose("Compiling {0} {1}", file, success ? "SUCCESS" : "FAILED");
            lfdResults.Add(success);
        });

        if (lfdResults.Any(r => !r))
            return false;
        Logger.Information("Compiled {0} lfd files", lfdResults.Count);

        // -------------------------------------------- Step 5: Check if the editor must copy all files and folders or zip the contents and move the zip.
        bool usePlainFiles = proj.ProjectConfig.Get<bool>("UsePlainFilesPackaging").Value;
        if (usePlainFiles)
        {
            // Plain files/folders
            Logger.Debug("Compiling to plain files...");
            string output = Path.Combine(Path.GetDirectoryName(proj.ProjectConfig.Get<string>("LuaSTGExecutablePath").Value), target.BuildDirectory, proj.Name);
            Directory.Copy(CompileCache, output);
            Logger.Information("Mod folder created at {0}", output);
        }
        else
        {
            Logger.Debug("Compiling to zip...");
            // Zip file
            string output = Path.Combine(Path.GetDirectoryName(proj.ProjectConfig.Get<string>("LuaSTGExecutablePath").Value), target.BuildDirectory);
            if (!Directory.Exists(output))
            {
                Logger.Error("Output directory doesn't exist. As a matter of null-security, it will not be created for you.");
                Logger.Error("Output directory doesn't exist: {0}", output);
                return false;
            }
            output = Path.Combine(output, $"{proj.Name}.zip");
            if (File.Exists(output))
                File.Delete(output); // ZipFile doesn't know how to overwrite files.
            ZipFile.CreateFromDirectory(CompileCache, output);
            Logger.Information("Zip mod created at {0}", output);
        }

        // -------------------------------------------- Step 6: Put md5 hashes inside the dotfolder for next time.
        SaveMD5Hashes();
        Logger.Debug($"MD5 saved.");

        await Task.WhenAll();
        Logger.Information($"============================ End of Compilation");
        return true;
    }

    private bool CheckForEntryPoint()
    {
        string[] entryPoints = [.. Directory.GetFiles(proj.ProjectRoot, "main.*", SearchOption.AllDirectories)
            .Where(f => !f.Contains(".lunaforge"))
            .Where(f => f.EndsWith("main.lfd", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith("main.lua", StringComparison.OrdinalIgnoreCase))];

        if (entryPoints.Length == 0)
        {
            Logger.Error("No entry point 'main.lfd' or 'main.lua' found in project '{0}'.", proj.Name);
            return false;
        }

        Logger.Debug("Entry point found: '{0}'", entryPoints[0]);
        return true;
    }

    private bool CollectFiles(string extension, ref List<string> files, string folder)
    {
        try
        {
            string[] fileNames = Directory.GetFiles(folder, extension, SearchOption.AllDirectories);
            files.AddRange(fileNames.Where(f => !f.Contains(".lunaforge"))); // Ignore dotfolder since it contains metadata and cache.
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Couldn't get files. Reason:\n{ex}");
            return false;
        }
    }

    private async Task<bool> CompileLFDFile(string lfdFilePath, bool showOnly = false)
    {
        try
        {
            using FileStream fs = new(lfdFilePath, FileMode.Open, FileAccess.Read);
            var md5 = System.Security.Cryptography.MD5.Create();
            string md5str = Encoding.Default.GetString(md5.ComputeHash(fs));
            string relativePath = Path.GetRelativePath(proj.ProjectRoot, lfdFilePath);
            string relativeLuaPath = Path.ChangeExtension(relativePath, ".lua");
            string cachePath = Path.Combine(CompileCache, relativeLuaPath);

            if (computedMD5.TryGetValue(lfdFilePath, out string? cachedMD5) && cachedMD5 == md5str && File.Exists(cachePath))
                return true;

            DocumentFileLFD file = DocumentFileLFD.Load(lfdFilePath);
            if (file == null)
            {
                Logger.Error($"Couldn't load lfd file '{lfdFilePath}' for compilation.");
                return false;
            }
            
            Logger.Debug("Created lua from lfd at {0}", cachePath);

            StringBuilder sb = new();

            sb.AppendLine($"-- Generated from {relativePath} by LunaForge {App.AppVersion}");
            sb.AppendLine($"-- Compiled at {DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy")} (dd/mm/yyyy)");
            sb.AppendLine();

            foreach (TreeNode node in file.TreeNodes)
                foreach (var line in node.ToLua(0))
                    sb.Append(line);

            computedMD5[lfdFilePath] = md5str;

            if (!showOnly)
            {
                //Write output to cache (create folder if needed)
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
                File.WriteAllText(cachePath, sb.ToString(), Encoding.UTF8);
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Couldn't compile file '{lfdFilePath}'. Reason:\n{ex}");
            return false;
        }
    }

    private async Task<bool> CompileLFSFile(string lfsFilePath, bool showOnly = false)
    {
        return false;
    }

    private ConcurrentDictionary<string, string> ReadMD5Hashes()
    {
        try
        {
            if (!File.Exists(MD5HashesFilePath)) // Runa you absolute idiot, of course this was gonna cause issues if the file doesn't exist.
                return [];

            string contents = File.ReadAllText(MD5HashesFilePath, Encoding.UTF8);
            var dict = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(contents);
            return dict != null ? new(dict) : [];
        }
        catch (Exception ex)
        {
            Logger.Error($"Couldn't read md5 file hash file. Reason:\n{ex}");
            return [];
        }
    }

    private void SaveMD5Hashes()
    {
        try
        {
            string json = JsonConvert.SerializeObject(computedMD5, Formatting.Indented);
            File.WriteAllText(MD5HashesFilePath, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Logger.Error($"Couldn't save md5 file hash file. Reason:\n{ex}");
        }
    }
}
