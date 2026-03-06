using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDBattle.Data.Models;
using DnDBattle.Data.Services;
using DnDBattle.Data.Services.Interfaces;
using DnDBattle.Data.Models.Tiles;
using DnDBattle.Data.Services.UndoRedo;
using TileMapBuilder.Core.Services.Interfaces;

namespace TileMapBuilder.Core.ViewModels.TileViewModels
{
    public enum EditMode { Select, Paint, Erase, Properties } // There is 100% more i can add, just all i can think of atm

    public partial class TileMapControlViewModel : ObservableObject
    {
        private readonly ITileLibraryService _tileLibraryService;
        private readonly UndoManager _undoRedoService; // Will need to remake my only Undo/Redo service, it did NOT work...

        public TileMapControlViewModel(
            ITileLibraryService tileMapLibraryService,
            UndoManager undoManager)
        {
            _tileLibraryService = tileMapLibraryService;
            _undoRedoService = undoManager;

            _visibleLayers = new HashSet<TileLayer>()
            {
                TileLayer.Floor, TileLayer.Terrain, TileLayer.Wall, TileLayer.Door,
                TileLayer.Furniture, TileLayer.Props, TileLayer.Effects, TileLayer.Roof
            };

            _layerOpacities = new Dictionary<TileLayer, double>();
            foreach (TileLayer layer in Enum.GetValues<TileLayer>())
            {
                _layerOpacities[layer] = 1.0;
            }
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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ZoomPercentage))]
        [NotifyPropertyChangedFor(nameof(StatusText))] private double _zoomLevel = 1.0;
        [ObservableProperty] private double _panX;
        [ObservableProperty] private double _panY;

        [ObservableProperty] private bool _isRotateActive;
        [ObservableProperty] private bool _isFlipHActive;
        [ObservableProperty] private bool _isFlipVActive;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))] private bool _isShiftHeld;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))] private bool _isGridVisible = true;

        [ObservableProperty] private double _gridOpacity = 0.15;
        [ObservableProperty] private string _gridColor = "#FFFFFF";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))] private int _mouseGridX = -1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))] private int _mouseGridY = -1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSoloActive))]
        private TileLayer? _soloedLayer;

        // Non-observable states
        private HashSet<TileLayer> _visibleLayers;
        private Dictionary<TileLayer, double> _layerOpacities;
        private FogOfWarState _fogOfWar = new();
        private List<Tile> _clipboard = [];
        private (int X, int Y) _clipboardOrigin;
        private bool _recordingUndo = true;

        public string ZoomPercentage => $"{ZoomLevel * 100:F0}%";

        public bool IsSoloActive => SoloedLayer != null;

        public string StatusText
        {
            get
            {
                var effectiveMode = GetEffectiveMode();
                var shiftIndicator = IsShiftHeld ? " [SHIFT]" : "";
                return $"Mode: {effectiveMode}{shiftIndicator} | " +
                       $"Layer: {ActiveLayer} | " +
                       $"Tiles: {TileMap?.PlacedTiles.Count ?? 0} | " +
                       $"Grid: {(IsGridVisible ? "ON" : "OFF")} | " +
                       $"DM View: {(ShowDMView ? "ON" : "OFF")}";
            }
        }


        public bool CanUndo => _undoRedoService.CanUndo;
        public bool CanRedo => _undoRedoService.CanRedo;

        // Events
        public event Action? MapRenderRequested;

        public event Action<Tile>? TileDrawRequested;

        public event Action<Tile>? TileRemoveVisualRequested;

        public event Action<Tile>? TilePropertiesRequested;

        public event Action<string, string>? ActionLogged;

        public void RequestMapRenderPublic()
            => MapRenderRequested?.Invoke();

        // Grid Commands
        [RelayCommand]
        private void ToggleGrid()
        {
            IsGridVisible = !IsGridVisible;
            if (TileMap != null)
                TileMap.ShowGrid = IsGridVisible;
            RequestMapRender();
            ActionLogged?.Invoke("View", $"Grid {(IsGridVisible ? "shown" : "hidden")}");
        }

        public void SetGridOpacity(double opacity)
        {
            GridOpacity = Math.Clamp(opacity, 0.0, 1.0);
            if (TileMap != null)
                TileMap.GridOpacity = GridOpacity;
            RequestMapRender();
        }

        public void SetGridColor(string hexColor)
        {
            GridColor = hexColor;
            if (TileMap != null)
                TileMap.GridColor = hexColor;
            RequestMapRender();
        }

        // Zoom/Pan Commands
        public void ApplyZoom(double delta)
        {
            double zoomFactor = delta > 0 ? 1.1 : 0.9;
            double newScale = ZoomLevel * zoomFactor;
            ZoomLevel = Math.Clamp(newScale, 0.25, 4.0);
        }

        public void SetZoom(double zoom)
        {
            ZoomLevel = Math.Clamp(zoom, 0.25, 4.0);
        }

        public void ApplyPanDelta(double deltaX, double deltaY)
        {
            PanX += deltaX;
            PanY += deltaY;
        }

        [RelayCommand]
        private void ActualSize()
        {
            ZoomLevel = 1.0;
            ActionLogged?.Invoke("View", "Zoom set to 100%");
        }

        [RelayCommand]
        private void ResetView()
        {
            ZoomLevel = 1.0;
            PanX = 0;
            PanY = 0;
            ActionLogged?.Invoke("View", "View reset");
        }

        public void FitToWindow(double viewportWidth, double viewportHeight)
        {
            if (TileMap == null || viewportWidth <= 0 || viewportHeight <= 0) return;

            double mapPixelWidth = TileMap.Width * TileMap.CellSize;
            double mapPixelHeight = TileMap.Height * TileMap.CellSize;

            double scaleX = viewportWidth / mapPixelWidth;
            double scaleY = viewportHeight / mapPixelHeight;

            double fitZoom = Math.Min(scaleX, scaleY) * 0.95;
            ZoomLevel = Math.Clamp(fitZoom, 0.25, 4.0);
            PanX = 0;
            PanY = 0;

            ActionLogged?.Invoke("View", $"Fit to window ({ZoomPercentage})");
        }

        public void UpdateMousePosition(double screenX, double screenY)
        {
            if (TileMap == null)
            {
                MouseGridX = -1;
                MouseGridY = -1;
                return;
            }

            int gx = (int)(screenX / TileMap.CellSize);
            int gy = (int)(screenY / TileMap.CellSize);

            if (gx >= 0 && gx < TileMap.Width && gy >= 0 && gy < TileMap.Height)
            {
                MouseGridX = gx;
                MouseGridY = gy;
            }
            else
            {
                MouseGridX = -1;
                MouseGridY = -1;
            }
        }

        // Edit Mode Commands
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
            CurrentRotation = (CurrentRotation + 90) % 360;
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
            IsRotateActive = CurrentRotation != 0;
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

        public bool IsLayerVisible(TileLayer layer)
        {
            if (SoloedLayer != null)
                return layer == SoloedLayer;

            return _visibleLayers.Contains(layer);
        }

        public IReadOnlySet<TileLayer> VisibleLayers => _visibleLayers;

        public double GetLayerOpacity(TileLayer layer)
        {
            return _layerOpacities.TryGetValue(layer, out var opacity) ? opacity : 1.0;
        }

        [RelayCommand]
        private void ToggleLayerVisibility(TileLayer layer)
        {
            if (_visibleLayers.Contains(layer))
                _visibleLayers.Remove(layer);
            else
                _visibleLayers.Add(layer);

            RequestMapRender();
        }

        public void SetLayerOpacity(TileLayer layer, double opacity)
        {
            _layerOpacities[layer] = Math.Clamp(opacity, 0.0, 1.0);
            RequestMapRender();
        }

        [RelayCommand]
        private void SoloLayer(TileLayer layer)
        {
            if (SoloedLayer == layer)
            {
                SoloedLayer = null;
                ActionLogged?.Invoke("Layers", $"Solo: {layer}");
            }
            RequestMapRender();
        }

        [RelayCommand]
        private void ShowAllLayers()
        {
            SoloedLayer = null;
            foreach (TileLayer layer in Enum.GetValues<TileLayer>())
            {
                _visibleLayers.Add(layer);
                _layerOpacities[layer] = 1.0;
            }
            RequestMapRender();
            ActionLogged?.Invoke("Layers", "All layers visible at 100%");
        }

        [RelayCommand]
        private void HideAllLayers()
        {
            SoloedLayer = null;
            _visibleLayers.Clear();
            RequestMapRender();
            ActionLogged?.Invoke("Layers", "All layers hidden");
        }

        public void SetActiveLayer(string layerName)
            => ActiveLayer = Enum.Parse<TileLayer>(layerName);        

        private EditMode GetEffectiveMode()
        {
            if (!IsShiftHeld) return CurrentMode;

            return CurrentMode switch
            {
                EditMode.Paint => EditMode.Erase,
                EditMode.Erase => EditMode.Paint,
                _ => CurrentMode
            };
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

            var mode = GetEffectiveMode();

            switch (mode)
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
                RemoveTileAt(gridX, gridY); // NOTE why...? I do not think i need this anymore
        }

        private void PlaceTileAt(int gridX, int gridY)
        {
            if (TileMap == null || SelectedTileDefinition == null) return;

            var tilesToRemove = TileMap.GetTilesAt(gridX, gridY)
                .Where(t =>
                {
                    var def = _tileLibraryService.GetTileById(t.TileDefinitionId!);
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
                _undoRedoService.Record(batchAction, performNow: false);
            }
            else if (_recordingUndo)
            {
                var action = new TilePlaceAction(TileMap, newTile);
                _undoRedoService.Record(action, performNow: false);
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
                    _undoRedoService.Record(action, performNow: false);
                }
                else
                {
                    var batchAction = new TileBatchAction(TileMap, new List<Tile>(), tilesToRemove, "Remove Tiles");
                    _undoRedoService.Record(batchAction, performNow: false);
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

        #region Clipboard Commands
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
        #endregion

        #region Helpers

        private void RequestMapRender()
            => MapRenderRequested?.Invoke();

        partial void OnTileMapChanged(TileMap? value)
        {
            if (value != null)
            {
                IsGridVisible = value.ShowGrid;
                GridOpacity = value.GridOpacity;
                GridColor = value.GridColor;
            }

            RequestMapRender();
            OnPropertyChanged(nameof(StatusText));
        }
        #endregion
    }
}
