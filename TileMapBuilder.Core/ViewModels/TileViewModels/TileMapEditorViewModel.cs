using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using DnDBattle.Data.Models.Tiles;
using DnDBattle.Data.Services.Interfaces;
using TileMapBuilder.Core.Services.Interfaces;

namespace TileMapBuilder.Core.ViewModels.TileViewModels
{
    public partial class TileMapEditorViewModel : ObservableObject
    {
        private readonly ITileMapService _mapService;
        private readonly IDialogService _dialogService;
        private readonly IMapVisualProvider _mapVisualProvider;
        private readonly IImageExportService _imageExportService;
        private readonly ITileImageCacheService _imageCache;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(WindowTitle))]
        private TileMap? _currentMap;

        public string WindowTitle => CurrentMap != null
            ? $"Tile Map Builder - {CurrentMap.Name}"
            : "Tile Map Builder";

        private static readonly string[] _supportedExtensions = ["*.jpg", "*.jpeg", "*.png", "*.bmp"];
        [ObservableProperty] private string _currentFilePath = string.Empty;

        public event Action<string, bool>? StatusNotification;
        public event Action<TileMap>? MapLoaded;

        public TileMapEditorViewModel(
            ITileMapService mapService,
            IDialogService dialogService,
            IMapVisualProvider mapVisualProvider,
            IImageExportService imageExportService,
            ITileImageCacheService imageCache)
        {
            _dialogService = dialogService;
            _mapService = mapService;
            _mapVisualProvider = mapVisualProvider;
            _imageExportService = imageExportService;
            _imageCache = imageCache;

            var tileResources = Path.Combine(AppContext.BaseDirectory, "Resources", "Tiles");
            if (!Directory.Exists(tileResources))
                Directory.CreateDirectory(tileResources);

            _imageCache.PreloadImages(_supportedExtensions
                .SelectMany(ext => Directory.EnumerateFiles(
                    Path.Combine(AppContext.BaseDirectory, "Resources", "Tiles"),
                    ext,
                    SearchOption.AllDirectories)));
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
                    CellSize = 48,
                    ShowGrid = true
                };

                StatusNotification?.Invoke($"Created new map: {CurrentMap.Name} ({CurrentMap.Width}x{CurrentMap.Height})", true);
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
                StatusNotification?.Invoke($"Loaded: {map.Name}", true);
                MapLoaded?.Invoke(map);
            }
            else
                _dialogService.ShowInfo("Error", "Failed to load map.", DialogIcon.Error);
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

            // TODO This is a very basic implementation.
            // I would eventually like to be able to write this data to a temp file first to avoid corruptions in the primary file
            if (success)
                StatusNotification?.Invoke("Map saved successfully", true);
            else
                StatusNotification?.Invoke("Failed to save map.", false);
        }

        [RelayCommand]
        private async Task SaveMapAs()
        {
            if (CurrentMap == null) return;

            var filePath = _dialogService.ShowSaveFileDialog(
                "Tile Map Files (*.json)|*.json|All Files (*.*)|*.*",
                "Save Tile Map As...",
                CurrentMap!.Name + ".json");


            if (filePath != null)
            {
                CurrentFilePath = filePath;
                bool success = (await _mapService.SaveMapAsync(CurrentMap, CurrentFilePath)) != null;

                // TODO This is a very basic implementation.
                // I would eventually like to be able to write this data to a temp file first to avoid corruptions in the primary file
                if (success)
                    StatusNotification?.Invoke($"Saved: {Path.GetFileName(filePath)}", true);
                else
                    StatusNotification?.Invoke("Failed to save map.", false);
            }
        }

        [RelayCommand]
        private async Task ExportAsImage()
        {
            if (CurrentMap == null)
            {
                _dialogService.ShowInfo("Export", "No map to export.", DialogIcon.Warning);
                return;
            }

            var filePath = _dialogService.ShowSaveFileDialog(
                "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg",
                "Export Map as Image",
                CurrentMap.Name + ".png");

            if (filePath == null) return;

            var visual = _mapVisualProvider.GetMapVisual();
            if (visual == null)
            {
                _dialogService.ShowInfo("Export", "Could not access map viusal for export.", DialogIcon.Error);
                return;
            }

            try
            {
                await _imageExportService.ExportAsAsync(visual, filePath, scale: 2.0);
                StatusNotification?.Invoke($"Exported: {Path.GetFileName(filePath)}", true);
            }
            catch (Exception ex)
            {
                _dialogService.ShowInfo("Export Error", ex.Message, DialogIcon.Error);
            }
        }
    }
}
