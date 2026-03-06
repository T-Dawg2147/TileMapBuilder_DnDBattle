using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Data.Models.Tiles.Metadata
{
    public sealed class SpawnMetadata : TileMetadata
    {
        public override TileMetadataType Type => TileMetadataType.Spawn;

        public string CreatureTemplateId { get; set; } = string.Empty;

        public string CreatureName { get; set; } = string.Empty;

        public int SpawnCount { get; set; } = 1;

        public int SpawnRadius { get; set; } = 0;

        public SpawnTrigger TriggerCondition { get; set; } = SpawnTrigger.Manual;

        public bool SpawnOnMapLoad { get; set; } = false;

        public int SpawnOnRound { get; set; } = 1;

        public int TriggerDistance { get; set; } = 3;

        public bool HasSpawned { get; set; } = false;

        public bool IsReusable { get; set; } = false;

        public int SpawnDelay { get; set; } = 0;
    }

    public enum SpawnTrigger
    {
        Manual,
        CombatStart,
        RoundNumber,
        Proximity,
        Event
    }
}
