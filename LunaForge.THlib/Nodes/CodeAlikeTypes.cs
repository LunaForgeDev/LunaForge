using LunaForge.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.THlib.Nodes;

public class CodeAlikeTypes : ITypeEnumerable
{
    private static readonly Type[] types = [];

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
