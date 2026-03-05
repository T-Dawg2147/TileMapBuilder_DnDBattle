using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TileMapBuilder.Core.Services.Interfaces
{
    public interface IMapVisualProvider
    {
        Visual? GetMapVisual();
    }
}
