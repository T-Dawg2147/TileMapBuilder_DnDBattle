using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TileMapBuilder.App.Controls;
using TileMapBuilder.App.Services;
using TileMapBuilder.Core.ViewModels.Controls;
using TileMapBuilder.Core.ViewModels.TileViewModels;

namespace TileMapBuilder.App.Views
{
    /// <summary>
    /// Interaction logic for TileMapEditorWindow.xaml
    /// </summary>
    public partial class TileMapEditorWindow : UserControl
    {
        private TileMapEditorViewModel? _editorVm;
        private TileMapControlViewModel? _mapControlVm;
        private TilePaletteViewModel? _paletteVm;
        private TileEditorViewModel? _tileEditorVm;

        public TileMapEditorWindow()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // REMEMBER You better remember this or on god I'll break >:(

            // ── Step 1: Get the parent ViewModel (set by DataTemplate) ──
            _editorVm = DataContext as TileMapEditorViewModel;
            if (_editorVm == null) return;

            // ── Step 2: Resolve child ViewModels from DI ──
            _mapControlVm = App.Services.GetRequiredService<TileMapControlViewModel>();
            _paletteVm = App.Services.GetRequiredService<TilePaletteViewModel>();
            _tileEditorVm = App.Services.GetRequiredService<TileEditorViewModel>();

            // ── Step 3: Set DataContexts on child controls ──
            MapEditorControl.DataContext = _mapControlVm;
            PalettePanel.DataContext = _paletteVm;
            TileEditorControl.DataContext = _tileEditorVm;

            // ── Step 4: Wire up cross-ViewModel communication ──

            // When the user picks a tile in the palette, tell the map control
            _paletteVm.TileSelected += tileDef =>
            {
                _mapControlVm.SelectedTileDefinition = tileDef;
            };

            // When the editor creates/loads a map, pass it to the map control
            _editorVm.MapLoaded += map =>
            {
                _mapControlVm.TileMap = map;
            };

            // When the user right-clicks or uses Properties mode on a tile,
            // open it in the tile editor panel
            _mapControlVm.TilePropertiesRequested += tile =>
            {
                _tileEditorVm.LoadTile(tile);
            };

            // When the tile editor applies changes, re-render the map
            _tileEditorVm.MapRenderRequested += () =>
            {
                _mapControlVm.RequestMapRenderPublic();
            };

            // If the editor VM already has a map loaded, pass it through
            if (_editorVm.CurrentMap != null)
            {
                _mapControlVm.TileMap = _editorVm.CurrentMap;
            }
        }
    }
}
