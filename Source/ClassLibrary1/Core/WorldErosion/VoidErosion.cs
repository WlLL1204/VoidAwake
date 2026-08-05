using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VoidAwake
{
    /// <summary>
    /// 世界浸食の状態管理。
    /// 見た目は WorldDrawLayer_VoidErosion が ErodedTiles を参照して描画する。
    /// </summary>
    public class VoidAwake_VoidErosion : WorldComponent
    {
        public PlanetTile originTile = PlanetTile.Invalid;
        public float radiusInTiles;

        /// <summary>現在浸食されているタイル（描画レイヤが参照する）</summary>
        public HashSet<PlanetTile> ErodedTiles = new HashSet<PlanetTile>();

        private int lastDay = -1;
        private int cachedErodedCount;

        // --- バランス（確認用の大きめ値。本番で戻す） ---
        public const float StartRadius = 1f;
        public const float TilesPerDay = 1.0f;

        private VoidAwake_WorldErosionMarker marker;

        public VoidAwake_VoidErosion(World world) : base(world)
        {
        }

        public int TotalTiles => Find.WorldGrid.TilesCount;

        public int ErodedTileCount => cachedErodedCount;

        /// <summary>0〜1。円内マス数 / ワールド全マス数。</summary>
        public float ErosionRate
        {
            get
            {
                int total = TotalTiles;
                if (total <= 0) return 0f;
                return (float)cachedErodedCount / total;
            }
        }

        public string ErosionRatePercentLabel => ErosionRate.ToStringPercent();

        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);

            if (!originTile.Valid)
            {
                ChooseOriginTile();
            }

            if (radiusInTiles < StartRadius)
            {
                radiusInTiles = StartRadius;
            }

            RecalculateErodedTiles();
            lastDay = GenDate.DaysPassed;
            EnsureMarker();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref originTile, "originTile", PlanetTile.Invalid);
            Scribe_Values.Look(ref radiusInTiles, "radiusInTiles", StartRadius);
            Scribe_Values.Look(ref lastDay, "lastDay", -1);
            Scribe_Values.Look(ref cachedErodedCount, "cachedErodedCount", 0);
            // ErodedTiles はセーブしない（ロード後 FinalizeInit / Recalculate で復元）
            Scribe_References.Look(ref marker, "erosionMarker");
        }

        public override void WorldComponentTick()
        {
            int day = GenDate.DaysPassed;
            if (day == lastDay) return;

            lastDay = day;
            radiusInTiles = StartRadius + day * TilesPerDay;
            RecalculateErodedTiles();
        }

        // ★ 円描画の WorldComponentUpdate は削除（レイヤに任せる）

        private void ChooseOriginTile()
        {
            List<PlanetTile> landTiles = new List<PlanetTile>();
            WorldGrid grid = Find.WorldGrid;

            for (int i = 0; i < grid.TilesCount; i++)
            {
                PlanetTile tile = i;
                if (!grid[tile].WaterCovered)
                {
                    landTiles.Add(tile);
                }
            }

            originTile = landTiles.Count == 0 ? (PlanetTile)0 : landTiles.RandomElement();
            radiusInTiles = StartRadius;
            Log.Message($"[VoidAwake] Void erosion origin tile = {originTile}");
            EnsureMarker();
        }

        /// <summary>浸食タイル集合を作り直し、描画レイヤを dirty にする。</summary>
        private void RecalculateErodedTiles()
        {
            ErodedTiles.Clear();

            if (!originTile.Valid)
            {
                cachedErodedCount = 0;
                NotifyDrawLayerDirty();
                return;
            }

            WorldGrid grid = Find.WorldGrid;
            for (int i = 0; i < grid.TilesCount; i++)
            {
                PlanetTile other = i;
                if (grid.ApproxDistanceInTiles(originTile, other) <= radiusInTiles)
                {
                    ErodedTiles.Add(other);
                }
            }

            cachedErodedCount = ErodedTiles.Count;
            NotifyDrawLayerDirty();
        }

        private void NotifyDrawLayerDirty()
        {
            // 1.6: WorldRenderer.SetDirty<T>(PlanetLayer)
            PlanetLayer layer = originTile.Valid ? originTile.Layer : Find.WorldGrid.Surface;
            if (layer == null || Find.World?.renderer == null) return;

            Find.World.renderer.SetDirty<VoidAwake_VoidTile>(layer);
        }

        public void JumpToOrigin()
        {
            if (!originTile.Valid) return;
            CameraJumper.TryJump(originTile, CameraJumper.MovementMode.Pan);
        }

        private void EnsureMarker()
        {
            if (!originTile.Valid) return;

            if (marker != null && marker.Spawned)
            {
                marker.Tile = originTile;
                return;
            }

            marker = Find.WorldObjects.AllWorldObjects
                .OfType<VoidAwake_WorldErosionMarker>()
                .FirstOrDefault();

            if (marker != null)
            {
                marker.Tile = originTile;
                return;
            }

            WorldObjectDef def = DefDatabase<WorldObjectDef>.GetNamedSilentFail("VoidAwake_ErosionOrigin");
            if (def == null) return;

            marker = (VoidAwake_WorldErosionMarker)WorldObjectMaker.MakeWorldObject(def);
            marker.Tile = originTile;
            Find.WorldObjects.Add(marker);
        }
    }
}