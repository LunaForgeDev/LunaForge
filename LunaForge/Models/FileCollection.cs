using LunaForge.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunaForge.Models;

public class FileCollection : ObservableCollection<DocumentFile>
{
    public int MaxHash { get; private set; } = 0;

    public static ILogger Logger { get; private set; } = CoreLogger.Create("FileCollection");

    public new void Add(DocumentFile a)
    {
        base.Add(a);
        a.Parent = this;
        MaxHash += 1;
        Logger.Debug($"Document {a.FileName} added to collection. New MaxHash={MaxHash}.");
    }
}
