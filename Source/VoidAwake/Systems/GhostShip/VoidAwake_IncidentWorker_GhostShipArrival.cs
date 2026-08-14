using RimWorld;
using Verse;

namespace VoidAwake
{
	public class VoidAwake_IncidentWorker_GhostShipArrival : IncidentWorker
	{
		protected override bool CanFireNowSub(IncidentParms parms)
		{
			if (!(parms.target is Map map) || !map.IsPlayerHome)
			{
				return false;
			}

			VoidAwake_MapComponent_GhostShip comp = map.GetComponent<VoidAwake_MapComponent_GhostShip>();
			return comp != null && !comp.IsOceanActive;
		}

		protected override bool TryExecuteWorker(IncidentParms parms)
		{
			if (!(parms.target is Map map) || !map.IsPlayerHome)
			{
				return false;
			}

			VoidAwake_MapComponent_GhostShip comp = map.GetComponent<VoidAwake_MapComponent_GhostShip>();
			if (comp == null || !comp.TryStartOcean())
			{
				return false;
			}

			SendStandardLetter(parms, new LookTargets(map.Center, map));
			return true;
		}
	}
}
