namespace TileMapBuilder.Core.Services.Interfaces
{
    /// <summary>
    /// Platform-agnostic icon type for dialog messages.
    /// Maps to MessageBoxImage in WPF implementations.
    /// </summary>
    public enum DialogIcon
    {
        Information,
        Warning,
        Error,
        Question
    }

    public interface IDialogService
    {
        public bool ShowFolderDialog(out string path);

        string? ShowOpenFileDialog(string filter, string title);
        string? ShowSaveFileDialog(string filter, string title, string? defaultFileName);

        /// <summary>
        /// Opens the "New Tile Map" dialog.
        /// Returns true if the user confirmed, false if they cancelled.
        /// </summary>
        bool ShowNewTileMapDialog(out string mapName, out int? width, out int? height);

        void ShowInfo(string title, string message, DialogIcon icon = DialogIcon.Information);

        bool ShowConfirm(string title, string message);

        bool ShowImportTileDialog(out string[] filePaths, out string category);

        bool ShowMapPropertiesDialog(
            DnDBattle.Data.Models.Tiles.TileMap currentMap,
            out string name, out string description, out string author,
            out string environmentType, out double cellSize, out int feetPerSqare,
            out string backgroundColor);
    }
}
