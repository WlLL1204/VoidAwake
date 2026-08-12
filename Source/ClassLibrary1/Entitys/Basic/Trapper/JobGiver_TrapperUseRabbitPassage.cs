using Verse;
using Verse.AI;

namespace VoidAwake
{
	public class JobGiver_TrapperUseRabbitPassage : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn?.Map == null || pawn.Downed || pawn.Dead)
			{
				return null;
			}

			VoidAwake_TrapperComp comp = pawn.TryGetComp<VoidAwake_TrapperComp>();
			if (comp == null || !comp.IsStealth || !comp.CanPlaceTrapNow)
			{
				return null;
			}

			IntVec3 goal = RabbitPassageUtility.FindBestTrapGoalWithPassages(pawn);
			if (!goal.IsValid)
			{
				return null;
			}

			if (!RabbitPassageUtility.TryFindUsePassageToward(pawn, goal, out Building_VoidAwake_RabbitPassage entrance))
			{
				return null;
			}

			return MakeUseJob(entrance);
		}

		public static Job MakeUseJob(Building_VoidAwake_RabbitPassage entrance)
		{
			Job job = JobMaker.MakeJob(VoidAwake_TrapperDefOf.VoidAwake_UseRabbitPassage, entrance);
			job.canBashDoors = false;
			job.canBashFences = false;
			return job;
		}
	}
}
