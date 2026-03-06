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

        private TileMapControlViewModel? _vm;

        private bool _isSubscribed = false;

        public event Action<bool>? SaveRequested;
        public event Action? NewMapRequested;
        public event Action? OpenMapRequested;

        public TileMapToolbarControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;

            Loaded += (s, e) =>
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.PreviewKeyDown += OnWindowKeyDown;
                    window.PreviewKeyUp += OnWindowKeyUp;
                }
            };
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null && _isSubscribed)
            {
                _vm.MapRenderRequested -= RenderMap;
                _vm.TileDrawRequested -= DrawTile;
                _vm.TileRemoveVisualRequested -= RemoveTileVisual;
                _isSubscribed = false;
            }

            _vm = DataContext as TileMapControlViewModel;
            if (_vm == null) return;

            _tileLibrary = App.Services?.GetService<ITileLibraryService>();

            _vm.MapRenderRequested += RenderMap;
            _vm.TileDrawRequested += DrawTile;
            _vm.TileRemoveVisualRequested += RemoveTileVisual;
            _isSubscribed = true;

            BuildLayerControls();
            RenderMap();
        }

        #region Keyboard Commands

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (_vm == null) return;

            if (e.OriginalSource is TextBox) return;

            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
                _vm.IsShiftHeld = true;

            if (ctrl)
            {
                switch (e.Key)
                {
                    case Key.Z:
                        _vm.UndoCommand.Execute(null);
                        e.Handled = true;
                        return;
                    case Key.Y:
                        _vm.RedoCommand.Execute(null);
                        e.Handled = true;
                        return;
                    case Key.S:
                        SaveRequested?.Invoke(shift);
                        e.Handled = true;
                        return;
                    case Key.N:
                        NewMapRequested?.Invoke();
                        e.Handled = true;
                        return;
                    case Key.O:
                        OpenMapRequested?.Invoke();
                        e.Handled = true;
                        return;
                }
            }

            switch (e.Key)
            {
                case Key.B:
                    _vm.SetPaintModeCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.E:
                    _vm.SetEraseModeCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Q:
                    _vm.SetSelectModeCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.I:
                    _vm.SetPropertiesModeCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.G:
                    _vm.ToggleGridCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.R:
                    if (shift)
                        _vm.RotateLeftCommand.Execute(null);
                    else
                        _vm.RotateRightCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.H:
                    _vm.FlipHorizontalCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.V when !ctrl:
                    _vm.FlipVerticalCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.X when !ctrl:
                    _vm.ResetTransformsCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Delete:
                    _vm.SetEraseModeCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    _vm.SetSelectModeCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }

        private void OnWindowKeyUp(object sender, KeyEventArgs e)
        {
            if (_vm == null) return;

            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                _vm.IsShiftHeld = false;
            }
        }

        #endregion

        // Grid event handlers

        private void GridToggle_Click(object sender, RoutedEventArgs e)
        {
            if (_vm == null) return;
            if (_vm.TileMap != null)
                _vm.TileMap.ShowGrid = _vm.IsGridVisible;
            RenderMap();
        }

        private void GridOpacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_vm == null) return;
            _vm.SetGridOpacity(e.NewValue);
        }

        private void GridColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hexColor && _vm != null)
            {
                _vm.SetGridColor(hexColor);
            }
        }

        // Zoom event

        private void FitToWindow_Click(object sender, RoutedEventArgs e)
        {
            if (_vm == null) return;
            _vm.FitToWindow(MapBorder.ActualWidth, MapBorder.ActualHeight);
        }

        // Layer controls

        private void BuildLayerControls()
        {
            if (_vm == null) return;

            var panel = new StackPanel();

            foreach (TileLayer layer in Enum.GetValues<TileLayer>())
            {
                var layerRow = new StackPanel { Margin = new Thickness(0, 1, 0, 1) };

                var headerRow = new StackPanel { Orientation = Orientation.Horizontal };

                var checkbox = new CheckBox()
                {
                    Content = GetLayerEmoji(layer) + " " + layer.ToString(),
                    IsChecked = _vm.VisibleLayers.Contains(layer),
                    Tag = layer,
                    FontSize = 11,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                checkbox.Checked += LayerVisibility_Changed;
                checkbox.Unchecked += LayerVisibility_Changed;
                headerRow.Children.Add(checkbox);

                var soloBtn = new Button()
                {
                    Content = "S",
                    Width = 22,
                    Height = 20,
                    FontSize = 9,
                    Tag = layer,
                    ToolTip = $"Solo {layer}",
                    Margin = new Thickness(2, 0, 0, 0)
                };
                soloBtn.Click += (s, args) =>
                {
                    if (s is Button b && b.Tag is TileLayer l)
                        _vm.SoloLayerCommand.Execute(l);
                };
                headerRow.Children.Add(soloBtn);

                layerRow.Children.Add(headerRow);

                var opacitySlider = new Slider()
                {
                    Minimum = 0,
                    Maximum = 1,
                    Value = _vm.GetLayerOpacity(layer),
                    SmallChange = 0.05,
                    LargeChange = 0.1,
                    Tag = layer,
                    Height = 18,
                    Margin = new Thickness(18, 0, 0, 2)
                };
                opacitySlider.ValueChanged += (s, args) =>
                {
                    if (s is Slider sl && sl.Tag is TileLayer l)
                        _vm.SetLayerOpacity(l, args.NewValue);
                };
                layerRow.Children.Add(opacitySlider);

                panel.Children.Add(layerRow);
            }

            LayerControlsList.Items.Clear();
            LayerControlsList.Items.Add(panel);
        }

        // NOTE I would quite like to swap out these stinky emojis for images instead eventually. But right now, they work
        private static string GetLayerEmoji(TileLayer layer) => layer switch
        {
            TileLayer.Floor => "🟫",
            TileLayer.Terrain => "🏔️",
            TileLayer.Wall => "🧱",
            TileLayer.Door => "🚪",
            TileLayer.Furniture => "🪑",
            TileLayer.Props => "📦",
            TileLayer.Effects => "✨",
            TileLayer.Roof => "🏠",
            _ => "❓"
        };

        private void ActiveLayer_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (CmbActiveLayer.SelectedItem is ComboBoxItem item && item.Tag is string layerName)
                _vm?.SetActiveLayer(layerName);
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

        private void MapCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_vm != null)
            {
                _vm.UpdateMousePosition(-1, -1);
            }
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
            Color gridBaseColor;
            try
            {
                gridBaseColor = (Color)ColorConverter.ConvertFromString(_vm?.GridColor ?? "#FFFFFF");
            }
            catch
            {
                gridBaseColor = Colors.White;
            }

            double opacity = _vm?.GridOpacity ?? 0.15;
            byte alpha = (byte)(255 * Math.Clamp(opacity, 0.0, 1.0));
            var gridBrush = new SolidColorBrush(Color.FromArgb(alpha, gridBaseColor.R, gridBaseColor.G, gridBaseColor.B));

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
                Tag = tile,
                Opacity = _vm.GetLayerOpacity(tileDef.Layer)
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

        private void LayerVisibility_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Tag is string layerName)
            {
                var layer = Enum.Parse<TileLayer>(layerName);
                _vm?.ToggleLayerVisibilityCommand.Execute(layer);
            }
        }
    }
}
