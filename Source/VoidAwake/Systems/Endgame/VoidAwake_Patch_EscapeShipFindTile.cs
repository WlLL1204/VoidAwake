using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace VoidAwake
{
    /// <summary>
    /// Ship to the Stars (EscapeShip) の生成タイルを、
    /// ヴォイド浸食原点（ワールド中心）から遠い外周寄りに寄せる。
    /// </summary>
    [HarmonyPatch(typeof(QuestNode_EndGame_ShipEscape_FindShipTile), "TryFindDestinationTileActual")]
    public static class VoidAwake_Patch_EscapeShipFindTile
    {
        // プレイヤー拠点から最低この距離は離す（すぐ隣に出るのを防ぐ）
        private const float MinDistFromPlayer = 80f;

        // 最遠距離の何割以上を「外周候補」とするか
        private const float PeripheralBandRatio = 0.90f;

        public static bool Prefix(
            PlanetTile rootTile,
            int minDist,
            ref PlanetTile tile,
            ref bool __result)
        {
            if (!TryFindPeripheralShipTile(rootTile, out PlanetTile found))
            {
                // 見つからなければバニラ処理へ
                return true;
            }

            tile = found;
            __result = true;
            Log.Message($"[VoidAwake] EscapeShip peripheral tile = {found}");
            return false; // バニラをスキップ
        }

        private static bool TryFindPeripheralShipTile(PlanetTile playerRoot, out PlanetTile result)
        {
            result = PlanetTile.Invalid;

            PlanetTile origin = GetVoidOriginTile();
            if (!origin.Valid)
                return false;

            WorldGrid grid = Find.WorldGrid;
            float bestDist = -1f;
            List<PlanetTile> band = new List<PlanetTile>();

            for (int i = 0; i < grid.TilesCount; i++)
            {
                PlanetTile t = i;
                if (!IsValidEscapeShipTile(t))
                    continue;

                // プレイヤーから近すぎるタイルは除外
                if (playerRoot.Valid)
                {
                    float fromPlayer = grid.ApproxDistanceInTiles(playerRoot, t);
                    if (fromPlayer < MinDistFromPlayer)
                        continue;
                }

                float dist = grid.ApproxDistanceInTiles(origin, t);

                if (dist > bestDist)
                {
                    bestDist = dist;
                    band.Clear();
                    band.Add(t);
                }
                else if (bestDist > 0f && dist >= bestDist * PeripheralBandRatio)
                {
                    band.Add(t);
                }
            }

            if (band.Count == 0)
                return false;

            return band.TryRandomElement(out result);
        }

        private static PlanetTile GetVoidOriginTile()
        {
            var erosion = Find.World?.GetComponent<VoidAwake_WorldComponent_VoidErosion>();
            if (erosion != null && erosion.originTile.Valid)
                return erosion.originTile;

            // フォールバック: SurfaceViewCenter 最近傍の陸タイル
            WorldGrid grid = Find.WorldGrid;
            Vector3 mapCenter = grid.SurfaceViewCenter;
            PlanetTile best = PlanetTile.Invalid;
            float bestAngle = float.MaxValue;

            for (int i = 0; i < grid.TilesCount; i++)
            {
                PlanetTile tile = i;
                if (grid[tile].WaterCovered)
                    continue;

                float angle = Vector3.Angle(grid.GetTileCenter(tile), mapCenter);
                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    best = tile;
                }
            }

            return best;
        }

        private static bool IsValidEscapeShipTile(PlanetTile tile)
        {
            if (Find.WorldObjects.AnyWorldObjectAt(tile))
                return false;

            if (Find.World.Impassable(tile))
                return false;

            Tile tileData = Find.WorldGrid[tile];
            if (tileData.WaterCovered)
                return false;

            BiomeDef biome = tileData.PrimaryBiome;
            if (biome == null)
                return false;

            if (!biome.canBuildBase)
                return false;

            if (!biome.canAutoChoose)
                return false;

            return true;
        }
    }
}