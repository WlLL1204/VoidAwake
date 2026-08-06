using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VoidAwake
{
    public class VoidAwake_ErosionLevel4 : GameCondition
    {
        private bool converted;

        public override void Init()
        {
        }

        public override void GameConditionTick()
        {
            Map map = SingleMap;
            if (map == null || !ModsConfig.AnomalyActive || !map.IsPlayerHome) return;
            if (!converted)//最初のTickのみ
            {
                ConvertMapToMetalHell(map); // WholeMapChanged は削除推奨
                ClearWildAnimals(map);
                SpawnMetalhorrors(map, 5);
                converted = true;
                return;
            }


            if (Find.TickManager.TicksGame % 250 != 0) return;
            ClearWildAnimals(map); // 後から湧いた野生も落とす

            // 任意: 数が少ないときだけ追加スポーン
            // if (MetalhorrorCount(map) < 3) SpawnMetalhorrors(map, 1);
        }
        private void TryConvert()
        {
            Map map = SingleMap;
            if (map == null) return;
            ConvertMapToMetalHell(map);
            converted = true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref converted, "converted", false);
        }

        private static void ConvertMapToMetalHell(Map map)
        {
            TerrainDef floor = TerrainDefOf.Voidmetal;
            ThingDef wall = ThingDefOf.VoidmetalWall;

            foreach (IntVec3 c in map.AllCells)
            {
                // 1) 地面
                map.terrainGrid.SetTerrain(c, floor);

                // 2) 植物は消す（床が変わっても残ることがある）
                List<Thing> things = c.GetThingList(map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Thing t = things[i];
                    if (t is Plant)
                    {
                        t.Destroy();
                        continue;
                    }

                    // 3) 自然岩・鉱脈壁 → 虚無金属壁
                    if (t.def.building != null && t.def.building.isNaturalRock)
                    {
                        t.Destroy();
                        GenSpawn.Spawn(wall, c, map);
                    }
                }
            }
           // 見た目更新（任意だが推奨）
            map.mapDrawer.WholeMapChanged(MapMeshFlagDefOf.Terrain);
            map.mapDrawer.WholeMapChanged(MapMeshFlagDefOf.Things);
        }



        //野生動物を消す処理
        private static void ClearWildAnimals(Map map)
        {
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned.ToList())
            {
                if (p.Destroyed || p.Faction != null || !p.IsAnimal) continue;
                if (p.kindDef == PawnKindDefOf.Metalhorror) continue; // 既出は残す
                p.Destroy();
            }
        }

        //メタルホラーが湧く処理
        private static void SpawnMetalhorrors(Map map, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (!RCellFinder.TryFindRandomPawnEntryCell(
                        out IntVec3 cell, map, CellFinder.EdgeRoadChance_Animal))
                    continue;

                Pawn horror = PawnGenerator.GeneratePawn(
                    new PawnGenerationRequest(PawnKindDefOf.Metalhorror, Faction.OfEntities));
                GenSpawn.Spawn(horror, cell, map);
                // FindOrCreateEmergedLord は呼ばない（前回の NRE 原因）
            }
        }
    }
}
