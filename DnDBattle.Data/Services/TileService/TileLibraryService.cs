using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using DnDBattle.Data.Models.Tiles;
using DnDBattle.Data.Services.Interfaces;

namespace DnDBattle.Data.Services.TileService
{
    public sealed class TileLibraryService : ITileLibraryService
    {
        private static readonly string _supportedFileTypes = "*.png;*.jpg;*.jpeg;*.bmp";

        private readonly ITileImageCacheService? _imageCache;

        public ObservableCollection<TileDefinition> AvailableTiles { get; private set; }

        private readonly string _tileDirectory;

        public TileLibraryService() : this(null) { }

        public TileLibraryService(ITileImageCacheService? imageCache = null)
        {
            _imageCache = imageCache;
            AvailableTiles = new ObservableCollection<TileDefinition>();
            _tileDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Tiles");

            Directory.CreateDirectory(_tileDirectory);
        }

        public void RefreshLibrary()
            => LoadTileLibrary();

        public void LoadTileLibrary()
        {
            AvailableTiles.Clear();

            if (!Directory.Exists(_tileDirectory)) return; // SWALLOW

            var imageFiles = Directory.GetFiles(_tileDirectory, _supportedFileTypes, SearchOption.AllDirectories);

            Debug.WriteLine($"[TileLibrary] Found {imageFiles.Length} tile images");

            foreach (var filePath in imageFiles)
            {
                try
                {
                    var relativePath = GetRelativePath(filePath);
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var category = GetCategoryFromPath(filePath);

                    var tileDef = new TileDefinition()
                    {
                        Id = GenerateDeterministicId(relativePath),
                        ImagePath = relativePath,
                        DisplayName = FormatDisplayName(fileName),
                        Category = category,
                        IsEnabled = true
                    };

                    AvailableTiles.Add(tileDef);

                    _imageCache?.GetOrLoadImage(relativePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TileLibrary] Error loading tile {filePath}: {ex.Message}");
                }
                Debug.WriteLine($"[TileLibrary] Loaded {AvailableTiles.Count} tiles into library");
            }
        }

        #region Helpers

        public Dictionary<string, List<TileDefinition>> GetTilesByCategory()
            => AvailableTiles
                .GroupBy(t => t.Category ?? "General")
                .ToDictionary(g => g.Key, g => g.ToList());

        public TileDefinition? GetTileById(string id)
            => AvailableTiles.FirstOrDefault(t => t.Id == id);

        public TileDefinition? GetTilesByImagePath(string imagePath) // This is more of a fallback if (for whatever reason) Id lookup fails
        {
            if (string.IsNullOrEmpty(imagePath)) return null;
            return AvailableTiles.FirstOrDefault(t =>
                string.Equals(t.ImagePath, imagePath, StringComparison.OrdinalIgnoreCase));
        }

        #region String Helpers

        private static string GenerateDeterministicId(string relativePath)
        {
            var normalized = relativePath.Replace('\\', '/').ToLowerInvariant();
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));

            // Uses first 16 bytes to create GUID-like string
            var guid = new Guid(hash.Take(16).ToArray());
            return guid.ToString();
        }

        private string GetRelativePath(string absolutePath)
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            if (absolutePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                return absolutePath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar);
            return absolutePath;
        }

        private string GetCategoryFromPath(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (dir == null) return "General";

            var parts = dir.Split(Path.DirectorySeparatorChar);

            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i].Equals("Tiles", StringComparison.OrdinalIgnoreCase))
                    return parts[i + 1];
            }
            return "General";
        }

        private string FormatDisplayName(string fileName)
            => fileName
            .Replace("-", " ")
            .Trim();

        #endregion

        #endregion
    }
}
