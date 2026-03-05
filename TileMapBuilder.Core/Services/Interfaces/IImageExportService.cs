using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TileMapBuilder.Core.Services.Interfaces
{
    public interface IImageExportService
    {
        Task ExportAsAsync(Visual visual, string filePath, double scale = 2.0);
    }
}
