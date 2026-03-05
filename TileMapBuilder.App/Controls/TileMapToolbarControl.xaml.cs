using DnDBattle.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TileMapBuilder.Core.Models.Tiles;
using TileMapBuilder.Core.Models.Tiles.Metadata;
using TileMapBuilder.Core.Services.TileService;
using TileMapBuilder.Core.ViewModels.TileViewModels;

namespace TileMapBuilder.App.Controls
{
    /// <summary>
    /// Interaction logic for TileMapToolbarControl.xaml
    /// </summary>
    public partial class TileMapToolbarControl : UserControl
    {
        public static readonly DependencyProperty TileMapProperty =
            DependencyProperty.Register(nameof(TileMap), typeof(TileMap), typeof(TileMapToolbarControl),
                new PropertyMetadata(null, OnTileMapChanged));

        public TileMap TileMap
        {
            get => (TileMap)GetValue(TileMapProperty);
        }

        private bool _isPanning;
        private Point _lastPanPoint;
        private bool _isPainting;

        public TileMapToolbarControl()
        {
            InitializeComponent();

        }

        private TileMapControlViewModel? _vm => DataContext as TileMapControlViewModel;

        private static void OnTileMapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TileMapToolbarControl)d;
            control.RenderMap();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_vm == null) return;

            _vm.MapRenderRequested += RenderMap;
            _vm.TileDrawRequested += DrawTile;
            _vm.TileRemoveVisualRequested += RemoveTileVisual;

            RenderMap();
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
                var line = new Line()
                {
                    X1 = 0, Y1 = y * cellSize,
                    X2 = tileMap.Width * cellSize, Y2 = y * cellSize,
                    Stroke = gridBrush, StrokeThickness = 1
                };
                GridLayer.Children.Add(line);
            }
        }

        private void DrawTile(Tile tile)
        {
            if (_vm?.TileMap == null) return;
            var tileMap = _vm.TileMap;

            var tileDef = TileLibraryService.Instance.GetTileById(tile.TileDefinitionId);
            if (tileDef == null) return;

            if (!_vm.IsLayerVisible(tileDef.Layer)) return;

            var image = TileImageCacheService.Instance.GetOrLoadImage(tileDef.ImagePath);
            if (image == null) return;

            var tileImage = new Image()
            {
                Source = image,
                Width = tileMap.CellSize,
                Height = tileMap.CellSize,
                Stretch = Stretch.Fill,
                Tag = tile
            };

            var transformGroup = new TransformGroup();
            if (tile.Rotation != 0)
                transformGroup.Children.Add(new RotateTransform(tile.Rotation, tileMap.CellSize / 2, tileMap.CellSize / 2));
            if (tile.FlipHorizontal)
                transformGroup.Children.Add(new ScaleTransform(-1, 1, tileMap.CellSize / 2, tileMap.CellSize / 2));
            if (tile.FlipVertical)
                transformGroup.Children.Add(new ScaleTransform(1, -1, tileMap.CellSize / 2, tileMap.CellSize / 2));
            tileImage.RenderTransform = transformGroup;

            Canvas.SetLeft(tileImage, tile.GridX * tileMap.CellSize);
            Canvas.SetTop(tileImage, tile.GridY * tileMap.CellSize);
            Canvas.SetZIndex(tileImage, tile.GetEffectiveZIndex(tileDef));

            TilesLayer.Children.Add(tileImage);
        }

        private void RemoveTileVisual(Tile tile)
        {
            var visual = TilesLayer.Children.OfType<Image>()
                .FirstOrDefault(i => i.Tag == tile);
            if (visual != null)
                TilesLayer.Children.Remove(visual);
        }

        // TODO Im sure this method can be optimized a little better
        // Interating of EACH tile seems like it could be a waste?
        private void RenderFogOfWar(TileMap tileMap)
        {
            var fogState = _vm!.GetFogOfWarState();
            double cellSize = tileMap.CellSize;

            for (int x = 0; x < tileMap.Width; x++)
            {
                for (int y = 0; y < tileMap.Height; y++)
                {
                    bool isRevealed = fogState.IsTileRevealed(x, y);
                    bool isVisible = fogState.IsTileVisible(x, y);
                    Rectangle? fogTile = null;

                    if (!isRevealed)
                    {
                        fogTile = new Rectangle()
                        {
                            Width = cellSize, Height = cellSize,
                            Fill = new SolidColorBrush(Color.FromArgb(220, 0,0,0)),
                            IsHitTestVisible = false
                        };
                    }
                    else if (fogState.Mode == DnDBattle.Data.Services.FogMode.Dynamic && !isVisible)
                    {
                        fogTile = new Rectangle()
                        {
                            Width = cellSize, Height = cellSize,
                            Fill = new SolidColorBrush(Color.FromArgb(120,0,0,0)),
                            IsHitTestVisible = false
                        };
                    }

                    if (fogTile != null)
                    {
                        Canvas.SetLeft(fogTile, x * cellSize);
                        Canvas.SetTop(fogTile, 7 * cellSize);
                        Canvas.SetZIndex(fogTile, 2000);
                        MetadataLayer.Children.Add(fogTile);
                    }
                }
            }
        }

        #region Metadata Handling

        private void DrawMetadataOverlays(TileMap tileMap)
        {
            if (tileMap == null) return;

            foreach (var tile in tileMap.PlacedTiles.Where(t => t.HasMetadata))
            {
                DrawMetadataIndicator(tile);
            }
        }

        private void DrawMetadataIndicator(Tile tile)
        {
            double cellSize = TileMap.CellSize;
            double x = tile.GridX * cellSize;
            double y = tile.GridY * cellSize;

            // Create pulsing border animation for active metadata
            var border = new Border
            {
                Width = cellSize,
                Height = cellSize,
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(4),
                IsHitTestVisible = false
            };

            var metadataTypes = tile.Metadata.Select(m => m.Type).ToList();
            border.BorderBrush = GetMetadataBorderBrush(metadataTypes);

            // Add glow effect for traps
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

            // Enhanced icon panel with better layout
            var iconPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                IsHitTestVisible = false
            };

            foreach (var metadata in tile.Metadata.Take(4))
            {
                var iconBorder = new Border
                {
                    Background = GetMetadataIconBackground(metadata.Type),
                    Padding = new Thickness(4),
                    Margin = new Thickness(2)
                };

                var icon = new TextBlock
                {
                    Text = metadata.Type.GetIcon(),
                    FontSize = cellSize * 0.25,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                iconBorder.Child = icon;
                iconPanel.Children.Add(iconBorder);
            }

            if (tile.Metadata.Count > 4)
            {
                var moreText = new TextBlock
                {
                    Text = $"+{tile.Metadata.Count - 4}",
                    FontSize = cellSize * 0.18,
                    Margin = new Thickness(4),
                    Foreground = Brushes.Yellow,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                iconPanel.Children.Add(moreText);
            }

            Canvas.SetLeft(iconPanel, x);
            Canvas.SetTop(iconPanel, y);
            Canvas.SetZIndex(iconPanel, 1001);

            MetadataLayer.Children.Add(iconPanel);

            var spawns = tile.Metadata.OfType<SpawnMetadata>().ToList();
            if (spawns.Any())
            {
                DrawSpawnPreview(tile, spawns.First());
            }

            var tooltip = CreateMetadataTooltip(tile);
            border.ToolTip = tooltip;
            iconPanel.ToolTip = tooltip;
        }

        private Brush GetMetadataBorderBrush(List<TileMetadataType> types)
        {
            if (types.Contains(TileMetadataType.Trap))
                return new SolidColorBrush(Color.FromArgb(200, 244, 67, 54)); // Red

            if (types.Contains(TileMetadataType.Hazard))
                return new SolidColorBrush(Color.FromArgb(200, 255, 152, 0)); // Orange

            if (types.Contains(TileMetadataType.Secret))
                return new SolidColorBrush(Color.FromArgb(200, 255, 235, 59)); // Yellow

            if (types.Contains(TileMetadataType.Interactive))
                return new SolidColorBrush(Color.FromArgb(200, 33, 150, 243)); // Blue

            if (types.Contains(TileMetadataType.Trigger))
                return new SolidColorBrush(Color.FromArgb(200, 156, 39, 176)); // Purple

            if (types.Contains(TileMetadataType.Spawn))
                return new SolidColorBrush(Color.FromArgb(200, 244, 67, 54)); // Red

            if (types.Contains(TileMetadataType.Teleporter))
                return new SolidColorBrush(Color.FromArgb(200, 0, 188, 212)); // Cyan

            if (types.Contains(TileMetadataType.Healing))
                return new SolidColorBrush(Color.FromArgb(200, 76, 175, 80)); // Green

            return new SolidColorBrush(Color.FromArgb(200, 158, 158, 158)); // Gray
        }

        private Brush GetMetadataIconBackground(TileMetadataType type)
        {
            return type switch
            {
                TileMetadataType.Trap => new SolidColorBrush(Color.FromArgb(200, 244, 67, 54)),
                TileMetadataType.Secret => new SolidColorBrush(Color.FromArgb(200, 255, 235, 59)),
                TileMetadataType.Interactive => new SolidColorBrush(Color.FromArgb(200, 33, 150, 243)),
                TileMetadataType.Hazard => new SolidColorBrush(Color.FromArgb(200, 255, 152, 0)),
                TileMetadataType.Teleporter => new SolidColorBrush(Color.FromArgb(200, 0, 188, 212)),
                TileMetadataType.Healing => new SolidColorBrush(Color.FromArgb(200, 76, 175, 80)),
                TileMetadataType.Spawn => new SolidColorBrush(Color.FromArgb(200, 244, 67, 54)),
                _ => new SolidColorBrush(Color.FromArgb(200, 158, 158, 158))
            };
        }

        private ToolTip CreateMetadataTooltip(Tile tile)
        {
            var tooltip = new ToolTip
            {
                Background = (Brush)Application.Current.Resources["Brush_Background_Control"],
                BorderBrush = (Brush)Application.Current.Resources["Brush_Border_Normal"],
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10)
            };

            var panel = new StackPanel();

            // Header
            var header = new TextBlock
            {
                Text = $"📍 Tile ({tile.GridX}, {tile.GridY})",
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = (Brush)Application.Current.Resources["Brush_Text_Primary"],
                Margin = new Thickness(0, 0, 0, 8)
            };
            panel.Children.Add(header);

            // Metadata list
            foreach (var metadata in tile.Metadata)
            {
                var metaPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 3, 0, 3)
                };

                var icon = new TextBlock
                {
                    Text = metadata.Type.GetIcon(),
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                metaPanel.Children.Add(icon);

                var info = new StackPanel();

                var nameText = new TextBlock
                {
                    Text = metadata.Name ?? metadata.Type.GetDisplayName(),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)Application.Current.Resources["Brush_Text_Primary"]
                };
                info.Children.Add(nameText);

                var typeText = new TextBlock
                {
                    Text = metadata.Type.GetDisplayName(),
                    FontSize = 10,
                    Foreground = (Brush)Application.Current.Resources["Brush_Text_Hint"]
                };
                info.Children.Add(typeText);

                // Add trap-specific info
                if (metadata is TrapMetadata trap)
                {
                    var trapInfo = new TextBlock
                    {
                        Text = $"DC {trap.DetectionDC} Perception | {trap.DamageDice} {trap.DamageType.GetDisplayName()}",
                        FontSize = 10,
                        Foreground = (Brush)Application.Current.Resources["Brush_Warning"]
                    };
                    info.Children.Add(trapInfo);

                    if (trap.IsDetected)
                    {
                        var detected = new TextBlock
                        {
                            Text = "🔍 Detected",
                            FontSize = 10,
                            Foreground = (Brush)Application.Current.Resources["Brush_Success"]
                        };
                        info.Children.Add(detected);
                    }

                    if (trap.IsDisarmed)
                    {
                        var disarmed = new TextBlock
                        {
                            Text = "✅ Disarmed",
                            FontSize = 10,
                            Foreground = (Brush)Application.Current.Resources["Brush_Success"]
                        };
                        info.Children.Add(disarmed);
                    }
                }

                metaPanel.Children.Add(info);
                panel.Children.Add(metaPanel);
            }

            // Footer hint
            var footer = new TextBlock
            {
                Text = "Right-click to edit properties",
                FontSize = 10,
                FontStyle = FontStyles.Italic,
                Foreground = (Brush)Application.Current.Resources["Brush_Text_Hint"],
                Margin = new Thickness(0, 8, 0, 0)
            };
            panel.Children.Add(footer);

            tooltip.Content = panel;
            return tooltip;
        }

        private void DrawSpawnPreview(Tile tile, SpawnMetadata spawn)
        {
            if (spawn.SpawnRadius == 0) return;

            double cellSize = TileMap.CellSize;
            double centerX = (tile.GridX + 0.5) * cellSize;
            double centerY = (tile.GridY + 0.5) * cellSize;
            double radius = spawn.SpawnRadius * cellSize;

            // Draw spawn radius circle
            var circle = new System.Windows.Shapes.Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Stroke = new SolidColorBrush(Color.FromArgb(150, 244, 67, 54)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = new SolidColorBrush(Color.FromArgb(30, 244, 67, 54)),
                IsHitTestVisible = false
            };

            Canvas.SetLeft(circle, centerX - radius);
            Canvas.SetTop(circle, centerY - radius);
            Canvas.SetZIndex(circle, 999);

            MetadataLayer.Children.Add(circle);

            // Add spawn count label
            var label = new TextBlock
            {
                Text = $"{spawn.SpawnCount}× {spawn.CreatureName}",
                FontSize = cellSize * 0.2,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(180, 244, 67, 54)),
                Padding = new Thickness(0, 4, 0, 2),
                IsHitTestVisible = false
            };

            Canvas.SetLeft(label, centerX - (label.ActualWidth / 2));
            Canvas.SetTop(label, centerY + cellSize * 0.3);
            Canvas.SetZIndex(label, 1002);

            MetadataLayer.Children.Add(label);
        }
        #endregion

        #region Mouse Events

        private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_vm == null) return;

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                _isPanning = true;
                _lastPanPoint = e.GetPosition(MapBorder);
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
                var currentPoint = e.GetPosition(MapBorder);
                var delta = currentPoint - _lastPanPoint;
                _vm.ApplyPanDelta(delta.X, delta.Y);
                _lastPanPoint = currentPoint;
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

        #endregion

        #region Keyboard Events

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (_vm == null) return;

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.Z:
                        if (_vm.CanUndo) { _vm.UndoCommand.Execute(null); e.Handled = true; }
                        break;
                    case Key.Y:
                        if (_vm.CanRedo) { _vm.RedoCommand.Execute(null); e.Handled = true; }
                        break;
                    case Key.C:
                        var copyPos = GetCurrentGridPosition();
                        _vm.CopyAtCommand.Execute(copyPos);
                        e.Handled = true;
                        break;
                    case Key.V:
                        var pastePos = GetCurrentGridPosition();
                        _vm.PasteAtCommand.Execute(pastePos);
                        e.Handled = true;
                        break;
                    case Key.X:
                        var cutPos = GetCurrentGridPosition();
                        _vm.CutAtCommand.Execute(cutPos);
                        e.Handled = true;
                        break;
                }
            }

            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                switch (e.Key)
                {
                    case Key.Q: _vm.RotateLeftCommand.Execute(null); e.Handled = true; break;
                    case Key.E: _vm.RotateRightCommand.Execute(null); e.Handled = true; break;
                    case Key.H: _vm.FlipHorizontalCommand.Execute(null); e.Handled = true; break;
                    case Key.V: _vm.FlipVerticalCommand.Execute(null); e.Handled = true; break;
                    case Key.R: _vm.ResetTransformsCommand.Execute(null); e.Handled = true; break;
                }
            }
        }

        #endregion

        // Helper
        private (int X, int Y) GetCurrentGridPosition()
        {
            var mousePos = Mouse.GetPosition(MapCanvas);
            var grid = _vm!.ScreenToGrid(mousePos.X, mousePos.Y);
            return (grid.X, grid.Y);
        }

    }
}
