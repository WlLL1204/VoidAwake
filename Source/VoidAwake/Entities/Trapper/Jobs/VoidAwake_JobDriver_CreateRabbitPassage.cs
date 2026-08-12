using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace VoidAwake
{
	public class VoidAwake_JobDriver_CreateRabbitPassage : JobDriver
	{
		private const int DigTicks = 120;
		private const TargetIndex EntranceInd = TargetIndex.A;
		private const TargetIndex ExitInd = TargetIndex.B;
		private const TargetIndex StandInd = TargetIndex.C;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(EntranceInd), job, 1, -1, null, errorOnFailed: false)
				&& pawn.Reserve(job.GetTarget(ExitInd), job, 1, -1, null, errorOnFailed: false);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOn(() => !job.GetTarget(EntranceInd).Cell.IsValid
				|| !job.GetTarget(ExitInd).Cell.IsValid
				|| !job.GetTarget(StandInd).Cell.IsValid);
			this.FailOn(() => !VoidAwake_RabbitPassageUtility.IsValidPassageCell(Map, job.GetTarget(EntranceInd).Cell)
				|| !VoidAwake_RabbitPassageUtility.IsValidPassageCell(Map, job.GetTarget(ExitInd).Cell));

			// Dig from a neighbouring cell: the holes are impassable, so standing on one would
			// seal the trapper in and break the running job.
			yield return Toils_Goto.GotoCell(StandInd, PathEndMode.OnCell);

			Toil dig = Toils_General.Wait(DigTicks);
			dig.WithProgressBarToilDelay(EntranceInd);
			dig.AddFinishAction(SpawnPair);
			yield return dig;
		}

		private void SpawnPair()
		{
			if (pawn == null || !pawn.Spawned || pawn.Map == null || job == null)
			{
				return;
			}

			IntVec3 entrance = job.GetTarget(EntranceInd).Cell;
			IntVec3 exit = job.GetTarget(ExitInd).Cell;
			VoidAwake_RabbitPassageUtility.TrySpawnPassagePairAndNotify(pawn, entrance, exit);
		}
	}
}
