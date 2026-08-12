using Verse;
using Verse.AI;

namespace VoidAwake
{
	public class VoidAwake_JobGiver_TrapperEscapeOutside : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn?.Map == null || pawn.Downed || pawn.Dead)
			{
				return null;
			}

			VoidAwake_CompTrapper comp = pawn.TryGetComp<VoidAwake_CompTrapper>();
			if (comp == null || !comp.IsStealth || !comp.WantsEscapeOutside)
			{
				return null;
			}

			if (VoidAwake_RabbitPassageUtility.CanReachOutsideNormally(pawn))
			{
				comp.Notify_EscapedOutside(pawn.Position);
				return null;
			}

			if (!VoidAwake_RabbitPassageUtility.TryFindUsePassageTowardOutside(pawn, out VoidAwake_Building_RabbitPassage entrance))
			{
				// No usable passage — abandon escape so wander can proceed.
				comp.Notify_EscapedOutside(pawn.Position);
				return null;
			}

			return VoidAwake_JobGiver_TrapperUseRabbitPassage.MakeUseJob(entrance);
		}
	}
}
