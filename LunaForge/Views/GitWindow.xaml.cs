using LunaForge.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LunaForge.Views;

public partial class GitWindow : Window
{
    private GitVM vm = null!;
    public string ProjectRoot { get; }

    public GitWindow(string projectRoot, Window? owner = null)
    {
        InitializeComponent();

        vm = new GitVM(projectRoot);
        DataContext = vm;

        ProjectRoot = projectRoot;
        AttachPlaceholder();

        Closed += (_, _) => vm.Dispose();
    }

    private void AttachPlaceholder()
    {
        Loaded += (_, _) =>
        {
            var box = FindCommitTextBox(this);
            if (box == null) return;

            var placeholder = new TextBlock
            {
                Text = "Commit message..;",
                Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x60)),
                IsHitTestVisible = false,
                Margin = new Thickness(8, 5, 0, 0),
                FontSize = 12
            };

            placeholder.SetBinding(VisibilityProperty, new Binding(nameof(TextBox.Text))
            {
                Source = box,
                Converter = new BoolToVisibilityConverter(),
                ConverterParameter = "Inverse",
            });

            var layer = AdornerLayer.GetAdornerLayer(box);
            if (layer != null)
                layer.Add(new PlaceholderAdorner(box, placeholder));
        };
    }

    private static TextBox? FindCommitTextBox(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is TextBox tb && tb.AcceptsReturn)
                return tb;
            var result = FindCommitTextBox(child);
            if (result != null) return result;
        }
        return null;
    }

    public class PlaceholderAdorner : Adorner
    {
        private readonly UIElement _placeholder;

        public PlaceholderAdorner(UIElement adornedElement, UIElement placeholder)
            : base(adornedElement)
        {
            _placeholder = placeholder;
            AddVisualChild(placeholder);
            IsHitTestVisible = false;
        }

        protected override int VisualChildrenCount => 1;
        protected override Visual GetVisualChild(int index) => _placeholder;

        protected override Size ArrangeOverride(Size finalSize)
        {
            _placeholder.Arrange(new Rect(finalSize));
            return finalSize;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            var box = (TextBox)AdornedElement;
            _placeholder.Visibility = (string.IsNullOrEmpty(box.Text) && !box.IsFocused)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}
