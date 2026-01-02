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
    /// Logique d'interaction pour NewFileWindow.xaml
    /// </summary>
    public partial class NewFileWindow : Window
    {
        public NewFileWindow()
        {
            InitializeComponent();
            DataContext = new NewFileWindowModel(this);
        }
    }
}
