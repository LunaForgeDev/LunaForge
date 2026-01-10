using System;
using System.IO;

namespace LunaForge.Models;

public class RecentFileItem
{
    public string RelativePath { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string FileName => Path.GetFileName(RelativePath);
    public string DisplayName => $"{FileName} - {Path.GetDirectoryName(RelativePath)}";
    public DateTime LastAccessed { get; set; }
    
    public RecentFileItem() { }
    
    public RecentFileItem(string relativePath, string projectRoot)
    {
        RelativePath = relativePath;
        FullPath = Path.Combine(projectRoot, relativePath);
        LastAccessed = File.Exists(FullPath) ? File.GetLastAccessTime(FullPath) : DateTime.MinValue;
    }
}
