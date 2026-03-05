using System.Collections.ObjectModel;
using DnDBattle.Data.Models.Tiles;

namespace DnDBattle.Data.Services.Interfaces
{
    public interface ITileLibraryService
    {
        ObservableCollection<TileDefinition> AvailableTiles { get; }

        void RefreshLibrary();
        void LoadTileLibrary();

        TileDefinition? GetTileById(string id);

        TileDefinition? GetTilesByImagePath(string imagePath);

        Dictionary<string, List<TileDefinition>> GetTilesByCategory();
    }
}
