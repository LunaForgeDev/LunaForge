using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Backend.Attributes.TreeNodesAttributes;

/// <summary>
/// Identify a <see cref="TreeNode"/> cannot be drag and dropped in the Tree View.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CannotDragDropAttribute : Attribute
{
}
