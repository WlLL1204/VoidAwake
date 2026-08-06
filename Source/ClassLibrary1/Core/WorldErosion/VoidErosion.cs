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
        public const float StartRadius = 1f;//開始時の浸食範囲
        public const float TilesPerDay = 1.0f;//浸食の進行速度

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
            //タイルの保護パッチ
            var purified = Find.World.GetComponent<VoidAwake_VoidProtect_Path>();
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
            ApplyVoidSettlementDestruction();
        }

        private void NotifyDrawLayerDirty()
        {
            // 1.6: WorldRenderer.SetDirty<T>(PlanetLayer)
            PlanetLayer layer = originTile.Valid ? originTile.Layer : Find.WorldGrid.Surface;
            if (layer == null || Find.World?.renderer == null) return;

            Find.World.renderer.SetDirty<VoidAwake_VoidTile>(layer);
        }

        //タイルごとの浸食レベル
        public enum VoidErosionLevel { None, Light, Medium, Heavy, Extreme }

        public VoidErosionLevel GetErosionLevel(PlanetTile tile)//タイルの浸食レベルを調べる
        {

            if (!originTile.Valid || radiusInTiles <= 0f)
                return VoidErosionLevel.None;

            float dist = Find.WorldGrid.ApproxDistanceInTiles(originTile, tile);
            if (dist > radiusInTiles)
                return VoidErosionLevel.None;

            //浸食範囲と、そのタイルの基点からの距離の比率からレベルを算出
            float ratio = dist / radiusInTiles;
            if (ratio <= 0.25f) return VoidErosionLevel.Extreme;//黒
            if (ratio <= 0.50f) return VoidErosionLevel.Heavy;//濃い紫
            if (ratio <= 0.75f) return VoidErosionLevel.Medium;//紫
            return VoidErosionLevel.Light;//薄い紫
        }
        //基点へのジャンプボタン
        public void JumpToOrigin()
        {
            if (!originTile.Valid) return;
            CameraJumper.TryJump(originTile, CameraJumper.MovementMode.Pan);
        }

        //基点マーカー
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

        //浸食による派閥の破壊
        private HashSet<PlanetTile> prevHeavyExtreme = new HashSet<PlanetTile>();

        private void ApplyVoidSettlementDestruction()
        {
            var nowHeavy = new HashSet<PlanetTile>();
            foreach (PlanetTile t in ErodedTiles)
            {
                VoidErosionLevel lvl = GetErosionLevel(t);
                if (lvl >= VoidErosionLevel.Heavy) // Heavy, Extreme
                    nowHeavy.Add(t);
            }

            // 新規に Heavy 以上になったタイルだけ
            List<Settlement> victims = new List<Settlement>();
            foreach (Settlement s in Find.WorldObjects.Settlements)
            {
                if (!nowHeavy.Contains(s.Tile)) continue;
                if (prevHeavyExtreme.Contains(s.Tile)) continue; // 既に処理済み帯
                if (s.Faction == null || s.Faction.IsPlayer) continue;
                if (s.Destroyed) continue;
                victims.Add(s);
            }

            foreach (Settlement s in victims)
                DestroyNpcSettlementByVoid(s);

            prevHeavyExtreme = nowHeavy;
        }

        private void DestroyNpcSettlementByVoid(Settlement settlement)
        {
            PlanetTile tile = settlement.Tile;
            Faction faction = settlement.Faction;
            string label = settlement.Label;

            // プレイヤーが今そのマップにいる場合は一旦スキップ（任意）
            if (settlement.HasMap && settlement.Map.mapPawns.AnyColonistSpawned)
                return;

            bool hasOtherBase = false;
            foreach (Settlement other in Find.WorldObjects.Settlements)
            {
                if (other != settlement && other.Faction == faction)
                {
                    hasOtherBase = true;
                    break;
                }
            }

            // 廃墟マーカー（拠点が破壊された見た目）
            var ruined = (DestroyedSettlement)WorldObjectMaker.MakeWorldObject(
                tile.LayerDef.DestroyedSettlementWorldObjectDef);
            ruined.Tile = tile;
            ruined.SetFaction(faction);
            Find.WorldObjects.Add(ruined);

            if (settlement.HasMap)
                settlement.Map.info.parent = ruined;

            string body = "LetterFactionBaseDefeatedNoRaids".Translate(label);
            if (!hasOtherBase)
            {
                faction.defeated = true;
                body += "\n\n" + "LetterFactionBaseDefeated_FactionDestroyed".Translate(faction.Name);
            }

            Find.LetterStack.ReceiveLetter(
                "LetterLabelFactionBaseDefeated".Translate(),
                body,
                LetterDefOf.NeutralEvent, 
                new GlobalTargetInfo(tile),
                faction);

            settlement.Destroy();
        }

    }
}