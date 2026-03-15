using LunaForge.Models.TreeNodes;
using LunaForge.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;

namespace LunaForge.EditWindows;

public sealed class EditWindowRegistry
{
    private static readonly ILogger Logger = CoreLogger.Create("EditWindowRegistry");

    private readonly Dictionary<string, Type> editors = [];
    public IReadOnlyDictionary<string, Type> Editors => editors;
    private readonly Dictionary<string, List<string>> sourceKeys = [];

    public static EditWindowRegistry Instance { get; } = new();

    public void Register<T>(string key, string sourceKey = "") where T : EditWindow
        => Register(key, typeof(T), sourceKey);

    public void Register(string key, Type editWindowType, string sourceKey = "")
    {
        if (!typeof(EditWindow).IsAssignableFrom(editWindowType))
            throw new ArgumentException($"Type {editWindowType.Name} does not inherit from EditWindow.", nameof(editWindowType));
        if (editWindowType.IsAbstract)
            throw new ArgumentException($"Type {editWindowType.Name} is abstract and thus cannot be registered.", nameof(editWindowType));
        if (editors.TryGetValue(key, out var existing))
            Logger.Warning("Edit window key '{0}' overwritten ({1} -> {2})", key, existing.Name, editWindowType.Name);

        editors[key] = editWindowType;
        TrackSource(sourceKey, key);

        Logger.Information("Registered edit window '{0}' -> {1}", key, editWindowType.Name);
    }

    /// <summary>
    /// Scans an entire assembly and registers the <see cref="EditWindow"/>s decorated with <see cref="EditWindowKeyAttribute"/> it can find.
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="sourceKey"></param>
    public void RegisterFromAssembly(Assembly assembly, string sourceKey)
    {
        var types = assembly.GetTypes().Where(t => typeof(EditWindow).IsAssignableFrom(t)
            && !t.IsAbstract
            && t.GetCustomAttribute<EditWindowKeyAttribute>() != null);

        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<EditWindowKeyAttribute>()!;
            Register(attr.Key, type, sourceKey);
        }
    }

    /// <summary>
    /// Unregisters all edit windows registered with the plugin key (source)
    /// </summary>
    /// <param name="sourceKey"></param>
    public void UnregisterBySource(string sourceKey)
    {
        if (!sourceKeys.TryGetValue(sourceKey, out var keys))
            return;

        foreach (var key in keys)
        {
            if (editors.Remove(key))
                Logger.Information("Unregistered edit window '{0}'", key);
        }

        sourceKeys.Remove(sourceKey);
    }

    public EditWindow? Create(string key)
    {
        if (!editors.TryGetValue(key, out var type))
            return null;

        try
        {
            return (EditWindow?)Activator.CreateInstance(type);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to create edit window '{0}'. Reason:\n{1}", key, ex.Message);
            return null;
        }
    }

    public bool HasEditor(string key)
        => editors.ContainsKey(key);

    public string? ShowDialog(string key, string currentValue, NodeAttribute? source = null, Window? owner = null)
    {
        var editor = Create(key);
        if (editor == null)
        {
            Logger.Warning("No edit window registered for key '{0}'", key);
            return null;
        }

        editor.InitialValue = currentValue;
        editor.SourceAttribute = source;

        if (owner != null)
            editor.Owner = owner;

        if (editor.ShowDialog() == true && editor.Confirmed)
            return editor.Result;

        return null;
    }

    private void TrackSource(string sourceKey, string editorKey)
    {
        if (string.IsNullOrEmpty(sourceKey))
            return;

        if (!sourceKeys.TryGetValue(sourceKey, out var keys))
        {
            keys = [];
            sourceKeys[sourceKey] = keys;
        }

        keys.Add(editorKey);
    }
}
