using DnDBattle.Data.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Globalization;
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
    public partial class TileMapToolbarControl : UserControl
    {
        private bool _isPanning;
        private Point _lastPanPoint;
        private bool _isPainting;
        private ITileLibraryService? _tileLibrary;

        private TileMapControlViewModel? _vm;
        private bool _isSubscribed = false;

        // Drag-select state
        private bool _isDragSelecting;
        private Point _dragSelectStart;
        private Rectangle? _dragSelectRect;

        private bool _isShapeDrawing;

        public TileMapToolbarControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null && _isSubscribed)
            {
                _vm.MapRenderRequested -= RenderMap;
                _vm.TileDrawRequested -= DrawTile;
                _vm.TileRemoveVisualRequested -= RemoveTileVisual;
                _vm.SelectionChanged -= DrawSelectionHighlights;
                _vm.ShapePreviewChanged += DrawShapePreview;
                _isSubscribed = false;
            }

            _vm = DataContext as TileMapControlViewModel;
            if (_vm == null) return;

            _tileLibrary = App.Services?.GetService<ITileLibraryService>();

            _vm.MapRenderRequested += RenderMap;
            _vm.TileDrawRequested += DrawTile;
            _vm.TileRemoveVisualRequested += RemoveTileVisual;
            _vm.SelectionChanged += DrawSelectionHighlights;
            _vm.ShapePreviewChanged += DrawShapePreview;
            _isSubscribed = true;

            BuildLayerControls();
            RenderMap();
        }

        #region Keyboard + Mouse Handlers
        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (_vm == null) return;

            bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

            switch (e.Key)
            {
                case Key.Delete:
                    if (_vm.HasSelection)
                    {
                        _vm.DeleteSelectedCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    if (_vm.IsDrawingShape)
                    {
                        _vm.CancelShapeDraw();
                        _isShapeDrawing = false;
                    }
                    _vm.ClearSelectionCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.A when ctrl:
                    _vm.SelectAllCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.C when ctrl:
                    if (_vm.HasSelection)
                    {
                        _vm.CopySelectedCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;

                case Key.X when ctrl:
                    if (_vm.HasSelection)
                    {
                        _vm.CutSelectedCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;

                case Key.V when ctrl:
                    PasteAtCurrentMousePosition();
                    e.Handled = true;
                    break;

                case Key.Up:
                    if (_vm.HasSelection) { _vm.MoveSelected(0, -1); e.Handled = true; }
                    break;
                case Key.Down:
                    if (_vm.HasSelection) { _vm.MoveSelected(0, 1); e.Handled = true; }
                    break;
                case Key.Left:
                    if (_vm.HasSelection) { _vm.MoveSelected(-1, 0); e.Handled = true; }
                    break;
                case Key.Right:
                    if (_vm.HasSelection) { _vm.MoveSelected(1, 0); e.Handled = true; }
                    break;
            }
        }

        private void PasteAtCurrentMousePosition()
        {
            if (_vm == null) return;

            // Use the current mouse grid position from the VM
            int gx = _vm.MouseGridX;
            int gy = _vm.MouseGridY;

            if (gx >= 0 && gy >= 0)
                _vm.PasteAtPositionCommand.Execute((gx, gy));
        }

        private void PasteAtCursor_Click(object sender, RoutedEventArgs e)
        {
            PasteAtCurrentMousePosition();
        }
        #endregion

        #region Grid event handlers
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
        #endregion

        #region Zoom event handlers
        private void FitToWindow_Click(object sender, RoutedEventArgs e)
        {
            if (_vm == null) return;
            _vm.FitToWindow(MapBorder.ActualWidth, MapBorder.ActualHeight);
        }
        #endregion

        #region Map Resize Handler
        private void ResizeMap_Click(object sender, RoutedEventArgs e)
        {
            if (_vm == null) return;

            if (!int.TryParse(TxtResizeTop.Text, out int top)) top = 0;
            if (!int.TryParse(TxtResizeBottom.Text, out int bottom)) bottom = 0;
            if (!int.TryParse(TxtResizeLeft.Text, out int left)) left = 0;
            if (!int.TryParse(TxtResizeRight.Text, out int right)) right = 0;

            if (top == 0 && bottom == 0 && left == 0 && right == 0) return;

            _vm.ResizeMap(top, bottom, left, right);

            TxtResizeTop.Text = "0";
            TxtResizeBottom.Text = "0";
            TxtResizeLeft.Text = "0";
            TxtResizeRight.Text = "0";
        }
        #endregion

        #region Layer controls
        private void BuildLayerControls()
        {
            if (_vm == null) return;

            var panel = new StackPanel();

            foreach (TileLayer layer in Enum.GetValues<TileLayer>())
            {
                var layerRow = new StackPanel { Margin = new Thickness(0, 1, 0, 1) };

                var headerRow = new StackPanel { Orientation = Orientation.Horizontal };

                var checkbox = new CheckBox
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

                var soloBtn = new Button
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

                var opacitySlider = new Slider
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
        #endregion

        // =============================================
        // Mouse handlers (with drag-select support)
        // =============================================

        private void ActiveLayer_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (CmbActiveLayer.SelectedItem is ComboBoxItem item && item.Tag is string layerName)
                _vm?.SetActiveLayer(layerName);
        }

        private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_vm == null) return;

            this.Focus();

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                _isPanning = true;
                _lastPanPoint = e.GetPosition(MapBorder);
                MapCanvas.CaptureMouse();
            }
            else if (_vm.CurrentMode == EditMode.Select)
            {
                _isDragSelecting = true;
                _dragSelectStart = e.GetPosition(MapCanvas);
                MapCanvas.CaptureMouse();
            }
            else if (_vm.CurrentMode == EditMode.DrawRect || _vm.CurrentMode == EditMode.DrawLine)
            {
                var pos = e.GetPosition(MapCanvas);
                var grid = _vm.ScreenToGrid(pos.X, pos.Y);
                _vm.StartShapeDraw(grid.X, grid.Y);
                _isShapeDrawing = true;
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

            var canvasPos = e.GetPosition(MapCanvas);
            _vm.UpdateMousePosition(canvasPos.X, canvasPos.Y);

            if (_isPanning)
            {
                var current = e.GetPosition(MapBorder);
                var delta = current - _lastPanPoint;
                _vm.ApplyPanDelta(delta.X, delta.Y);
                _lastPanPoint = current;
            }
            else if (_isDragSelecting)
            {
                // Draw/update the selection rectangle
                UpdateDragSelectRect(canvasPos);
            }
            else if (_isShapeDrawing)
            {
                var grid = _vm.ScreenToGrid(canvasPos.X, canvasPos.Y);
                bool shiftHeld = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
                _vm.UpdateShapeDraw(grid.X, grid.Y, shiftHeld);
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
            if (_vm == null)
            {
                _isPanning = false;
                _isPainting = false;
                _isDragSelecting = false;
                _isShapeDrawing = false;
                MapCanvas.ReleaseMouseCapture();
                return;
            }

            if (_isDragSelecting)
            {
                var endPos = e.GetPosition(MapCanvas);
                FinishDragSelect(endPos);
                _isDragSelecting = false;
            }
            else if (_isShapeDrawing)
            {
                _vm.FinishShapeDraw();
                _isShapeDrawing = false;
            }

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

        // =============================================
        // Drag-select rectangle
        // =============================================

        private void UpdateDragSelectRect(Point currentCanvasPos)
        {
            var startScreen = MapCanvas.TranslatePoint(_dragSelectStart, SelectionRectCanvas);
            var endScreen = MapCanvas.TranslatePoint(currentCanvasPos, SelectionRectCanvas);

            double x = Math.Min(startScreen.X, endScreen.X);
            double y = Math.Min(startScreen.Y, endScreen.Y);
            double w = Math.Abs(endScreen.X - startScreen.X);
            double h = Math.Abs(endScreen.Y - startScreen.Y);

            if (_dragSelectRect == null)
            {
                _dragSelectRect = new Rectangle
                {
                    Stroke = new SolidColorBrush(Color.FromArgb(200, 0, 200, 255)),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 },
                    Fill = new SolidColorBrush(Color.FromArgb(30, 0, 200, 255)),
                    IsHitTestVisible = false
                };
                SelectionRectCanvas.Children.Add(_dragSelectRect);
            }

            Canvas.SetLeft(_dragSelectRect, x);
            Canvas.SetTop(_dragSelectRect, y);
            _dragSelectRect.Width = w;
            _dragSelectRect.Height = h;
        }

        private void FinishDragSelect(Point endCanvasPos)
        {
            if (_dragSelectRect != null)
            {
                SelectionRectCanvas.Children.Remove(_dragSelectRect);
                _dragSelectRect = null;
            }

            if (_vm?.TileMap == null) return;

            double minX = Math.Min(_dragSelectStart.X, endCanvasPos.X);
            double minY = Math.Min(_dragSelectStart.Y, endCanvasPos.Y);
            double maxX = Math.Max(_dragSelectStart.X, endCanvasPos.X);
            double maxY = Math.Max(_dragSelectStart.Y, endCanvasPos.Y);

            var gridMin = _vm.ScreenToGrid(minX, minY);
            var gridMax = _vm.ScreenToGrid(maxX, maxY);

            double dragDistance = Math.Abs(endCanvasPos.X - _dragSelectStart.X) +
                                  Math.Abs(endCanvasPos.Y - _dragSelectStart.Y);

            bool shiftHeld = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

            if (dragDistance < 5)
            {
                var grid = _vm.ScreenToGrid(endCanvasPos.X, endCanvasPos.Y);
                _vm.HandleSelectClick(grid.X, grid.Y, shiftHeld);
            }
            else
            {
                _vm.SelectInRect(gridMin.X, gridMin.Y, gridMax.X, gridMax.Y, addToSelection: shiftHeld);
            }
        }

        // =============================================
        // Selection highlight rendering
        // =============================================

        private void DrawSelectionHighlights()
        {
            SelectionLayer.Children.Clear();

            if (_vm?.TileMap == null) return;

            double cellSize = _vm.TileMap.CellSize;
            var highlightBrush = new SolidColorBrush(Color.FromArgb(160, 0, 200, 255));

            foreach (var tile in _vm.SelectedTiles)
            {
                var border = new Rectangle
                {
                    Width = cellSize,
                    Height = cellSize,
                    Stroke = highlightBrush,
                    StrokeThickness = 2,
                    Fill = new SolidColorBrush(Color.FromArgb(40, 0, 200, 255)),
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(border, tile.GridX * cellSize);
                Canvas.SetTop(border, tile.GridY * cellSize);
                Canvas.SetZIndex(border, 900);

                SelectionLayer.Children.Add(border);
            }
        }

        #region Shape preview rendering

        private void DrawShapePreview()
        {
            ShapePreviewLayer.Children.Clear();

            if (_vm?.TileMap == null || _vm.CurrentShapePreview.Count == 0) return;

            double cellSize = _vm.TileMap.CellSize;
            var previewBrush = new SolidColorBrush(Color.FromArgb(80, 0, 255, 100));
            var previewStroke = new SolidColorBrush(Color.FromArgb(180, 0, 255, 100));

            foreach (var (px, py) in _vm.CurrentShapePreview)
            {
                if (px < 0 || px >= _vm.TileMap.Width || py < 0 || py >= _vm.TileMap.Height)
                    continue;

                var rect = new Rectangle()
                {
                    Width = cellSize,
                    Height = cellSize,
                    Fill = previewBrush,
                    Stroke = previewBrush,
                    StrokeThickness = 1,
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(rect, px * cellSize);
                Canvas.SetTop(rect, py * cellSize);
                Canvas.SetZIndex(rect, 800);

                ShapePreviewLayer.Children.Add(rect);
            }
        }
        #endregion

        // =============================================
        // Rendering
        // =============================================

        private void RenderMap()
        {
            if (_vm?.TileMap == null) return;
            var tileMap = _vm.TileMap;

            TilesLayer.Children.Clear();
            GridLayer.Children.Clear();
            SelectionLayer.Children.Clear();
            ShapePreviewLayer.Children.Clear();
            MetadataLayer.Children.Clear();

            MapCanvas.Width = tileMap.Width * tileMap.CellSize;
            MapCanvas.Height = tileMap.Height * tileMap.CellSize;

            if (_vm.IsGridVisible)
                DrawGrid(tileMap);

            foreach (var tile in tileMap.PlacedTiles.OrderBy(t => t.ZIndex ?? 0))
                DrawTile(tile);

            if (_vm.ShowDMView)
                DrawMetadataOverlays(tileMap);

            if (_vm.IsFogOfWarEnabled)
                RenderFogOfWar(tileMap);

            // Redraw selection highlights after a full render
            DrawSelectionHighlights();
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
                    X1 = 0,
                    Y1 = y * cellSize,
                    X2 = tileMap.Width * cellSize,
                    Y2 = y * cellSize,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                });
            }

            for (int x = 0; x <= tileMap.Width; x++)
            {
                GridLayer.Children.Add(new Line()
                {
                    X1 = x * cellSize,
                    Y1 = 0,
                    X2 = x * cellSize,
                    Y2 = tileMap.Height * cellSize,
                    Stroke = gridBrush,
                    StrokeThickness = 1
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
            if (sender is CheckBox cb && cb.Tag is TileLayer layer)
            {
                _vm?.ToggleLayerVisibilityCommand.Execute(layer);
            }
        }
    }
}