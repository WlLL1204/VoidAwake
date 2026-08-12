using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace VoidAwake
{
	public class JobDriver_UseRabbitPassage : JobDriver
	{
		private const TargetIndex PassageInd = TargetIndex.A;

		private Building_VoidAwake_RabbitPassage Passage =>
			job.GetTarget(PassageInd).Thing as Building_VoidAwake_RabbitPassage;

		private int EnterTicks =>
			pawn.TryGetComp<VoidAwake_TrapperComp>()?.Props.passageUseTicks ?? 120;

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

			yield return Toils_General.Do(TeleportThrough);
		}

		private void TeleportThrough()
		{
			Building_VoidAwake_RabbitPassage entrance = Passage;
			Building_VoidAwake_RabbitPassage linked = entrance?.LinkedPassage;
			if (entrance == null || linked == null || !pawn.Spawned)
			{
				return;
			}

			IntVec3 dest = RabbitPassageUtility.FindStandableBeside(Map, linked.Position, pawn.Position);
			if (!dest.IsValid)
			{
				dest = linked.Position;
				if (!dest.Standable(Map))
				{
					return;
				}
			}

			pawn.Position = dest;
			pawn.Notify_Teleported(false, true);
			pawn.TryGetComp<VoidAwake_TrapperComp>()?.Notify_UsedPassage(linked.Position);
		}
	}
}
