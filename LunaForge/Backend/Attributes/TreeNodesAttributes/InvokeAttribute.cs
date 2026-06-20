using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Backend.Attributes.TreeNodesAttributes;

/// <summary>
/// Marks a node as having an invokable attribute. When the user double clicks or creates this node, the attribute index will open its<br/>
/// Editor Window for editing.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class InvokeAttribute(string attributeName) : Attribute
{
    public string Name { get; } = attributeName;
}
