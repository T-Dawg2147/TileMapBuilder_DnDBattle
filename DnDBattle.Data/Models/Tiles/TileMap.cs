using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Data.Models.Tiles
{

    public partial class TileMap : ObservableObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [ObservableProperty] private string _name = "Untitled Map";

        public int Width { get; set; } = 50;
        public int Height { get; set; } = 50;

        public double CellSize { get; set; } = 48.0; // TODO Need to look into what this relates to?

        public ObservableCollection<Tile> PlacedTiles { get; set; } = [];

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        public string BackgroundColor { get; set; } = "#FF1A1A1A";
        public bool ShowGrid { get; set; } = true;

        public int FeetPerSquare { get; set; } = 5;

        public List<MapNote> Notes { get; set; } = [];

        public IEnumerable<Tile> GetTilesAt(int x, int y, TileLayer layer = TileLayer.Floor)
        {
            return PlacedTiles.Where(t => t.GridX == x && t.GridY == y);
        }
             
        public void ClearTileAt(int x, int y, TileLayer layer = TileLayer.Floor)
        {
            var tilesToRemove = GetTilesAt(x, y).ToList();
            if (tilesToRemove.Count > 0)
            {
                foreach (var tile in tilesToRemove)
                    PlacedTiles.Remove(tile);
                ModifiedDate = DateTime.UtcNow;
            }
        }

        public void AddTile(Tile tile)
        {
            if (tile.GridX < 0 || tile.GridX >= Width || tile.GridY < 0 || tile.GridY >= Height)
                return;

            PlacedTiles.Add(tile);
            ModifiedDate = DateTime.UtcNow;
        }

        public void RemoveTile(Tile tile)
        {
            PlacedTiles.Remove(tile);
            ModifiedDate = DateTime.UtcNow;
        }
    }

    public sealed class TileMapDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double CellSize { get; set; }
        public string BackgroundColor { get; set; }
        public bool ShowGrid { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public List<TileDto> PlacedTiles { get; set; } = [];

        public int FeetPerSquare { get; set; } = 5;

        public List<MapNote> Notes { get; set; } = [];
    }
}
