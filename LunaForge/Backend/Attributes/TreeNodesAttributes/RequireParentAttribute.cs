using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Backend.Attributes.TreeNodesAttributes;

/// <summary>
/// Identify a <see cref="TreeNode"/> need parent of give type.
/// Types are idenfitied by OR operator.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequireParentAttribute(params Type[] parent) : Attribute
{
    public Type[] ParentType { get; } = parent;
}