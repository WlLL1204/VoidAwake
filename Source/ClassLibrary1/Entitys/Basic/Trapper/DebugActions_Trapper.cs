using LudeonTK;
using RimWorld;
using Verse;

namespace VoidAwake
{
	public static class DebugActions_Trapper
	{
		[DebugAction("VoidAwake", "Trapper arrival", false, false, false, false, false, 0, false,
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void TrapperArrival()
		{
			Map map = Find.CurrentMap;
			if (map == null)
			{
				return;
			}

			IncidentDef def = VoidAwake_TrapperDefOf.VoidAwake_TrapperArrival;
			if (def?.Worker == null)
			{
				Messages.Message("VoidAwake_TrapperArrival incident def missing.", MessageTypeDefOf.RejectInput, false);
				return;
			}

			IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, map);
			parms.forced = true;
			parms.target = map;
			def.Worker.TryExecute(parms);
		}
	}
}
