using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
    public class VoidAwake_WorkGiver_ExtractGene : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;
        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            if (pawn?.Map?.designationManager == null)
                yield break;

            foreach (Designation des in pawn.Map.designationManager.AllDesignations)
            {
                if (des.def != VoidAwake_DesignationDefOf.VoidAwake_ExtractGene)
                    continue;
                if (des.target.Thing is Pawn target)
                    yield return target;
            }
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            foreach (Designation des in pawn.Map.designationManager.AllDesignations)
            {
                if (des.def == VoidAwake_DesignationDefOf.VoidAwake_ExtractGene)
                    return false;
            }
            return true;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var target = t as Pawn;
            if (!ModsConfig.BiotechActive)
                return false;
            if (!VoidAwake_GeneExtractionUtility.IsValidGeneExtractTarget(target))
                return false;
            if (pawn.Map.designationManager.DesignationOn(
                    t, VoidAwake_DesignationDefOf.VoidAwake_ExtractGene) == null)
                return false;

            var mapComp = pawn.Map.GetComponent<VoidAwake_MapComponent_GeneExtraction>();
            if (mapComp?.GetPending(target) == null)
            {
                JobFailReason.Is("抽出する遺伝子が指定されていません。");
                return false;
            }

            // 収容所（親）基準で到達・予約判定
            if (!VoidAwake_GeneExtractionUtility.CanReachExtractTarget(pawn, target, forced))
            {
                JobFailReason.Is("収容所に到達できません。");
                return false;
            }

            if (VoidAwake_GeneExtractionUtility.FindAvailableGeneExtractionKit(pawn) == null)
            {
                JobFailReason.Is("遺伝子抽出キットが必要です。");
                return false;
            }

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var target = t as Pawn;
            Thing kit = VoidAwake_GeneExtractionUtility.FindAvailableGeneExtractionKit(pawn);
            return VoidAwake_GeneExtractionUtility.MakeExtractGeneJob(target, kit);
        }
    }
}
