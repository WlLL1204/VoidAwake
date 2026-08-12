using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
	public static class VoidAwake_BearTrapCaughtUtility
	{
		public const int HelperDisarmTicks = 120;
		public const int SelfDisarmTicks = HelperDisarmTicks * 6;

		public static int GetDisarmTicks(Pawn worker, Pawn victim)
		{
			return worker == victim ? SelfDisarmTicks : HelperDisarmTicks;
		}

		public static bool HasCaught(Pawn pawn)
		{
			return pawn != null
				&& !pawn.Dead
				&& pawn.health?.hediffSet?.HasHediff(VoidAwake_TrapperDefOf.VoidAwake_CaughtInTrap) == true;
		}

		public static void TryApplyCaught(Pawn pawn)
		{
			if (pawn == null || pawn.Dead || pawn.kindDef?.immuneToTraps == true)
			{
				return;
			}

			if (HasCaught(pawn))
			{
				return;
			}

			pawn.health.AddHediff(VoidAwake_TrapperDefOf.VoidAwake_CaughtInTrap);
			if (!pawn.Downed)
			{
				pawn.health.forceDowned = true;
			}

			if (pawn.jobs?.curJob != null)
			{
				pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
			}
		}

		public static void TryRemoveCaught(Pawn pawn)
		{
			if (pawn?.health?.hediffSet == null)
			{
				return;
			}

			Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(VoidAwake_TrapperDefOf.VoidAwake_CaughtInTrap);
			if (hediff != null)
			{
				pawn.health.RemoveHediff(hediff);
			}
		}

		public static AcceptanceReport CanDisarm(Pawn worker, Pawn victim)
		{
			if (worker == null || victim == null)
			{
				return false;
			}

			if (!HasCaught(victim))
			{
				return false;
			}

			if (!worker.IsColonistPlayerControlled)
			{
				return false;
			}

			if (worker.Downed || worker.Dead || !worker.Spawned)
			{
				return false;
			}

			if (victim.Dead || !victim.Spawned)
			{
				return false;
			}

			if (worker == victim)
			{
				return true;
			}

			if (!worker.CanReach(victim, PathEndMode.Touch, Danger.Deadly))
			{
				return "NoPath".Translate();
			}

			return true;
		}

		public static void StartDisarmJob(Pawn worker, Pawn victim)
		{
			AcceptanceReport can = CanDisarm(worker, victim);
			if (!can.Accepted)
			{
				if (!can.Reason.NullOrEmpty())
				{
					Messages.Message(can.Reason, worker, MessageTypeDefOf.RejectInput, false);
				}

				return;
			}

			Job job = JobMaker.MakeJob(VoidAwake_TrapperDefOf.VoidAwake_DisarmBearTrap, victim);
			worker.jobs.TryTakeOrderedJob(job);
		}
	}
}
