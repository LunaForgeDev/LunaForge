using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace LunaForge.Services;

public enum TraceSeverity
{
    Error,
    Warning,
    Information,
}

public sealed class TraceEntry(TraceSeverity severity, string message, string? file = null, int? line = null, ITraceSource? source = null)
{
    public TraceSeverity Severity { get; } = severity;
    public string Message { get; } = message;
    public string? File { get; } = file;
    public int? Line { get; } = line;
    public DateTime Timestamp { get; } = DateTime.Now;
    public ITraceSource? Source { get; } = source;

    public override string ToString()
    {
        return $"[{Severity}]{(Source is null ? "" : $" ({Source.TraceSourceName})")} {Message}";
    }
}

public sealed class TraceHandle
{
    private Action? resolveCallback;

    public TraceEntry Entry { get; }
    public bool IsResolved { get; private set; }

    internal TraceHandle(TraceEntry entry, Action? resolveCallback = null)
    {
        Entry = entry;
        this.resolveCallback = resolveCallback;
    }

    public void Resolve()
    {
        if (IsResolved)
            return;
        IsResolved = true;
        resolveCallback?.Invoke();
        resolveCallback = null;
    }
}

public interface ITraceSource
{
    string TraceSourceName { get; }

    TraceHandle CommitTrace(TraceSeverity severity, string message, string? file = null, int? line = null);
}

public abstract class TraceSource : ITraceSource
{
    public abstract string TraceSourceName { get; }

    public TraceHandle CommitTrace(TraceSeverity severity, string message, string? file = null, int? line = null)
        => TraceService.Instance.Commit(severity, message, this, file, line);
}

public class TraceService : INotifyPropertyChanged
{
    public static TraceService Instance
    {
        get
        {
            field ??= new TraceService();
            return field;
        }
    } = null!;

    private TraceService() { }

    public ObservableCollection<TraceEntry> Entries { get; } = [];

    public event EventHandler<TraceEntry>? TraceAdded;
    public event EventHandler<TraceEntry>? TraceResolved;
    public event EventHandler? TracesCleared;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool showErrors = true;
    private bool showWarnings = true;
    private bool showInformation = true;

    public bool ShowErrors
    {
        get => showErrors;
        set
        {
            if (showErrors != value)
            {
                showErrors = value;
                OnPropertyChanged();
                filteredEntries?.Refresh();
            }
        }
    }

    public bool ShowWarnings
    {
        get => showWarnings;
        set
        {
            if (showWarnings != value)
            {
                showWarnings = value;
                OnPropertyChanged();
                filteredEntries?.Refresh();
            }
        }
    }

    public bool ShowInformation
    {
        get => showInformation;
        set
        {
            if (showInformation != value)
            {
                showInformation = value;
                OnPropertyChanged();
                filteredEntries?.Refresh();
            }
        }
    }

    public int ErrorCount => Entries.Count(e => e.Severity == TraceSeverity.Error);
    public int WarningCount => Entries.Count(e => e.Severity == TraceSeverity.Warning);
    public int InformationCount => Entries.Count(e => e.Severity == TraceSeverity.Information);

    private ICollectionView? filteredEntries;
    public ICollectionView FilteredEntries => filteredEntries ??= CreateFilteredView();

    private ICollectionView CreateFilteredView()
    {
        var view = CollectionViewSource.GetDefaultView(Entries);
        view.Filter = FilterTrace;
        TraceAdded += (_, _) => RefreshCounts();
        TraceResolved += (_, _) => RefreshCounts();
        TracesCleared += (_, _) => RefreshCounts();
        return view;
    }

    private bool FilterTrace(object obj) => obj is TraceEntry entry && entry.Severity switch
    {
        TraceSeverity.Error => ShowErrors,
        TraceSeverity.Warning => ShowWarnings,
        TraceSeverity.Information => ShowInformation,
        _ => true
    };

    private void RefreshCounts()
    {
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(WarningCount));
        OnPropertyChanged(nameof(InformationCount));
        filteredEntries?.Refresh();
    }

    public TraceHandle Commit(
        TraceSeverity severity,
        string message,
        ITraceSource? source = null,
        string? file = null,
        int? line = null)
    {
        var entry = new TraceEntry(severity, message, file, line, source);
        var handle = new TraceHandle(entry, () => Remove(entry));

        RunOnUI(() =>
        {
            Entries.Add(entry);
            TraceAdded?.Invoke(this, entry);
        });

        return handle;
    }

    public void Clear()
    {
        RunOnUI(() =>
        {
            Entries.Clear();
            TracesCleared?.Invoke(this, EventArgs.Empty);
        });
    }

    public void ClearFrom(ITraceSource source)
    {
        RunOnUI(() =>
        {
            for (int i = Entries.Count - 1; i >= 0; i--)
                if (ReferenceEquals(Entries[i].Source, source))
                    Entries.RemoveAt(i);
        });
    }

    private void Remove(TraceEntry entry)
    {
        RunOnUI(() =>
        {
            if (Entries.Remove(entry))
                TraceResolved?.Invoke(this, entry);
        });
    }

    private static void RunOnUI(Action action)
    {
        if (Application.Current?.Dispatcher is { } d && !d.CheckAccess())
            d.Invoke(action);
        else
            action();
    }
}
