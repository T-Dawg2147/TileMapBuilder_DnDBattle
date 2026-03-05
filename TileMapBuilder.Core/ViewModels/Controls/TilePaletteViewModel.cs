using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TileMapBuilder.Core.Models.Tiles;
using TileMapBuilder.Core.Services.Interfaces;
using TileMapBuilder.Core.Services.TileService;

namespace TileMapBuilder.Core.ViewModels.Controls
{
    public partial class TilePaletteViewModel : ObservableObject
    {
        private IDialogService _dialogService;

        public event Action<TileDefinition> TileSelected;

        [ObservableProperty] private string _searchFilter = "";

        public TilePaletteViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        private void LoadTiles()
        {
            try
            {
                TileLibraryService.Instance.LoadTileLibrary();

                ApplyFilter();
            }
            catch (Exception ex)
            {
                // NOTE Some kind of pop up, maybe initialize the user to locate the tile files?
            }
        }

        private void ApplyFilter()
        {
            var allTiles = TileLibraryService.Instance.AvailableTiles;

            if (allTiles == null || allTiles.Count == 0)
            {
                // TODO Need to make some way to clear the TilesList element that will hold all tiles
                return;
            }

            var filtered = string.IsNullOrWhiteSpace(SearchFilter)
                ? allTiles
                : allTiles.Where(t =>
                    (t.DisplayName ?? t.Id).Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                    (t.Category ?? "").Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));

            var grouped = filtered
                .GroupBy(t => t.Category ?? "General")
                .OrderBy(g => g.Key)
                .ToList();

            // TODO Again, i need some way to set grouped to be the TilesList ItemSource
        }

        [RelayCommand]
        private void TextSearch(string searchText)
        {
            SearchFilter = searchText;
            ApplyFilter();
        }

        [RelayCommand]
        private void RefreshLibrary()
            => LoadTiles();

        [RelayCommand]
        private async Task ImportTiles() // TODO hmmmm... This one is a little more confusing.
                                         // In the original, it wants TileImportService and TileCategoryDialog,
                                         // two classes i dont currently have, but im not even sure if i need them?
                                         // TileImportService Is onlt used here for 2 methods, maybe they could be moved into this class?
        {
            try
            {
                var dialog = new OpenFileDialog()
                {
                    Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                    Title = "Import Tile Images",
                    Multiselect = true
                };

                if (dialog.ShowDialog() == true)
                {
                    //var importService = new TileImportService
                }
            }
        }
    }
}
