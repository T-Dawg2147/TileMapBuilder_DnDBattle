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
    /// Interaction logic for MapPropertiesDialog.xaml
    /// </summary>
    public partial class MapPropertiesDialog : Window
    {
        public MapPropertiesDialog()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is MapPropertiesViewModel vm)
            {
                vm.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(MapPropertiesViewModel.DialogResult))
                    {
                        if (vm.DialogResult.HasValue)
                        {
                            DialogResult = vm.DialogResult;
                            Close();
                        }
                    }
                };
            }
        }
    }
}
