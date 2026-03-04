using System.Globalization;
using System.Windows.Data;
using TileMapBuilder.Core.Services.TileService;

namespace TileMapBuilder.Core.Converters
{
    public sealed class TileImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string imagePath)
                return TileImageCacheService.Instance.GetOrLoadImage(imagePath);
            return null;
        }

        public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
