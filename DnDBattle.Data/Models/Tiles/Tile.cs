using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using DnDBattle.Data.Models.Tiles.Metadata;

namespace DnDBattle.Data.Models.Tiles
{
    public partial class Tile : ObservableObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? TileDefinitionId { get; set; }

        [ObservableProperty] private int _gridX;
        [ObservableProperty] private int _gridY;

        public int Rotation { get; set; } = 0;
        public bool FlipHorizontal { get; set; } = false; // TODO is this really nesicarry? im sure i can break it down to one property that takes care of this?
        public bool FlipVertical { get; set; } = false; // TODO is this really nesicarry? im sure i can break it down to one property that takes care of this?
        public int? ZIndex { get; set; } = null;
        public string Notes { get; set; } = string.Empty; // This should probably also use map notes?

        public ObservableCollection<TileMetadata> Metadata { get; set; } = [];

        public bool HasMetadata => Metadata != null && Metadata.Count > 0;

        public bool HasMetadataType(TileMetadataType type)
        {
            foreach (var meta in Metadata)
            {
                if (meta.Type == type)
                    return true;
            }
            return false;
        }

        public List<TileMetadata> GetMetadata(TileMetadataType type)
        {
            var result = new List<TileMetadata>();
            foreach (var meta in Metadata)
            {
                if (meta.Type == type)
                    result.Add(meta);
            }
            return result;
        }

        public int GetEffectiveZIndex(TileDefinition tileDef)
        {
            if (ZIndex.HasValue)
                return ZIndex.Value;
            return tileDef?.Layer.GetDefaultZIndex() ?? 0;
        }
    }

    public sealed class TileDto
    {
        public Guid Id { get; set; }
        public string TileDefinitionId { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public int GridX { get; set; }
        public int GridY { get; set; }
        public int Rotation { get; set; }
        public bool FlipHorizontal { get; set; }
        public bool FlipVertical { get; set; }
        public int? ZIndex { get; set; }
        public string Notes { get; set; }

        public List<TileMetadataDto> Metadata { get; set; } = [];
    }
}
