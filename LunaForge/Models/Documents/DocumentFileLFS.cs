using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace LunaForge.Models.Documents;

[Serializable]
public partial class DocumentFileLFS : DocumentFile
{
    [ObservableProperty]
    public ObservableCollection<object> nodes = [];

    public DocumentFileLFS() : base(string.Empty)
    {
        FileExtension = ".lfs";
    }

    public DocumentFileLFS(string filePath) : base(filePath)
    {
        FileExtension = ".lfs";
    }

    public static DocumentFileLFS Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Logger.Error($"LFS file '{filePath}' doesn't exist. Can't load.");
            return null;
        }

        try
        {
            DocumentFileLFS lfsFile = new(filePath)
            {
                FileContent = File.ReadAllText(filePath)
            };

            Logger.Information($"Loaded LFS file: {filePath}");
            return lfsFile;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load LFS file '{filePath}'. Reason:\n{ex}");
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
                Logger.Error("Cannot save LFS file: file path is empty.");
                return false;
            }

            File.WriteAllText(filePath, FileContent);
            
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            
            PushSavedCommand();
            
            Logger.Information($"Saved LFS file: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save LFS file '{filePath}'. Reason:\n{ex}");
            return false;
        }
    }
}
