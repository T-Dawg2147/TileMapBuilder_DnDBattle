using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TileMapBuilder.Core.Models.Tiles
{
    public sealed partial class TileDefinition : ObservableObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ImagePath { get; set; } = string.Empty;

        [ObservableProperty] private string _displayName = string.Empty;

        public string Category { get; set; } = "General";
        public string Description { get; set; } = string.Empty;
        public int ZIndex { get; set; } = 0;
        
        public bool BlocksMovement { get; set; } = false;
        public bool BlocksSight { get; set;} = false;
        public bool BlocksLight { get; set; } = false;
        public bool IsEnabled { get; set; } = true;

        public Color? TintColor { get; set; } = null;
        public TileLayer Layer { get; set; } = TileLayer.Floor;

        public override string ToString() => DisplayName ?? ImagePath ?? "Unamed Tile";        
    }
}
