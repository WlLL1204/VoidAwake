using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using Verse;

namespace VoidAwake
{
    public static class VoidAwake_DebugActions_Containment
    {
        /// <summary>クリックしたセルの部屋にある保持台のエンティティを全て脱走させる。</summary>
        [DebugAction("VoidAwake", "Containment: force escape in room", false, false, false, true, false, 0, false,
            actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceEscapeInRoom()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }

            IntVec3 cell = UI.MouseCell();
            Room room = cell.InBounds(map) ? cell.GetRoom(map) : null;
            if (room == null)
            {
                Messages.Message("No room at that cell.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            ReportForcedEscape(VoidAwake_ContainmentEscapeUtility.ForceEscapeRoom(room));
        }

        /// <summary>
        /// 収容可能なアノマリーごとに、解決される脱走設定とイベントをログに出す。
        /// 既定 Def にフォールバックしている＝専用イベント未定義なので、種類が増えたときの取りこぼし確認に使う。
        /// </summary>
        [DebugAction("VoidAwake", "Containment: dump escape events", false, false, false, true, false, 0, false,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void DumpEscapeEvents()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[VoidAwake] Containment escape events");

            sb.AppendLine("anomaly entities:");
            int fallbackCount = 0;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.race == null || !def.race.IsAnomalyEntity
                    || !VoidAwake_ContainmentEscapeUtility.IsCapturableEntityDef(def))
                {
                    continue;
                }

                if (AppendLine(sb, def.defName, VoidAwake_ContainmentEscapeUtility.ContainmentEscapeDefFor(def, null)))
                {
                    fallbackCount++;
                }
            }

            sb.AppendLine("mutants:");
            foreach (MutantDef mutant in DefDatabase<MutantDef>.AllDefsListForReading)
            {
                if (!mutant.canBeCapturedToHoldingPlatform)
                {
                    continue;
                }

                if (AppendLine(sb, mutant.defName,
                    VoidAwake_ContainmentEscapeUtility.ContainmentEscapeDefFor(ThingDefOf.Human, mutant)))
                {
                    fallbackCount++;
                }
            }

            Log.Message(sb.ToString());
            Messages.Message($"Escape events written to log ({fallbackCount} using the default).",
                MessageTypeDefOf.TaskCompletion, false);
        }

        /// <summary>戻り値は既定 Def にフォールバックしたかどうか。</summary>
        private static bool AppendLine(StringBuilder sb, string label, VoidAwake_ContainmentEscapeDef escapeDef)
        {
            if (escapeDef == null)
            {
                sb.AppendLine($"  {label} -> (none)");
                return false;
            }

            var events = new List<string>();
            for (int i = 0; i < escapeDef.events.Count; i++)
            {
                events.Add(escapeDef.events[i]?.defName ?? "null");
            }

            bool isFallback = escapeDef.IsFallback;
            sb.AppendLine($"  {label} -> {escapeDef.defName}{(isFallback ? " (DEFAULT)" : "")}"
                + $" [{string.Join(", ", events.ToArray())}]");
            return isFallback;
        }

        public static void ReportForcedEscape(int escaped)
        {
            if (escaped > 0)
            {
                Messages.Message($"Forced {escaped} contained entity(s) to escape.",
                    MessageTypeDefOf.TaskCompletion, false);
            }
            else
            {
                Messages.Message("No contained entity in that room.", MessageTypeDefOf.RejectInput, false);
            }
        }
    }

    /// <summary>
    /// 開発者モード中、保持台に部屋単位で脱走を誘発するボタンを追加する。
    /// バニラの "DEV: Escape" は選択中の 1 体だけなので、収容室全体を試したい場合はこちらを使う。
    /// </summary>
    [HarmonyPatch(typeof(Building_HoldingPlatform), nameof(Building_HoldingPlatform.GetGizmos))]
    public static class VoidAwake_Patch_HoldingPlatform_GetGizmos_ForceEscapeRoom
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Building_HoldingPlatform __instance)
        {
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }

            if (!DebugSettings.ShowDevGizmos)
            {
                yield break;
            }

            Building_HoldingPlatform platform = __instance;
            yield return new Command_Action
            {
                defaultLabel = "DEV: Escape (room)",
                defaultDesc = "この保持台がある部屋の収容エンティティを全て脱走させる。収容強度は無視する。",
                action = delegate
                {
                    VoidAwake_DebugActions_Containment.ReportForcedEscape(
                        VoidAwake_ContainmentEscapeUtility.ForceEscapeRoom(platform.GetRoom()));
                }
            };
        }
    }
}
