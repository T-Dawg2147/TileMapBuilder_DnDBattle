namespace TileMapBuilder.Core.Services.Interfaces
{
    public interface IImageExportService
    {
        /// <summary>
        /// Exports a visual element to an image file.
        /// The visual parameter is platform-specific (e.g., WPF Visual).
        /// </summary>
        Task ExportAsAsync(object visual, string filePath, double scale = 2.0);
    }
}
