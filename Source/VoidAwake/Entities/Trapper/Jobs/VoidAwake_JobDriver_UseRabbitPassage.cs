using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace VoidAwake
{
	public class VoidAwake_JobDriver_UseRabbitPassage : JobDriver
	{
		private const TargetIndex PassageInd = TargetIndex.A;

		private VoidAwake_Building_RabbitPassage Passage =>
			job.GetTarget(PassageInd).Thing as VoidAwake_Building_RabbitPassage;

		private int EnterTicks =>
			pawn.TryGetComp<VoidAwake_CompTrapper>()?.Props.passageUseTicks ?? 120;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(PassageInd);
			this.FailOn(() => Passage?.LinkedPassage == null);

			yield return Toils_Goto.GotoThing(PassageInd, PathEndMode.Touch);

			Toil enter = Toils_General.Wait(EnterTicks);
			enter.WithProgressBarToilDelay(PassageInd);
			yield return enter;

			yield return Toils_General.Do(UsePassage);
		}

		private void UsePassage()
		{
			VoidAwake_RabbitPassageUtility.TeleportThrough(pawn, Passage);
		}
	}
}
