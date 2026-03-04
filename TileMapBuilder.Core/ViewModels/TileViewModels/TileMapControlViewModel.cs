using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDBattle.Data.Models;
using DnDBattle.Data.Services;
using TileMapBuilder.Core.Models.Tiles;
using TileMapBuilder.Core.Services;
using TileMapBuilder.Core.Services.UndoRedo;

namespace TileMapBuilder.Core.ViewModels.TileViewModels
{
    public enum EditMode { Select, Paint, Erase, Properties } // There is 100% more i can add, just all i can think of atm

    public partial class TileMapControlViewModel : ObservableObject
    {
        private readonly ITileLibraryService _tileLibraryService;
        private readonly UndoManager _undoRedoService; // Will need to remake my only Undo/Redo service, it did NOT work...

        public TileMapControlViewModel(
            ITileLibraryService tileMapLibraryService)
        {
            _tileLibraryService = tileMapLibraryService;
            _undoRedoService = new();

            _visibleLayers = new HashSet<TileLayer>()
            {
                TileLayer.Floor, TileLayer.Terrain, TileLayer.Wall, TileLayer.Door,
                TileLayer.Furniture, TileLayer.Props, TileLayer.Effects, TileLayer.Roof
            };
        }

        // Pbservable states
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))] private TileMap? _tileMap;

        [ObservableProperty] private TileDefinition? _selectedTileDefinition;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))] private EditMode _currentMode = EditMode.Select;

        [ObservableProperty] private int _currentRotation;
        [ObservableProperty] private bool _currentFlipH;
        [ObservableProperty] private bool _currentFlipV;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))] private bool _showDMView = true;

        [ObservableProperty] private TileLayer _activeLayer = TileLayer.Floor;

        [ObservableProperty] private bool _isFogOfWarEnabled;

        [ObservableProperty] private double _zoomLevel = 1.0;
        [ObservableProperty] private double _panX;
        [ObservableProperty] private double _panY;

        [ObservableProperty] private bool _isRotateActive;
        [ObservableProperty] private bool _isFlipHActive;
        [ObservableProperty] private bool _isFlipVActive;

        // Non-observable states
        private HashSet<TileLayer> _visibleLayers;
        private FogOfWarState _fogOfWar = new();
        private List<Tile> _clipboard = [];
        private (int X, int Y) _clipboardOrigin;
        private bool _recordingUndo = true;

        public string StatusText =>
            $"Mode: {CurrentMode} | Tiles: {TileMap?.PlacedTiles.Count ?? 0} | DM View: {(ShowDMView ? "ON" : "OFF")}";

        public bool CanUndo => _undoRedoService.CanUndo;
        public bool CanRedo => _undoRedoService.CanRedo;

        // Events
        public event Action? MapRenderRequested;

        public event Action<Tile>? TileDrawRequested;

        public event Action<Tile>? TileRemoveVisualRequested;

        public event Action<Tile>? TilePropertiesRequested;

        public event Action<string, string>? ActionLogged;

        // Commands
        [RelayCommand] private void SetSelectMode() => CurrentMode = EditMode.Select;
        [RelayCommand] private void SetPaintMode() => CurrentMode = EditMode.Paint;
        [RelayCommand] private void SetEraseMode() => CurrentMode = EditMode.Erase;
        [RelayCommand] private void SetPropertiesMode() => CurrentMode = EditMode.Properties;

        // NOTE I am convinced that i do not need individual methods for each of these, im sure i could normalize them down somehow?
        [RelayCommand]
        private void RotateLeft()
        {
            CurrentRotation = (CurrentRotation - 90 + 360) % 360;
            UpdateTransformState();
        }

        [RelayCommand]
        private void RotateRight()
        {
            CurrentRotation = (CurrentRotation - 90) % 360;
            UpdateTransformState();
        }

        [RelayCommand]
        private void FlipHorizontal()
        {
            CurrentFlipH = !CurrentFlipH;
            UpdateTransformState();
        }

        [RelayCommand]
        private void FlipVertical()
        {
            CurrentFlipV = !CurrentFlipV;
            UpdateTransformState();
        }

        [RelayCommand]
        private void ResetTransforms()
        {
            CurrentRotation = 0;
            CurrentFlipH = false;
            CurrentFlipV = false;
            UpdateTransformState();
        }

        private void UpdateTransformState()
        {
            IsRotateActive = CurrentRotation > 0;
            IsFlipHActive = CurrentFlipH;
            IsFlipVActive = CurrentFlipV;
        }

        // =============================================

        [RelayCommand]
        private void ToggleDMView()
        {
            ShowDMView = !ShowDMView;
            RequestMapRender();
        }

        [RelayCommand]
        private void ToggleFogOfWar()
        {
            IsFogOfWarEnabled = !IsFogOfWarEnabled;
            _fogOfWar.IsEnabled = IsFogOfWarEnabled;
            RequestMapRender();
        }

        public FogOfWarState GetFogOfWarState() => _fogOfWar;

        public void RevealFogAroundToken(Token token, int visionRange) // NOTE Why is this in here? Theres no need to reveal AROUND tokens, this is not the playable map...
        {
            _fogOfWar.RevealArea(token.GridX, token.GridY, visionRange);
            RequestMapRender();
        }

        // =============================================

        public bool IsLayerVisible(TileLayer layer) => _visibleLayers.Contains(layer);

        public IReadOnlySet<TileLayer> VisibleLayers => _visibleLayers;

        [RelayCommand]
        private void ToggleLayerVisibility(TileLayer layer)
        {
            if (_visibleLayers.Contains(layer))
                _visibleLayers.Remove(layer);
            else
                _visibleLayers.Add(layer);

            RequestMapRender();
        }

        public void SetActiveLayer(string layerName)
            => ActiveLayer = Enum.Parse<TileLayer>(layerName);

        public void ApplyZoom(double delta)
        {
            double zoomFactor = delta > 0 ? 1.1 : 0.9;
            double newScale = ZoomLevel * zoomFactor;
            ZoomLevel = Math.Clamp(newScale, 0.25, 4.0);
        }

        public void ApplyPanDelta(double deltaX, double deltaY)
        {
            PanX += deltaX;
            PanY += deltaY;
        }

        /// <summary>
        /// Called by the view when the user clicks/drags on a grid cell.
        /// The view is responsible for converting screen coords to grid coords.
        /// </summary>
        public void ProcessTileAction(int gridX, int gridY)
        {
            if (TileMap == null) return;

            if (gridX < 0 || gridX >= TileMap.Width || gridY < 0 || gridY >= TileMap.Height)
                return;

            switch (CurrentMode)
            {
                case EditMode.Paint:
                    PlaceTileAt(gridX, gridY);
                    break;
                case EditMode.Erase:
                    RemoveTileAt(gridX, gridY);
                    break;
                case EditMode.Properties:
                    var tile = GetTileAt(gridX, gridY);
                    if (tile != null)
                        TilePropertiesRequested?.Invoke(tile);
                    break;
            }
        }

        /// <summary>
        /// Called by the view on right-click.
        /// </summary>
        public void HandleRightClick(int gridX, int gridY)
        {
            var tile = GetTileAt(gridX, gridY);
            if (tile != null)
                TilePropertiesRequested?.Invoke(tile);
            else
                RemoveTileAt(gridX, gridY);
        }

        private void PlaceTileAt(int gridX, int gridY)
        {
            if (TileMap == null || SelectedTileDefinition == null) return;

            var tilesToRemove = TileMap.GetTilesAt(gridX, gridY)
                .Where(t =>
                {
                    var def = _tileLibraryService.GetTileById(t.TileDefinitionId);
                    return def?.Layer == ActiveLayer;
                })
                .ToList();

            var newTile = new Tile()
            {
                TileDefinitionId = SelectedTileDefinition.Id,
                GridX = gridX,
                GridY = gridY,
                Rotation = CurrentRotation,
                FlipHorizontal = CurrentFlipH,
                FlipVertical = CurrentFlipV
            };

            if (_recordingUndo && tilesToRemove.Any())
            {
                var batchAction = new TileBatchAction(TileMap, new List<Tile> { newTile }, tilesToRemove, "Replace Tile");
                _undoRedoService.Record(batchAction);
            }
            else if (_recordingUndo)
            {
                var action = new TilePlaceAction(TileMap, newTile);
                _undoRedoService.Record(action);
            }

            foreach (var tile in tilesToRemove)
            {
                TileMap.RemoveTile(tile);
                TileRemoveVisualRequested?.Invoke(tile);
            }

            TileMap.AddTile(newTile);
            TileDrawRequested?.Invoke(newTile);

            OnPropertyChanged(nameof(StatusText));
        }

        private void RemoveTileAt(int gridX, int gridY)
        {
            if (TileMap == null) return;

            var tilesToRemove = TileMap.GetTilesAt(gridX, gridY).ToList();
            if (tilesToRemove.Count == 0) return;

            if (_recordingUndo)
            {
                if (tilesToRemove.Count == 1)
                {
                    var action = new TileRemoveAction(TileMap, tilesToRemove[0]);
                    _undoRedoService.Record(action);
                }
                else
                {
                    var batchAction = new TileBatchAction(TileMap, new List<Tile>(), tilesToRemove, "Remove Tiles");
                    _undoRedoService.Record(batchAction);
                }
            }

            foreach (var tile in tilesToRemove)
            {
                TileMap.RemoveTile(tile);
                TileRemoveVisualRequested?.Invoke(tile);
            }

            OnPropertyChanged(nameof(StatusText));
            
            RequestMapRender();
        }

        public Tile? GetTileAt(int gridX, int gridY)
            => TileMap?.GetTilesAt(gridX, gridY)
                .OrderByDescending(t => t.ZIndex ?? 0)
                .FirstOrDefault();

        public (int X, int Y) ScreenToGrid(double screenX, double screenY)
        {
            if (TileMap == null) return (0, 0);

            int gridX = (int)(screenX / TileMap.CellSize);
            int gridY = (int)(screenY / TileMap.CellSize);

            return (gridX, gridY);
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
        {
            _recordingUndo = false;
            _undoRedoService.Undo();
            RequestMapRender();
            _recordingUndo = true;

            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo()
        {
            _recordingUndo = false;
            _undoRedoService.Redo();
            RequestMapRender();
            _recordingUndo = true;

            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }

        // Clipboard
        [RelayCommand]
        private void CopyAt((int x, int y) positions)
        {
            if (TileMap == null) return;

            var tiles = TileMap.GetTilesAt(positions.x, positions.y).ToList();

            if (tiles.Count == 0)
            {
                ActionLogged?.Invoke("Edit", "No tiles to copy");
                return;
            }

            _clipboard.Clear();
            _clipboardOrigin = (positions.x, positions.y);

            foreach (var tile in tiles)
            {
                _clipboard.Add(new Tile
                {
                    TileDefinitionId = tile.TileDefinitionId,
                    GridX = tile.GridX,
                    GridY = tile.GridY,
                    Rotation = tile.Rotation,
                    FlipHorizontal = tile.FlipHorizontal,
                    FlipVertical = tile.FlipVertical,
                    ZIndex = tile.ZIndex,
                    Notes = tile.Notes
                });
            }

            ActionLogged?.Invoke("Edit", $"📋 Copied {_clipboard.Count} tiles");
        }

        [RelayCommand]
        private void PasteAt((int x, int y) positions)
        {
            if (TileMap == null || _clipboard.Count == 0)
            {
                ActionLogged?.Invoke("Edit", "Clipboard is empty");
                return;
            }

            var pastedTiles = new List<Tile>();

            foreach (var clipTile in _clipboard)
            {
                int offsetX = clipTile.GridX - _clipboardOrigin.X;
                int offsetY = clipTile.GridY - _clipboardOrigin.Y;

                var newTile = new Tile
                {
                    TileDefinitionId = clipTile.TileDefinitionId,
                    GridX = positions.x + offsetX,
                    GridY = positions.y + offsetY,
                    Rotation = clipTile.Rotation,
                    FlipHorizontal = clipTile.FlipHorizontal,
                    FlipVertical = clipTile.FlipVertical,
                    ZIndex = clipTile.ZIndex,
                    Notes = clipTile.Notes
                };

                if (newTile.GridX >= 0 && newTile.GridX < TileMap.Width &&
                    newTile.GridY >= 0 && newTile.GridY < TileMap.Height)
                {
                    TileMap.AddTile(newTile);
                    pastedTiles.Add(newTile);
                }
            }

            if (_recordingUndo && pastedTiles.Count > 0)
            {
                var action = new TileBatchAction(TileMap, pastedTiles, new List<Tile>(), "Paste Tiles");
                _undoRedoService.Record(action);
            }

            RequestMapRender();
            ActionLogged?.Invoke("Edit", $"📋 Pasted {pastedTiles.Count} tiles");
        }

        [RelayCommand]
        private void CutAt((int x, int y) positions)
        {
            if (TileMap == null) return;

            var tiles = TileMap.GetTilesAt(positions.x, positions.y).ToList();
            if (tiles.Count == 0) return;

            CopyAt((positions.x, positions.y));

            foreach (var tile in tiles)
            {
                TileMap.RemoveTile(tile);
            }

            if (_recordingUndo)
            {
                var action = new TileBatchAction(TileMap, new List<Tile>(), tiles, "Cut Tiles");
                _undoRedoService.Record(action);
            }

            RequestMapRender();
            ActionLogged?.Invoke("Edit", $"✂️ Cut {tiles.Count} tiles");
        }

        #region Helpers

        private void RequestMapRender()
            => MapRenderRequested?.Invoke();

        partial void OnTileMapChanged(TileMap? value)
        {
            RequestMapRender();
            OnPropertyChanged(nameof(StatusText));
        }
        #endregion
    }
}
