using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System;
using System.IO;
using System.Reflection;
using System.Xml;

namespace LunaForge.Helpers;

public static class SyntaxHighlightingHelper
{
    private static IHighlightingDefinition? luaHighlighting;

    public static IHighlightingDefinition GetLuaHighlighting()
    {
        if (luaHighlighting != null)
            return luaHighlighting;

        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "LunaForge.Resources.Lua.xshd";

            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return HighlightingManager.Instance.GetDefinition("Lua") 
                    ?? HighlightingManager.Instance.GetDefinition("C#")!;
            }

            using XmlReader reader = XmlReader.Create(stream);
            luaHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            
            HighlightingManager.Instance.RegisterHighlighting(
                "Lua", [".lua"], luaHighlighting);

            return luaHighlighting;
        }
        catch (Exception)
        {
            return HighlightingManager.Instance.GetDefinition("Lua") 
                ?? HighlightingManager.Instance.GetDefinition("C#")!;
        }
    }
}
