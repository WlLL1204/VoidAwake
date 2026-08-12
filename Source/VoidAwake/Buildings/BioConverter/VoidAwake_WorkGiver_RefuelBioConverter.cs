using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
    public abstract class VoidAwake_WorkGiver_RefuelBioConverterBase<TComp> : WorkGiver_Scanner
        where TComp : VoidAwake_CompRefuelable_BioConverterBase
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

    public class VoidAwake_WorkGiver_RefuelTwistedMeat
        : VoidAwake_WorkGiver_RefuelBioConverterBase<VoidAwake_CompRefuelable_TwistedMeat>
    {
        public override JobDef JobDef => VoidAwake_JobDefOf.VoidAwake_RefuelTwistedMeat;
        public override string NoFuelMessage => "捻じれた肉がありません。";
    }

    public class VoidAwake_WorkGiver_RefuelDreadLeather
        : VoidAwake_WorkGiver_RefuelBioConverterBase<VoidAwake_CompRefuelable_DreadLeather>
    {
        public override JobDef JobDef => VoidAwake_JobDefOf.VoidAwake_RefuelDreadLeather;
        public override string NoFuelMessage => "ドレッドレザーがありません。";
    }
}
