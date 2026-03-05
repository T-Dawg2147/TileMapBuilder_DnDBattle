using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public Visual? GetMapVisual()
        {
            return _visualFactory?.Invoke();
        }
    }
}
