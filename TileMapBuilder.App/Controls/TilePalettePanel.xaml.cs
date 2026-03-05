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
using TileMapBuilder.Core.ViewModels.Controls;

namespace TileMapBuilder.App.Controls
{
    /// <summary>
    /// Interaction logic for TilePalettePanel.xaml
    /// </summary>
    public partial class TilePalettePanel : UserControl
    {
        public TilePalettePanel()
        {
            InitializeComponent();
        }

        private TilePaletteViewModel? ViewModel => DataContext as TilePaletteViewModel;

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel?.TextSearchCommand.Execute(TxtSearch.Text);
        }
    }
}
