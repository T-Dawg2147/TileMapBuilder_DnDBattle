using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using TileMapBuilder.App.Services;
using TileMapBuilder.Core.ViewModels.Dialogs;

namespace TileMapBuilder.App.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for NewTileMapDialog.xaml
    /// </summary>
    public partial class NewTileMapDialog : Window
    {
        public NewTileMapDialog()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is NewTileMapViewModel oldVm)
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;

            if (e.NewValue is NewTileMapViewModel newVm)
                newVm.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NewTileMapViewModel.DialogResult))
            {
                var vm = (NewTileMapViewModel)sender!;
                if (vm.DialogResult.HasValue)
                {
                    DialogResult = vm.DialogResult.Value;
                }
            }
        }
    }
}
