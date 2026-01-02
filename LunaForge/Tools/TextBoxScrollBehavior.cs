using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using TextBox = System.Windows.Controls.TextBox;

namespace LunaForge.Tools;

public static class TextBoxScrollBehavior
{
    public static readonly DependencyProperty AutoScrollToEndProperty =
        DependencyProperty.RegisterAttached(
            "AutoScrollToEnd",
            typeof(bool),
            typeof(TextBoxScrollBehavior),
            new PropertyMetadata(false, OnAutoScrollToEndChanged)
        );

    public static bool GetAutoScrollToEnd(DependencyObject obj)
    {
        return (bool)obj.GetValue(AutoScrollToEndProperty);
    }

    public static void SetAutoScrollToEnd(DependencyObject obj, bool value)
    {
        obj.SetValue(AutoScrollToEndProperty, value);
    }

    private static void OnAutoScrollToEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            if ((bool)e.NewValue)
                textBox.TextChanged += TextBox_TextChanged;
            else
                textBox.TextChanged -= TextBox_TextChanged;
        }
    }

    private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
            textBox.ScrollToEnd();
    }
}
