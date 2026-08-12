using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
    public abstract class VoidAwake_WorkGiver_RefuelSampleBase<TComp> : WorkGiver_Scanner
        where TComp : VoidAwake_CompRefuelable_SampleBase
    {
        public abstract JobDef JobDef { get; }
        public override PathEndMode PathEndMode => PathEndMode.Touch;
        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForDef(ThingDef.Named("VoidAwake_SampleAnalysisBench"));

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
                JobFailReason.Is("サンプルがありません。");
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

    public class VoidAwake_WorkGiver_RefuelBasicSample
        : VoidAwake_WorkGiver_RefuelSampleBase<VoidAwake_CompRefuelable_BasicSample>
    {
        public override JobDef JobDef => VoidAwake_JobDefOf.VoidAwake_RefuelBasicSample;
    }

    public class VoidAwake_WorkGiver_RefuelAdvancedSample
        : VoidAwake_WorkGiver_RefuelSampleBase<VoidAwake_CompRefuelable_AdvancedSample>
    {
        public override JobDef JobDef => VoidAwake_JobDefOf.VoidAwake_RefuelAdvancedSample;
    }
}
