using LunaForge.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LunaForge.Models.TreeNodes;

public partial class TreeNode
{
    public List<NodeAttribute> Attributes { get; private set; } = [];

    private void InitializeAttributes()
    {
        var properties = GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<Backend.Attributes.TreeNodesAttributes.NodeAttributeAttribute>() != null);

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
            .Where(p => p.GetCustomAttribute<Backend.Attributes.TreeNodesAttributes.NodeAttributeAttribute>() != null)
            .ToDictionary(p => p.Name);

        foreach (var attribute in Attributes)
        {
            if (properties.TryGetValue(attribute.Name, out var property))
            {
                try
                {
                    property.SetValue(this, attribute.Value);
                }
                catch (Exception ex)
                {
                    CoreLogger.Create("TreeNode").Error($"Failed to set property '{property.Name}' from attribute: {ex.Message}");
                }
            }
        }
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
