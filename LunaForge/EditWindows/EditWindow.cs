using LunaForge.Models.TreeNodes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace LunaForge.EditWindows;

/// <summary>
/// Base class for all edit windows.
/// Subclasses call <see cref="Confirm"/> to return the result or <see cref="Cancel"/> to return nothing like your dad after getting milk.
/// </summary>
public class EditWindow : Window
{
    public string Result { get; protected set; } = string.Empty;

    public string InitialValue { get; set; } = string.Empty;

    public NodeAttribute? SourceAttribute { get; set; }

    public bool Confirmed { get; private set; }

    public EditWindow()
    {
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
    }

    protected void Confirm(string result)
    {
        Result = result;
        Confirmed = true;
        DialogResult = true;
    }

    protected void Cancel()
    {
        Confirmed = false;
        DialogResult = false;
    }
}
