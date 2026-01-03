using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Backend.Attributes.TreeNodesAttributes;

/// <summary>
/// Identify a <see cref="TreeNode"/> need ancestor of give type.
/// Types are idenfitied by OR operator.
/// Multiple attributes are connected by AND operator.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequireAncestorAttribute(params Type[] typed) : Attribute
{
    public Type[] RequiredTypes { get; } = typed;
}