using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
	public class JobDriver_TrapperKidnap : JobDriver
	{
		private const TargetIndex VictimInd = TargetIndex.A;
		private const TargetIndex ExitCellInd = TargetIndex.B;

		private Pawn Victim => job.GetTarget(VictimInd).Thing as Pawn;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(VictimInd), job, 1, -1, null, errorOnFailed);
		}

		public override void Notify_Starting()
		{
			base.Notify_Starting();
			AddFinishAction(OnKidnapJobFinished);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedOrNull(VictimInd);
			this.FailOn(KidnapVictimInvalid);

			Toil gotoVictim = Toils_Goto.GotoThing(VictimInd, PathEndMode.ClosestTouch);
			gotoVictim.FailOnSomeonePhysicallyInteracting(VictimInd);
			yield return gotoVictim;

			Toil uninstall = Toils_Construct.UninstallIfMinifiable(VictimInd);
			uninstall.FailOnSomeonePhysicallyInteracting(VictimInd);
			yield return uninstall;

			yield return Toils_Haul.StartCarryThing(VictimInd, false, false, false);

			Toil verifyCarry = ToilMaker.MakeToil("VoidAwake_TrapperKidnap_VerifyCarry");
			verifyCarry.initAction = VerifyCarryingVictim;
			verifyCarry.defaultCompleteMode = ToilCompleteMode.Instant;
			yield return verifyCarry;

			yield return Toils_General.Do(FindExitCell);
			yield return Toils_Goto.GotoCell(ExitCellInd, PathEndMode.OnCell);
			yield return Toils_General.Do(CompleteKidnap);
		}

		private bool KidnapVictimInvalid()
		{
			Pawn victim = Victim;
			if (victim == null || victim.Dead)
			{
				NotifyKidnapJobFailed();
				return true;
			}

			if (!victim.Downed && victim.Awake())
			{
				NotifyKidnapJobFailed();
				return true;
			}

			return false;
		}

		private void VerifyCarryingVictim()
		{
			Pawn victim = Victim;
			if (victim != null && pawn.carryTracker?.CarriedThing == victim)
			{
				return;
			}

			NotifyKidnapJobFailed();
			EndJobWith(JobCondition.Incompletable);
		}

		private void NotifyKidnapJobFailed()
		{
			if (pawn.TryGetComp<VoidAwake_TrapperComp>()?.IsKidnap == true)
			{
				pawn.TryGetComp<VoidAwake_TrapperComp>()?.Notify_KidnapJobFailed();
			}
		}

		private void OnKidnapJobFinished(JobCondition condition)
		{
			if (condition == JobCondition.Succeeded)
			{
				return;
			}

			if (pawn.carryTracker?.CarriedThing != null)
			{
				return;
			}

			NotifyKidnapJobFailed();
		}

		private void FindExitCell()
		{
			IntVec3 cell;
			if (!CellFinder.TryFindRandomPawnExitCell(pawn, out cell))
			{
				cell = CellFinder.RandomEdgeCell(Map);
			}

			job.SetTarget(ExitCellInd, cell);
		}

		private void CompleteKidnap()
		{
			Pawn victim = pawn.carryTracker?.CarriedThing as Pawn ?? Victim;
			Map map = Map;
			if (victim != null && map != null)
			{
				GameComponent_VoidAwake_TrapperKidnaps.Get()?.RegisterKidnap(victim, pawn, map);
			}

			pawn.TryGetComp<VoidAwake_TrapperComp>()?.PrepareExitAfterKidnap();

			if (pawn.Spawned)
			{
				pawn.ExitMap(true, Rot4.Random);
			}
		}
	}
}
