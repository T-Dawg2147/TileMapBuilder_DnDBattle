using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDBattle.Data.Models.Tiles;
using DnDBattle.Data.Services;
using DnDBattle.Data.Services.Interfaces;
using DnDBattle.Data.Services.UndoRedo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TileMapBuilder.Core.ViewModels.Controls
{
    public sealed partial class TileEditorViewModel : ObservableObject
    {
        private readonly ITileLibraryService _tileLibraryService;
        private readonly UndoManager _undoManager;

        private Tile? _originalTile;
        private TileDefinition? _tileDef;

        public TileEditorViewModel(ITileLibraryService tileLibraryService, UndoManager undoManager)
        {
            _tileLibraryService = tileLibraryService;
            _undoManager = undoManager;
        }

        [ObservableProperty] private bool _isVisible;

        [ObservableProperty] private int _rotation;
        [ObservableProperty] private bool _flipHorizontal;
        [ObservableProperty] private bool _flipVertical;
        [ObservableProperty] private int? _zIndex;
        [ObservableProperty] private string _notes = string.Empty;

        [ObservableProperty] private string _displayName = string.Empty;
        [ObservableProperty] private string _category = string.Empty;
        [ObservableProperty] private string _description = string.Empty;
        [ObservableProperty] private TileLayer _layer;
        [ObservableProperty] private bool _blocksMovement;
        [ObservableProperty] private bool _blocksSight;
        [ObservableProperty] private bool _blocksLight;
        [ObservableProperty] private bool _isEnabled = true;
        [ObservableProperty] private string? _tintColor;

        [ObservableProperty] private string _tilePositionText = string.Empty;
        [ObservableProperty] private string _tileIdText = string.Empty;
        [ObservableProperty] private string _imagePath = string.Empty;

        public TileLayer[] AvailableLayers { get; } = Enum.GetValues<TileLayer>();

        public event Action? MapRenderRequested;

        public void LoadTile(Tile tile)
        {
            _originalTile = tile;
            _tileDef = tile.TileDefinitionId != null
                ? _tileLibraryService.GetTileById(tile.TileDefinitionId)
                : null;

            Rotation = tile.Rotation;
            FlipHorizontal = tile.FlipHorizontal;
            FlipVertical = tile.FlipVertical;
            ZIndex = tile.ZIndex;
            Notes = tile.Notes;

            TilePositionText = $"({tile.GridX}, {tile.GridY})";
            TileIdText = tile.Id.ToString()[..8];

            if (_tileDef != null)
            {
                DisplayName = _tileDef.DisplayName;
                Category = _tileDef.Category;
                Description = _tileDef.Description;
                Layer = _tileDef.Layer;
                BlocksMovement = _tileDef.BlocksMovement;
                BlocksSight = _tileDef.BlocksSight;
                BlocksLight = _tileDef.BlocksLight;
                IsEnabled = _tileDef.IsEnabled;
                TintColor = _tileDef.TintColor;
                ImagePath = _tileDef.ImagePath;
            }

            IsVisible = true;
        }

        [RelayCommand]
        private void ApplyChanges()
        {
            if (_originalTile == null) return;

            RecordAndApply(_originalTile, nameof(Tile.Rotation), _originalTile.Rotation, Rotation);
            RecordAndApply(_originalTile, nameof(Tile.FlipHorizontal), _originalTile.FlipHorizontal, FlipHorizontal);
            RecordAndApply(_originalTile, nameof(Tile.FlipVertical), _originalTile.FlipVertical, FlipVertical);
            RecordAndApply(_originalTile, nameof(Tile.ZIndex), _originalTile.ZIndex, ZIndex);
            RecordAndApply(_originalTile, nameof(Tile.Notes), _originalTile.Notes, Notes);

            if (_tileDef != null)
            {
                RecordAndApply(_tileDef, nameof(TileDefinition.DisplayName), _tileDef.DisplayName, DisplayName);
                RecordAndApply(_tileDef, nameof(TileDefinition.Category), _tileDef.Category, Category);
                RecordAndApply(_tileDef, nameof(TileDefinition.Description), _tileDef.Description, Description);
                RecordAndApply(_tileDef, nameof(TileDefinition.Layer), _tileDef.Layer, Layer);
                RecordAndApply(_tileDef, nameof(TileDefinition.BlocksMovement), _tileDef.BlocksMovement, BlocksMovement);
                RecordAndApply(_tileDef, nameof(TileDefinition.BlocksSight), _tileDef.BlocksSight, BlocksSight);
                RecordAndApply(_tileDef, nameof(TileDefinition.BlocksLight), _tileDef.BlocksLight, BlocksLight);
                RecordAndApply(_tileDef, nameof(TileDefinition.IsEnabled), _tileDef.IsEnabled, IsEnabled);
                RecordAndApply(_tileDef, nameof(TileDefinition.TintColor), _tileDef.TintColor, TintColor);
            }
            MapRenderRequested?.Invoke();
        }

        [RelayCommand]
        private void Close()
        {
            IsVisible = false;
            _originalTile = null;
            _tileDef = null;
        }

        private void RecordAndApply(object target, string propertyName, object? oldValue, object? newValue)
        {
            if (Equals(oldValue, newValue)) return;

            var action = new TilePropertyChangeAction(target, propertyName, oldValue, newValue);
            _undoManager.Record(action);
        }
    }
}
