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

        [ObservableProperty] private string _name = "New Map";

        public int Width { get; set; } = 24;
        public int Height { get; set; } = 24;

        public double CellSize { get; set; } = 48.0;

        public ObservableCollection<Tile> PlacedTiles { get; set; } = [];

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        public string BackgroundColor { get; set; } = "#FF1A1A1A";
        public bool ShowGrid { get; set; } = true;
        public double GridOpacity { get; set; } = 0.15;
        public string GridColor { get; set; } = "#FFFFFF";

        public int FeetPerSquare { get; set; } = 5;

        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string EnvironmentType { get; set; } = "Dungeon";

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

        public List<Tile> Resize(int addTop, int addBottom, int addLeft, int addRight)
        {
            int newWidth = Width + addLeft + addRight;
            int newHeight = Height + addTop + addBottom;

            if (newWidth < 1 || newHeight < 1)
                return new List<Tile>();

            if (addTop != 0 || addLeft != 0)
            {
                foreach (var tile in PlacedTiles)
                {
                    tile.GridX += addLeft;
                    tile.GridY += addTop;
                }
            }

            var removedTiles = PlacedTiles
                .Where(t => t.GridX < 0 || t.GridX >= newWidth || t.GridY < 0 || t.GridY >= newHeight)
                .ToList();

            foreach (var tile in removedTiles)
                PlacedTiles.Remove(tile);

            Width = newWidth;
            Height = newHeight;
            ModifiedDate = DateTime.UtcNow;

            return removedTiles;
        }
    }

    public sealed class TileMapDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public double CellSize { get; set; }
        public string BackgroundColor { get; set; } = "#FF1A1A1A";
        public bool ShowGrid { get; set; }
        public double GridOpacity { get; set; } = 0.15;
        public string GridColor { get; set; } = "#FFFFFF";
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public List<TileDto> PlacedTiles { get; set; } = [];

        public int FeetPerSquare { get; set; } = 5;

        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Environment { get; set; } = "Dungeon";

        public List<MapNote> Notes { get; set; } = [];
    }
}
