using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Backend.Attributes.TreeNodesAttributes;

/// <summary>
/// Identify a <see cref="TreeNode"/> is a folder.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class IsFolderAttribute : Attribute
{

}