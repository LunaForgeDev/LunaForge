using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LunaForge.ViewModels;

public partial class ProjectItem : ObservableObject
{
    public string Path { get; set; }
    public string Name { get; set; }
    public DateTime LastAccessedAt { get; set; }

    [ObservableProperty]
    public bool isPinned;

    [ObservableProperty]
    public string pinButtonContent = "📌";

    [ObservableProperty]
    public string pinTooltip = "Pin project";

    public ProjectItem(string path, string name, DateTime lastAccessedAt, bool isPinned = false)
    {
        Path = path;
        Name = name;
        LastAccessedAt = lastAccessedAt;
        IsPinned = isPinned;
        UpdatePinState();
    }

    partial void OnIsPinnedChanged(bool value)
    {
        UpdatePinState();
    }

    private void UpdatePinState()
    {
        PinButtonContent = IsPinned ? "📍" : "📌";
        PinTooltip = IsPinned ? "Unpin project" : "Pin project";
    }
}
