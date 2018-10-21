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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace pokeone_plus
{
    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow : UserControl
    {
        public LogWindow()
        {
            InitializeComponent();
        }
        private void UserControl_GotFocus(object sender, RoutedEventArgs e)
        {
            Header.Background = (Brush)new BrushConverter().ConvertFromString("#007ACC");
        }

        private void UserControl_LostFocus(object sender, RoutedEventArgs e)
        {
            Header.Background = (Brush)new BrushConverter().ConvertFromString("#2D2D30");
        }
    }
}
