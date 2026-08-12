using Verse;
using Verse.AI;

namespace VoidAwake
{
	/// <summary>Wander near the trapper's outside wait anchor when set.</summary>
	public class VoidAwake_JobGiver_TrapperWanderOutside : JobGiver_Wander
	{
		public VoidAwake_JobGiver_TrapperWanderOutside()
		{
			wanderRadius = 6f;
			ticksBetweenWandersRange = new IntRange(180, 720);
			locomotionUrgency = LocomotionUrgency.Walk;
			maxDanger = Danger.Deadly;
			expiryInterval = -1;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			VoidAwake_CompTrapper comp = pawn.TryGetComp<VoidAwake_CompTrapper>();
			if (comp == null || !comp.IsStealth)
			{
				return null;
			}

			return base.TryGiveJob(pawn);
		}

		protected override IntVec3 GetWanderRoot(Pawn pawn)
		{
			VoidAwake_CompTrapper comp = pawn.TryGetComp<VoidAwake_CompTrapper>();
			if (comp != null && comp.WaitAnchorCell.IsValid)
			{
				return comp.WaitAnchorCell;
			}

			return pawn.Position;
		}
	}
}
