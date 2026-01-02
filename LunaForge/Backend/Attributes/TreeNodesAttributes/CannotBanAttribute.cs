using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Backend.Attributes.TreeNodesAttributes;

/// <summary>
/// Identify a <see cref="TreeNode"/> cannot be banned.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CannotBanAttribute : Attribute
{

}