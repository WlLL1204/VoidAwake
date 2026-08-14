using RimWorld;
using Verse;

namespace VoidAwake
{
	public class VoidAwake_ClockEvent_Hour4 : VoidAwake_ClockEventWorker_FixedHourBase
	{
		public override bool CanFire(int hour)
		{
			Map map = Find.AnyPlayerHomeMap;
			if (map == null || !map.IsPlayerHome)
			{
				return false;
			}

			VoidAwake_MapComponent_GhostShip comp = map.GetComponent<VoidAwake_MapComponent_GhostShip>();
			return comp != null && !comp.IsOceanActive;
		}

		public override void TryExecute(int hour)
		{
			// レターは Incident 側で送る（二重通知を避ける）
			Log.Message($"[VoidAwake] FixedClockEvent '{def.defName}' at hour {hour}");
			ExecuteFixedEvent(hour);
		}

		protected override void ExecuteFixedEvent(int hour)
		{
			Map map = Find.AnyPlayerHomeMap;
			if (map == null)
			{
				return;
			}

			IncidentDef incidentDef = VoidAwake_GhostShipDefOf.VoidAwake_GhostShipArrival;
			if (incidentDef?.Worker == null)
			{
				Log.Error("[VoidAwake] VoidAwake_GhostShipArrival incident def missing.");
				return;
			}

			IncidentParms parms = StorytellerUtility.DefaultParmsNow(incidentDef.category, map);
			parms.forced = true;
			parms.target = map;
			incidentDef.Worker.TryExecute(parms);
		}
	}
}
