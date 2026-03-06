using System;

namespace DnDBattle.Data.Models.Tiles
{
    public enum NoteCategory
    {
        General,
        Trap,
        Treasure,
        NPC,
        Quest,
        Lore,
        Secret,
        Other
    }

    public class MapNote
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public int GridX { get; set; }
        public int GridY { get; set; }

        /// <summary>Notes text body.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>Is note visible to players.</summary>
        public bool IsPlayerVisible { get; set; }

        /// <summary>Notes category type.</summary>
        public NoteCategory Category { get; set; } = NoteCategory.General;

        public string BackgroundColor { get; set; } = "#FFFFFFF00";
        public double FontSize { get; set; } = 12;
        public bool IsBold { get; set; }        
    }
}
