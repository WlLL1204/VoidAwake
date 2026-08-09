using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
    public class VoidAwake_Extraction_JobGiver : JobDriver
    {
        private Pawn Target => (Pawn)job.GetTarget(TargetIndex.A).Thing;
        private const int ExtractTicks = 180; // 3秒

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Target, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !VoidAwake_ExtractionUtility.IsExtractableAnomaly(Target));
            this.FailOn(() => !VoidAwake_ExtractionUtility.HasExtractionGear(pawn));
            this.FailOn(() => VoidAwake_ExtractionUtility.GetSampleDef(Target) == null);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            var extract = Toils_General.WaitWith(TargetIndex.A, ExtractTicks, true, true, false, TargetIndex.A);
            extract.WithProgressBarToilDelay(TargetIndex.A);
            extract.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return extract;

            yield return Toils_General.Do(() =>
            {
                var target = Target;
                var sampleDef = VoidAwake_ExtractionUtility.GetSampleDef(target);
                if (sampleDef == null) return;

                var sample = ThingMaker.MakeThing(sampleDef);
                sample.stackCount = 1;
                GenPlace.TryPlaceThing(sample, target.Position, Map, ThingPlaceMode.Near);

                var des = Map.designationManager.DesignationOn(target, VoidAwake_DesignationDefOf.VoidAwake_ExtractSample);
                if (des != null) Map.designationManager.RemoveDesignation(des);

                target.Kill(new DamageInfo(DamageDefOf.ExecutionCut, 9999f, 999f, -1f, pawn));
            });
        }
    }
}