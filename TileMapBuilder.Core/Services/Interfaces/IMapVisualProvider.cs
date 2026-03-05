namespace TileMapBuilder.Core.Services.Interfaces
{
    public interface IMapVisualProvider
    {
        /// <summary>
        /// Gets the map visual element for export.
        /// Returns a platform-specific visual (e.g., WPF Visual).
        /// </summary>
        object? GetMapVisual();
    }
}
