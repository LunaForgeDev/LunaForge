using CommunityToolkit.Mvvm.Input;
using LunaForge.Services;
using System.ComponentModel;
using System.Windows.Data;

namespace LunaForge.ViewModels;

public partial class MainWindowModel
{
    public bool ShowErrors
    {
        get => TraceService.Instance.ShowErrors;
        set { TraceService.Instance.ShowErrors = value; OnPropertyChanged(); }
    }

    public bool ShowWarnings
    {
        get => TraceService.Instance.ShowWarnings;
        set { TraceService.Instance.ShowWarnings = value; OnPropertyChanged(); }
    }

    public bool ShowInformation
    {
        get => TraceService.Instance.ShowInformation;
        set { TraceService.Instance.ShowInformation = value; OnPropertyChanged(); }
    }

    public int ErrorCount => TraceService.Instance.ErrorCount;
    public int WarningCount => TraceService.Instance.WarningCount;
    public int InformationCount => TraceService.Instance.InformationCount;

    private ICollectionView? _filteredTraceEntriesCache;
    public ICollectionView FilteredTraceEntries =>
        _filteredTraceEntriesCache ??= SubscribeAndGetView();

    private ICollectionView SubscribeAndGetView()
    {
        TraceService.Instance.PropertyChanged += OnTraceServicePropertyChanged;
        return TraceService.Instance.FilteredEntries;
    }

    private void OnTraceServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
        => OnPropertyChanged(e.PropertyName);

    [RelayCommand]
    private void ClearTraces() => TraceService.Instance.Clear();
}
