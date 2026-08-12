using Verse;
using Verse.AI;

namespace VoidAwake
{
	public class JobGiver_TrapperCreateRabbitPassage : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn?.Map == null || pawn.Downed || pawn.Dead)
			{
				return null;
			}

			// Digging is deliberately not gated on the trap cooldown: the trapper prepares its way
			// in while it waits outside.
			VoidAwake_TrapperComp comp = pawn.TryGetComp<VoidAwake_TrapperComp>();
			if (comp == null || !comp.IsStealth || comp.ChainPlacing || !comp.CanSearchPassageNow)
			{
				return null;
			}

			// One pair per trapper. Anything else it needs to cross is reached by reusing a passage.
			if (RabbitPassageUtility.HasOwnPassage(pawn.Map, pawn.thingIDNumber))
			{
				return null;
			}

			if (!RabbitPassageUtility.TryFindPassagePair(pawn, out IntVec3 entrance, out IntVec3 exit))
			{
				comp.Notify_PassageSearchFailed();
				return null;
			}

			IntVec3 standCell = RabbitPassageUtility.FindDigStandCell(pawn, entrance, exit);
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
