using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDBattle.Data.Models.Tiles;

namespace TileMapBuilder.Core.ViewModels.Dialogs
{
    public sealed partial class MapPropertiesViewModel : ObservableObject
    {
        [ObservableProperty] private string _mapName = string.Empty;
        [ObservableProperty] private string _description = string.Empty;
        [ObservableProperty] private string _author = string.Empty;
        [ObservableProperty] private string _environmentType = "Dungeon";
        [ObservableProperty] private int _width;
        [ObservableProperty] private int _height;
        [ObservableProperty] private double _cellSize = 48.0;
        [ObservableProperty] private int _feetPerSquare = 5;
        [ObservableProperty] private string _backgroundColor = "#FF1A1A1A";
        [ObservableProperty] private bool? _dialogResult;

        public static string[] EnvironmentTypes =>
        [
            "Dungeon",
            "Cave",
            "Forest",
            "Grassland",
            "Mountain", 
            "Swamp",
            "Desert",
            "Coastal",
            "Underwater",
            "Town",
            "City",
            "Castle",
            "Ruins",
            "Planar",
            "Other"
        ];

        public MapPropertiesViewModel(TileMap map)
        {
            MapName = map.Name;
            Description = map.Description;
            Author = map.Author;
            EnvironmentType = map.EnvironmentType;
            Width = map.Width;
            Height = map.Height;
            CellSize = map.CellSize;
            FeetPerSquare = map.FeetPerSquare;
            BackgroundColor = map.BackgroundColor;
        }

        public void ApplyTo(TileMap map)
        {
            map.Name = MapName;
            map.Description = Description;
            map.Author = Author;
            map.EnvironmentType = EnvironmentType;
            map.Width = Width;
            map.Height = Height;
            map.CellSize = CellSize;
            map.FeetPerSquare = FeetPerSquare;
            map.BackgroundColor = BackgroundColor;
        }

        [RelayCommand] private void Confirm() => DialogResult = true;

        [RelayCommand] private void Cancel() => DialogResult = false;
    }
}
