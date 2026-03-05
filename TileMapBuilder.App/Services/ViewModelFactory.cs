using Microsoft.Extensions.DependencyInjection;
using TileMapBuilder.Core.Services.Interfaces;

namespace TileMapBuilder.App.Services
{
    public sealed class ViewModelFactory : IViewModelFactory
    {
        private readonly IServiceProvider _provider;

        public ViewModelFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public T Create<T>() where T : class
        {
            return _provider.GetRequiredService<T>();
        }
    }
}
