using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace LunaForge.LFTheme;

public enum ThemeType
{
    Dark,
    Light,
}

public static class ThemeTypeExtension
{
    public static string GetName(this ThemeType type)
    {
        switch (type)
        {
            case ThemeType.Dark:
                return "Dark";
            case ThemeType.Light:
                return "Light";
            default:
                return "Unknown";
        }
    }
}

public static class ThemeController
{
    public static ThemeType CurrentTheme { get; set; }

    private static ResourceDictionary ThemeDictionary
    {
        get => Application.Current.Resources.MergedDictionaries[0];
        set => Application.Current.Resources.MergedDictionaries[0] = value;
    }

    private static ResourceDictionary ControlColours
    {
        get => Application.Current.Resources.MergedDictionaries[1];
        set => Application.Current.Resources.MergedDictionaries[1] = value;
    }

    private static void RefreshControls()
    {
        Collection<ResourceDictionary> merged = Application.Current.Resources.MergedDictionaries;
        ResourceDictionary dict = merged[2];
        merged.RemoveAt(2);
        merged.Insert(2, dict);
    }

    public static void SetTheme(ThemeType theme)
    {
        string themeName = theme.GetName();
        if (string.IsNullOrEmpty(themeName))
            return;

        CurrentTheme = theme;
        ThemeDictionary = new() { Source = new Uri($"LFTheme/ColourDictionaries/{themeName}.xaml", UriKind.Relative) };
        ControlColours = new() { Source = new Uri("LFTheme/ControlColours.xaml", UriKind.Relative) };
        RefreshControls();
    }

    public static object GetResource(object key)
    {
        return ThemeDictionary[key];
    }

    public static SolidColorBrush GetBrush(string name)
    {
        return GetResource(name) is SolidColorBrush brush ? brush : new(Colors.White);
    }
}
