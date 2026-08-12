using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace VoidAwake
{
	public class VoidAwake_JobDriver_DisarmBearTrap : JobDriver
	{
		private const TargetIndex VictimInd = TargetIndex.A;

		private Pawn Victim => job.GetTarget(VictimInd).Pawn;

		private int DisarmTicks => VoidAwake_BearTrapCaughtUtility.GetDisarmTicks(pawn, Victim);

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(VictimInd), job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(VictimInd);
			this.FailOn(() => !VoidAwake_BearTrapCaughtUtility.HasCaught(Victim));

			if (pawn != Victim)
			{
				yield return Toils_Goto.GotoThing(VictimInd, PathEndMode.Touch);
			}

			Toil disarm = Toils_General.Wait(DisarmTicks);
			disarm.WithProgressBarToilDelay(VictimInd);
			yield return disarm;

			yield return Toils_General.Do(() =>
			{
				if (VoidAwake_BearTrapCaughtUtility.HasCaught(Victim))
				{
					VoidAwake_BearTrapCaughtUtility.TryRemoveCaught(Victim);
				}
			});
		}
	}
}
