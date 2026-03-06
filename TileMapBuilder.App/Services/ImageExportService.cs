using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TileMapBuilder.Core.Services.Interfaces;

namespace TileMapBuilder.App.Services
{
    public sealed class ImageExportService : IImageExportService
    {
        public async Task ExportAsAsync(object visual, string filePath, double scale = 2.0)
        {
            if (visual is not Visual wpfVisual)
                throw new InvalidOperationException("Export visual must be a WPF Visual.");

            if (wpfVisual is FrameworkElement fe)
            {
                fe.UpdateLayout();
            }

            if (wpfVisual is not FrameworkElement element)
                throw new InvalidOperationException("Export visual must be a FrameworkElement.");

            int width = (int)Math.Ceiling(element.ActualWidth * scale);
            int height = (int)Math.Ceiling(element.ActualHeight * scale);

            if (width <= 0 || height <= 0)
                throw new InvalidOperationException($"Nothing to export: element size is {element.ActualWidth}x{element.ActualHeight}.");

            var renderBitmap = new RenderTargetBitmap(
                width,
                height,
                96 * scale,
                96 * scale,
                PixelFormats.Pbgra32);

            var dv = new DrawingVisual();
            using (var ctx = dv.RenderOpen())
            {
                var brush = new VisualBrush(element);
                ctx.PushTransform(new ScaleTransform(scale, scale));
                ctx.DrawRectangle(brush, null, new Rect(0, 0, element.ActualWidth, element.ActualHeight));
            }

            renderBitmap.Render(dv);

            BitmapEncoder encoder =
                filePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                filePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                    ? new JpegBitmapEncoder { QualityLevel = 95 }
                    : new PngBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            await using var stream = File.Create(filePath);
            encoder.Save(stream);
        }
    }
}
