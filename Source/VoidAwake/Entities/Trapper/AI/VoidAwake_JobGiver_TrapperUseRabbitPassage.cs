using Verse;
using Verse.AI;

namespace VoidAwake
{
	public class VoidAwake_JobGiver_TrapperUseRabbitPassage : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn?.Map == null || pawn.Downed || pawn.Dead)
			{
				return null;
			}

			VoidAwake_CompTrapper comp = pawn.TryGetComp<VoidAwake_CompTrapper>();
			if (comp == null || !comp.IsStealth || !comp.CanPlaceTrapNow)
			{
				return null;
			}

			IntVec3 goal = VoidAwake_RabbitPassageUtility.FindBestTrapGoalWithPassages(pawn);
			if (!goal.IsValid)
			{
				return null;
			}

			if (!VoidAwake_RabbitPassageUtility.TryFindUsePassageToward(pawn, goal, out VoidAwake_Building_RabbitPassage entrance))
			{
				return null;
			}

			return MakeUseJob(entrance);
		}

		public static Job MakeUseJob(VoidAwake_Building_RabbitPassage entrance)
		{
			Job job = JobMaker.MakeJob(VoidAwake_TrapperDefOf.VoidAwake_UseRabbitPassage, entrance);
			job.canBashDoors = false;
			job.canBashFences = false;
			return job;
		}
	}
}
