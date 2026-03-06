namespace DnDBattle.Data.Services.Interfaces
{
    /// <summary>
    /// Abstraction for loading and caching tile images.
    /// The implementation is platform-specific (e.g., WPF uses BitmapImage).
    /// </summary>
    public interface ITileImageCacheService
    {
        /// <summary>
        /// Gets a cached image or loads it from disk. Returns an opaque image object
        /// whose concrete type depends on the UI platform (e.g., BitmapImage for WPF).
        /// </summary>
        object? GetOrLoadImage(string imagePath);

        /// <summary>
        /// Preloads a set of images into the cache.
        /// </summary>
        void PreloadImages(IEnumerable<string> imagePaths);

        /// <summary>
        /// Clears the entire image cache.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Removes a single image from the cache.
        /// </summary>
        void RemoveFromCache(string imagePath);
    }
}
