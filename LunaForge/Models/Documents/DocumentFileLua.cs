using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;

namespace LunaForge.Models.Documents;

[Serializable]
public partial class DocumentFileLua : DocumentFile
{
    private string originalChecksum = string.Empty;

    public override bool IsUnsaved
    {
        get
        {
            string currentContent = FileContent ?? string.Empty;
            return CalculateChecksum(currentContent) != originalChecksum;
        }
    }

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
            string content = File.ReadAllText(filePath);

            DocumentFileLua luaFile = new(filePath)
            {
                FileContent = content,
                originalChecksum = CalculateChecksum(content)
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

    public new bool Save()
    {
        return Save(FilePath);
    }

    public override bool Save(string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Logger.Error("Cannot save Lua file: file path is empty.");
                return false;
            }

            string currentContent = FileContent ?? string.Empty;
            File.WriteAllText(filePath, currentContent);

            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            originalChecksum = CalculateChecksum(currentContent);

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
    
    private static string CalculateChecksum(string content)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(content);
        byte[] hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToHexString(hashBytes);
    }

    public void TriggerUnsavedCheck()
    {
        OnPropertyChanged(nameof(IsUnsaved));
        UpdateFileNameDisplay();
    }
}
