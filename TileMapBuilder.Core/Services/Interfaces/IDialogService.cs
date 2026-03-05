using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TileMapBuilder.Core.Services.Interfaces
{
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

        void ShowInfo(string title, string message, MessageBoxImage image = MessageBoxImage.Information);

        public bool Confirm(string title, string message);
    }
}
