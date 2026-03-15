using LunaForge.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace LunaForge.Views
{
    public partial class AddNodeWindow : Window
    {
        public AddNodeWindow()
        {
            InitializeComponent();
            DataContext = new AddNodeVM(this);
            Loaded += (_, _) => SearchBox.Focus();
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                NodeListBox.Focus();
                if (NodeListBox.Items.Count > 0)
                    NodeListBox.SelectedIndex = 0;
                e.Handled = true;
            }
        }

        private void NodeListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is AddNodeVM vm)
            {
                if (vm.OkCommand.CanExecute(null))
                    vm.OkCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
