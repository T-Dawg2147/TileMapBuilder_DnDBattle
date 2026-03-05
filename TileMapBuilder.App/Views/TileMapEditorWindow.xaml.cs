using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TileMapBuilder.App.Views
{
    /// <summary>
    /// Interaction logic for TileMapEditorWindow.xaml
    /// </summary>
    public partial class TileMapEditorWindow : Window
    {
        public TileMapEditorWindow()
        {
            InitializeComponent();
        }

        // This is only here because i have NO idea how to pass a canvas to a ViewModel
        private void ExportAs_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog()
            {
                Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg",
                Title = "Export Map as Image",
                FileName = CurrentMap.Name + ".png"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var canvas = EditorControl.MapCanvas;

                    double scale = 2.0;
                    int width = (int)(canvas.ActualWidth * scale);
                    int height = (int)(canvas.ActualHeight * scale);

                    var renderBitmap = new RenderTargetBitmap(
                        width,
                        height,
                        96 * scale,
                        96 * scale,
                        PixelFormats.Pbgra32);

                    var visual = new DrawingVisual();
                    using (var context = visual.RenderOpen())
                    {
                        var brush = new VisualBrush(canvas);
                        context.PushTransform(new ScaleTransform(scale, scale));
                        context.DrawRectangle(brush, null, new Rect(0, 0, canvas.ActualWidth, canvas.ActualHeight));
                    }

                    renderBitmap.Render(visual);

                    BitmapEncoder encoder;
                    if (dialog.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                    {
                        encoder = new JpegBitmapEncoder { QualityLevel = 95 };
                    }
                    else
                    {
                        encoder = new PngBitmapEncoder();
                    }

                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                    using (var stream = File.Create(dialog.FileName))
                    {
                        encoder.Save(stream);
                    }

                    MessageBox.Show(
                        $"Map exported successfully!\n\nResolution: {width}x{height}\nFile: {dialog.FileName}",
                        "Export Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to export image:\n\n{ex.Message}",
                        "Export Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // Errm, should i do something?
        }
    }
}
