using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
    public class VoidAwake_WorkGiver_ExtractSample : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;
        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var target = t as Pawn;
            if (!VoidAwake_ExtractionUtility.IsExtractableAnomaly(target))
                return false;
            if (VoidAwake_ExtractionUtility.GetSampleDef(target) == null)
                return false;
            if (pawn.Map.designationManager.DesignationOn(t, VoidAwake_DesignationDefOf.VoidAwake_ExtractSample) == null)
                return false;
            if (!pawn.CanReserve(t, 1, -1, null, forced))
                return false;
            if (!VoidAwake_ExtractionUtility.HasExtractionGear(pawn))
            {
                JobFailReason.Is("抽出キットか抽出ベルトが必要です。");
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(VoidAwake_JobDefOf.VoidAwake_ExtractSample, t);
        }
    }
}