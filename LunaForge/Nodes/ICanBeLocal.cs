using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Nodes;

/// <summary>
/// Interface for local-able nodes, like Modules.
/// </summary>
public interface ICanBeLocal
{
    public string IsLocal { get; set; }
}
