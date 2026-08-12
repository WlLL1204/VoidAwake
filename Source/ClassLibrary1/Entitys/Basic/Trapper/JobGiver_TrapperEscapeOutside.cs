using Verse;
using Verse.AI;

namespace VoidAwake
{
	public class JobGiver_TrapperEscapeOutside : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn?.Map == null || pawn.Downed || pawn.Dead)
			{
				return null;
			}

			VoidAwake_TrapperComp comp = pawn.TryGetComp<VoidAwake_TrapperComp>();
			if (comp == null || !comp.IsStealth || !comp.WantsEscapeOutside)
			{
				return null;
			}

			if (RabbitPassageUtility.CanReachOutsideNormally(pawn))
			{
				comp.Notify_EscapedOutside(pawn.Position);
				return null;
			}

			if (!RabbitPassageUtility.TryFindUsePassageTowardOutside(pawn, out Building_VoidAwake_RabbitPassage entrance))
			{
				// No usable passage — abandon escape so wander can proceed.
				comp.Notify_EscapedOutside(pawn.Position);
				return null;
			}

			return JobGiver_TrapperUseRabbitPassage.MakeUseJob(entrance);
		}
	}
}
