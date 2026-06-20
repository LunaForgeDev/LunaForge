using LunaForge.Nodes.General;
using LunaForge.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Nodes;

// TODO: Create aggregation types to be overridden by plugins, so that plugins can add their own code-alike types to the list as well as keep the existing ones.
public class CodeAlikeTypes : ITypeEnumerable
{
    private static readonly Type[] types = [
        typeof(DefineFunction)
    ];

    public IEnumerator<Type> GetEnumerator()
    {
        foreach (Type t in types)
            yield return t;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}