using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using LunaForge.Models.TreeNodes;

namespace LunaForge.Backend.Attributes.TreeNodesAttributes;

/// <summary>
/// Marks a <see cref="TreeNode"/> subclass as an exported symbol and declares its type name and parameter list for the symbol indexer.<br/>
/// Example usage: <c>[ExportSymbol("Enemy", "name", "hp", "speed")]</c>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ExportSymbolAttribute(string symbolType, params string[] parameters) : Attribute
{
    public string SymbolType { get; set; } = symbolType;
    public string[] Parameters { get; set; } = parameters;
}
