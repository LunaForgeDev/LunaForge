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
    private ConcurrentDictionary<string, string> computedImageMD5 = []; // Separate MD5 tracking for images (never cached, copied directly to output).
    private string MD5HashesFilePath => Path.Combine(proj.DotFolder, "hashes_cache.json");
    private string ImageMD5HashesFilePath => Path.Combine(proj.DotFolder, "hashes_images_cache.json");
    private string CompileCache => Path.Combine(proj.DotFolder, "compilecache");

    public ProjectCompilerService(Project _proj)
    {
        proj = _proj;
    }

    private void CompiledFileHeader(ref StringBuilder sb, string relativePath)
    {
        sb.AppendLine($"-- Generated from {relativePath} by LunaForge {App.AppVersion}");
        sb.AppendLine($"-- Compiled at {DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy")} (dd/mm/yyyy)");
        sb.AppendLine();
    }

    public async Task<bool> Compile()
    {
        return await Compile(null, false);
    }

    /*
     * TODO: (Compile TODOs)
     * - Allow selecting project packing type (plain or zip) in settings.
     * - Compile LFS.
     * - Gather all lua files as well as lfs and lfd.
     */

    private static readonly HashSet<string> ImageExtensions = [".png", ".jpg", ".jpeg", ".webp"]; //Supported for lstg.
    public async Task<bool> Compile(ICompilationTarget? target = null, bool forceRepack = false)
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

        if (TraceService.Instance.ErrorCount != 0)
        {
            Logger.Error("There are errors in your project. Please fix them before compiling.");
            MessageBox.Show("There are errors in your project. Please fix them before compiling.", "Compilation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        // If "main.lua" or "main.lfd" doesn't exist somewhere in the project, that's a fatal compiler error.
        // This should be handled by the trace system but this is doubled checked just in case the trace system didn't catch it.
        if (!CheckForEntryPoint())
            return false;
        Directory.CreateDirectory(CompileCache);

        // -------------------------------------------- Step 2: Call PreCompile on plugin target
        target.PreCompile(CompileCache);

        // -------------------------------------------- Step 3: Collect everything and separate lfd and lfs files

        // List of all files, key is ".*".
        Dictionary<string, List<string>>? fileTypes = CollectFiles(proj.ProjectRoot);
        if (fileTypes == null)
        {
            Logger.Error("Couldn't collect files for compilation.");
            return false;
        }
        if (fileTypes.TryGetValue(".lfd", out var lfdFiles))
            Logger.Information("Found {0} lfd files", lfdFiles.Count);
        if (fileTypes.TryGetValue(".lfs", out var lfsFiles))
            Logger.Information("Found {0} lfs files", lfsFiles.Count);
        List<string> imageFiles = [.. fileTypes.Where(kv => ImageExtensions.Contains(kv.Key)).SelectMany(kv => kv.Value)];
        Logger.Information("Found {0} image files", imageFiles.Count);
        if (forceRepack)
        {
            Logger.Information("Forcing a repack. Deleting file meta infos.");
            if (Directory.Exists(CompileCache))
                Directory.Delete(CompileCache, recursive: true);
            computedMD5 = [];
            computedImageMD5 = [];
        }
        else
        {
            computedMD5 = ReadMD5Hashes();
            computedImageMD5 = ReadImageMD5Hashes();
        }

        HashSet<string> currentImagePaths = [.. imageFiles];
        foreach (string trackedImage in computedImageMD5.Keys)
        {
            if (!currentImagePaths.Contains(trackedImage))
                Logger.Warning("Image '{0}' was previously tracked but no longer exists in the project. It will be missing from the output.", trackedImage);
        }
        //Remove orphan md5
        foreach (string stale in computedImageMD5.Keys.Except(currentImagePaths).ToList())
            computedImageMD5.TryRemove(stale, out _);

        // -------------------------------------------- Step 4: Compile everything
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
            if (forceRepack && Directory.Exists(output))
            {
                Logger.Information("Force repack: deleting existing output folder '{0}'.", output);
                Directory.Delete(output, recursive: true);
            }
            Directory.Copy(CompileCache, output);
            CopyImagesDirect(imageFiles, output, forceRepack);
            Logger.Information("Mod folder created at {0}", output);
        }
        else
        {
            Logger.Debug("Compiling to zip...");
            // Zip file
            string outputDir = Path.Combine(Path.GetDirectoryName(proj.ProjectConfig.Get<string>("LuaSTGExecutablePath").Value), target.BuildDirectory);
            if (!Directory.Exists(outputDir))
            {
                Logger.Error("Output directory doesn't exist. As a matter of null-security, it will not be created for you.");
                Logger.Error("Output directory doesn't exist: {0}", outputDir);
                return false;
            }
            string output = Path.Combine(outputDir, $"{proj.Name}.zip");
            if (forceRepack && File.Exists(output))
            {
                Logger.Information("Force repack: deleting existing zip '{0}'.", output);
                File.Delete(output);
            }
            using (ZipArchive archive = File.Exists(output)
                ? ZipFile.Open(output, ZipArchiveMode.Update)
                : ZipFile.Open(output, ZipArchiveMode.Create))
            {
                foreach (string cacheFile in Directory.GetFiles(CompileCache, "*", SearchOption.AllDirectories))
                {
                    string entryName = Path.GetRelativePath(CompileCache, cacheFile).Replace('\\', '/');
                    archive.GetEntry(entryName)?.Delete();
                    archive.CreateEntryFromFile(cacheFile, entryName);
                }
            }
            AddImagesToZip(imageFiles, output, forceRepack);
            Logger.Information("Zip mod created at {0}", output);
        }

        // -------------------------------------------- Step 6: Put md5 hashes inside the dotfolder for next time.
        SaveMD5Hashes();
        SaveImageMD5Hashes();
        Logger.Debug($"MD5 saved.");

        await Task.WhenAll();
        Logger.Information($"============================ End of Compilation");
        return true;
    }

    private bool CheckForEntryPoint()
    {
        string[] entryPoints = [.. Directory.GetFiles(proj.ProjectRoot, "main.*", SearchOption.AllDirectories)
            .Where(f => !f.Contains(".lunaforge") || !f.Contains(".git"))
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

    private Dictionary<string, List<string>>? CollectFiles(string folder)
    {
        try
        {
            Dictionary<string, List<string>> results = [];

            string[] fileNames = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
            // Ignore dotfolder since it contains metadata and cache, as well as the project file itself.
            foreach (string file in fileNames.Where(f => !f.Contains(".lunaforge") || !f.Contains(".git") || Path.GetExtension(f) != ".lfp"))
            {
                string ext = Path.GetExtension(file);
                if (results.TryGetValue(ext, out var list))
                {
                    list.Add(file);
                    results[ext] = list;
                }
                else
                {
                    list = [file];
                    results[ext] = list;
                }
            }
            return results;
        }
        catch (Exception ex)
        {
            Logger.Error($"Couldn't get files. Reason:\n{ex}");
            return null;
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
            CompiledFileHeader(ref sb, relativePath);

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
        // lfs are not implemented in the alpha release.

        string relativePath = Path.GetRelativePath(proj.ProjectRoot, lfsFilePath);
        string relativeLuaPath = Path.ChangeExtension(relativePath, ".hlsl");

        StringBuilder sb = new();
        CompiledFileHeader(ref sb, relativePath);

        return false;
    }

    private void CopyImagesDirect(List<string> imageFiles, string outputFolder, bool force = false)
    {
        foreach (string imgPath in imageFiles)
        {
            string relativePath = Path.GetRelativePath(proj.ProjectRoot, imgPath);
            string destPath = Path.Combine(outputFolder, relativePath);
            string? currentMD5 = ComputeImageMD5(imgPath);
            if (!force
                && currentMD5 != null
                && computedImageMD5.TryGetValue(imgPath, out string? cachedMD5)
                && cachedMD5 == currentMD5
                && File.Exists(destPath))
            {
                Logger.Verbose("Skipped unchanged image {0}", relativePath);
                continue;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Copy(imgPath, destPath, overwrite: true);
            if (currentMD5 != null)
                computedImageMD5[imgPath] = currentMD5;
            Logger.Verbose("Copied image {0}", relativePath);
        }
    }

    private void AddImagesToZip(List<string> imageFiles, string zipPath, bool force = false)
    {
        using ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Update);
        foreach (string imgPath in imageFiles)
        {
            string relativePath = Path.GetRelativePath(proj.ProjectRoot, imgPath);
            string entryName = relativePath.Replace('\\', '/');
            string? currentMD5 = ComputeImageMD5(imgPath);
            if (!force
                && currentMD5 != null
                && computedImageMD5.TryGetValue(imgPath, out string? cachedMD5)
                && cachedMD5 == currentMD5
                && archive.GetEntry(entryName) != null)
            {
                Logger.Verbose("Skipped unchanged image {0}", relativePath);
                continue;
            }
            archive.GetEntry(entryName)?.Delete();
            archive.CreateEntryFromFile(imgPath, entryName, CompressionLevel.NoCompression);
            if (currentMD5 != null)
                computedImageMD5[imgPath] = currentMD5;
            Logger.Verbose("Added image to zip {0}", relativePath);
        }
    }

    private string? ComputeImageMD5(string imagePath)
    {
        try
        {
            using FileStream fs = new(imagePath, FileMode.Open, FileAccess.Read);
            var md5 = System.Security.Cryptography.MD5.Create();
            return Encoding.Default.GetString(md5.ComputeHash(fs));
        }
        catch (Exception ex)
        {
            Logger.Warning("Couldn't compute MD5 for image '{0}': {1}", imagePath, ex.Message);
            return null;
        }
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

    private ConcurrentDictionary<string, string> ReadImageMD5Hashes()
    {
        try
        {
            if (!File.Exists(ImageMD5HashesFilePath))
                return [];

            string contents = File.ReadAllText(ImageMD5HashesFilePath, Encoding.UTF8);
            var dict = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(contents);
            return dict != null ? new(dict) : [];
        }
        catch (Exception ex)
        {
            Logger.Error($"Couldn't read image md5 hash file. Reason:\n{ex}");
            return [];
        }
    }

    private void SaveImageMD5Hashes()
    {
        try
        {
            string json = JsonConvert.SerializeObject(computedImageMD5, Formatting.Indented);
            File.WriteAllText(ImageMD5HashesFilePath, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Logger.Error($"Couldn't save image md5 hash file. Reason:\n{ex}");
        }
    }
}
