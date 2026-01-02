using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Backend.Attributes.TreeNodesAttributes;

/// <summary>
/// Indicates that a TreeNode class represents a class-type node.
/// Is set as a exportable symbol. Can be accessed in code via : _editor_symbol["<name>"]
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ClassAttribute : Attribute
{
}
