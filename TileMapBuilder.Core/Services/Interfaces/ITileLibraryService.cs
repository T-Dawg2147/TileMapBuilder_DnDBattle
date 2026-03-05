using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TileMapBuilder.Core.Models.Tiles;

namespace TileMapBuilder.Core.Services.Interfaces
{
    public interface ITileLibraryService
    {
        ObservableCollection<TileDefinition> AvailableTiles { get; }

        void RefreshLibrary();
        void LoadTileLibrary();

        TileDefinition? GetTileById(string id);

        Dictionary<string, List<TileDefinition>> GetTilesByCategory();
    }
}
