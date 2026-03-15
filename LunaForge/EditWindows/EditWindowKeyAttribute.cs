using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.EditWindows;

/// <summary>
/// Gives an <see cref="EditWindow"/> subclass a *unique* key used for the registry.
/// </summary>
/// <param name="key"></param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class EditWindowKeyAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}
