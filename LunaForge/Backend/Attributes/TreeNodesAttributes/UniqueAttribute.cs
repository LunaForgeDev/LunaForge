using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Backend.Attributes.TreeNodesAttributes;

/// <summary>
/// Identify a <see cref="TreeNode"/> must be unique within the current context.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class UniqueAttribute : Attribute
{

}