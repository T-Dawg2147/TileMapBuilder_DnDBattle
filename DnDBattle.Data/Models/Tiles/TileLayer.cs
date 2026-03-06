using System;

namespace DnDBattle.Data.Models.Tiles
{
    public enum TileLayer
    {
        Floor = 0,
        Terrain = 10,
        Wall = 20,
        Door = 30,
        Furniture = 40,
        Props = 50,
        Effects = 60,
        Roof = 70
    }

    public static class TileLayerExtensions
    {
        public static string ToString(this TileLayer layer)
        {
            return layer switch
            {
                TileLayer.Floor => "Floor",
                TileLayer.Terrain => "Terrain",
                TileLayer.Wall => "Walls",
                TileLayer.Door => "Doors",
                TileLayer.Furniture => "Furniture",
                TileLayer.Props => "Props",
                TileLayer.Effects => "Effects",
                TileLayer.Roof => "Roof",
                _ => layer.ToString()
            };
        }

        // TODO WILL be replaced with actual icons at some point!!
        public static string ToIcon(this TileLayer layer)
        {
            return layer switch
            {
                TileLayer.Floor => "🟫",
                TileLayer.Terrain => "🏔️",
                TileLayer.Wall => "🧱",
                TileLayer.Door => "🚪",
                TileLayer.Furniture => "🪑",
                TileLayer.Props => "📦",
                TileLayer.Effects => "✨",
                TileLayer.Roof => "🏠",
                _ => "📍"
            };
        }

        public static int GetDefaultZIndex(this TileLayer layer)
            => (int)layer;
    }
}
