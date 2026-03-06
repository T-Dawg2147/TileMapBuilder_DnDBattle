using DnDBattle.Data.Services;
using DnDBattle.Data.Services.Interfaces;
using DnDBattle.Data.Services.TileService;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using TileMapBuilder.App.Services;
using TileMapBuilder.App.Views;
using TileMapBuilder.Core.Services.Interfaces;
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

        public static IServiceProvider Services { get; private set; } = null!;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            Services = _serviceProvider;
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Services - UI-facing (Core interfaces, App implementations)
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IImageExportService, ImageExportService>();

            // Services - Data (Data interfaces, Data implementations)
            services.AddSingleton<ITileImageCacheService>(sp => TileImageCacheService.Instance);
            services.AddSingleton<ITileLibraryService>(sp =>
                new TileLibraryService(sp.GetRequiredService<ITileImageCacheService>()));
            services.AddSingleton<ITileMapService, TileMapService>();

            // Map visual provider (App-specific)
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
