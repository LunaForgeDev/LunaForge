using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Controls.LuaEditor;

public class LuaApiItem
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public string Usage { get; set; }
    public List<LuaParameter> Parameters { get; set; } = [];
}

public class LuaParameter
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
}