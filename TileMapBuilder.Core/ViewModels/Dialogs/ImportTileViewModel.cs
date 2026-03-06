using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDBattle.Data.Models.Tiles;
using System.Collections.ObjectModel;
using TileMapBuilder.Core.Services.Interfaces;

namespace TileMapBuilder.Core.ViewModels.Dialogs
{
    public sealed partial class ImportTileViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FileCount))]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        private List<string> _selectedFilePaths = [];

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        private string _selectedCategory = string.Empty;

        [ObservableProperty] private bool? _dialogResult;

        public ObservableCollection<string> AvailableCategories { get; }

        public int FileCount => SelectedFilePaths.Count;

        public ImportTileViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            AvailableCategories = new ObservableCollection<string>(
                Enum.GetNames<TileLayer>().Append("General"));
        }

        [RelayCommand]
        private void BrowseFiles()
        {
            var filePath = _dialogService.ShowOpenFileDialog(
                "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                "Select Tile Images");

            if (filePath != null)
            {
                SelectedFilePaths = [filePath];
            }
        }

        private bool CanConfirm()
            => SelectedFilePaths.Count > 0 && !string.IsNullOrWhiteSpace(SelectedCategory);

        [RelayCommand(CanExecute = nameof(CanConfirm))]
        private void Confirm() => DialogResult = true;

        [RelayCommand]
        private void Cancel() => DialogResult = false;
    }
}
