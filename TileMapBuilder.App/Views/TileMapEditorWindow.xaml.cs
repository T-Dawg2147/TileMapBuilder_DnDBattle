using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;
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

            // ── Step 3: Set DataContexts on child controls ──
            MapEditorControl.DataContext = _mapControlVm;
            PalettePanel.DataContext = _paletteVm;

            // ── Step 4: Wire up cross-ViewModel communication ──

            // When the user picks a tile in the palette, tell the map control which tile is selected
            _paletteVm.TileSelected += (tileDef) =>
            {
                _mapControlVm.SelectedTileDefinition = tileDef;
            };

            // When the editor creates/loads a new map, push it to the map control
            _editorVm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(TileMapEditorViewModel.CurrentMap))
                {
                    _mapControlVm.TileMap = _editorVm.CurrentMap;
                }
            };

            // If a map is already loaded, push it now
            if (_editorVm.CurrentMap != null)
            {
                _mapControlVm.TileMap = _editorVm.CurrentMap;
            }

            // ── Step 5: Connect MapVisualProvider for image export ──
            var holder = App.Services.GetRequiredService<MapVisualProviderHolder>();
            holder.SetVisualFactory(() => MapEditorControl.FindName("MapCanvas") as System.Windows.Media.Visual);
        }
    }
}
