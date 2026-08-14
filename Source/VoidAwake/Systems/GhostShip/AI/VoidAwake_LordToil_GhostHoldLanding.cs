using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VoidAwake
{
	public class VoidAwake_LordToil_GhostHoldLanding : LordToil
	{
		public override void UpdateAllDuties()
		{
			VoidAwake_LordJob_GhostHoldLanding job = lord.LordJob as VoidAwake_LordJob_GhostHoldLanding;
			for (int i = 0; i < lord.ownedPawns.Count; i++)
			{
				Pawn pawn = lord.ownedPawns[i];
				if (pawn == null || pawn.Destroyed || pawn.mindState == null)
				{
					continue;
				}

				IntVec3 cell = job != null ? job.LandingFor(pawn) : pawn.PositionHeld;
				if (!cell.IsValid)
				{
					cell = pawn.PositionHeld;
				}

				pawn.mindState.duty = new PawnDuty(VoidAwake_GhostShipDefOf.VoidAwake_GhostHoldLanding, cell, 1f);
			}
		}
	}
}
