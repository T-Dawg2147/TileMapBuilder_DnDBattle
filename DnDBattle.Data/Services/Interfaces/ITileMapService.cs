using DnDBattle.Data.Models.Tiles;

namespace DnDBattle.Data.Services.Interfaces
{
    public interface ITileMapService
    {
        Task<bool?> SaveMapAsync(TileMap map, string filePath);

        Task<TileMap> LoadMapAsync(string filePath);
    }
}
