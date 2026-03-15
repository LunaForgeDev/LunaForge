using LunaForge.Backend.EditorCommands;
using LunaForge.Models;
using LunaForge.Models.Documents;
using LunaForge.Models.TreeNodes;
using LunaForge.Services;
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
                //this.Focus(); // TODO: See to fix this. Using this.Focus(); doesn't clear the focus to the control.
                // But not doing this doesn't redirect the focus to the window, so keybinds can't be used till focus is regiven to a control.
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

        #region LFD Drag/Drop

        private Point dragStartPoint;
        private bool isDragging;
        private TreeNode? draggedNode;
        private TreeViewItem? dragSourceItem;
        private AdornerLayer? adornerLayer;
        private DropPositionAdorner? currentAdorner;
        private TreeViewItem? adornerTarget;
        private InsertMode adornerMode;

        private void TreeNode_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragStartPoint = e.GetPosition(null);

            if (e.OriginalSource is DependencyObject source)
                dragSourceItem = FindParent<TreeViewItem>(source);
        }

        private void TreeNode_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || isDragging)
                return;

            if (sender is not TreeViewItem item || item != dragSourceItem)
                return;

            Vector diff = dragStartPoint - e.GetPosition(null);
            if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            if (item.DataContext is not TreeNode node)
                return;

            // Special case for root or undeletable.
            // TODO: Node Metadata for this (CanBeDragSource, CanBeDragDestination)
            if (node.ParentNode == null || !node.CanLogicallyDelete() || node.MetaData.CannotBeDragDropped)
                return;

            isDragging = true;
            try
            {
                draggedNode = node;
                DataObject dragData = new("LFDTreeNode", true);
                DragDrop.DoDragDrop(item, dragData, DragDropEffects.Move);
            }
            finally
            {
                isDragging = false;
                draggedNode = null;
                dragSourceItem = null;
                ClearDropAdorner();
            }
        }

        private void TreeNode_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;

            if (!e.Data.GetDataPresent("LFDTreeNode")
                || draggedNode == null
                || sender is not TreeViewItem item
                || item.DataContext is not TreeNode targetNode)
            {
                e.Handled = true;
                return;
            }

            if (draggedNode == targetNode || IsDescendantOf(draggedNode, targetNode) || targetNode.MetaData.CannotBeDragDropped)
            {
                ClearDropAdorner();
                return;
            }

            InsertMode mode = GetDropInsertMode(item, e);

            TreeNode validationParent = mode == InsertMode.Child ? targetNode : targetNode.ParentNode;
            if (validationParent != null && validationParent.ValidateChild(draggedNode))
            {
                e.Effects = DragDropEffects.Move;
                UpdateDropAdorner(item, mode);
            }
            else
            {
                ClearDropAdorner();
            }

            e.Handled = true;
        }

        private void TreeNode_DragLeave(object sender, DragEventArgs e)
        {
            ClearDropAdorner();
        }

        private void TreeNode_Drop(object sender, DragEventArgs e)
        {
            ClearDropAdorner();
            e.Handled = true;

            if (!e.Data.GetDataPresent("LFDTreeNode")
                || draggedNode == null
                || sender is not TreeViewItem item
                || item.DataContext is not TreeNode targetNode)
                return;

            if (draggedNode == targetNode || IsDescendantOf(draggedNode, targetNode) || targetNode.MetaData.CannotBeDragDropped)
                return;

            if (_viewModel.SelectedFile is not DocumentFileLFD doc)
                return;

            InsertMode mode = GetDropInsertMode(item, e);

            TreeNode validationParent = mode == InsertMode.Child ? targetNode : targetNode.ParentNode;
            if (validationParent == null || !validationParent.ValidateChild(draggedNode))
                return;

            TreeNode nodeToMove = draggedNode;
            TreeNode target = targetNode;

            Dispatcher.BeginInvoke(() =>
            {
                var command = new MoveNodeCommand(nodeToMove, target, mode);
                doc.AddAndExecuteCommand(command);
                nodeToMove.FixParentDoc(doc);
            });
        }

        private static FrameworkElement GetHeaderElement(TreeViewItem item)
        {
            return item.Template.FindName("Bd", item) as FrameworkElement
                ?? item.Template.FindName("PART_Header", item) as FrameworkElement
                ?? item;
        }

        /// <summary>
        /// Determines the insert mode based on cursor pos relative to the header of the node.
        /// Top 30% -> Before. Bottom 30% -> After. Middle 40% -> Child.
        /// </summary>
        private static InsertMode GetDropInsertMode(TreeViewItem item, DragEventArgs e)
        {
            FrameworkElement header = GetHeaderElement(item);
            Point pos = e.GetPosition(header);

            double height = header == item ? Math.Min(item.ActualHeight, 26.0) : header.ActualHeight;
            if (height <= 0 || pos.Y < 0 || pos.Y > height)
                return InsertMode.Child;

            double ratio = pos.Y / height;
            if (ratio <= 0.30)
                return InsertMode.Before;
            if (ratio >= 0.70)
                return InsertMode.After;
            return InsertMode.Child;
        }

        private static bool IsDescendantOf(TreeNode potentialAncestor, TreeNode node)
        {
            TreeNode current = node.ParentNode;
            while (current != null)
            {
                if (current == potentialAncestor)
                    return true;
                current = current.ParentNode;
            }
            return false;
        }

        #region Drop Adorner

        private void UpdateDropAdorner(TreeViewItem item, InsertMode mode)
        {
            if (adornerTarget == item && adornerMode == mode && currentAdorner != null)
                return;

            ClearDropAdorner();

            FrameworkElement header = GetHeaderElement(item);
            adornerLayer = AdornerLayer.GetAdornerLayer(header);
            if (adornerLayer != null)
            {
                currentAdorner = new DropPositionAdorner(item, mode);
                adornerTarget = item;
                adornerMode = mode;
                adornerLayer.Add(currentAdorner);
            }
        }

        private void ClearDropAdorner()
        {
            if (currentAdorner != null && adornerLayer != null)
            {
                adornerLayer.Remove(currentAdorner);
                currentAdorner = null;
                adornerLayer = null;
            }
            adornerTarget = null;
        }

        private sealed class DropPositionAdorner : Adorner
        {
            private static readonly Pen IndicatorPen = new(new SolidColorBrush(Color.FromRgb(108, 156, 246)), 2);
            private static readonly Brush HighlightBrush = new SolidColorBrush(Color.FromArgb(40, 108, 156, 246));

            private readonly InsertMode _mode;

            static DropPositionAdorner()
            {
                IndicatorPen.Freeze();
                HighlightBrush.Freeze();
            }

            public DropPositionAdorner(UIElement adornedElement, InsertMode mode) : base(adornedElement)
            {
                _mode = mode;
                IsHitTestVisible = false;
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                double width = AdornedElement.RenderSize.Width;
                double height = AdornedElement.RenderSize.Height;

                switch (_mode)
                {
                    case InsertMode.Before:
                        drawingContext.DrawLine(IndicatorPen, new Point(0, 0), new Point(width, 0));
                        break;
                    case InsertMode.After:
                        drawingContext.DrawLine(IndicatorPen, new Point(0, height), new Point(width, height));
                        break;
                    case InsertMode.Child:
                        drawingContext.DrawRectangle(HighlightBrush, IndicatorPen, new Rect(0, 0, width, height));
                        break;
                }
            }
        }

        #endregion

        #endregion

        private void OnEditAttributeClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not NodeAttribute attribute)
            {
                CoreLogger.Logger.Warning("OnEditAttributeClick: could not resolve NodeAttribute from sender");
                return;
            }

            attribute.OpenEditor(this);
        }
    }
}