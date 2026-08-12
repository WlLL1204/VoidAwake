using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
	public class JobDriver_TrapperKidnap : JobDriver
	{
		private const TargetIndex VictimInd = TargetIndex.A;
		private const TargetIndex ExitTargetInd = TargetIndex.B;
		private const TargetIndex DigStandInd = TargetIndex.C;
		private const int DigTicks = 120;

		private KidnapExitStep exitStep;
		private IntVec3 createEntrance;
		private IntVec3 createExit;

		private Pawn Victim => job.GetTarget(VictimInd).Thing as Pawn;

		private int PassageUseTicks =>
			pawn.TryGetComp<VoidAwake_TrapperComp>()?.Props.passageUseTicks ?? 120;

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

			Toil exitLoop = Toils_General.Label();
			yield return exitLoop;

			Toil planExit = ToilMaker.MakeToil("VoidAwake_TrapperKidnap_PlanExit");
			planExit.initAction = PlanExitStep;
			planExit.defaultCompleteMode = ToilCompleteMode.Instant;
			yield return planExit;

			Toil directExit = Toils_General.Label();
			Toil createPassage = Toils_General.Label();

			yield return Toils_Jump.JumpIf(directExit, () => exitStep == KidnapExitStep.DirectExit);
			yield return Toils_Jump.JumpIf(createPassage, () => exitStep == KidnapExitStep.CreatePassage);

			Toil gotoPassage = Toils_Goto.GotoThing(ExitTargetInd, PathEndMode.Touch);
			gotoPassage.FailOnDespawnedNullOrForbidden(ExitTargetInd);
			gotoPassage.FailOn(() =>
				(job.GetTarget(ExitTargetInd).Thing as Building_VoidAwake_RabbitPassage)?.LinkedPassage == null);
			yield return gotoPassage;

			Toil usePassage = Toils_General.Wait(PassageUseTicks);
			usePassage.WithProgressBarToilDelay(ExitTargetInd);
			yield return usePassage;

			yield return Toils_General.Do(UsePassageThrough);
			yield return Toils_Jump.Jump(exitLoop);

			yield return createPassage;

			Toil gotoDig = Toils_Goto.GotoCell(DigStandInd, PathEndMode.OnCell);
			gotoDig.FailOn(() => !createEntrance.IsValid || !createExit.IsValid
				|| !RabbitPassageUtility.IsValidPassageCell(Map, createEntrance)
				|| !RabbitPassageUtility.IsValidPassageCell(Map, createExit));
			yield return gotoDig;

			Toil dig = Toils_General.Wait(DigTicks);
			dig.WithProgressBarToilDelay(ExitTargetInd);
			dig.AddFinishAction(SpawnExitPassage);
			yield return dig;

			yield return Toils_Jump.Jump(exitLoop);

			yield return directExit;
			yield return Toils_Goto.GotoCell(ExitTargetInd, PathEndMode.OnCell);
			yield return Toils_General.Do(CompleteKidnap);
		}

		private bool KidnapVictimInvalid()
		{
			Pawn victim = Victim;
			if (victim == null || victim.Dead)
			{
				return true;
			}

			return !victim.Downed && victim.Awake();
		}

		private void VerifyCarryingVictim()
		{
			Pawn victim = Victim;
			if (victim != null && pawn.carryTracker?.CarriedThing == victim)
			{
				return;
			}

			EndJobWith(JobCondition.Incompletable);
		}

		private void PlanExitStep()
		{
			VoidAwake_TrapperComp comp = pawn.TryGetComp<VoidAwake_TrapperComp>();
			if (!RabbitPassageUtility.TryPlanKidnapExitStep(pawn, comp, out KidnapExitPlan plan))
			{
				EndJobWith(JobCondition.Incompletable);
				return;
			}

			exitStep = plan.Step;
			switch (exitStep)
			{
				case KidnapExitStep.DirectExit:
					job.SetTarget(ExitTargetInd, plan.ExitCell);
					break;
				case KidnapExitStep.UsePassage:
					job.SetTarget(ExitTargetInd, plan.Passage);
					break;
				case KidnapExitStep.CreatePassage:
					createEntrance = plan.CreateEntrance;
					createExit = plan.CreateExit;
					job.SetTarget(ExitTargetInd, createEntrance);
					job.SetTarget(DigStandInd, plan.CreateStand);
					break;
			}
		}

		private void UsePassageThrough()
		{
			RabbitPassageUtility.TeleportThrough(
				pawn,
				job.GetTarget(ExitTargetInd).Thing as Building_VoidAwake_RabbitPassage);
		}

		private void SpawnExitPassage()
		{
			if (pawn == null || !pawn.Spawned)
			{
				return;
			}

			RabbitPassageUtility.TrySpawnPassagePairAndNotify(pawn, createEntrance, createExit);
		}

		private void OnKidnapJobFinished(JobCondition condition)
		{
			if (condition == JobCondition.Succeeded)
			{
				return;
			}

			VoidAwake_TrapperComp comp = pawn.TryGetComp<VoidAwake_TrapperComp>();
			if (comp == null || !comp.IsKidnap)
			{
				return;
			}

			if (pawn.carryTracker?.CarriedThing != null)
			{
				return;
			}

			comp.Notify_KidnapJobFailed();
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
