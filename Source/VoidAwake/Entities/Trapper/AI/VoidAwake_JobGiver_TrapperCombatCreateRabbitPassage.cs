using Verse;
using Verse.AI;

namespace VoidAwake
{
	public class VoidAwake_JobGiver_TrapperCombatCreateRabbitPassage : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn?.Map == null || pawn.Downed || pawn.Dead)
			{
				return null;
			}

			VoidAwake_CompTrapper comp = pawn.TryGetComp<VoidAwake_CompTrapper>();
			if (comp == null || !comp.IsCombat || !comp.CanSearchPassageNow)
			{
				return null;
			}

			if (VoidAwake_RabbitPassageUtility.HasOwnPassage(pawn.Map, pawn.thingIDNumber))
			{
				return null;
			}

			IntVec3 goal = VoidAwake_RabbitPassageUtility.FindNearestUnreachableHostilePosition(pawn);
			if (!goal.IsValid)
			{
				return null;
			}

			if (!VoidAwake_RabbitPassageUtility.TryFindCombatPassagePair(pawn, goal, out IntVec3 entrance, out IntVec3 exit))
			{
				comp.Notify_PassageSearchFailed();
				return null;
			}

			IntVec3 standCell = VoidAwake_RabbitPassageUtility.FindDigStandCell(pawn, entrance, exit);
			if (!standCell.IsValid)
			{
				comp.Notify_PassageSearchFailed();
				return null;
			}

			Job job = JobMaker.MakeJob(VoidAwake_TrapperDefOf.VoidAwake_CreateRabbitPassage, entrance);
			job.targetB = exit;
			job.targetC = standCell;
			job.canBashDoors = false;
			job.canBashFences = false;
			return job;
		}
	}
}
