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

namespace LunaForge.Views
{
    public partial class CodePreviewWindow : Window
    {
        public string CodeText { get; set; }

        public CodePreviewWindow()
        {
            InitializeComponent();
        }

        public CodePreviewWindow(string code)
            : this()
        {
            CodeText = code;
        }
    }
}
