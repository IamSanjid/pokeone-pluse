using System;
using System.Windows;
using System.Windows.Controls;

namespace pokeone_plus
{
    public partial class ButtonTabHeader : UserControl
    {
        public Action CloseButton;
        public ButtonTabHeader()
        {
            InitializeComponent();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            CloseButton?.Invoke();
        }
    }
}
