namespace DnDBattle.Data.Enums
{
    [Flags]
    public enum DamageType
    {
        None = 0,
        Bludgeoning = 1 << 0,
        Piercing = 1 << 1,
        Slashing = 1 << 2,
        Fire = 1 << 3,
        Cold = 1 << 4,
        Lightning = 1 << 5,
        Thunder = 1 << 6,
        Acid = 1 << 7,
        Poison = 1 << 8,
        Necrotic = 1 << 9,
        Radiant = 1 << 10,
        Force = 1 << 11,
        Psychic = 1 << 12,

        Physical = Bludgeoning | Piercing | Slashing
    }

    public static class DamageTypeExtensions
    {
        public static string GetDisplayName(this DamageType type)
            => type switch
            {
                DamageType.Bludgeoning => "Bludgeoning",
                DamageType.Piercing => "Piercing",
                DamageType.Slashing => "Slashing",
                DamageType.Fire => "Fire",
                DamageType.Cold => "Cold",
                DamageType.Lightning => "Lightning",
                DamageType.Thunder => "Thunder",
                DamageType.Acid => "Acid",
                DamageType.Poison => "Poison",
                DamageType.Necrotic => "Necrotic",
                DamageType.Radiant => "Radiant",
                DamageType.Force => "Force",
                DamageType.Psychic => "Psychic",
                _ => type.ToString()
            };

        public static string GetIcon(this DamageType type)
            => type switch
            {
                DamageType.Bludgeoning => "🔨",
                DamageType.Piercing => "🗡️",
                DamageType.Slashing => "⚔️",
                DamageType.Fire => "🔥",
                DamageType.Cold => "❄️",
                DamageType.Lightning => "⚡",
                DamageType.Thunder => "💥",
                DamageType.Acid => "🧪",
                DamageType.Poison => "☠️",
                DamageType.Necrotic => "💀",
                DamageType.Radiant => "✨",
                DamageType.Force => "💫",
                DamageType.Psychic => "🧠",
                _ => "⚪"
            };


        /// <summary>
        /// Returns the color for a damage type as an (R, G, B) tuple.
        /// UI layers can convert this to their platform-specific color type.
        /// </summary>
        public static (byte R, byte G, byte B) GetColor(this DamageType type)
            => type switch
            {
                DamageType.Bludgeoning => (139, 119, 101),
                DamageType.Piercing => (192, 192, 192),
                DamageType.Slashing => (169, 169, 169),
                DamageType.Fire => (255, 87, 34),
                DamageType.Cold => (79, 195, 247),
                DamageType.Lightning => (255, 235, 59),
                DamageType.Thunder => (171, 71, 188),
                DamageType.Acid => (76, 175, 80),
                DamageType.Poison => (156, 39, 176),
                DamageType.Necrotic => (66, 66, 66),
                DamageType.Radiant => (255, 241, 118),
                DamageType.Force => (100, 181, 246),
                DamageType.Psychic => (236, 64, 122),
                _ => (158, 158, 158)
            };

        public static DamageType ParseFromString(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return DamageType.None;

            text = text.ToLower().Trim();

            DamageType result = DamageType.None;

            if (text.Contains("bludgeoning")) result |= DamageType.Bludgeoning;
            if (text.Contains("piercing")) result |= DamageType.Piercing;
            if (text.Contains("slashing")) result |= DamageType.Slashing;
            if (text.Contains("fire")) result |= DamageType.Fire;
            if (text.Contains("cold")) result |= DamageType.Cold;
            if (text.Contains("lightning")) result |= DamageType.Lightning;
            if (text.Contains("thunder")) result |= DamageType.Thunder;
            if (text.Contains("acid")) result |= DamageType.Acid;
            if (text.Contains("poison")) result |= DamageType.Poison;
            if (text.Contains("necrotic")) result |= DamageType.Necrotic;
            if (text.Contains("radiant")) result |= DamageType.Radiant;
            if (text.Contains("force")) result |= DamageType.Force;
            if (text.Contains("psychic")) result |= DamageType.Psychic;

            return result;
        }

        public static IEnumerable<DamageType> GetIndividualTypes(this DamageType types)
        {
            foreach (DamageType type in Enum.GetValues(typeof(DamageType)))
            {
                if (type != DamageType.None && type != DamageType.Physical && types.HasFlag(type))
                {
                    yield return type;
                }
            }
        }

        public static List<DamageType> GetAllDamageTypes()
            => new List<DamageType>
            {
                DamageType.Bludgeoning,
                DamageType.Piercing,
                DamageType.Slashing,
                DamageType.Fire,
                DamageType.Cold,
                DamageType.Lightning,
                DamageType.Thunder,
                DamageType.Acid,
                DamageType.Poison,
                DamageType.Necrotic,
                DamageType.Radiant,
                DamageType.Force,
                DamageType.Psychic
            };
    }
}
