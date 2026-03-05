using Microsoft.Extensions.DependencyInjection;
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
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
            {
                WireUpChildViewModels();
            });
        }

        private void WireUpChildViewModels()
        {
            var palettePanel = FindVisualChild<TilePalettePanel>(this);
            var mapEditor = FindVisualChild<TileMapToolbarControl>(this);

            if (palettePanel == null || mapEditor == null) return;

            var editorVm = DataContext is TileMapBuilder.Core.ViewModels.ShellViewModel shell
                ? shell.CurrentView as TileMapEditorViewModel
                : null;

            if (editorVm == null) return;

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