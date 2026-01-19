using LunaForge.Models;
using LunaForge.Backend.Enums;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Tomlyn;
using Tomlyn.Model;
using LunaForge.Backend.Attributes;

namespace LunaForge.Services;

public enum ConfigSystemCategory
{
    General,
    Services,
    DefaultProject,
    CurrentProject,
}

public interface IConfigSystemEntry
{
    public ConfigSystemCategory Category { get; }
    string Key { get; }
    object TempValueObj { get; set; }

    public void Commit();
    public void Revert();
}

public class ConfigSystemEntry<T> : IConfigSystemEntry
{
    [IgnoreDataMember]
    public ConfigSystemCategory Category { get; }
    public string Key { get; }
    public T Value { get; set; }
    [IgnoreDataMember]
    public T? TempValue { get; set; }

    public object TempValueObj
    {
        get => TempValue;
        set
        {
            if (value is T tempValue)
                TempValue = tempValue;
            else
                throw new InvalidCastException($"Cannot cast {value.GetType()} to {typeof(T)} for key '{Key}'.");
        }
    }

    public ConfigSystemEntry() { }

    public ConfigSystemEntry(ConfigSystemCategory category, string key, T defaultValue)
    {
        Category = category;
        Key = key;
        Value = defaultValue;
        TempValue = defaultValue;
    }

    public void Commit() => Value = TempValue!;

    public void Revert() => TempValue = Value;

    public E GetEnum<E>() where E: struct, Enum
    {
        if (Value == null)
            return default;

        return Enum.Parse<E>(TempValue.ToString());
    }

    public object GetEnum([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
    {
        if (TempValue == null)
            return Activator.CreateInstance(type);

        return Enum.Parse(type, TempValue.ToString());
    }
}

public class ConfigSystem
{
    public string configPath { get; set; }

    private Dictionary<string, IConfigSystemEntry> entries { get; set; } = [];
    public IEnumerable<KeyValuePair<string, IConfigSystemEntry>> AllEntries => entries.OrderBy(e => e.Key);

    private static readonly ILogger Logger = CoreLogger.Create("ConfigSystem");

    public void Register<T>(ConfigSystemCategory category, string key, T defaultValue)
    {
        if (!entries.ContainsKey(key))
            entries[key] = new ConfigSystemEntry<T>(category, key, defaultValue);
    }

    public ConfigSystemEntry<T> Get<T>(string key, ConfigSystemCategory category = ConfigSystemCategory.General)
    {
        if (!entries.TryGetValue(key, out var entry))
        {
            Logger.Warning($"Config entry '{key}' not found.");
            return new ConfigSystemEntry<T>(category, key, default!);
        }
        if (entry is ConfigSystemEntry<T> e)
            return e;

        // Fucked up TOML deserialization because why????
        try
        {
            dynamic dyn = entry;
            object rawValue = dyn.Value;

            // Genuinely, why.
            if (rawValue != null && typeof(T).IsAssignableFrom(rawValue.GetType()) == false)
            {
                T convertedValue = (T)Convert.ChangeType(rawValue, typeof(T));
                var converted = new ConfigSystemEntry<T>(dyn.Category, dyn.Key, convertedValue);
                entries[key] = converted;
                return converted;
                // I'm so done.
            }
        }
        catch (Exception ex)
        {
            Logger.Warning($"Config entry '{key}' has type mismatch. Expected: {typeof(T)}, got: {entry.GetType()}. Error: {ex.Message}");
        }

        return new ConfigSystemEntry<T>(category, key, default!);
    }

    public void SetOrCreate<T>(string key, T value, ConfigSystemCategory category = ConfigSystemCategory.General)
    {
        if (!entries.ContainsKey(key))
            Register(category, key, value);

        Get<T>(key).TempValue = value;
    }

    public void Set<T>(string key, T value)
    {
        Get<T>(key).TempValue = value;
    }

    public void CopyCategory(ConfigSystemCategory sourceCategory, ConfigSystemCategory targetCategory)
    {
        var entriesToCopy = entries.Values
            .Where(e => e.Category == sourceCategory)
            .ToList();

        foreach (var entry in entriesToCopy)
        {
            dynamic dyn = entry;
            var type = typeof(ConfigSystemEntry<>).MakeGenericType(dyn.Value.GetType());
            var newEntry = (IConfigSystemEntry)Activator.CreateInstance(type, targetCategory, dyn.Key, dyn.Value);
            
            entries[dyn.Key] = newEntry;
        }

        Logger.Debug($"Copied {entriesToCopy.Count} entries from {sourceCategory} to {targetCategory}");
    }

    public void CommitAll()
    {
        foreach (var entry in entries.Values)
        {
            var commitMethod = entry.GetType().GetMethod("Commit");
            commitMethod?.Invoke(entry, null);
        }
    }

    public void CommitAllAndSave()
    {
        CommitAll();
        Save();
    }

    public void RevertAll()
    {
        foreach (var entry in entries.Values)
        {
            var revertMethod = entry.GetType().GetMethod("Revert");
            revertMethod?.Invoke(entry, null);
        }
    }

    public void Save()
    {
        Save(configPath);
    }

    public void Save(string filePath)
    {
        var model = new TomlTable();

        foreach (var entry in entries.Values)
        {
            dynamic dyn = entry;
            string section = dyn.Category.ToString();

            if (!model.ContainsKey(section))
                model[section] = new TomlTable();

            ((TomlTable)model[section])[dyn.Key] = dyn.Value;
        }

        File.WriteAllText(filePath, Toml.FromModel(model));
    }

    public static T Load<T>(string configPath) where T: ConfigSystem, new()
    {
        T config = new()
        {
            configPath = configPath,
        };

        if (File.Exists(configPath))
        {
            try
            {
                var table = Toml.Parse(File.ReadAllText(configPath)).ToModel();

                foreach (var (sectionKey, sectionValue) in table)
                {
                    if (sectionValue is not TomlTable section)
                        continue;

                    if (!Enum.TryParse(sectionKey, out ConfigSystemCategory category))
                    {
                        Logger.Warning($"Unknown config section '{sectionKey}' in {configPath}. Skipping.");
                        continue;
                    }

                    foreach (var (key, val) in section)
                    {
                        var type = typeof(ConfigSystemEntry<>).MakeGenericType(val.GetType());
                        config.entries[key] = (IConfigSystemEntry)Activator.CreateInstance(type, category, key, val);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load config. Reason:\n{ex}");
            }
        }

        if (config is EditorConfig)
        {
            RegisterBaseConfigs(ref config);

            config.CommitAll();
            config.Save();
        }

        return config;
    }

    /// <summary>
    /// Registers all base configuration values for the specified configuration system, optionally filtering by category.
    /// </summary>
    /// <typeparam name="T">The type of configuration system to register base configurations for. Must inherit from ConfigSystem.</typeparam>
    /// <param name="config">A reference to the configuration system instance in which to register the base configuration values.</param>
    /// <param name="category">An optional category to filter which base configuration values are registered. If null, all categories are included.</param>
    public static void RegisterBaseConfigs<T>(ref T config, ConfigSystemCategory? category = null) where T : ConfigSystem
    {
        foreach (BaseConfigEnum enumVal in Enum.GetValues<BaseConfigEnum>())
        {
            BaseConfigAttribute attr = enumVal.GetAttributeOfType<BaseConfigAttribute>();
            if (category != null && attr.Category != category)
                continue;

            var method = typeof(T).GetMethod("Register");
            var register = method.MakeGenericMethod(attr.DefaultValueType);
            register.Invoke(config, [attr.Category, Enum.GetName(enumVal), attr.DefaultValue]);
        }
    }
}