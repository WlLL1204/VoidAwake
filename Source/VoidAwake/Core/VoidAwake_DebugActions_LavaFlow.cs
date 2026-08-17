using LudeonTK;
using RimWorld;
using Verse;

namespace VoidAwake
{
    public static class VoidAwake_DebugActions_LavaFlow
    {
        [DebugAction("VoidAwake", "LavaFlow: start lava flow", false, false, false, false, true, 0, false,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void StartLavaFlow()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }

            IncidentDef def = DefDatabase<IncidentDef>.GetNamedSilentFail("LavaFlow");
            if (def?.Worker == null)
            {
                Messages.Message("LavaFlow incident def missing.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            GameConditionDef conditionDef = DefDatabase<GameConditionDef>.GetNamedSilentFail("LavaFlow");
            if (conditionDef != null && map.GameConditionManager.ConditionIsActive(conditionDef))
            {
                Messages.Message("Lava flow is already active.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, map);
            parms.forced = true;
            parms.target = map;
            if (!def.Worker.TryExecute(parms))
            {
                Messages.Message("Failed to start lava flow.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (conditionDef != null && !map.GameConditionManager.ConditionIsActive(conditionDef))
            {
                Messages.Message("Lava flow ended immediately (no volcanic rock on map edge).",
                    MessageTypeDefOf.RejectInput, false);
                return;
            }

            Messages.Message("Started lava flow.", MessageTypeDefOf.TaskCompletion, false);
        }
    }
}
