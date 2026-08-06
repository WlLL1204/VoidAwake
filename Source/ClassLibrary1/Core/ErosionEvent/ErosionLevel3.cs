using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VoidAwake
{
    public class VoidAwake_ErosionLevel3 : MapComponent
    {
        private bool didInitialConvert;

        public VoidAwake_ErosionLevel3(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            if (!ModsConfig.AnomalyActive) return;
            if (!map.IsPlayerHome) return;

            var erosion = Find.World.GetComponent<VoidAwake_VoidErosion>();
            if (erosion == null) return;
            if (erosion.GetErosionLevel(map.Tile) < VoidAwake_VoidErosion.VoidErosionLevel.Heavy)
            {
                didInitialConvert = false;
                return;
            }

            // 250 tick ごと程度で十分
            if (Find.TickManager.TicksGame % 250 != 0) return;

            ConvertWildAnimals();
            didInitialConvert = true;
        }

        private void ConvertWildAnimals()
        {
            List<Pawn> list = map.mapPawns.AllPawnsSpawned.ToList();
            foreach (Pawn p in list)
            {
                if (p.Destroyed || p.Faction != null || !p.IsAnimal) continue;
                if (p.kindDef == PawnKindDefOf.Chimera) continue;
                ReplaceWithChimera(p);
            }
        }

        private static void ReplaceWithChimera(Pawn animal)
        {
            IntVec3 pos = animal.PositionHeld;
            Map m = animal.MapHeld;
            if (m == null) return;

            // 演出は任意（MeatExplosion など）
            if (animal.Corpse != null) animal.Corpse.Destroy();
            else animal.Destroy();

            Pawn chimera = PawnGenerator.GeneratePawn(
                new PawnGenerationRequest(PawnKindDefOf.Chimera, Faction.OfEntities));
            GenSpawn.Spawn(chimera, pos, m);
        }
    }
}
