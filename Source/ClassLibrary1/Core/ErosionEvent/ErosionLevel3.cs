using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VoidAwake
{
    public class VoidAwake_ErosionLevel3 : GameCondition
    {
        private bool didInitialConvert;

        public override void GameConditionTick()
        {
            Map map = SingleMap;
            if (map == null) return;

            if (!ModsConfig.AnomalyActive) return;
            if (!map.IsPlayerHome) return;


            if (Find.TickManager.TicksGame % 250 != 0) return;

            ConvertWildAnimals(map);
            didInitialConvert = true;
        }

        private void ConvertWildAnimals(Map map)
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

            if (animal.Corpse != null) animal.Corpse.Destroy();
            else animal.Destroy();

            Pawn chimera = PawnGenerator.GeneratePawn(
                new PawnGenerationRequest(PawnKindDefOf.Chimera, Faction.OfEntities));
            GenSpawn.Spawn(chimera, pos, m);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref didInitialConvert, "didInitialConvert", false);
        }
    }
}