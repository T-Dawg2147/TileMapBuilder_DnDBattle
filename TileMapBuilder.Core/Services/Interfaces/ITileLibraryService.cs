using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TileMapBuilder.Core.Models.Tiles;

namespace TileMapBuilder.Core.Services.Interfaces
{
    public interface ITileLibraryService
    {
        public void RefreshLibrary();
        public void LoadTileLibrary();

        public TileDefinition? GetTileById(string id);
    }
}
