using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
	public class VoidAwake_JobDriver_PlaceBearTrap : JobDriver
	{
		private const int PlaceTicks = 90;
		private const TargetIndex CellInd = TargetIndex.A;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(CellInd), job, 1, -1, null, errorOnFailed: false);
		}

		public override void Notify_Starting()
		{
			base.Notify_Starting();
			pawn.TryGetComp<VoidAwake_CompTrapper>()?.EnsureStealth();
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOn(() => !job.GetTarget(CellInd).Cell.IsValid);
			this.FailOn(() => !VoidAwake_JobGiver_TrapperPlaceTrap.IsValidTrapCell(pawn, job.GetTarget(CellInd).Cell));

			yield return Toils_Goto.GotoCell(CellInd, PathEndMode.OnCell);

			Toil place = Toils_General.Wait(PlaceTicks);
			place.WithProgressBarToilDelay(CellInd);
			place.AddFinishAction(SpawnTrapAndMaybeContinue);
			yield return place;
		}

		private void SpawnTrapAndMaybeContinue()
		{
			IntVec3 cell = job.GetTarget(CellInd).Cell;
			VoidAwake_CompTrapper comp = pawn.TryGetComp<VoidAwake_CompTrapper>();
			if (!VoidAwake_JobGiver_TrapperPlaceTrap.IsValidTrapCell(pawn, cell))
			{
				comp?.Notify_ChainEnded();
				return;
			}

			VoidAwake_TrapperUtility.ClearCellObstacles(Map, cell);

			Thing trap = ThingMaker.MakeThing(VoidAwake_TrapperDefOf.VoidAwake_BearTrap);
			trap.SetFaction(Faction.OfEntities);
			GenSpawn.Spawn(trap, cell, Map, WipeMode.Vanish);

			if (comp == null)
			{
				return;
			}

			comp.Notify_TrapPlaced(cell);

			if (!comp.CanPlaceTrapNow || !comp.ChainDoorCell.IsValid)
			{
				comp.Notify_ChainEnded();
				return;
			}

			IntVec3 next = VoidAwake_JobGiver_TrapperPlaceTrap.FindNextCellAroundDoor(pawn, comp.ChainDoorCell, cell);
			if (!next.IsValid)
			{
				comp.Notify_ChainEnded();
				return;
			}

			comp.Notify_ChainContinue();
			pawn.jobs.jobQueue.EnqueueFirst(VoidAwake_JobGiver_TrapperPlaceTrap.MakePlaceTrapJob(next));
		}
	}
}
