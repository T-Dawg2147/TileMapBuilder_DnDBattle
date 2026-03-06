using DnDBattle.Data.Enums;
using DnDBattle.Data.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TileMapBuilder.App.Services;
using DnDBattle.Data.Models.Tiles;
using DnDBattle.Data.Models.Tiles.Metadata;
using TileMapBuilder.Core.ViewModels.TileViewModels;

namespace TileMapBuilder.App.Controls
{
    /// <summary>
    /// Interaction logic for TileMapToolbarControl.xaml
    /// </summary>
    public partial class TileMapToolbarControl : UserControl
    {
        private bool _isPanning;
        private Point _lastPanPoint;
        private bool _isPainting;
        private ITileLibraryService? _tileLibrary;

        public TileMapToolbarControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private TileMapControlViewModel? _vm => DataContext as TileMapControlViewModel;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_vm == null) return;

            _tileLibrary = App.Services?.GetService<ITileLibraryService>();

            _vm.MapRenderRequested += RenderMap;
            _vm.TileDrawRequested += DrawTile;
            _vm.TileRemoveVisualRequested += RemoveTileVisual;

            RenderMap();
        }

        private void ActiveLayer_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (CmbActiveLayer.SelectedItem is ComboBoxItem item && item.Tag is string layerName)
                _vm?.SetActiveLayer(layerName);
        }

        private void LayerVisibility_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Tag is string layerName)
            {
                var layer = Enum.Parse<TileLayer>(layerName);
                _vm?.ToggleLayerVisibilityCommand.Execute(layer);
            }
        }

        private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_vm == null) return;

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                _isPanning = true;
                _lastPanPoint = e.GetPosition(MapCanvas);
                MapCanvas.CaptureMouse();
            }
            else
            {
                _isPainting = true;
                var pos = e.GetPosition(MapCanvas);
                var grid = _vm.ScreenToGrid(pos.X, pos.Y);
                _vm.ProcessTileAction(grid.X, grid.Y);
                MapCanvas.CaptureMouse();
            }
        }

        private void MapCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_vm == null) return;

            if (_isPanning)
            {
                var current = e.GetPosition(MapBorder);
                var delta = current - _lastPanPoint;
                _vm.ApplyPanDelta(delta.X, delta.Y);
                _lastPanPoint = current;
            }
            else if (_isPainting && e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(MapCanvas);
                var grid = _vm.ScreenToGrid(pos.X, pos.Y);
                _vm.ProcessTileAction(grid.X, grid.Y);
            }
        }

        private void MapCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isPanning = false;
            _isPainting = false;
            MapCanvas.ReleaseMouseCapture();
        }

        private void MapCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_vm == null) return;
            var pos = e.GetPosition(MapCanvas);
            var grid = _vm.ScreenToGrid(pos.X, pos.Y);
            _vm.HandleRightClick(grid.X, grid.Y);
        }

        private void MapCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _vm?.ApplyZoom(e.Delta);
        }

        private void RenderMap()
        {
            if (_vm?.TileMap == null) return;
            var tileMap = _vm.TileMap;

            TilesLayer.Children.Clear();
            GridLayer.Children.Clear();
            MetadataLayer.Children.Clear();

            MapCanvas.Width = tileMap.Width * tileMap.CellSize;
            MapCanvas.Height = tileMap.Height * tileMap.CellSize;

            if (tileMap.ShowGrid)
                DrawGrid(tileMap);

            foreach (var tile in tileMap.PlacedTiles.OrderBy(t => t.ZIndex ?? 0))
                DrawTile(tile);

            if (_vm.ShowDMView)
                DrawMetadataOverlays(tileMap);

            if (_vm.IsFogOfWarEnabled)
                RenderFogOfWar(tileMap);
        }

        private void DrawGrid(TileMap tileMap)
        {
            var gridBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
            double cellSize = tileMap.CellSize;

            for (int y = 0; y <= tileMap.Height; y++)
            {
                GridLayer.Children.Add(new Line()
                {
                    X1 = 0, Y1 = y * cellSize,
                    X2 = tileMap.Width * cellSize, Y2 = y * cellSize,
                    Stroke = gridBrush, StrokeThickness = 1
                });
            }

            for (int x = 0; x <= tileMap.Width; x++)
            {
                GridLayer.Children.Add(new Line()
                {
                    X1 = x * cellSize, Y1 = 0,
                    X2 = x * cellSize, Y2 = tileMap.Height * cellSize,
                    Stroke = gridBrush, StrokeThickness = 1
                });
            }
        }

        private void DrawTile(Tile tile)
        {
            if (_vm?.TileMap == null) return;
            var tileMap = _vm.TileMap;

            var tileDef = _tileLibrary?.GetTileById(tile.TileDefinitionId!);
            if (tileDef == null) return;
            if (!_vm.IsLayerVisible(tileDef.Layer)) return;

            var image = TileImageCacheService.Instance.GetOrLoadImage(tileDef.ImagePath);
            if (image == null) return;

            var tileImage = new System.Windows.Controls.Image()
            {
                Source = image,
                Width = tileMap.CellSize,
                Height = tileMap.CellSize,
                Stretch = Stretch.Fill,
                Tag = tile
            };

            var transform = new TransformGroup();
            if (tile.Rotation != 0)
                transform.Children.Add(new RotateTransform(tile.Rotation, tileMap.CellSize / 2, tileMap.CellSize / 2));
            if (tile.FlipHorizontal)
                transform.Children.Add(new ScaleTransform(-1, 1, tileMap.CellSize / 2, tileMap.CellSize / 2));
            if (tile.FlipVertical)
                transform.Children.Add(new ScaleTransform(1, -1, tileMap.CellSize / 2, tileMap.CellSize / 2));
            tileImage.RenderTransform = transform;

            Canvas.SetLeft(tileImage, tile.GridX * tileMap.CellSize);
            Canvas.SetTop(tileImage, tile.GridY * tileMap.CellSize);
            Canvas.SetZIndex(tileImage, tile.GetEffectiveZIndex(tileDef));

            TilesLayer.Children.Add(tileImage);
        }

        private void RemoveTileVisual(Tile tile)
        {
            var visual = TilesLayer.Children.OfType<System.Windows.Controls.Image>()
                .FirstOrDefault(i => i.Tag == tile);
            if (visual != null)
                TilesLayer.Children.Remove(visual);
        }

        #region Metadata Handling
        private void DrawMetadataOverlays(TileMap tileMap)
        {
            foreach (var tile in tileMap.PlacedTiles.Where(t => t.HasMetadata))
                DrawMetadataIndicator(tile);
        }
        
        private void DrawMetadataIndicator(Tile tile)
        {
            if (_vm?.TileMap == null) return;
            double cellSize = _vm.TileMap.CellSize;
            double x = tile.GridX * cellSize;
            double y = tile.GridY * cellSize;

            var metadataTypes = tile.Metadata.Select(m => m.Type).ToList();

            var border = new Border
            {
                Width = cellSize,
                Height = cellSize,
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(4),
                IsHitTestVisible = false,
                BorderBrush = GetMetadataBorderBrush(metadataTypes)
            };

            if (metadataTypes.Contains(TileMetadataType.Trap))
            {
                border.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Red,
                    BlurRadius = 15,
                    ShadowDepth = 0,
                    Opacity = 0.8
                };
            }

            Canvas.SetLeft(border, x);
            Canvas.SetTop(border, y);
            Canvas.SetZIndex(border, 1000);
            MetadataLayer.Children.Add(border);
        }

        private Brush GetMetadataBorderBrush(List<TileMetadataType> types)
        {
            if (types.Contains(TileMetadataType.Trap)) return new SolidColorBrush(Color.FromArgb(200, 244, 67, 54));
            if (types.Contains(TileMetadataType.Hazard)) return new SolidColorBrush(Color.FromArgb(200, 255, 152, 0));
            if (types.Contains(TileMetadataType.Secret)) return new SolidColorBrush(Color.FromArgb(200, 255, 235, 59));
            if (types.Contains(TileMetadataType.Interactive)) return new SolidColorBrush(Color.FromArgb(200, 33, 150, 243));
            return new SolidColorBrush(Color.FromArgb(200, 158, 158, 158));
        }
        #endregion

        private void RenderFogOfWar(TileMap tileMap)
        {
            var fogState = _vm!.GetFogOfWarState();
            double cellSize = tileMap.CellSize;

            for (int x = 0; x < tileMap.Width; x++)
            {
                for (int y = 0; y < tileMap.Height; y++)
                {
                    bool isRevealed = fogState.IsTileRevealed(x, y);
                    Rectangle? fogTile = null;

                    if (!isRevealed)
                    {
                        fogTile = new Rectangle()
                        {
                            Width = cellSize,
                            Height = cellSize,
                            Fill = new SolidColorBrush(Color.FromArgb(220, 0, 0, 0)),
                            IsHitTestVisible = false
                        };
                    }

                    if (fogTile != null)
                    {
                        Canvas.SetLeft(fogTile, x * cellSize);
                        Canvas.SetTop(fogTile, y * cellSize);
                        Canvas.SetZIndex(fogTile, 5000);
                        MetadataLayer.Children.Add(fogTile);
                    }
                }
            }
        }
    }
}
