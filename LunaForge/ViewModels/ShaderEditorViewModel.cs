using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace LunaForge.ViewModels;

public partial class ConnectorViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private Point anchor;
}

public partial class ShaderNodeViewModel : ObservableObject
{
    [ObservableProperty]
    public string title = string.Empty;

    [ObservableProperty]
    public ObservableCollection<ConnectorViewModel> inputs = [];
    [ObservableProperty]
    public ObservableCollection<ConnectorViewModel> outputs = [];
}

public partial class ShaderEditorViewModel : ObservableObject
{
    [ObservableProperty]
    public ObservableCollection<ShaderNodeViewModel> nodes = [];

    public ShaderEditorViewModel()
    {
        Nodes.Add(new()
        {
            Title = "Test",
            Inputs =
            [
                new() { Title = "Input 1" },
            ],
            Outputs =
            [
                new() { Title = "Output 1" },
                new() { Title = "Output 2" },
            ]
        });
    }
}
