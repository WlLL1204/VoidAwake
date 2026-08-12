using Verse;
using Verse.AI;

namespace VoidAwake
{
	public class JobGiver_TrapperKidnap : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn?.Map == null || pawn.Dead)
			{
				return null;
			}

			VoidAwake_TrapperComp comp = pawn.TryGetComp<VoidAwake_TrapperComp>();
			if (comp == null)
			{
				return null;
			}

			if (!comp.CanGiveKidnapJobNow)
			{
				return null;
			}

			if (pawn.jobs?.curJob?.def == VoidAwake_TrapperDefOf.VoidAwake_TrapperKidnap)
			{
				return null;
			}

			if (pawn.carryTracker?.CarriedThing != null)
			{
				return null;
			}

			if (!VoidAwake_TrapperKidnapUtility.TryFindKidnapTarget(pawn, out Pawn victim))
			{
				if (comp.IsKidnap)
				{
					comp.ExitKidnap();
				}

				return null;
			}

			if (comp.IsStealth)
			{
				comp.EnterKidnap(victim);
			}
			else if (comp.IsKidnap && comp.KidnapTarget != victim)
			{
				comp.SetKidnapTarget(victim);
			}

			Job job = JobMaker.MakeJob(VoidAwake_TrapperDefOf.VoidAwake_TrapperKidnap, victim);
			job.count = 1;
			comp.Notify_KidnapJobStarted();
			return job;
		}
	}
}
