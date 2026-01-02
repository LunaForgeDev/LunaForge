using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunaForge.ViewModels;

public partial class ProjectCategory : ObservableObject
{
    public string CategoryName { get; set; }
    public ObservableCollection<ProjectItem> Projects { get; set; } = [];

    public ProjectCategory(string categoryName)
    {
        CategoryName = categoryName;
    }
}
