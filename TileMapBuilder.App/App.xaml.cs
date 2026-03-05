using DnDBattle.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using TileMapBuilder.App.Services;
using TileMapBuilder.App.Views;
using TileMapBuilder.Core.Services.Interfaces;
using TileMapBuilder.Core.Services.TileService;
using TileMapBuilder.Core.ViewModels;
using TileMapBuilder.Core.ViewModels.Controls;
using TileMapBuilder.Core.ViewModels.Dialogs;
using TileMapBuilder.Core.ViewModels.TileViewModels;

namespace TileMapBuilder.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Services
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ITileLibraryService, TileLibraryService>();
            services.AddSingleton<ITileMapService, TileMapService>();
            services.AddSingleton<IDialogService, DialogService>();

            services.AddSingleton<MapVisualProviderHolder>();
            services.AddSingleton<IMapVisualProvider>(sp => sp.GetRequiredService<MapVisualProviderHolder>());

            // ViewModels
            services.AddTransient<ShellViewModel>();
            services.AddTransient<TileMapEditorViewModel>();
            services.AddTransient<TileMapControlViewModel>();
            services.AddTransient<NewTileMapViewModel>();
            services.AddTransient<TilePaletteViewModel>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var shell = _serviceProvider.GetRequiredService<ShellViewModel>();

            var mainWindow = new MainWindow()
            {
                DataContext = shell
            };

            mainWindow.Show();
        }
    }

}
