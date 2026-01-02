using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Backend.Attributes.TreeNodesAttributes;

/// <summary>
/// Identify a <see cref="TreeNode"/> cannot be deleted.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CannotDeleteAttribute : Attribute
{

}