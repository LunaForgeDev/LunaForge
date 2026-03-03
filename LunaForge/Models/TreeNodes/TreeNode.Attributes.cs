using LunaForge.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using LunaForge.Backend.Attributes.TreeNodesAttributes;

namespace LunaForge.Models.TreeNodes;

public partial class TreeNode
{
    [JsonProperty]
    public ObservableCollection<NodeAttribute> Attributes { get; private set; } = [];

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
                else if (attribute.IsDynamic)
                    AttachDynamicAttributeHandler(attribute);
            }
            
            OnAttributesDeserialized();
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

        OnInitializeDefaultDynamicAttributes();
    }

    protected virtual void OnInitializeDefaultDynamicAttributes() { }
    protected virtual void OnAttributesDeserialized() { }

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

    #region Dynamic Attributes

    public NodeAttribute AddDynamicAttribute(string name, string defaultValue = "")
    {
        var attr = NodeAttribute.CreateDynamic(name, defaultValue, this);
        AttachDynamicAttributeHandler(attr);
        Attributes.Add(attr);
        return attr;
    }

    public NodeAttribute AddDynamicDependencyAttribute(string name, string defaultValue = "")
    {
        var attr = NodeAttribute.CreateDynamicDependency(name, defaultValue, this);
        AttachDynamicAttributeHandler(attr);
        Attributes.Add(attr);
        return attr;
    }

    private void AttachDynamicAttributeHandler(NodeAttribute attr)
    {
        attr.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(NodeAttribute.Value))
            {
                OnPropertyChanged(nameof(ScreenString));
            }
        };
    }

    public bool RemoveDynamicAttribute(string name)
    {
        var attr = Attributes.FirstOrDefault(a => a.Name == name && a.IsDynamic);
        if (attr != null)
        {
            Attributes.Remove(attr);
            NotifyAttributesChanged();
            return true;
        }
        return false;
    }

    public void RemoveDynamicAttributesFromIndex(int startIndex, int count = -1)
    {
        if (startIndex < 0 || startIndex >= Attributes.Count)
            return;

        int removeCount = count < 0 ? Attributes.Count - startIndex : count;
        removeCount = Math.Min(removeCount, Attributes.Count - startIndex);

        for (int i = 0; i < removeCount; i++)
        {
            if (startIndex < Attributes.Count && Attributes[startIndex].IsDynamic)
                Attributes.RemoveAt(startIndex);
            else
                startIndex++; // Non-dynamic: skip
        }
        NotifyAttributesChanged();
    }

    private void NotifyAttributesChanged()
    {
        OnPropertyChanged(nameof(Attributes));
        OnPropertyChanged(nameof(ScreenString));
    }

    public IEnumerable<NodeAttribute> GetDynamicAttributes()
    {
        return Attributes.Where(a => a.IsDynamic);
    }

    public int DynamicAttributeCount => Attributes.Count(a => a.IsDynamic);

    #endregion
}
