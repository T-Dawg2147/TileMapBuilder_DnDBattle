using DnDBattle.Data.Models.Tiles;
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

        public void ShowInfo(string title, string message, DialogIcon icon = DialogIcon.Information)
        {
            var wpfIcon = icon switch
            {
                DialogIcon.Warning => MessageBoxImage.Warning,
                DialogIcon.Error => MessageBoxImage.Error,
                DialogIcon.Question => MessageBoxImage.Question,
                _ => MessageBoxImage.Information
            };
            MessageBox.Show(message, title, MessageBoxButton.OK, wpfIcon);
        }

        public bool ShowConfirm(string title, string message)
            => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

        public bool ShowImportTileDialog(out string[] filePaths, out string category)
        {
            var viewModel = new ImportTileViewModel(this);

            var dialog = new ImportTileDialog()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            var result = dialog.ShowDialog() == true;

            filePaths = result ? viewModel.SelectedFilePaths.ToArray() : [];
            category = result ? viewModel.SelectedCategory : string.Empty;

            return result;
        }

        public bool ShowMapPropertiesDialog(
            TileMap currentMap,
            out string name, out string description, out string author,
            out string environmentType, out double cellSize, out int feetPerSquare,
            out string backgroundColor)
        {
            var viewModel = new MapPropertiesViewModel(currentMap);

            var dialog = new MapPropertiesDialog()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            var result = dialog.ShowDialog() == true;

            name = viewModel.MapName;
            description = viewModel.Description;
            author = viewModel.Author;
            environmentType = viewModel.EnvironmentType;
            cellSize = viewModel.CellSize;
            feetPerSquare = viewModel.FeetPerSquare;
            backgroundColor = viewModel.BackgroundColor;

            return result;
        }
    }
}
