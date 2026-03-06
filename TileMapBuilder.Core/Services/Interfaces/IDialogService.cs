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

        public string? ShowOpenFileDialog(string filter, string title);
        public string? ShowSaveFileDialog(string filter, string title, string? defaultFileName);

        /// <summary>
        /// Opens the "New Tile Map" dialog.
        /// Returns true if the user confirmed, false if they cancelled.
        /// </summary>
        /// <param name="mapName">The name entered by the user.</param>
        /// <param name="width">The width entered by the user.</param>
        /// <param name="height">The height entered by the user.</param>
        bool ShowNewTileMapDialog(out string mapName, out int? width, out int? height);

        void ShowInfo(string title, string message, DialogIcon icon = DialogIcon.Information);

        public bool ShowConfirm(string title, string message);
    }
}
