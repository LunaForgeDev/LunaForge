using LunaForge.Models;
using Serilog;
using System.Collections.Generic;
using System.IO;
using Tomlyn;

namespace LunaForge.Services;

public class LuaSTGInstance
{
    public string BranchName { get; set; } = string.Empty;
    public string BranchVersion { get; set; } = string.Empty;
    public Dictionary<string, string> Libraries { get; set; } = []; // Key=name, Value=version
    public string Path { get; set; } = string.Empty;

    public override string ToString() => $"{BranchName} {BranchVersion}";
}

public static class LuaSTGInstancesService
{
    private static readonly ILogger Logger = CoreLogger.Create("LuaSTGInstancesService");
    private static readonly List<LuaSTGInstance> _instances = [];
    public static IReadOnlyList<LuaSTGInstance> Instances => _instances;

    public static void Initialize()
    {
        _instances.Clear();
        _instances.AddRange(LoadInstances());
    }

    public static void AddInstance(LuaSTGInstance instance)
    {
        if (_instances.Any(x => x.Path == instance.Path))
        {
            Logger.Warning("Instance {path} already exists.", instance.Path);
            return;
        }
        _instances.Add(instance);
        Save();
    }

    public static bool RemoveInstance(LuaSTGInstance instance)
    {
        if (!_instances.Remove(instance))
        {
            Logger.Warning("Instance {path} was not found.", instance.Path);
            return false;
        }
        Save();
        return true;
    }

    private static List<LuaSTGInstance> LoadInstances()
    {
        var instancesStr = EditorConfig.Default.Get<string>("LuaSTGInstances", ConfigSystemCategory.General).Value;
        if (string.IsNullOrEmpty(instancesStr))
            return [];

        var paths = instancesStr.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        List<LuaSTGInstance> result = [];
        foreach (var path in paths)
        {
            try
            {
                string? dir = Path.GetDirectoryName(path);
                if (dir is null)
                {
                    Logger.Warning("Could not resolve directory for instance path {path}. Skipping.", path);
                    continue;
                }

                string configFile = Path.Combine(dir, ".lunaforge");
                var instance = TomlSerializer.Deserialize<LuaSTGInstance>(File.ReadAllText(configFile),
                    new TomlSerializerOptions { DefaultIgnoreCondition = TomlIgnoreCondition.WhenWritingNull });
                instance.Path = configFile;
                result.Add(instance);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to parse LuaSTG instance from path {path}. Skipping.", path);
            }
        }
        return result;
    }

    private static void Save()
    {
        string instancesStr = string.Join(';', _instances.Select(i => i.Path));
        EditorConfig.Default.SetOrCreate("LuaSTGInstances", instancesStr);
        EditorConfig.Default.Save();
    }
}
