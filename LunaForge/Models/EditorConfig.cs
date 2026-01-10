using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LunaForge.Services;
using Newtonsoft.Json;

namespace LunaForge.Models;

public class EditorConfig : ConfigSystem
{
    private const string configFile = "config.toml";
    private static readonly new string configPath = Path.Combine(GetConfigPath(), configFile);
    public static readonly string BasePath = GetConfigPath();

    public static EditorConfig Default { get; } = Load<EditorConfig>(configPath);

    private static string GetConfigPath()
    {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LunaForge");

        Directory.CreateDirectory(path);
        return path;
    }
}
