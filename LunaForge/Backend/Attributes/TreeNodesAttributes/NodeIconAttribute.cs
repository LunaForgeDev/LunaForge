using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Backend.Attributes.TreeNodesAttributes;

/// <summary>
/// Identify the icon of a <see cref="TreeNode"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class NodeIconAttribute(string path) : Attribute
{
    public string Path { get; } = path;
}