using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace LunaForge.Backend.Attributes.TreeNodesAttributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class NodeAttributeAttribute(string defaultValue = "", [CallerMemberName] string name = "") : Attribute
{
    public string Name { get; set; } = name;
    public string DefaultValue { get; set; } = defaultValue;
}
