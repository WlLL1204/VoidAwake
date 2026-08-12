using Verse;
using Verse.AI;

namespace VoidAwake
{
	public class JobGiver_TrapperCombatUseRabbitPassage : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn?.Map == null || pawn.Downed || pawn.Dead)
			{
				return null;
			}

			VoidAwake_TrapperComp comp = pawn.TryGetComp<VoidAwake_TrapperComp>();
			if (comp == null || !comp.IsCombat)
			{
				return null;
			}

			IntVec3 goal = RabbitPassageUtility.FindBestCombatGoalWithPassages(pawn);
			if (!goal.IsValid)
			{
				return null;
			}

			if (!RabbitPassageUtility.TryFindUsePassageToward(pawn, goal, out Building_VoidAwake_RabbitPassage entrance))
			{
				return null;
			}

			return JobGiver_TrapperUseRabbitPassage.MakeUseJob(entrance);
		}
	}
}
