using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows;
using TileMapBuilder.App.Controls;
using TileMapBuilder.App.Services;
using TileMapBuilder.Core.ViewModels.Controls;
using TileMapBuilder.Core.ViewModels.TileViewModels;

namespace TileMapBuilder.App.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isWiredUp = false;
        private int _wiredUpAttempts = 0;
        private const int MaxWireUpAttempts = 10;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!TryWireUpChildViewModels())
            {
                Debug.WriteLine("[MainWindow] Visual tree not ready on Loaded, subscribing to LayoutUpdated");
                LayoutUpdated += OnLayoutUpdated;
            }
        }

        private void OnLayoutUpdated(object? sender, EventArgs e)
        {
            _wiredUpAttempts++;

            if (TryWireUpChildViewModels())
            {
                LayoutUpdated -= OnLayoutUpdated;
                Debug.WriteLine("[MainWindow] Successfully wired up child ViewModels on LayoutUpdated attempt");
            }
            else if (_wiredUpAttempts >= MaxWireUpAttempts)
            {
                LayoutUpdated -= OnLayoutUpdated;
                Debug.WriteLine("[MainWindow] WARNING: Failed to wire up child ViewModels after max attempts");
            }
        }

        private bool TryWireUpChildViewModels()
        {
            if (_isWiredUp) return true;

            var palettePanel = FindVisualChild<TilePalettePanel>(this);
            var mapEditor = FindVisualChild<TileMapToolbarControl>(this);

            if (palettePanel == null || mapEditor == null)
            {
                Debug.WriteLine($"[MainWindow] FindVisualChild: palettePanel={palettePanel != null}, mapEditor={mapEditor != null}");
                return false;
            }

            var editorVm = DataContext is TileMapBuilder.Core.ViewModels.ShellViewModel shell
                ? shell.CurrentView as TileMapEditorViewModel
                : null;

            if (editorVm == null)
            {
                Debug.WriteLine("[MainWindow] editorVm is null - ShellViewModel.CurrentView not set yet.");
                return false;
            }

            var mapControlVm = App.Services.GetRequiredService<TileMapControlViewModel>();
            var paletteVm = App.Services.GetRequiredService<TilePaletteViewModel>();

            palettePanel.DataContext = paletteVm;
            mapEditor.DataContext = mapControlVm;

            paletteVm.TileSelected += (tileDef) =>
            {
                mapControlVm.SelectedTileDefinition = tileDef;
            };

            editorVm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(TileMapEditorViewModel.CurrentMap))
                {
                    mapControlVm.TileMap = editorVm.CurrentMap;
                }
            };

            if (editorVm.CurrentMap != null)
            {
                mapControlVm.TileMap = editorVm.CurrentMap;
            }

            var holder = App.Services.GetRequiredService<MapVisualProviderHolder>();
            holder.SetVisualFactory(() => mapEditor.FindName("MapCanvas") as System.Windows.Media.Visual);

            _isWiredUp = true;
            Debug.WriteLine("[MainWindow] Child ViewModels wired up successfully");
            return true;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is T found)
                    return found;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}