using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VoidAwake
{
    public static class VoidAwake_VoidMagicUtility
    {
        public const int TicksPerHour = 2500;
        public const int TicksPerDay = 60000;

        /// <summary>瞑想スポット以外で瞑想した場合に収容アノマリーを探す半径。</summary>
        public const float DefaultMeditationRadius = 9.9f;

        /// <summary>全繋がりの合計％がこの値ごとに心情デバフ 1 スタック（-2）。</summary>
        public const float BondStrainPercentPerStack = 50f;

        /// <summary>繋がりデバフ 1 スタックあたりの心情。</summary>
        public const float BondStrainMoodPerStack = -2f;

        private const int ContainedScanIntervalTicks = 600;

        private static Dictionary<ThingDef, VoidAwake_VoidMagicDef> overridesByEntity;
        private static List<ThingDef> linkableEntityDefs;

        private static readonly HashSet<ThingDef> containedNowCache = new HashSet<ThingDef>();
        private static int containedNowCacheTick = -99999;

        public static bool Active => ModsConfig.AnomalyActive;

        public static VoidAwake_VoidMagicDef DefaultDef =>
            VoidAwake_VoidMagicDefOf.VoidAwake_VoidMagicDefault;

        /// <summary>
        /// エンティティ専用の Def があればそれを、なければ既定テンプレートを返す。
        /// これにより Anomaly の収容可能エンティティ全てが Def を書かずに対象になる。
        /// </summary>
        public static VoidAwake_VoidMagicDef DefFor(ThingDef entityDef)
        {
            if (entityDef == null)
            {
                return null;
            }

            EnsureCaches();
            if (overridesByEntity.TryGetValue(entityDef, out VoidAwake_VoidMagicDef found))
            {
                return found;
            }
            return DefaultDef;
        }

        public static List<ThingDef> LinkableEntityDefs
        {
            get
            {
                EnsureCaches();
                return linkableEntityDefs;
            }
        }

        private static void EnsureCaches()
        {
            if (overridesByEntity != null)
            {
                return;
            }

            overridesByEntity = new Dictionary<ThingDef, VoidAwake_VoidMagicDef>();
            foreach (VoidAwake_VoidMagicDef def in DefDatabase<VoidAwake_VoidMagicDef>.AllDefsListForReading)
            {
                if (def.entityDef == null)
                {
                    continue;
                }
                overridesByEntity[def.entityDef] = def;
            }

            linkableEntityDefs = new List<ThingDef>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (IsLinkableEntityDef(def))
                {
                    linkableEntityDefs.Add(def);
                }
            }
            linkableEntityDefs.SortBy(d => d.label);
        }

        /// <summary>
        /// 収容可能なアノマリー種か。シャンブラー等は Human 扱いになるため人型は除外する。
        /// </summary>
        public static bool IsLinkableEntityDef(ThingDef def)
        {
            if (def?.race == null || def.race.Humanlike || def.comps == null)
            {
                return false;
            }

            for (int i = 0; i < def.comps.Count; i++)
            {
                if (def.comps[i] is CompProperties_HoldingPlatformTarget)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsLinkableEntity(Pawn entity)
        {
            return entity != null && !entity.Dead && IsLinkableEntityDef(entity.def);
        }

        public static bool IsContainedEntity(Pawn entity)
        {
            return IsLinkableEntity(entity) && entity.IsOnHoldingPlatform;
        }

        public static VoidAwake_CompVoidMagic GetComp(Pawn pawn)
        {
            return pawn?.TryGetComp<VoidAwake_CompVoidMagic>();
        }

        public static bool CanMeditateForVoidMagic(Pawn pawn)
        {
            return Active
                && pawn != null
                && !pawn.Dead
                && !pawn.Downed
                && pawn.RaceProps.Humanlike
                && GetComp(pawn) != null;
        }

        /// <summary>
        /// 捻じれた瞑想だけでなく、バニラの瞑想（および JobDriver_Meditate 派生）も繋がりを伸ばす。
        /// </summary>
        public static bool IsMeditationJob(JobDef jobDef)
        {
            if (jobDef == null)
            {
                return false;
            }
            if (jobDef == VoidAwake_VoidMagicDefOf.VoidAwake_VoidMeditate)
            {
                return true;
            }
            return jobDef.driverClass != null
                && typeof(JobDriver_Meditate).IsAssignableFrom(jobDef.driverClass);
        }

        public static bool IsMeditatingNow(Pawn pawn)
        {
            if (pawn?.Map == null || pawn.Dead || pawn.Downed)
            {
                return false;
            }
            if (pawn.pather != null && pawn.pather.MovingNow)
            {
                return false;
            }
            return IsMeditationJob(pawn.CurJobDef);
        }

        /// <summary>瞑想中の入植者が乗っている瞑想スポット。無ければ null。</summary>
        public static VoidAwake_CompMeditationAnchor AnchorUnder(Pawn pawn)
        {
            Map map = pawn?.Map;
            if (map == null)
            {
                return null;
            }

            List<Thing> things = pawn.Position.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                VoidAwake_CompMeditationAnchor anchor =
                    things[i].TryGetComp<VoidAwake_CompMeditationAnchor>();
                if (anchor != null)
                {
                    return anchor;
                }
            }
            return null;
        }

        /// <summary>
        /// 現在プレイヤーの各マップで収容されているアノマリーの ThingDef 集合。走査は 600 tick キャッシュ。
        /// </summary>
        public static HashSet<ThingDef> ContainedEntityDefsNow()
        {
            int now = Find.TickManager?.TicksGame ?? 0;
            if (now >= containedNowCacheTick && now - containedNowCacheTick < ContainedScanIntervalTicks)
            {
                return containedNowCache;
            }

            containedNowCacheTick = now;
            containedNowCache.Clear();

            List<Map> maps = Find.Maps;
            if (maps == null)
            {
                return containedNowCache;
            }

            for (int i = 0; i < maps.Count; i++)
            {
                foreach (Building_HoldingPlatform platform in
                    maps[i].listerBuildings.AllBuildingsColonistOfClass<Building_HoldingPlatform>())
                {
                    Pawn held = platform.HeldPawn;
                    if (IsLinkableEntity(held))
                    {
                        containedNowCache.Add(held.def);
                    }
                }
            }
            return containedNowCache;
        }

        public static bool IsEntityDefContainedNow(ThingDef entityDef)
        {
            return entityDef != null && ContainedEntityDefsNow().Contains(entityDef);
        }

        /// <summary>指定セルから半径内で収容されているアノマリーを集める。</summary>
        public static void ContainedEntitiesNear(Map map, IntVec3 center, float radius, List<Pawn> outEntities)
        {
            outEntities.Clear();
            if (map == null || !center.IsValid)
            {
                return;
            }

            float radiusSquared = radius * radius;
            foreach (Building_HoldingPlatform platform in
                map.listerBuildings.AllBuildingsColonistOfClass<Building_HoldingPlatform>())
            {
                if ((platform.Position - center).LengthHorizontalSquared > radiusSquared)
                {
                    continue;
                }

                Pawn held = platform.HeldPawn;
                if (IsLinkableEntity(held))
                {
                    outEntities.Add(held);
                }
            }
        }

        public static string EntityLabel(ThingDef entityDef)
        {
            return entityDef?.LabelCap ?? "???";
        }
    }
}
