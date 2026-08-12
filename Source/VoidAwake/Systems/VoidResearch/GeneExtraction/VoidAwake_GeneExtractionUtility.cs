using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
    public static class VoidAwake_GeneExtractionUtility
    {
        public static bool ResearchFinished =>
            ModsConfig.BiotechActive
            && VoidAwake_ResearchProjectDefOf.VoidAwake_GeneExtraction != null
            && VoidAwake_ResearchProjectDefOf.VoidAwake_GeneExtraction.IsFinished;

        public static bool IsValidGeneExtractTarget(Pawn p)
        {
            if (!ModsConfig.BiotechActive || p == null || p.Dead)
                return false;
            if (!ResearchFinished)
                return false;
            if (!p.IsShambler || !p.IsOnHoldingPlatform)
                return false;
            if (p.genes == null)
                return false;
            return GetExtractableGenes(p).Count > 0;
        }

        public static List<GeneDef> GetExtractableGenes(Pawn p)
        {
            var result = new List<GeneDef>();
            if (!ModsConfig.BiotechActive || p?.genes == null)
                return result;

            foreach (Gene gene in p.genes.GenesListForReading)
            {
                if (gene?.def == null)
                    continue;
                if (!CanPutInGenepack(gene.def))
                    continue;
                if (result.Contains(gene.def))
                    continue;
                result.Add(gene.def);
            }

            result.SortBy(g => g.label);
            return result;
        }

        public static bool CanPutInGenepack(GeneDef def)
        {
            if (def == null)
                return false;
            if (!def.canGenerateInGeneSet)
                return false;
            if (def.biostatArc > 0)
                return false;
            return true;
        }

        public static Thing GetInteractionTarget(Pawn target)
        {
            if (target == null)
                return null;
            return target.SpawnedParentOrMe ?? target;
        }

        public static bool CanReachExtractTarget(Pawn worker, Pawn target, bool forced = false)
        {
            Thing interact = GetInteractionTarget(target);
            if (interact == null)
                return false;
            return worker.CanReserveAndReach(interact, PathEndMode.Touch, Danger.Deadly, 1, -1, null, forced);
        }

        public static Thing FindAvailableGeneExtractionKit(Pawn pawn)
        {
            if (pawn == null || VoidAwake_ThingDefOf.VoidAwake_GeneExtractionKit == null)
                return null;

            ThingDef kitDef = VoidAwake_ThingDefOf.VoidAwake_GeneExtractionKit;

            Thing carried = pawn.carryTracker?.CarriedThing;
            if (carried != null && carried.def == kitDef)
                return carried;

            List<Thing> found = HaulAIUtility.FindFixedIngredientCount(pawn, kitDef, 1);
            if (found != null && found.Count > 0)
                return found[0];

            if (pawn.Map != null)
            {
                Thing closest = GenClosest.ClosestThingReachable(
                    pawn.Position,
                    pawn.Map,
                    ThingRequest.ForDef(kitDef),
                    PathEndMode.ClosestTouch,
                    TraverseParms.For(pawn),
                    9999f,
                    t => !t.IsForbidden(pawn) && pawn.CanReserve(t, 1, 1));
                if (closest != null)
                    return closest;
            }

            return pawn.inventory?.innerContainer?
                .FirstOrDefault(t => t.def == kitDef);
        }

        public static Job MakeExtractGeneJob(Pawn target, Thing kit)
        {
            if (target == null || kit == null)
                return null;
            Job job = JobMaker.MakeJob(VoidAwake_JobDefOf.VoidAwake_ExtractGene, target, kit);
            job.count = 1;
            return job;
        }

        public static bool TryConsumeGeneExtractionKit(Pawn pawn)
        {
            if (pawn == null)
                return false;

            Thing carried = pawn.carryTracker?.CarriedThing;
            if (carried != null && carried.def == VoidAwake_ThingDefOf.VoidAwake_GeneExtractionKit)
            {
                pawn.carryTracker.DestroyCarriedThing();
                return true;
            }

            Thing inInv = pawn.inventory?.innerContainer?
                .FirstOrDefault(t => t.def == VoidAwake_ThingDefOf.VoidAwake_GeneExtractionKit);
            if (inInv != null)
            {
                inInv.SplitOff(1).Destroy();
                return true;
            }

            return false;
        }

        public static Thing CreateGenepack(GeneDef geneDef)
        {
            if (!ModsConfig.BiotechActive || geneDef == null)
                return null;

            var pack = (Genepack)ThingMaker.MakeThing(ThingDefOf.Genepack);
            pack.Initialize(new List<GeneDef> { geneDef });
            return pack;
        }

        public static void RemoveAllGenes(Pawn pawn)
        {
            if (pawn?.genes == null)
                return;

            var genes = pawn.genes.GenesListForReading.ToList();
            for (int i = 0; i < genes.Count; i++)
                pawn.genes.RemoveGene(genes[i]);
        }

        public static void ApplyExtractionAftermath(Pawn target, Pawn extractor)
        {
            if (target == null || target.Dead)
                return;

            RemoveAllGenes(target);
            target.Kill(new DamageInfo(DamageDefOf.ExecutionCut, 9999f, 999f, -1f, extractor));
        }

        public static AcceptanceReport CanColonistPerformExtraction(Pawn colonist, Pawn target)
        {
            if (!ModsConfig.BiotechActive || !ModsConfig.AnomalyActive)
                return "Biotech / Anomaly が必要です。";
            if (!ResearchFinished)
                return "遺伝子の抽出の研究が完了していません。";
            if (!IsValidGeneExtractTarget(target))
                return "この対象からは遺伝子を抽出できません。";
            if (colonist == null || colonist.Dead || colonist.Downed)
                return "入植者が作業できません。";
            if (!colonist.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                return "手先を使えません。";
            if (!CanReachExtractTarget(colonist, target, forced: true))
                return "収容所に到達できません。";
            if (FindAvailableGeneExtractionKit(colonist) == null)
                return "遺伝子抽出キットが必要です。";

            Map map = target.MapHeld;
            if (map != null
                && map.designationManager.DesignationOn(
                    target, VoidAwake_DesignationDefOf.VoidAwake_ExtractGene) != null)
            {
                return "すでに遺伝子抽出が指示されています。";
            }

            return AcceptanceReport.WasAccepted;
        }

        public static void DesignateGeneExtraction(Pawn target, GeneDef geneDef, Pawn orderedBy = null)
        {
            Map map = target?.MapHeld;
            if (map == null || geneDef == null)
                return;

            var mapComp = map.GetComponent<VoidAwake_MapComponent_GeneExtraction>();
            if (mapComp == null)
                return;

            var existing = map.designationManager.DesignationOn(
                target, VoidAwake_DesignationDefOf.VoidAwake_ExtractGene);
            if (existing != null)
                return;

            mapComp.SetPending(target, geneDef);
            map.designationManager.AddDesignation(
                new Designation(target, VoidAwake_DesignationDefOf.VoidAwake_ExtractGene));

            TryOrderExtractJob(target, orderedBy);
        }

        /// <summary>
        /// 指示直後に強制 Job を渡す。preferred 指定時はその入植者のみ。
        /// </summary>
        public static void TryOrderExtractJob(Pawn target, Pawn preferred = null)
        {
            Map map = target?.MapHeld;
            if (map == null)
                return;

            if (preferred != null)
            {
                // Designation 直後なので「すでに指示」は無視し、作業可否だけ見る
                Thing kit = FindAvailableGeneExtractionKit(preferred);
                if (preferred.Dead || preferred.Downed
                    || !preferred.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
                    || !CanReachExtractTarget(preferred, target, forced: true)
                    || kit == null)
                {
                    Messages.Message(
                        "選択した入植者は遺伝子抽出を実行できません（キット・到達を確認してください）。",
                        preferred, MessageTypeDefOf.RejectInput, false);
                    return;
                }

                Job job = MakeExtractGeneJob(target, kit);
                if (job == null)
                    return;
                job.playerForced = true;
                preferred.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                return;
            }

            Pawn best = null;
            Thing bestKit = null;
            float bestScore = float.MaxValue;

            foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
            {
                if (colonist.Dead || colonist.Downed)
                    continue;
                if (!colonist.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                    continue;
                if (!CanReachExtractTarget(colonist, target, forced: true))
                    continue;

                Thing kit = FindAvailableGeneExtractionKit(colonist);
                if (kit == null)
                    continue;

                float score = colonist.Position.DistanceToSquared(target.PositionHeld);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = colonist;
                    bestKit = kit;
                }
            }

            if (best == null || bestKit == null)
            {
                Messages.Message(
                    "遺伝子抽出を実行できる入植者がいません（キット・到達を確認してください）。",
                    target, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Job autoJob = MakeExtractGeneJob(target, bestKit);
            if (autoJob == null)
                return;

            autoJob.playerForced = true;
            best.jobs.TryTakeOrderedJob(autoJob, JobTag.Misc);
        }
    }
}
