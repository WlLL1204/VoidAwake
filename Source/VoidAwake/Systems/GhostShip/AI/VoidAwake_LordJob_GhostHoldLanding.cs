using Verse;
using Verse.AI.Group;

namespace VoidAwake
{
	public class VoidAwake_LordJob_GhostHoldLanding : LordJob
	{
		private System.Collections.Generic.Dictionary<int, IntVec3> landingByPawnId;

		public override StateGraph CreateGraph()
		{
			StateGraph graph = new StateGraph();
			graph.StartingToil = new VoidAwake_LordToil_GhostHoldLanding();
			return graph;
		}

		public void SetLanding(Pawn pawn, IntVec3 cell)
		{
			if (pawn == null || !cell.IsValid)
			{
				return;
			}

			if (landingByPawnId == null)
			{
				landingByPawnId = new System.Collections.Generic.Dictionary<int, IntVec3>();
			}

			landingByPawnId[pawn.thingIDNumber] = cell;
		}

		public IntVec3 LandingFor(Pawn pawn)
		{
			if (pawn == null)
			{
				return IntVec3.Invalid;
			}

			if (landingByPawnId != null && landingByPawnId.TryGetValue(pawn.thingIDNumber, out IntVec3 cell) && cell.IsValid)
			{
				return cell;
			}

			return pawn.PositionHeld;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref landingByPawnId, "landingByPawnId", LookMode.Value, LookMode.Value);
			if (Scribe.mode == LoadSaveMode.PostLoadInit && landingByPawnId == null)
			{
				landingByPawnId = new System.Collections.Generic.Dictionary<int, IntVec3>();
			}
		}
	}
}
