using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace LunaForge.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public event Action? RequestClose;

    #region Commands

    [RelayCommand]
    private void ApplyAndClose()
    {
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Apply()
    {
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }

    #endregion
}
