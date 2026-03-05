using System.Windows.Media;
using TileMapBuilder.Core.Services.Interfaces;

namespace TileMapBuilder.App.Services
{
    public sealed class MapVisualProviderHolder : IMapVisualProvider
    {
        private Func<Visual?>? _visualFactory;

        public void SetVisualFactory(Func<Visual?>? visualFactory)
        {
            _visualFactory = visualFactory;
        }

        public object? GetMapVisual()
        {
            return _visualFactory?.Invoke();
        }
    }
}
