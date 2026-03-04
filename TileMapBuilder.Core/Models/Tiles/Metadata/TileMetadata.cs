using CommunityToolkit.Mvvm.ComponentModel;

namespace TileMapBuilder.Core.Models.Tiles.Metadata
{
    public abstract partial class TileMetadata : ObservableObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public abstract TileMetadataType Type { get; }

        public string Name { get; set; } = string.Empty;

        public bool IsVisibleToPlayer { get; set; } = false;
        public bool IsTriggered { get; set; } = false;
        public bool IsEnabled { get; set; } = true;

        public string Notes { get; set; } = string.Empty;
    }

    [Flags]
    public enum TileMetadataType
    {
        None = 0,
        Trap = 1 << 0,
        Hazard = 1 << 1,
        Secret = 1 << 2,
        Interactive = 1 << 3,
        Trigger = 1 << 4,
        Spawn = 1 << 5,
        Teleporter = 1 << 6,
        Healing = 1 << 7,
        Aura = 1 << 8,
        Lore = 1 << 9,
        Custom = 1 << 10
    }

    public static class TileMetadataTypeExtension
    {
        public static string ToString(this TileMetadataType type)
        {
            return type switch
            {
                TileMetadataType.Trap => "Trap",
                TileMetadataType.Hazard => "Environmental Hazard",
                TileMetadataType.Secret => "Secret",
                TileMetadataType.Interactive => "Interactive Object",
                TileMetadataType.Spawn => "Spawn Point",
                TileMetadataType.Teleporter => "Teleporter",
                TileMetadataType.Healing => "Healing Zone",
                TileMetadataType.Aura => "Aura Effect",
                TileMetadataType.Lore => "Lore Point",
                TileMetadataType.Custom => "Custom",
                _ => "Unknown"
            };
        }

        // TODO Would like to replace with actual images eventually if possible.
        public static string ToIcon(this TileMetadataType type)
        {
            return type switch
            {
                TileMetadataType.Trap => "⚠️",
                TileMetadataType.Hazard => "☠️",
                TileMetadataType.Secret => "🔍",
                TileMetadataType.Interactive => "⚙️",
                TileMetadataType.Trigger => "⚡",
                TileMetadataType.Spawn => "👹",
                TileMetadataType.Teleporter => "🌀",
                TileMetadataType.Healing => "💚",
                TileMetadataType.Aura => "✨",
                TileMetadataType.Lore => "📜",
                TileMetadataType.Custom => "🔧",
                _ => "❓"
            };
        }
    }

    public sealed class TileMetadataDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty; // TileMetadataType parsed as sting
        public string Name { get; set; } = string.Empty;
        public bool IsVisibleToPlayers { get; set; }
        public bool IsTriggered { get; set; }
        public string Notes { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }

        public string Data { get; set; } = string.Empty; // Polymorphic data stored as JSON string
    }
}
