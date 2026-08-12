using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
    public class VoidAwake_JobDriver_ExtractGene : JobDriver
    {
        private Pawn Target => (Pawn)job.GetTarget(TargetIndex.A).Thing;
        private Thing Kit => job.GetTarget(TargetIndex.B).Thing;
        private Thing InteractTarget => VoidAwake_GeneExtractionUtility.GetInteractionTarget(Target);
        private const int ExtractTicks = 180;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Thing interact = InteractTarget;
            if (interact == null)
                return false;

            // 収容所を予約（非Spawnedの対象ポーン単体予約は不安定）
            if (!pawn.Reserve(interact, job, 1, -1, null, errorOnFailed))
                return false;

            // 可能なら対象ポーンも予約
            if (Target != null && Target != interact)
                pawn.Reserve(Target, job, 1, -1, null, false);

            Thing kit = Kit;
            if (kit != null)
                return pawn.Reserve(kit, job, 1, 1, null, errorOnFailed);

            return true;
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            // Cleanup は non-virtual のため finish action で中断時の予約解放
            AddFinishAction(ReleaseReservationsIfInterrupted);
        }

        private void ReleaseReservationsIfInterrupted(JobCondition condition)
        {
            if (condition == JobCondition.Succeeded)
                return;

            Map map = pawn.MapHeld ?? InteractTarget?.Map ?? Target?.MapHeld;
            if (map?.reservationManager == null)
                return;

            // 徴兵・中断時に収容所/キット予約が残ると他が入らない
            map.reservationManager.ReleaseClaimedBy(pawn, job);
            map.reservationManager.ReleaseAllClaimedBy(pawn);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => Target == null || Target.Destroyed);
            this.FailOn(() => !Target.SpawnedOrAnyParentSpawned);
            this.FailOn(() => !VoidAwake_GeneExtractionUtility.IsValidGeneExtractTarget(Target));
            this.FailOn(() =>
            {
                var mapComp = Map.GetComponent<VoidAwake_MapComponent_GeneExtraction>();
                return mapComp?.GetPending(Target) == null;
            });
            this.FailOnDestroyedNullOrForbidden(TargetIndex.B);

            // キットを運ぶ
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch, true);
            yield return Toils_Haul.StartCarryThing(
                TargetIndex.B,
                putRemainderInQueue: false,
                subtractNumTakenFromJobCount: true,
                failIfStackCountLessThanJobCount: false,
                reserve: true,
                canTakeFromInventory: true);

            // 収容所へ（親 Spawned へ）
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, true);

            var extract = Toils_General.Wait(ExtractTicks, TargetIndex.A);
            extract.WithProgressBarToilDelay(TargetIndex.A);
            // FailOnCannotTouch(A) は非Spawnedで落ちるため、親への到達で判定
            extract.FailOn(() =>
            {
                Thing interact = InteractTarget;
                if (interact == null)
                    return true;
                return !ReachabilityImmediate.CanReachImmediate(pawn, interact, PathEndMode.Touch);
            });
            yield return extract;

            yield return Toils_General.Do(() =>
            {
                var target = Target;
                var mapComp = Map.GetComponent<VoidAwake_MapComponent_GeneExtraction>();
                GeneDef geneDef = mapComp?.GetPending(target);
                if (geneDef == null)
                    return;

                if (!VoidAwake_GeneExtractionUtility.TryConsumeGeneExtractionKit(pawn))
                    return;

                Thing pack = VoidAwake_GeneExtractionUtility.CreateGenepack(geneDef);
                if (pack != null)
                    GenPlace.TryPlaceThing(pack, target.PositionHeld, Map, ThingPlaceMode.Near);

                var des = Map.designationManager.DesignationOn(
                    target, VoidAwake_DesignationDefOf.VoidAwake_ExtractGene);
                if (des != null)
                    Map.designationManager.RemoveDesignation(des);
                mapComp?.ClearPending(target);

                VoidAwake_GeneExtractionUtility.ApplyExtractionAftermath(target, pawn);
            });
        }
    }
}
