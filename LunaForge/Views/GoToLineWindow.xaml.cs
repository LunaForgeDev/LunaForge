using System.Windows;

namespace LunaForge.Views;

public partial class GoToLineWindow : Window
{
    public int LineNumber { get; private set; }

    public GoToLineWindow()
    {
        InitializeComponent();
    }

    private void OnGoClick(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(LineInput.Text, out int line) && line > 0)
        {
            LineNumber = line;
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("Invalid line number.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
