using LunaForge.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using LunaForge.Backend.Attributes.TreeNodesAttributes;

namespace LunaForge.Models.TreeNodes;

public partial class TreeNode
{
    [JsonProperty]
    public List<NodeAttribute> Attributes { get; private set; } = [];

    private void InitializeAttributes()
    {
        var properties = GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<NodeAttributeAttribute>() != null);

        if (Attributes.Count > 0)
        {
            var propertyDict = properties.ToDictionary(p => p.Name);
            
            foreach (var attribute in Attributes)
            {
                attribute.ParentNode = this;
                
                // Use PropertyName if available, otherwise fall back to Name
                string propertyName = !string.IsNullOrEmpty(attribute.PropertyName) 
                    ? attribute.PropertyName
                    : attribute.Name;
                
                if (propertyDict.TryGetValue(propertyName, out var property))
                {
                    // Set PropertyName if it wasn't set during deserialization
                    if (string.IsNullOrEmpty(attribute.PropertyName))
                        attribute.PropertyName = property.Name;
                    
                    try
                    {
                        property.SetValue(this, attribute.Value);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to sync deserialized attribute '{attribute.Name}' to property: {ex.Message}");
                    }
                    
                    attribute.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(NodeAttribute.Value) && s is NodeAttribute attr)
                        {
                            try
                            {
                                property.SetValue(this, attr.Value);
                                OnPropertyChanged(property.Name);
                                OnPropertyChanged(nameof(ScreenString));
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"Failed to sync attribute '{attr.Name}' to property '{property.Name}': {ex.Message}");
                            }
                        }
                    };
                }
            }
            return;
        }

        // No deserialized attributes - initialize from properties
        Attributes.Clear();

        foreach (var property in properties)
        {
            var nodeAttribute = NodeAttribute.CreateFromProperty(property, this);

            if (nodeAttribute != null)
            {
                var currentValue = property.GetValue(this);
                if (currentValue == null || string.IsNullOrEmpty(currentValue.ToString()))
                {
                    property.SetValue(this, nodeAttribute.DefaultValue);
                    nodeAttribute.Value = nodeAttribute.DefaultValue;
                }

                nodeAttribute.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(NodeAttribute.Value) && s is NodeAttribute attr)
                    {
                        try
                        {
                            property.SetValue(this, attr.Value);
                            OnPropertyChanged(property.Name);
                            OnPropertyChanged(nameof(ScreenString));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Failed to sync attribute '{attr.Name}' to property '{property.Name}': {ex.Message}");
                        }
                    }
                };

                Attributes.Add(nodeAttribute);
            }
        }
    }

    private void SyncAttributesToProperties()
    {
        var properties = GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<NodeAttributeAttribute>() != null)
            .ToDictionary(p => p.Name);

        foreach (var attribute in Attributes)
        {
            // Use PropertyName if available, otherwise fall back to Name
            string propertyName = !string.IsNullOrEmpty(attribute.PropertyName) 
                ? attribute.PropertyName 
                : attribute.Name;
            
            if (properties.TryGetValue(propertyName, out var property))
            {
                // Set PropertyName if it wasn't set
                if (string.IsNullOrEmpty(attribute.PropertyName))
                    attribute.PropertyName = property.Name;
                
                try
                {
                    property.SetValue(this, attribute.Value);
                    OnPropertyChanged(property.Name);
                }
                catch (Exception ex)
                {
                    CoreLogger.Create("TreeNode").Error($"Failed to set property '{property.Name}' from attribute: {ex.Message}");
                }
            }
        }
        
        OnPropertyChanged(nameof(ScreenString));
    }

    public string GetAttributeValue(string name, string defaultValue = "")
    {
        return Attributes.FirstOrDefault(a => a.Name == name)?.Value ?? defaultValue;
    }

    public bool SetAttributeValue(string name, string value)
    {
        var attribute = Attributes.FirstOrDefault(a => a.Name == name);
        if (attribute != null)
        {
            attribute.Value = value;
            return true;
        }
        return false;
    }
}
