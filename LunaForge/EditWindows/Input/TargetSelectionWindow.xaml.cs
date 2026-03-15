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

[EditWindowKey("targetSelection")]
public partial class TargetSelectionWindow : EditWindow
{
    public TargetSelectionWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        foreach (ListBoxItem item in TargetList.Items)
        {
            if (item.Content.ToString() == InitialValue)
            {
                TargetList.SelectedItem = item;
                break;
            }
        }
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        var selected = (TargetList.SelectedItem as ListBoxItem)?.Content?.ToString() ?? string.Empty;
        Confirm(selected);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Cancel();
    }
}
