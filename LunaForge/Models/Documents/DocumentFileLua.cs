using Newtonsoft.Json;
using System;
using System.IO;

namespace LunaForge.Models.Documents;

[Serializable]
public partial class DocumentFileLua : DocumentFile
{
    public DocumentFileLua() : base(string.Empty)
    {
        FileExtension = ".lua";
    }

    public DocumentFileLua(string filePath) : base(filePath)
    {
        FileExtension = ".lua";
    }

    public static DocumentFileLua Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Logger.Error($"Lua file '{filePath}' doesn't exist. Can't load.");
            return null;
        }

        try
        {
            DocumentFileLua luaFile = new(filePath)
            {
                FileContent = File.ReadAllText(filePath)
            };

            Logger.Information($"Loaded Lua file: {filePath}");
            return luaFile;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load Lua file '{filePath}'. Reason:\n{ex}");
            return null;
        }
    }

    public bool Save()
    {
        return Save(FilePath);
    }

    public bool Save(string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Logger.Error("Cannot save Lua file: file path is empty.");
                return false;
            }

            File.WriteAllText(filePath, FileContent);
            
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            
            PushSavedCommand();
            
            Logger.Information($"Saved Lua file: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save Lua file '{filePath}'. Reason:\n{ex}");
            return false;
        }
    }
}
