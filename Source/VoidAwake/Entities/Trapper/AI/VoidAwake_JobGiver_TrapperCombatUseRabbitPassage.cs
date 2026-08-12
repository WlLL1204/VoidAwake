using Verse;
using Verse.AI;

namespace VoidAwake
{
	public class VoidAwake_JobGiver_TrapperCombatUseRabbitPassage : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn?.Map == null || pawn.Downed || pawn.Dead)
			{
				return null;
			}

			VoidAwake_CompTrapper comp = pawn.TryGetComp<VoidAwake_CompTrapper>();
			if (comp == null || !comp.IsCombat)
			{
				return null;
			}

			IntVec3 goal = VoidAwake_RabbitPassageUtility.FindBestCombatGoalWithPassages(pawn);
			if (!goal.IsValid)
			{
				return null;
			}

			if (!VoidAwake_RabbitPassageUtility.TryFindUsePassageToward(pawn, goal, out VoidAwake_Building_RabbitPassage entrance))
			{
				return null;
			}

			return VoidAwake_JobGiver_TrapperUseRabbitPassage.MakeUseJob(entrance);
		}
	}
}
