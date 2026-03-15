using LunaForge.Models.Documents;
using LunaForge.Models.TreeNodes;
using System;
using System.Collections.Generic;
using System.Text;

namespace LunaForge.Services;

public readonly record struct LineMapEntry(int StartLine, int EndLine, TreeNode Node);

/// <summary>
/// Time to nuke GetLines() baby
/// </summary>
public class LinesMapService
{
    private readonly List<LineMapEntry> entries = [];
    public IReadOnlyList<LineMapEntry> Entries => entries;

    /// <summary>
    /// Builds every lines from the root node. Call after compilation step or tree changes.
    /// </summary>
    /// <param name="doc"></param>
    public void Build(DocumentFileLFD doc)
    {
        entries.Clear();
        int currentLine = 1;

        // Header of the file
        //currentLine += 3;

        foreach (TreeNode root in doc.TreeNodes)
        {
            foreach (var (lineCount, node) in root.GetLines())
            {
                entries.Add(new(currentLine, currentLine + lineCount - 1, node));
                currentLine += lineCount;
            }
        }
    }

    public TreeNode? FindForLine(int luaLineNumber)
    {
        // Binary search time babyyyyyyy first time ever
        int low = 0, high = entries.Count - 1;
        while (low <= high)
        {
            int mid = (low + high) / 2;
            var entry = entries[mid];

            if (luaLineNumber < entry.StartLine)
                high = mid - 1;
            else if (luaLineNumber > entry.EndLine)
                low = mid + 1;
            else
                return entry.Node;
        }
        return null;
    }
}