using LunaForge.Models;
using LunaForge.Models.Documents;
using LunaForge.Models.TreeNodes;
using LunaForge.Plugins;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace LunaForge.Services;

public class ProjectCompilerService
{
    private static readonly ILogger Logger = CoreLogger.Create("Compile");
    private Project proj { get; set; }

    private Dictionary<string, string> computedMD5 = []; // If the md5 of a file changed or was created, this will be put inside the file.
    private string MD5HashesFilePath => Path.Combine(proj.DotFolder, "hashes_cache.json");
    private SemaphoreSlim semaphore = new(Environment.ProcessorCount);

    private string outputFinalPath = ""; // TODO: Replace with luastg mod path

    public ProjectCompilerService(Project _proj)
    {
        proj = _proj;
    }

    public async Task<bool> Compile()
    {
        return await Compile(null);
    }

    public async Task<bool> Compile(ICompilationTarget? target)
    {
        if (target == null)
        {
            Logger.Error($"Cannot compile the project '{proj.Name}'. No target selected.");
            MessageBox.Show("No compiliation target selected. See your project settings.", "Compilation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        // Step 0: Commit changes. Test for editor errors, and do other shit.

        // Step 1: Collect everything
        List<string> lfdFiles = [];
        if (CollectLFDFiles(ref lfdFiles))
            return false;
        computedMD5 = ReadMD5Hashes();

        // Step 2: Compile everything (compile dependencies before?)
        Directory.CreateDirectory(Path.Combine(proj.DotFolder, "compilecache"));
        foreach (string file in lfdFiles)
        {
            (bool, string) res = await CompileLFDFile(file);
        }

        // Step 3: ???

        // Step ?: Put md5 hashes inside the dotfolder for next time.
        SaveMD5Hashes();

        await Task.WhenAll();
        return true;
    }

    public bool CollectLFDFiles(ref List<string> lfdFiles)
    {
        try
        {
            string[] fileNames = Directory.GetFiles(proj.ProjectRoot, "*.lfd", SearchOption.AllDirectories);
            lfdFiles.AddRange(fileNames.Where(f => !f.Contains(".lunaforge"))); // Ignore dotfolder just to be safe. (There shouldn't be any lfd file inside)
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Couldn't get lfd files. Reason:\n{ex}");
            return false;
        }
    }

    public async Task<(bool, string)> CompileLFDFile(string lfdFilePath, bool showOnly = false)
    {
        try
        {
            using FileStream fs = new(lfdFilePath, FileMode.Open, FileAccess.Read);
            var md5 = System.Security.Cryptography.MD5.Create();
            string md5str = Encoding.Default.GetString(md5.ComputeHash(fs));

            if (computedMD5.TryGetValue(lfdFilePath, out string? cachedMD5) && cachedMD5 == md5str)
                return (true, "");

            DocumentFileLFD file = DocumentFileLFD.Load(lfdFilePath);
            if (file == null)
            {
                Logger.Error($"Couldn't load lfd file '{lfdFilePath}' for compilation.");
                return (false, "");
            }
            string relativePath = Path.GetRelativePath(proj.ProjectRoot, lfdFilePath);
            string relativeLuaPath = Path.ChangeExtension(relativePath, ".lua");
            string cachePath = Path.Combine(proj.DotFolder, "compilecache", relativeLuaPath);

            StringBuilder sb = new();

            sb.AppendLine($"-- Generated from {relativePath} by LunaForge {App.AppVersion}");
            sb.AppendLine($"-- Compiled at {DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy (day/month/year)")}");

            foreach (TreeNode node in file.TreeNodes)
                foreach (var line in node.ToLua(0))
                    sb.Append(line);

            md5.ComputeHash(fs);
            computedMD5[lfdFilePath] = md5str;

            if (!showOnly)
            {
                //Write output to cache (create folder if needed)
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
                File.WriteAllText(cachePath, sb.ToString(), Encoding.UTF8);
            }

            return (true, sb.ToString());
        }
        catch (Exception ex)
        {
            Logger.Error($"Couldn't compile file '{lfdFilePath}'. Reason:\n{ex}");
            return (false, "");
        }
    }

    private Dictionary<string, string> ReadMD5Hashes()
    {
        try
        {
            string contents = File.ReadAllText(MD5HashesFilePath, Encoding.UTF8);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(contents) ?? [];
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
