using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
    public abstract class VoidAwake_WorkGiver_RefuelBioConverterBase<TComp> : WorkGiver_Scanner
        where TComp : CompRefuelable
    {
        public abstract JobDef JobDef { get; }
        public abstract string NoFuelMessage { get; }

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForDef(ThingDef.Named("VoidAwake_BioConverter"));

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var comp = t.TryGetComp<TComp>();
            if (comp == null || comp.IsFull) return false;
            if (!forced && !comp.ShouldAutoRefuelNow) return false;
            if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
                return false;

            var fuel = FindBestFuel(pawn, comp);
            if (fuel == null)
            {
                JobFailReason.Is(NoFuelMessage);
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var comp = t.TryGetComp<TComp>();
            var fuel = FindBestFuel(pawn, comp);
            return JobMaker.MakeJob(JobDef, t, fuel);
        }

        private static Thing FindBestFuel(Pawn pawn, CompRefuelable comp)
        {
            var filter = comp.Props.fuelFilter;
            return GenClosest.ClosestThingReachable(
                pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver),
                PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f,
                x => !x.IsForbidden(pawn) && pawn.CanReserve(x) && filter.Allows(x));
        }
    }

    public class WorkGiver_RefuelTwistedMeat
        : VoidAwake_WorkGiver_RefuelBioConverterBase<CompRefuelable_TwistedMeat>
    {
        public override JobDef JobDef => VA_JobDefOf.VoidAwake_RefuelTwistedMeat;
        public override string NoFuelMessage => "捻じれた肉がありません。";
    }

    public class WorkGiver_RefuelDreadLeather
        : VoidAwake_WorkGiver_RefuelBioConverterBase<CompRefuelable_DreadLeather>
    {
        public override JobDef JobDef => VA_JobDefOf.VoidAwake_RefuelDreadLeather;
        public override string NoFuelMessage => "ドレッドレザーがありません。";
    }

    public abstract class JobDriver_RefuelBioConverterBase<TComp> : JobDriver
        where TComp : CompRefuelable
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

    public class JobDriver_RefuelTwistedMeat
        : JobDriver_RefuelBioConverterBase<CompRefuelable_TwistedMeat>
    { }

    public class JobDriver_RefuelDreadLeather
        : JobDriver_RefuelBioConverterBase<CompRefuelable_DreadLeather>
    { }
}