using Microsoft.Extensions.DependencyInjection;
using TileMapBuilder.Core.Services;

namespace TileMapBuilder.App.Services
{
    public sealed class ViewModelFactory : IViewModelFactory
    {
        public readonly IServiceProvider _provider;

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
