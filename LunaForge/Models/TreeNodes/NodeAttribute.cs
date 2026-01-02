using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LunaForge.Backend.Attributes.TreeNodesAttributes;

namespace LunaForge.Models.TreeNodes;

public struct NodeAttributeChangedEventArgs(string oldVal, string newVal)
{
    public string OldValue = oldVal;
    public string NewValue = newVal;
}

[Serializable]
public partial class NodeAttribute : ObservableObject
{
    [JsonProperty]
    public string Name { get; set; } = "Attr";
    
    [JsonProperty]
    public string EditorWindow { get; set; } = string.Empty;
    
    [JsonIgnore]
    public string DefaultValue { get; set; } = string.Empty;
    
    [ObservableProperty]
    [property: JsonProperty(nameof(Value))]
    private string _value = string.Empty;
    
    [JsonIgnore]
    public string TempValue { get; set; } = string.Empty;

    [JsonIgnore]
    public TreeNode? ParentNode { get; set; }

    public NodeAttribute()
    {
    }

    public NodeAttribute(string name, TreeNode parent)
    {
        Name = name;
        ParentNode = parent;
        _value = TempValue = string.Empty;
    }

    public NodeAttribute(string name, string defaultValue, TreeNode? parent = null)
    {
        Name = name;
        DefaultValue = defaultValue;
        _value = TempValue = defaultValue;
        ParentNode = parent;
    }

    public NodeAttribute(string name, string editorWindow, string defaultValue, TreeNode? parent = null)
    {
        Name = name;
        EditorWindow = editorWindow;
        DefaultValue = defaultValue;
        _value = TempValue = defaultValue;
        ParentNode = parent;
    }

    partial void OnValueChanging(string value)
    {
        TempValue = _value;
    }

    partial void OnValueChanged(string value)
    {
        ParentNode?.RaiseAttributeChanged(this, new NodeAttributeChangedEventArgs(TempValue, value));
    }

    public void EditAttr(string newValue, bool force = false)
    {
        string oldValue = Value;
        Value = newValue;
        ParentNode?.RaiseAttributeChanged(this, new NodeAttributeChangedEventArgs(oldValue, newValue));
    }

    public void ResetToDefault()
    {
        Value = DefaultValue;
    }

    /// <summary>
    /// Creates a NodeAttribute from a property decorated with [NodeAttribute]
    /// </summary>
    /// <param name="property">The PropertyInfo to extract attribute data from</param>
    /// <param name="parentNode">The TreeNode that owns this attribute</param>
    /// <returns>A new NodeAttribute instance, or null if the property is not decorated</returns>
    public static NodeAttribute? CreateFromProperty(PropertyInfo property, TreeNode parentNode)
    {
        var attributeDecorator = property.GetCustomAttribute<NodeAttributeAttribute>();
        
        if (attributeDecorator == null)
            return null;

        var currentValue = property.GetValue(parentNode)?.ToString() ?? attributeDecorator.DefaultValue;
        var nodeAttribute = new NodeAttribute(
            name: attributeDecorator.Name,
            defaultValue: attributeDecorator.DefaultValue,
            parent: parentNode
        )
        {
            Value = currentValue,
            EditorWindow = string.Empty
        };

        return nodeAttribute;
    }
}
