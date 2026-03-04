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
using TileMapBuilder.Core.ViewModels.Dialogs;

namespace TileMapBuilder.App.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for NewTileMapDialog.xaml
    /// </summary>
    public partial class NewTileMapDialog : Window
    {
        private readonly NewTileMapViewModel _vm = new();

        public NewTileMapDialog()
        {
            InitializeComponent();

            DataContext = _vm;
        }
    }
}
