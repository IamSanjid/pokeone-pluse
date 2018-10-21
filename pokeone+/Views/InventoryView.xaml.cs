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
using Poke1Bot;
using Poke1Protocol;


namespace pokeone_plus
{
    /// <summary>
    /// Interaction logic for InventoryView.xaml
    /// </summary>
    public partial class InventoryView : UserControl
    {
        private BotClient _bot;
        public InventoryView(BotClient bot)
        {
            InitializeComponent();
            _bot = bot;
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ItemsListView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
