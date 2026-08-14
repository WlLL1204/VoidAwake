using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace VoidAwake
{
    /// <summary>
    /// 世界浸食の状態管理。
    /// 見た目は WorldDrawLayer_VoidErosion が ErodedTiles を参照して描画する。
    /// </summary>
    public class VoidAwake_WorldComponent_VoidErosion : WorldComponent
    {
        public PlanetTile originTile = PlanetTile.Invalid;
        public float radiusInTiles;

        /// <summary>現在浸食されているタイル（描画レイヤが参照する）</summary>
        public HashSet<PlanetTile> ErodedTiles = new HashSet<PlanetTile>();

        private int lastDay = -1;
        private int cachedErodedCount;
        private int cachedLandTileCount;
        private int lastClockHour = -1;
        private bool erosionUiDragging;
        private Vector2 erosionUiDragOffset;

        // --- バランス（確認用の大きめ値。本番で戻す） ---
        public const float StartRadius = 1f;//開始時の浸食範囲
        public const float TilesPerDay = 1.0f;//浸食の進行速度

        private static Texture2D erosionClockTex;
        private static Texture2D erosionClockHandsTex;

        private VoidAwake_WorldObject_ErosionMarker marker;

        public VoidAwake_WorldComponent_VoidErosion(World world) : base(world)
        {
        }

        /// <summary>海洋を除いた陸タイル総数。</summary>
        public int TotalTiles => cachedLandTileCount;


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
                ChooseOriginTile(); // origin だけ決める。radius は触らない

            SyncRadiusFromCalendar();
            RecalculateLandTileCount();
            RecalculateErodedTiles();

            lastDay = GenDate.DaysPassed; // 同期後なら上書きしてOK
            EnsureMarker();
            lastClockHour = ErosionClockHour;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref originTile, "originTile", PlanetTile.Invalid);
            Scribe_Values.Look(ref radiusInTiles, "radiusInTiles", StartRadius);
            Scribe_Values.Look(ref lastDay, "lastDay", -1);
            Scribe_Values.Look(ref cachedErodedCount, "cachedErodedCount", 0);
            Scribe_Values.Look(ref lastClockHour, "lastClockHour", -1);
            // ErodedTiles はセーブしない（ロード後 FinalizeInit / Recalculate で復元）
            Scribe_References.Look(ref marker, "erosionMarker");
        }

        public override void WorldComponentTick()
        {
            int day = GenDate.DaysPassed;
            if (day != lastDay)
            {
                lastDay = day;
                SyncRadiusFromCalendar();
                RecalculateLandTileCount();
                RecalculateErodedTiles();
            }

            // 日替わり処理の外でも、針が進んだか毎回見る
            TryFireClockHourEvent();
        }

        private void TryFireClockHourEvent()
        {
            int hour = ErosionClockHour;
            if (lastClockHour < 0)
            {
                lastClockHour = hour;
                return;
            }
            if (hour <= lastClockHour)
                return;

            // セーブ跨ぎやジャンプで複数時間スキップしても全部拾う
            for (int h = lastClockHour + 1; h <= hour; h++)
                OnErosionClockHourAdvanced(h);

            lastClockHour = hour;
        }

        private void OnErosionClockHourAdvanced(int hour)
        {
            SoundDef sound = DefDatabase<SoundDef>.GetNamedSilentFail("VoidAwake_WorldClockTick");
            if (sound != null)
                sound.PlayOneShotOnCamera();

            VoidAwake_ClockEventUtility.TryFireRandomEvent(hour);
        }
        public override void WorldComponentUpdate()
        {
            if (ErodedTiles == null || ErodedTiles.Count == 0)
                return;
            // ワールド画面を開いているときだけスクロール
            if (Find.World == null || !WorldRendererUtility.WorldRendered)
                return;

            VoidAwake_WorldDrawLayer_VoidTile.UpdateSwirlScroll();
            VoidAwake_WorldDrawLayer_VoidTileEffect.UpdateCloudScroll();
        }



        private void ChooseOriginTile()
        {
            WorldGrid grid = Find.WorldGrid;
            Vector3 mapCenter = grid.SurfaceViewCenter;

            PlanetTile best = PlanetTile.Invalid;
            float bestDist = float.MaxValue;

            for (int i = 0; i < grid.TilesCount; i++)
            {
                PlanetTile tile = i;
                if (grid[tile].WaterCovered) continue;

                float dist = Vector3.Angle(grid.GetTileCenter(tile), mapCenter);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = tile;
                }
            }

            originTile = best.Valid ? best : (PlanetTile)0;
            Log.Message($"[VoidAwake] Void erosion origin tile = {originTile} (near map center)");
            EnsureMarker();
        }
        /// <summary>浸食タイル集合を作り直し、描画レイヤを dirty にする。</summary>
        private void RecalculateErodedTiles()
        {
            //タイルの保護パッチ
            var purified = Find.World.GetComponent<VoidAwake_WorldComponent_VoidProtect>();
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

            // 浸食率用: 海洋を除外した陸タイルだけ数える
            int landEroded = 0;
            foreach (PlanetTile t in ErodedTiles)
            {
                if (!grid[t].WaterCovered)
                    landEroded++;
            }
            cachedErodedCount = landEroded;

            NotifyDrawLayerDirty();
            ApplyVoidSettlementDestruction();
        }
        private void NotifyDrawLayerDirty()
        {
            // 1.6: WorldRenderer.SetDirty<T>(PlanetLayer)
            PlanetLayer layer = originTile.Valid ? originTile.Layer : Find.WorldGrid.Surface;
            if (layer == null || Find.World?.renderer == null) return;

            Find.World.renderer.SetDirty<VoidAwake_WorldDrawLayer_VoidTile>(layer);
            Find.World.renderer.SetDirty<VoidAwake_WorldDrawLayer_VoidTileEffect>(layer);
        }

        public VoidAwake_VoidErosionLevel GetErosionLevel(PlanetTile tile)//タイルの浸食レベルを調べる
        {
            if (!tile.Valid || !originTile.Valid || radiusInTiles <= 0f)
                return VoidAwake_VoidErosionLevel.None;

            float dist = Find.WorldGrid.ApproxDistanceInTiles(originTile, tile);
            if (dist > radiusInTiles)
                return VoidAwake_VoidErosionLevel.None;

            //浸食範囲と、そのタイルの基点からの距離の比率からレベルを算出
            float ratio = dist / radiusInTiles;
            if (ratio <= 0.25f) return VoidAwake_VoidErosionLevel.Extreme;//黒
            if (ratio <= 0.50f) return VoidAwake_VoidErosionLevel.Heavy;//濃い紫
            if (ratio <= 0.75f) return VoidAwake_VoidErosionLevel.Medium;//紫
            return VoidAwake_VoidErosionLevel.Light;//薄い紫
        }
        //基点へのジャンプ
        public void JumpToOrigin()
        {
            if (!originTile.Valid)
            {
                Messages.Message("VoidAwake_JumpOriginMissing".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

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
                .OfType<VoidAwake_WorldObject_ErosionMarker>()
                .FirstOrDefault();

            if (marker != null)
            {
                marker.Tile = originTile;
                return;
            }

            WorldObjectDef def = DefDatabase<WorldObjectDef>.GetNamedSilentFail("VoidAwake_ErosionOrigin");
            if (def == null) return;

            marker = (VoidAwake_WorldObject_ErosionMarker)WorldObjectMaker.MakeWorldObject(def);
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
                VoidAwake_VoidErosionLevel lvl = GetErosionLevel(t);
                if (lvl >= VoidAwake_VoidErosionLevel.Heavy) // Heavy, Extreme
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

            var affected = new HashSet<Faction>();
            foreach (Settlement s in victims)
            {
                if (s.Faction != null)
                    affected.Add(s.Faction);
                DestroyNpcSettlementByVoid(s);
            }

            foreach (Faction f in affected)
            {
                if (f.IsPlayer || f.defeated)
                    continue;
                bool anyLeft = false;
                foreach (Settlement other in Find.WorldObjects.Settlements)
                {
                    if (other.Faction == f)
                    {
                        anyLeft = true;
                        break;
                    }
                }
                if (!anyLeft)
                    f.defeated = true;
            }

            prevHeavyExtreme = nowHeavy;
        }

        private void DestroyNpcSettlementByVoid(Settlement settlement)
        {
            // プレイヤーが今そのマップにいる場合は一旦スキップ（任意）
            if (settlement.HasMap && settlement.Map.mapPawns.AnyColonistSpawned)
                return;

            settlement.Destroy();
        }

        //ヴォイドの世界の浸食率に使うやつ
        private static void EnsureErosionClockTextures()
        {
            if (erosionClockTex != null) return;
            erosionClockTex = ContentFinder<Texture2D>.Get("UI/Interface/WorldClock/Clock");
            erosionClockHandsTex = ContentFinder<Texture2D>.Get("UI/Interface/WorldClock/ClockHands");
        }
        /// <summary>0〜12。浸食率を12分割した硬い目盛り。</summary>
        public int ErosionClockHour
        {
            get
            {
                // 100% で 12（一周）。11までにしたいなら Clamp(..., 0, 11)
                return Mathf.Clamp(Mathf.FloorToInt(ErosionRate * 12f), 0, 12);
            }
        }
        public float ErosionClockAngle => ErosionClockHour * 30f;
        public override void WorldComponentOnGUI()
        {
            if (Find.ScreenshotModeHandler != null && Find.ScreenshotModeHandler.Active)
                return;
            if (VoidAwake_Mod.Settings == null || !VoidAwake_Mod.Settings.showErosionRateUI)
                return;
            EnsureErosionClockTextures();
            float size = 160f;
            float width = size;
            float height = size;
            float x = VoidAwake_Mod.Settings.erosionUiX;
            float y = VoidAwake_Mod.Settings.erosionUiY;
            if (x < 0f || y < 0f)
            {
                x = (float)UI.screenWidth - width - 16f;
                y = 80f;
            }
            x = Mathf.Clamp(x, 0f, UI.screenWidth - width);
            y = Mathf.Clamp(y, 0f, UI.screenHeight - height);
            Rect rect = new Rect(x, y, width, height);
            // --- 右クリック: 浸食基点へジャンプ ---
            if (Event.current.type == EventType.MouseDown
                && Event.current.button == 1
                && Mouse.IsOver(rect))
            {
                JumpToOrigin();
                Event.current.Use();
            }
            // --- 左ドラッグで移動 ---
            if (Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && Mouse.IsOver(rect))
            {
                erosionUiDragging = true;
                erosionUiDragOffset = Event.current.mousePosition - new Vector2(x, y);
                Event.current.Use();
            }
            if (erosionUiDragging && Event.current.type == EventType.MouseDrag)
            {
                Vector2 pos = Event.current.mousePosition - erosionUiDragOffset;
                pos.x = Mathf.Clamp(pos.x, 0f, UI.screenWidth - width);
                pos.y = Mathf.Clamp(pos.y, 0f, UI.screenHeight - height);
                VoidAwake_Mod.Settings.erosionUiX = pos.x;
                VoidAwake_Mod.Settings.erosionUiY = pos.y;
                rect.position = pos;
                Event.current.Use();
            }
            if (erosionUiDragging && Event.current.type == EventType.MouseUp)
            {
                erosionUiDragging = false;
                LoadedModManager.GetMod<VoidAwake_Mod>().WriteSettings();
                Event.current.Use();
            }
            // --- 時計描画 ---
            Widgets.DrawTextureFitted(rect, erosionClockTex, 1f);
            float angle = ErosionClockAngle;
            Widgets.DrawTextureFitted(
                rect,
                erosionClockHandsTex,
                1f,
                Vector2.one,
                new Rect(0f, 0f, 1f, 1f),
                angle);

            TooltipHandler.TipRegion(rect, GetWorldClockTooltip());
        }

        //陸地を数える
        private void RecalculateLandTileCount()
        {
            WorldGrid grid = Find.WorldGrid;
            int count = 0;
            for (int i = 0; i < grid.TilesCount; i++)
            {
                PlanetTile tile = i;
                if (!grid[tile].WaterCovered)
                    count++;
            }
            cachedLandTileCount = count;
        }

        private string GetWorldClockTooltip()
        {
            return "VoidAwake_WorldClockTitle".Translate()
                + "\n"
                + GetWorldClockFlavor(ErosionClockHour)
                + "\n\n"
                + "VoidAwake_WorldClockTipControls".Translate();
        }

        private static string GetWorldClockFlavor(int hour)
        {
            switch (hour)
            {
                case 0:
                    return "VoidAwake_WorldClockFlavor_0".Translate();
                case 1:
                case 2:
                case 3:
                    return "VoidAwake_WorldClockFlavor_1".Translate();
                case 4:
                case 5:
                case 6:
                    return "VoidAwake_WorldClockFlavor_4".Translate();
                case 7:
                case 8:
                case 9:
                    return "VoidAwake_WorldClockFlavor_7".Translate();
                case 10:
                case 11:
                    return "VoidAwake_WorldClockFlavor_10".Translate();
                default:
                    return "VoidAwake_WorldClockFlavor_12".Translate();
            }
        }

        //世界の浸食レベルの同期
        private void SyncRadiusFromCalendar()
        {
            radiusInTiles = StartRadius + GenDate.DaysPassed * TilesPerDay;
        }
    }

    //タイルごとの浸食レベル
    public enum VoidAwake_VoidErosionLevel { None, Light, Medium, Heavy, Extreme }
}