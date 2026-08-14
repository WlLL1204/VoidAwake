using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VoidAwake
{
	public class VoidAwake_LordToil_GhostShipWander : LordToil
	{
		public override void UpdateAllDuties()
		{
			IntVec3 center = lord.Map != null ? lord.Map.Center : IntVec3.Invalid;
			DutyDef dutyDef = VoidAwake_GhostShipDefOf.VoidAwake_GhostShipWander;
			if (dutyDef == null)
			{
				return;
			}

			for (int i = 0; i < lord.ownedPawns.Count; i++)
			{
				Pawn pawn = lord.ownedPawns[i];
				if (pawn?.mindState == null)
				{
					continue;
				}

				pawn.mindState.duty = new PawnDuty(dutyDef, center, 40f);
			}
		}
	}
}
