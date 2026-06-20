using LunaForge.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LunaForge.EditWindows.Input;

[EditWindowKey("editorFileSelector")]
public partial class EditorFileSelector : EditWindow
{
    public EditorFileSelector()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PathSelector.Text = InitialValue;
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        var selected = PathSelector.Text;
        Confirm(selected);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Cancel();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog()
        {
            InitialDirectory = MainWindowModel.Project?.ProjectRoot ?? "c:\\",
            Filter = "LFD files (*.lfd)|*.lfd|LUA files (*.lua)|*.lua",
            Multiselect = false,
        };
        if (dialog.ShowDialog() == true)
        {
            PathSelector.Text = dialog.FileName;
        }
    }
}
