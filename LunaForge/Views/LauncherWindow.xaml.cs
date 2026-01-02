using LunaForge.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LunaForge.Views
{
    /// <summary>
    /// Logique d'interaction pour LauncherWindow.xaml
    /// </summary>
    public partial class LauncherWindow : Window
    {
        public LauncherWindow()
        {
            InitializeComponent();

            if (DataContext is LauncherViewModel vm)
            {
                vm.ProjectSelected += OpenProject;
                vm.RequestClose += Close;
            }
        }

        private void OpenProject(string projectPath)
        {
            MainWindow main = new(projectPath);
            main.Show();
        }
    }
}
