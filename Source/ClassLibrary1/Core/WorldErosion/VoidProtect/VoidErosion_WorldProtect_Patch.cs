using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace VoidAwake
{
    /// <summary>
    /// グラブシップ通過タイル = 浄化された道。浸食から保護。
    /// </summary>
    public class VoidAwake_VoidProtect_Path : WorldComponent
    {
        public HashSet<PlanetTile> PurifiedTiles = new HashSet<PlanetTile>();

        // 同じ飛行を二重登録しない
        private readonly HashSet<int> processedGravshipIds = new HashSet<int>();

        public VoidAwake_VoidProtect_Path(World world) : base(world) { }

        public bool IsPurified(PlanetTile tile) =>
            tile.Valid && PurifiedTiles.Contains(tile);

        public override void ExposeData()
        {
            base.ExposeData();
            // PlanetTile のリスト保存（環境に合わせて調整）
            List<PlanetTile> list = null;
            if (Scribe.mode == LoadSaveMode.Saving)
                list = new List<PlanetTile>(PurifiedTiles);

            Scribe_Collections.Look(ref list, "purifiedTiles", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                PurifiedTiles.Clear();
                if (list != null)
                    foreach (var t in list) PurifiedTiles.Add(t);
            }
        }

        public override void WorldComponentTick()
        {
            if (!ModsConfig.OdysseyActive) return;

            // 飛行開始直後の Gravship を拾う（Harmony 不要）
            foreach (WorldObject wo in Find.WorldObjects.AllWorldObjects)
            {
                if (!(wo is Gravship ship)) continue;
                if (!processedGravshipIds.Add(ship.ID)) continue;

                RegisterFlight(ship.Tile, ship.destinationTile);
            }
        }

        //ここで移動経路上のマスを取得している
        public const int ProtectRadius = 0; // ここを変えると太さが変わる、5とかにするとAwabi作れる

        public void RegisterFlight(PlanetTile from, PlanetTile to)
        {
            if (!from.Valid || !to.Valid) return;

            bool added = false;
            WorldGrid grid = Find.WorldGrid;
            var neighbors = new List<PlanetTile>();
            var queue = new Queue<(PlanetTile tile, int dist)>();
            var seen = new HashSet<PlanetTile>();

            // 軌跡上のマスを種にする
            foreach (PlanetTile t in TilesAlongFlight(from, to))
            {
                if (seen.Add(t))
                    queue.Enqueue((t, 0));
            }

            while (queue.Count > 0)
            {
                (PlanetTile tile, int dist) = queue.Dequeue();
                if (PurifiedTiles.Add(tile))
                    added = true;

                if (dist >= ProtectRadius)
                    continue;

                neighbors.Clear();
                grid.GetTileNeighbors(tile, neighbors);
                foreach (PlanetTile n in neighbors)
                {
                    if (seen.Add(n))
                        queue.Enqueue((n, dist + 1));
                }
            }

            if (added)
                NotifyDrawLayersDirty(from);
        }


        private void NotifyDrawLayersDirty(PlanetTile hint)
        {
            PlanetLayer layer = hint.Valid ? hint.Layer : Find.WorldGrid.Surface;
            if (layer == null || Find.World?.renderer == null) return;

            Find.World.renderer.SetDirty<VoidAwake_WorldProtect>(layer);
        }

        // 前回案内した隣接greedy経路
        public static List<PlanetTile> TilesAlongFlight(PlanetTile from, PlanetTile to)
        {
            var path = new List<PlanetTile> { from };
            if (from == to) return path;

            if (from.Layer != to.Layer)
                from = to.Layer.GetClosestTile_NewTemp(from);

            WorldGrid grid = Find.WorldGrid;
            var neighbors = new List<PlanetTile>();
            PlanetTile cur = from;
            Vector3 end = grid.GetTileCenter(to).normalized;
            int guard = grid.TraversalDistanceBetween(from, to) * 3 + 16;

            while (cur != to && guard-- > 0)
            {
                neighbors.Clear();
                grid.GetTileNeighbors(cur, neighbors);

                PlanetTile best = PlanetTile.Invalid;
                float bestDot = float.MinValue;
                foreach (PlanetTile n in neighbors)
                {
                    float dot = Vector3.Dot(grid.GetTileCenter(n).normalized, end);
                    if (dot > bestDot) { bestDot = dot; best = n; }
                }
                if (!best.Valid || best == cur) break;
                cur = best;
                path.Add(cur);
            }
            if (path[path.Count - 1] != to) path.Add(to);
            return path;
        }
    }
}