using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using TileMapBuilder.Core.Models.Tiles;
using TileMapBuilder.Core.Services;
using TileMapBuilder.Core.Services.TileService;

namespace TileMapBuilder.Core.ViewModels.TileViewModels
{
    public partial class TileMapEditorViewModel
    {
        private TileMap _currentMap;
        private readonly TileMapService _mapService;
        private readonly IDialogService _dialogService; 

        private readonly string _searchPattern = "*.jpg;*.jpeg;*.png;*.bmp";
        private string _currentFilePath;

        private Canvas Canvas;

        public TileMapEditorViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            Canvas = new();

            TileImageCacheService.Instance.PreloadImages(Directory.EnumerateFiles(
                Path.Combine(AppContext.BaseDirectory, "Resources", "Tiles"),
                _searchPattern,
                SearchOption.AllDirectories));
        }

        [RelayCommand]
        private void CreateNewMap()
        {
            // Needs to open a small window asking for the height/width, name and maybe other properties?
            if (_dialogService.ShowNewTileMapDialog(
                out string tileMapName, out int? width, out int? height))
            {
                _currentMap = new TileMap()
                {
                    Name = tileMapName ?? "New Map",
                    Width = width ?? 50,
                    Height = height ?? 50,
                    CellSize = 48
                };
            }
        }

        [RelayCommand]
        private async Task LoadMap()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Tile Map Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Open Tile Map"
            };

            if (dialog.ShowDialog() == true)
            {
                var map = await _mapService.LoadMapAsync(dialog.FileName);
                if (map != null)
                {
                    _currentMap = map;
                    _currentFilePath = dialog.FileName;
                    EditorControl.TileMap = _currentMap; // OPENMAP - i have no way off the top of my head to get access to this Control?
                    Title = $"Tile Map Builder - {map.Name}"; // OPENMAP - Need to find a way to set the windows title? Preferably still in a MVVM structure
                }
                else
                {
                    MessageBox.Show("Failed to load map.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task SaveMap()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                await SaveMapAs();
                return;
            }

            bool success = (await _mapService.SaveMapAsync(_currentMap, _currentFilePath)) != null;

            // TODO This is a very basic implementation.
            // I would eventually like to be able to write this data to a temp file first to avoid corruptions in the primary file
            if (success)
                _dialogService.ShowInfo("Success", "Map saved successfully"); // Does i really want this to open a window on save?
            else
                _dialogService.ShowInfo("Error", "Failed to save map.", MessageBoxImage.Error);
        }

        [RelayCommand]
        private async Task SaveMapAs()
        {
            var dialog = new SaveFileDialog()
            {
                Filter = "Tile Map Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Save Tile Map As...",
                FileName = _currentMap.Name + ".json"
            };

            if (dialog.ShowDialog() == true)
            {
                _currentFilePath = dialog.FileName;
                bool success = (await _mapService.SaveMapAsync(_currentMap, _currentFilePath)) != null;

                // TODO This is a very basic implementation.
                // I would eventually like to be able to write this data to a temp file first to avoid corruptions in the primary file
                if (success)
                    _dialogService.ShowInfo("Success", "Map saved successfully"); // Does i really want this to open a window on save?
                else
                    _dialogService.ShowInfo("Error", "Failed to save map.", MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ExportAsImage()
        {
            var dialog = new SaveFileDialog()
            {
                Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg",
                Title = "Export Map as Image",
                FileName = _currentFilePath + ".png"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {

                }
                catch (Exception ex)
                {

                }
            }
        }
    }
}
