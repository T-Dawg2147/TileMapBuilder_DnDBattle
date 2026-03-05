using DnDBattle.Data.Services;
using Microsoft.Win32;
using System.Windows;
using TileMapBuilder.App.Views.Dialogs;
using TileMapBuilder.Core.Services.Interfaces;
using TileMapBuilder.Core.ViewModels.Dialogs;

namespace TileMapBuilder.App.Services
{
    public class DialogService : IDialogService
    {
        private readonly IViewModelFactory _factory;

        public DialogService(IViewModelFactory factory)
        {
            _factory = factory;
        }

        public bool ShowFolderDialog(out string path)
            => NativeFolderBrowser.ShowDialog(out path);

        public string? ShowOpenFileDialog(string filter, string title)
        {
            var dialog = new OpenFileDialog()
            {
                Filter = filter,
                Title = title
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? ShowSaveFileDialog(string filter, string title, string? defaultFileName = null)
        {
            var dialog = new SaveFileDialog()
            {
                Filter = filter,
                Title = title,
                FileName = defaultFileName ?? string.Empty
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public bool ShowNewTileMapDialog(
            out string mapName, out int? width, out int? height)
        {
            var viewModel = new NewTileMapViewModel(this, showDimensions: true, dialogTitle: "Create New Tile Map");

            var dialog = new NewTileMapDialog()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            var result = dialog.ShowDialog() == true;

            mapName = result ? viewModel.Name : "New Map";
            width = result ? viewModel.Width : 50;
            height = result ? viewModel.Height : 50;

            return result;
        }

        public void ShowInfo(string title, string message, MessageBoxImage image = MessageBoxImage.Information)
            => MessageBox.Show(message, title, MessageBoxButton.OK, image);

        public bool ShowConfirm(string title, string message)
            => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }
}
