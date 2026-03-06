using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using DnDBattle.Data.Models.Tiles;
using DnDBattle.Data.Services.Interfaces;
using TileMapBuilder.Core.Services.Interfaces;

namespace TileMapBuilder.Core.ViewModels.Controls
{
    public class TileCategoryGroup
    {
        public string Key { get; set; } = string.Empty;
        public List<TileDefinition> Tiles { get; set; } = [];
    }

    public partial class TilePaletteViewModel : ObservableObject
    {
        private IDialogService _dialogService;
        private readonly ITileLibraryService _tileLibraryService;

        [ObservableProperty] private ObservableCollection<TileCategoryGroup> _groupedTiles = [];

        [ObservableProperty] private TileDefinition? _selectedTile;

        [ObservableProperty] private string _searchFilter = "";

        [ObservableProperty] private string _statusText = "No tiles available";

        public event Action<TileDefinition> TileSelected;

        public TilePaletteViewModel(IDialogService dialogService, ITileLibraryService tileLibraryService)
        {
            _dialogService = dialogService;
            _tileLibraryService = tileLibraryService;

            LoadTiles();
        }

        [RelayCommand]
        private void RefreshLibrary()
            => LoadTiles();

        private void LoadTiles()
        {
            try
            {
                _tileLibraryService.LoadTileLibrary();
                ApplyFilter();

                int totalTiles = _tileLibraryService.AvailableTiles.Count;
                StatusText = $"{totalTiles} available"; ; 
            }
            catch (Exception ex)
            {
                // NOTE Some kind of pop up, maybe initialize the user to locate the tile files?
                Debug.WriteLine($"[TilePalette] Error loading tiles: {ex.Message}");
                StatusText = $"Error: {ex.Message}";
                _dialogService.ShowInfo("Tile Library Error",
                    $"Failed to load tile library:\n\n{ex.Message}",
                    DialogIcon.Warning);
            }
        }

        [RelayCommand]
        private void TextSearch(string searchText)
        {
            SearchFilter = searchText ?? "";
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var allTiles = _tileLibraryService.AvailableTiles;

            if (allTiles == null || allTiles.Count == 0)
            {
                GroupedTiles.Clear();
                StatusText = "No tiles available";
                return;
            }

            var filtered = string.IsNullOrWhiteSpace(SearchFilter)
                ? allTiles.AsEnumerable()
                : allTiles.Where(t =>
                    (t.DisplayName ?? t.Id).Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                    (t.Category ?? "").Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));

            var grouped = filtered
                .GroupBy(t => t.Category ?? "General")
                .OrderBy(g => g.Key)
                .Select(g => new TileCategoryGroup()
                {
                    Key = g.Key,
                    Tiles = g.ToList()
                })
                .ToList();

            // TODO Again, i need some way to set grouped to be the TilesList ItemSource
            GroupedTiles.Clear();
            foreach (var group in grouped)
                GroupedTiles.Add(group);

            int filteredCount = grouped.Sum(g => g.Tiles.Count);
            StatusText = !string.IsNullOrWhiteSpace(SearchFilter)
                ? $"{filteredCount} tiles match '{SearchFilter}'"
                : $"{filteredCount} tiles available";
        }

        [RelayCommand]
        private void SelectTile(TileDefinition? tile)
        {
            if (tile == null) return;

            SelectedTile = tile;
            TileSelected?.Invoke(tile);

            StatusText = $"Selected: {tile.DisplayName ?? tile.Id}";
        }

        [RelayCommand]
        private async Task ImportTiles() 
        {
            try
            {
                if (!_dialogService.ShowImportTileDialog(out var filePaths, out var category))
                    return;

                int imported = await ImportFileToLibraryAsync(filePaths, category);

                if (imported > 0)
                {
                    _dialogService.ShowInfo("Import Complete",
                        $"Successfully imported {imported} tile(s) into '{category}'.");

                    LoadTiles();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowInfo("Import Error",
                    $"Error importing tiles:\n\n{ex.Message}",
                    DialogIcon.Error);
            }
        }

        private async Task<int> ImportFileToLibraryAsync(string[] filePaths, string category)
        {
            var tileDir = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Resources", "Tiles", category);

            Directory.CreateDirectory(tileDir);

            int count = 0;
            foreach (var source in filePaths)
            {
                try
                {
                    var fileName = System.IO.Path.GetFileName(source);
                    var dest = System.IO.Path.Combine(tileDir, fileName);

                    if (!System.IO.File.Exists(dest))
                    {
                        await Task.Run(() => System.IO.File.Copy(source, dest));
                        count++;
                    }
                    else
                    {
                        Debug.WriteLine($"[TilePalette] Skipped (already exists): {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TilePalette] Failed to import {source}: {ex.Message}");
                }
            }
            return count;
        }
    }
}
