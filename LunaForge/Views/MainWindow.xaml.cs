using AvalonDock;
using LunaForge.Models;
using LunaForge.ViewModels;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Application = System.Windows.Application;
using TreeNode = LunaForge.Models.TreeNodes.TreeNode;

namespace LunaForge
{
    public partial class MainWindow : Window
    {
        private MainWindowModel _viewModel;

        public MainWindow(string projectPath)
        {
            InitializeComponent();
            _viewModel = new MainWindowModel(projectPath);
            DataContext = _viewModel;

            Closing += MainWindow_Closing;
            Closed += (s, e) =>
            {
                Application.Current.Shutdown();
            };
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!_viewModel.HandleWindowClosing())
            {
                e.Cancel = true;
            }
        }

        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem item && item.DataContext is Models.FileSystemItem fsItem)
            {
                if (e.OriginalSource is FrameworkElement element)
                {
                    var clickedItem = FindParent<TreeViewItem>(element);
                    if (clickedItem != item)
                        return;
                }

                var viewModel = (MainWindowModel)DataContext;
                viewModel.FileViewer.ItemDoubleClickCommand.Execute(fsItem);
                e.Handled = true;
            }
        }

        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item && item.DataContext is Models.FileSystemItem fsItem)
            {
                if (e.OriginalSource != item)
                    return;

                var viewModel = (MainWindowModel)DataContext;
                viewModel.FileViewer.ItemExpandedCommand.Execute(fsItem);
            }
        }

        private void TreeNode_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item && item.DataContext is TreeNode node)
            {
                if (e.OriginalSource != item)
                    return;

                var viewModel = (MainWindowModel)DataContext;
                if (viewModel.SelectedFile != null)
                {
                    viewModel.SelectedFile.SelectedNode = node;
                }
            }
        }

        private void AttributeTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommitAttributeChange(sender);
                e.Handled = true;
            }
        }

        private void AttributeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CommitAttributeChange(sender);
        }

        private void CommitAttributeChange(object sender)
        {
            string newValue = null;
            Models.TreeNodes.NodeAttribute attribute = null;

            if (sender is TextBox textBox)
            {
                newValue = textBox.Text;
                attribute = textBox.DataContext as Models.TreeNodes.NodeAttribute;
            }
            else if (sender is ComboBox comboBox)
            {
                newValue = comboBox.Text;
                attribute = comboBox.DataContext as Models.TreeNodes.NodeAttribute;
            }

            if (attribute != null && newValue != null)
            {
                attribute.ChangeValueWithCommand(newValue);
                Keyboard.ClearFocus();
            }
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
                return null;

            if (parentObject is T parent)
                return parent;

            return FindParent<T>(parentObject);
        }
    }
}