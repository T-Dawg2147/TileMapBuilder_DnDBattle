using System.ComponentModel;
using System.Windows;
using TileMapBuilder.Core.ViewModels.Dialogs;

namespace TileMapBuilder.App.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ImportTileDialog.xaml
    /// </summary>
    public partial class ImportTileDialog : Window
    {
        public ImportTileDialog()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ImportTileViewModel oldVm)
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;

            if (e.NewValue is ImportTileViewModel newVm)
                newVm.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ImportTileViewModel.DialogResult))
            {
                var vm = (ImportTileViewModel)sender!;
                if (vm.DialogResult.HasValue)
                {
                    DialogResult = vm.DialogResult.Value;
                }
            }
        }
    }
}
