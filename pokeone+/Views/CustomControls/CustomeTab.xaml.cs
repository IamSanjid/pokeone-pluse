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
    /// Interaction logic for CustomeTab.xaml
    /// </summary>
    public partial class CustomeTab : UserControl
    {
        public CustomeTab()
        {
            InitializeComponent();
        }

        private void UserControl_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TabWindow.Items.Count > 0)
                VerticalLine.Background = (Brush)new BrushConverter().ConvertFromString("#007ACC");
        }

        private void UserControl_LostFocus(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
