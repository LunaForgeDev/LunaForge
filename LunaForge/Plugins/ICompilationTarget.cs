using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Plugins;

[Flags]
public enum SupportedBranches
{
    Plus,
    ExPlus,
    Sub,
    Flux,
    Evo,

    All = Plus | ExPlus | Sub | Flux | Evo,
}

public interface ICompilationTarget
{
    public string TargetName { get; } // THlib example: "THlib"

    public SupportedBranches SupportedBranches { get; } // THlib example: ["ExPlus", "Sub", "Flux", "Evo", ...]

    public string BuildDirectory { get; } // Thlib example: "mod/"
    public bool SupportStageDebug { get; } // THlib example: true, because debugger.lua exists
    public bool SupportSCDebug { get; } // THlib example: true, because scdebugger.lua exists.

    public string RootBaseContents { get; } // THlib example: "Include('THlib.lua')"

    public void BeforeRun(); // Called just before running the lstg executable.
}
