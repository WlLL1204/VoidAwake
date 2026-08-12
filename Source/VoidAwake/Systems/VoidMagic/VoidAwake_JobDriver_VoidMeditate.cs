using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
    /// <summary>
    /// スポットへ行って瞑想し続けるだけのジョブ。繋がりの加算そのものは
    /// <see cref="VoidAwake_CompVoidMagic"/> 側が瞑想中の入植者を見て行う。
    /// </summary>
    public class VoidAwake_JobDriver_VoidMeditate : JobDriver
    {
        private const int MeditateTicks = 2500; // 約1時間
        private const int CheckIntervalTicks = 60;
        private const TargetIndex SpotInd = TargetIndex.A;

        private Thing Spot => job.GetTarget(SpotInd).Thing;

        private VoidAwake_CompMeditationAnchor Anchor =>
            Spot?.TryGetComp<VoidAwake_CompMeditationAnchor>();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(SpotInd), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(SpotInd);
            this.FailOn(() => Anchor == null);

            yield return Toils_Goto.GotoThing(SpotInd, PathEndMode.OnCell);

            Toil meditate = Toils_General.Wait(MeditateTicks);
            meditate.socialMode = RandomSocialMode.Off;
            meditate.tickAction += () =>
            {
                if (pawn.IsHashIntervalTick(CheckIntervalTicks))
                {
                    CheckStillWorthwhile();
                }
            };
            meditate.WithProgressBarToilDelay(SpotInd);
            yield return meditate;
        }

        private void CheckStillWorthwhile()
        {
            VoidAwake_CompMeditationAnchor anchor = Anchor;
            if (anchor == null || VoidAwake_VoidMagicUtility.GetComp(pawn) == null)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            if (!anchor.AnyEntityInRange())
            {
                Messages.Message(
                    "VoidAwake_VoidMeditateInterrupted".Translate(pawn.LabelShortCap),
                    pawn, MessageTypeDefOf.NeutralEvent, false);
                EndJobWith(JobCondition.Incompletable);
            }
        }
    }
}
