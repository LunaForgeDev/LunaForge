using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LunaForge.Services;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace LunaForge.ViewModels;

public partial class AddNodeVM : ObservableObject
{
    private readonly Window window;
    private readonly ICollectionView filteredItems;
    public ICollectionView FilteredItems => filteredItems;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    private ToolboxItem? selectedItem;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    private string searchText = string.Empty;

    public AddNodeVM() { }

    public AddNodeVM(Window window)
    {
        this.window = window;

        var allItems = MainWindowModel.Project?.ToolboxService?.GetAllItems()
                       ?? [];

        var source = new CollectionViewSource { Source = allItems.ToList() };
        filteredItems = source.View;
        filteredItems.Filter = FilterItem;
        SelectedItem = filteredItems.Cast<ToolboxItem>().FirstOrDefault();
    }

    partial void OnSearchTextChanged(string value)
    {
        filteredItems.Refresh();
        SelectedItem = filteredItems.Cast<ToolboxItem>().FirstOrDefault();
    }

    private bool FilterItem(object obj)
    {
        if (obj is not ToolboxItem item)
            return false;
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;
        return item.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.CategoryName.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand(CanExecute = nameof(CanOk))]
    private void Ok()
    {
        window.DialogResult = true;
    }
    private bool CanOk() => SelectedItem != null;

    [RelayCommand]
    private void Cancel()
    {
        window.DialogResult = false;
    }
}
