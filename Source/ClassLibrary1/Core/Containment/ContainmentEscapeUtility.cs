using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VoidAwake
{
    public static class ContainmentEscapeUtility
    {
        private static Dictionary<ThingDef, VoidAwake_ContainmentEscapeDef> byEntity;
        private static Dictionary<MutantDef, VoidAwake_ContainmentEscapeDef> byMutant;
        private static List<VoidAwake_ContainmentEscapeDef> byEntityAndMutant;
        private static VoidAwake_ContainmentEscapeDef fallback;

        public static bool Active => ModsConfig.AnomalyActive;

        /// <summary>
        /// 足場の収容強度がエンティティの必要収容強度を満たしているか。
        /// バニラの SafelyContains と同じ基準なので、足場の検査文の赤字表示と挙動が一致する。
        /// </summary>
        public static bool IsEscapeProof(Pawn pawn)
        {
            if (!Active || pawn == null)
            {
                return false;
            }

            Building_HoldingPlatform platform = pawn.GetComp<CompHoldingPlatformTarget>()?.HeldPlatform;
            return platform != null && platform.SafelyContains(pawn);
        }

        /// <summary>指定ポーンに対応する脱走設定。</summary>
        public static VoidAwake_ContainmentEscapeDef ContainmentEscapeDefFor(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }
            return ContainmentEscapeDefFor(pawn.def, pawn.mutant?.Def);
        }

        /// <summary>
        /// 一致の優先度は entityDef + mutantDef → mutantDef のみ → entityDef のみ → 既定 Def。
        /// 専用 Def が無いアノマリーは既定 Def に落ちるので、DLC や他 Mod で種類が増えても動く。
        /// </summary>
        public static VoidAwake_ContainmentEscapeDef ContainmentEscapeDefFor(ThingDef entityDef, MutantDef mutantDef)
        {
            if (entityDef == null)
            {
                return null;
            }

            EnsureCaches();

            if (mutantDef != null)
            {
                for (int i = 0; i < byEntityAndMutant.Count; i++)
                {
                    VoidAwake_ContainmentEscapeDef def = byEntityAndMutant[i];
                    if (def.entityDef == entityDef && def.mutantDef == mutantDef)
                    {
                        return def;
                    }
                }

                if (byMutant.TryGetValue(mutantDef, out VoidAwake_ContainmentEscapeDef mutantMatch))
                {
                    return mutantMatch;
                }
            }

            if (byEntity.TryGetValue(entityDef, out VoidAwake_ContainmentEscapeDef entityMatch))
            {
                return entityMatch;
            }
            return fallback;
        }

        /// <summary>保持台に載せられる（CompHoldingPlatformTarget を持つ）ThingDef か。</summary>
        public static bool IsCapturableEntityDef(ThingDef def)
        {
            if (def?.comps == null)
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

        /// <summary>このアノマリー用の脱走イベントにレターが設定されているか。</summary>
        public static bool HasEscapeLetterFor(Pawn pawn)
        {
            VoidAwake_ContainmentEscapeDef escapeDef = ContainmentEscapeDefFor(pawn);
            if (escapeDef?.events == null)
            {
                return false;
            }

            for (int i = 0; i < escapeDef.events.Count; i++)
            {
                if (escapeDef.events[i]?.HasLetter == true)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Def に設定されたレターを送信する。letterOnlyWhenInitiator が true のときは initiator のみ。
        /// </summary>
        public static void TrySendEscapeLetter(VoidAwake_EscapeEventDef eventDef, VoidAwake_EscapeContext ctx)
        {
            if (eventDef == null || !eventDef.HasLetter || !ctx.IsValid)
            {
                return;
            }
            if (eventDef.letterOnlyWhenInitiator && !ctx.initiator)
            {
                return;
            }

            LetterDef letterType = eventDef.letterDef ?? LetterDefOf.ThreatBig;
            TaggedString label = eventDef.letterLabel.Formatted(ctx.pawn.Named("PAWN"));
            TaggedString text = eventDef.letterText.Formatted(ctx.pawn.Named("PAWN"));
            Find.LetterStack.ReceiveLetter(
                LetterMaker.MakeLetter(label, text, letterType, ctx.pawn));
        }

        /// <summary>
        /// 収容強度に関係なく、指定セルの部屋にある保持台のエンティティを全て脱走させる（デバッグ用）。
        /// 戻り値は脱走させた数。
        /// </summary>
        public static int ForceEscapeRoomAt(Map map, IntVec3 cell)
        {
            if (map == null || !cell.InBounds(map))
            {
                return 0;
            }
            return ForceEscapeRoom(cell.GetRoom(map));
        }

        /// <summary>
        /// 部屋単位で脱走を誘発する。対象はバニラの連鎖脱走と同じ「部屋内および隣接」の保持台。
        /// 最初の 1 体だけ initiator として扱うので、レターは 1 通に収まる。
        /// </summary>
        public static int ForceEscapeRoom(Room room)
        {
            if (!Active || room == null)
            {
                return 0;
            }

            List<CompHoldingPlatformTarget> targets = new List<CompHoldingPlatformTarget>();
            HashSet<Building_HoldingPlatform> seen = new HashSet<Building_HoldingPlatform>();

            foreach (Thing thing in room.ContainedAndAdjacentThings)
            {
                if (!(thing is Building_HoldingPlatform platform) || !seen.Add(platform))
                {
                    continue;
                }

                CompHoldingPlatformTarget comp = platform.HeldPawn?.TryGetComp<CompHoldingPlatformTarget>();
                if (comp != null && comp.CurrentlyHeldOnPlatform)
                {
                    targets.Add(comp);
                }
            }

            int escaped = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                // 直前の initiator による連鎖脱走で既に台から出ている場合がある。
                if (!targets[i].CurrentlyHeldOnPlatform)
                {
                    continue;
                }

                targets[i].Escape(escaped == 0);
                escaped++;
            }
            return escaped;
        }

        /// <summary>
        /// 脱走が確定した後、そのアノマリーに紐づくイベントを順に発火する。
        /// 例外はイベント単位で握るので、1 つ落ちても後続のイベントは走る。
        /// </summary>
        public static void Notify_Escaped(VoidAwake_EscapeContext ctx)
        {
            if (!Active || !ctx.IsValid)
            {
                return;
            }

            VoidAwake_ContainmentEscapeDef escapeDef = ContainmentEscapeDefFor(ctx.pawn);
            if (escapeDef?.events == null)
            {
                return;
            }

            for (int i = 0; i < escapeDef.events.Count; i++)
            {
                VoidAwake_EscapeEventDef eventDef = escapeDef.events[i];
                if (eventDef == null)
                {
                    continue;
                }
                if (eventDef.onlyWhenInitiator && !ctx.initiator)
                {
                    continue;
                }
                if (eventDef.chance < 1f && !Rand.Chance(eventDef.chance))
                {
                    continue;
                }

                try
                {
                    eventDef.Worker.DoEscapeEvent(ctx);
                }
                catch (Exception ex)
                {
                    Log.Error("[VoidAwake] Escape event " + eventDef.defName + " (" + escapeDef.defName + ") for "
                        + ctx.pawn.def.defName + " threw an exception: " + ex);
                }
            }
        }

        private static void EnsureCaches()
        {
            if (byEntity != null)
            {
                return;
            }

            byEntity = new Dictionary<ThingDef, VoidAwake_ContainmentEscapeDef>();
            byMutant = new Dictionary<MutantDef, VoidAwake_ContainmentEscapeDef>();
            byEntityAndMutant = new List<VoidAwake_ContainmentEscapeDef>();

            foreach (VoidAwake_ContainmentEscapeDef def in
                DefDatabase<VoidAwake_ContainmentEscapeDef>.AllDefsListForReading)
            {
                if (def.entityDef != null && def.mutantDef != null)
                {
                    byEntityAndMutant.Add(def);
                }
                else if (def.mutantDef != null)
                {
                    byMutant[def.mutantDef] = def;
                }
                else if (def.entityDef != null)
                {
                    byEntity[def.entityDef] = def;
                }
                else
                {
                    fallback = def;
                }
            }
        }
    }
}
