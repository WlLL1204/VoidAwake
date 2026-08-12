using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
    public abstract class VoidAwake_JobDriver_RefuelBioConverterBase<TComp> : JobDriver
        where TComp : VoidAwake_CompRefuelable_BioConverterBase
    {
        private Thing Building => job.GetTarget(TargetIndex.A).Thing;
        private Thing FuelThing => job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Building, job, 1, -1, null, errorOnFailed)
                && pawn.Reserve(FuelThing, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_General.DoAtomic(() =>
            {
                var comp = Building.TryGetComp<TComp>();
                job.count = comp.GetFuelCountToFullyRefuel();
            });
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.B)
                .FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(240).FailOnDestroyedNullOrForbidden(TargetIndex.A)
                .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
                .WithProgressBarToilDelay(TargetIndex.A);
            yield return Toils_General.Do(() =>
            {
                var comp = Building.TryGetComp<TComp>();
                var carried = pawn.carryTracker.CarriedThing;
                if (comp == null || carried == null) return;
                var list = new List<Thing> { carried };
                comp.Refuel(list);
            });
        }
    }

    public class VoidAwake_JobDriver_RefuelTwistedMeat
        : VoidAwake_JobDriver_RefuelBioConverterBase<VoidAwake_CompRefuelable_TwistedMeat>
    { }

    public class VoidAwake_JobDriver_RefuelDreadLeather
        : VoidAwake_JobDriver_RefuelBioConverterBase<VoidAwake_CompRefuelable_DreadLeather>
    { }
}
