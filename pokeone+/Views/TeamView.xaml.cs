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
    /// Interaction logic for TeamView.xaml
    /// </summary>
    public partial class TeamView : UserControl
    {
        private BotClient _bot;
        private Point _startPoint;
        private Pokemon _selectedPokemon;
        public TeamView(BotClient bot)
        {
            InitializeComponent();
            _bot = bot;
        }

        private void PokemonsListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        private void PokemonsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PokemonsListView.SelectedItems.Count > 0)
            {
                _selectedPokemon = (Pokemon)PokemonsListView.SelectedItems[0];
            }
            else
            {
                _selectedPokemon = null;
            }
        }

        private void PokemonsListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void PokemonsListView_MouseMove(object sender, MouseEventArgs e)
        {
            // Get the current mouse position
            Point mousePos = e.GetPosition(null);
            Vector diff = _startPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                // Get the dragged ListViewItem
                ListView listView = sender as ListView;
                ListViewItem listViewItem =
                    FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);

                if (listViewItem != null)
                {
                    // Find the data behind the ListViewItem
                    Pokemon pokemon = (Pokemon)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);

                    // Initialize the drag & drop operation
                    DataObject dragData = new DataObject("PokeOnePokemon", pokemon);
                    DragDrop.DoDragDrop(listViewItem, dragData, DragDropEffects.Move);
                }
            }
        }

        private static T FindAnchestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        private void PokemonsListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("PokeOnePokemon"))
            {
                Pokemon sourcePokemon = e.Data.GetData("PokeOnePokemon") as Pokemon;
                // Get the dragged ListViewItem
                ListView listView = sender as ListView;
                ListViewItem listViewItem =
                    FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);

                if (listViewItem != null)
                {
                    // Find the data behind the ListViewItem
                    Pokemon destinationPokemon = (Pokemon)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);

                    lock (_bot)
                    {
                        if (_bot.Game != null)
                        {
                            _bot.Game.SwapPokemon(sourcePokemon.Uid, destinationPokemon.Uid);

                        }
                    }
                }
            }
        }

        private void PokemonsListView_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("PokeOnePokemon") || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }
    }
}
