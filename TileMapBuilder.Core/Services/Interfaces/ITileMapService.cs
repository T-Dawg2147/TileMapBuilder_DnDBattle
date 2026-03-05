using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TileMapBuilder.Core.Models.Tiles;

namespace TileMapBuilder.Core.Services.Interfaces
{
    public interface ITileMapService
    {
        Task<bool> SaveMapAsync(TileMap map, string filePath);

        Task<TileMap> LoadMapAsync(string filePath);
    }
}
