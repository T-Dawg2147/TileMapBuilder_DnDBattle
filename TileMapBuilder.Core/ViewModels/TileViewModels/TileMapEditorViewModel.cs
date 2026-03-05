using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows;
using TileMapBuilder.Core.Models.Tiles;
using TileMapBuilder.Core.Services.Interfaces;
using TileMapBuilder.Core.Services.TileService;

namespace TileMapBuilder.Core.ViewModels.TileViewModels
{
    public partial class TileMapEditorViewModel : ObservableObject
    {
        private readonly ITileMapService _mapService;
        private readonly IDialogService _dialogService;
        private readonly IMapVisualProvider _mapVisualProvider;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(WindowTitle))]
        private TileMap? _currentMap;

        public string WindowTitle => CurrentMap != null
            ? $"Tile Map Builder - {CurrentMap.Name}"
            : "Tile Map Builder";

        private readonly string _searchPattern = "*.jpg;*.jpeg;*.png;*.bmp";
        [ObservableProperty] private string _currentFilePath = string.Empty;

        public TileMapEditorViewModel(ITileMapService mapService, IDialogService dialogService, IMapVisualProvider mapVisualProvider)
        {
            _dialogService = dialogService;
            _mapService = mapService;
            _mapVisualProvider = mapVisualProvider;

            var tileResources = Path.Combine(AppContext.BaseDirectory, "Resources", "Tiles");
            if (!Directory.Exists(tileResources))
                Directory.CreateDirectory(tileResources);

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
                CurrentMap = new TileMap()
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
            var filePath = _dialogService.ShowOpenFileDialog(
                "Tile Map Files (*.json)|*.json|All Files (*.*)|*.*",
                "Open Tile Map");


            if (filePath == null) return;

            var map = await _mapService.LoadMapAsync(filePath);

            if (map != null)
            {
                CurrentMap = map;
                CurrentFilePath = filePath;
            }
            else
                _dialogService.ShowInfo("Error", "Failed to load map.", MessageBoxImage.Error);
        }

        [RelayCommand]
        private async Task SaveMap()
        {
            if (string.IsNullOrEmpty(CurrentFilePath))
            {
                await SaveMapAs();
                return;
            }

            bool success = (await _mapService.SaveMapAsync(CurrentMap, CurrentFilePath)) != null;

            // NOTE Do i really need this method AND SaveMapAs() to display success or error? Pretty sure only SaveMapAs() needs this

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
            var filePath = _dialogService.ShowSaveFileDialog(
                "Tile Map Files (*.json)|*.json|All Files (*.*)|*.*",
                "Save Tile Map As...",
                CurrentMap!.Name + ".json");


            if (filePath != null)
            {
                CurrentFilePath = filePath;
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
            if (CurrentMap == null)
            {
                _dialogService.ShowInfo("Export", "No map to export.", MessageBoxImage.Warning);
                return;
            }

            var filePath = _dialogService.ShowSaveFileDialog(
                "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg",
                "Export Map as Image",
                CurrentMap.Name + "png");

            if (filePath == null) return;

            var visual = _mapVisualProvider.GetMapVisual();
            if (visual == null)
            {
                _dialogService.ShowInfo("Export", "Could not access map viusal for export.", MessageBoxImage.Error);
                return;
            }

            try
            {
                await _imageExportService.ExportAsync(visual, filePath, scale: 2.0);
                _dialogService.ShowInfo("Export Complete", $"Map exported successfully!\n\nFile: {filePath}");
            }
            catch (Exception ex)
            {
                _dialogService.ShowInfo("Export Error", ex.Message, MessageBoxImage.Error);
            }
        }
    }
}
