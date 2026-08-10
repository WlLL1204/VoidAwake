using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidAwake
{
    public static class VoidAwake_GeneExtractionGizmos
    {
        private static readonly Color PendingGreen = new Color(0.35f, 0.95f, 0.4f);
        private static Texture2D cachedIcon;

        private static Texture2D FallbackIcon
        {
            get
            {
                if (cachedIcon == null)
                {
                    cachedIcon = ContentFinder<Texture2D>.Get("UI/Commands/ExtractGenes", false);
                    if (cachedIcon == null)
                        cachedIcon = ContentFinder<Texture2D>.Get("UI/Designators/Hunt", true);
                }
                return cachedIcon;
            }
        }

        public static IEnumerable<Gizmo> GetGizmosFor(Pawn target)
        {
            if (!ModsConfig.BiotechActive || !ModsConfig.AnomalyActive)
                yield break;
            if (target == null)
                yield break;

            // 収容中は Map が null。MapHeld を使う
            Map map = target.MapHeld;
            if (map == null)
                yield break;

            var mapComp = map.GetComponent<MapComponent_VoidAwakeGeneExtraction>();
            GeneDef pending = mapComp?.GetPending(target);

            if (pending != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "抽出予定: " + pending.LabelCap,
                    defaultDesc =
                        "この遺伝子の抽出が指示されています。\n遺伝子抽出キットを消費し、選んだ遺伝子のパックを得たうえで対象の全遺伝子を削除し、死亡させます。",
                    icon = pending.Icon ?? FallbackIcon,
                    defaultIconColor = PendingGreen,
                    Disabled = true,
                    disabledReason = "この遺伝子の抽出が指示されています。"
                };
                yield break;
            }

            if (!VoidAwake_GeneExtractionUtility.IsValidGeneExtractTarget(target))
                yield break;

            yield return new Command_Action
            {
                defaultLabel = "遺伝子を抽出",
                defaultDesc =
                    "遺伝子抽出キットを消費して、選んだ遺伝子1つを遺伝子パックとして抽出します。対象の全遺伝子は削除され、死亡します。",
                icon = FallbackIcon,
                action = () =>
                {
                    var genes = VoidAwake_GeneExtractionUtility.GetExtractableGenes(target);
                    if (genes.Count == 0)
                    {
                        Messages.Message("抽出できる遺伝子がありません。",
                            target, MessageTypeDefOf.RejectInput, false);
                        return;
                    }
                    Find.WindowStack.Add(new Dialog_VoidAwakeGeneExtraction(target, genes));
                }
            };
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_GetGizmos_GeneExtraction
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (Gizmo g in __result)
                yield return g;

            foreach (Gizmo g in VoidAwake_GeneExtractionGizmos.GetGizmosFor(__instance))
                yield return g;
        }
    }

    [HarmonyPatch(typeof(Building_HoldingPlatform), nameof(Building_HoldingPlatform.GetGizmos))]
    public static class Patch_HoldingPlatform_GetGizmos_GeneExtraction
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Building_HoldingPlatform __instance)
        {
            foreach (Gizmo g in __result)
                yield return g;

            Pawn held = __instance.HeldPawn;
            if (held == null)
                yield break;

            foreach (Gizmo g in VoidAwake_GeneExtractionGizmos.GetGizmosFor(held))
                yield return g;
        }
    }

    [HarmonyPatch(typeof(Building_HoldingPlatform), nameof(Building_HoldingPlatform.GetFloatMenuOptions))]
    public static class Patch_HoldingPlatform_GetFloatMenuOptions_GeneExtraction
    {
        public static IEnumerable<FloatMenuOption> Postfix(
            IEnumerable<FloatMenuOption> __result,
            Building_HoldingPlatform __instance,
            Pawn selPawn)
        {
            foreach (FloatMenuOption opt in __result)
                yield return opt;

            if (!ModsConfig.BiotechActive || !ModsConfig.AnomalyActive)
                yield break;
            if (selPawn == null || !selPawn.IsColonistPlayerControlled)
                yield break;

            Pawn held = __instance.HeldPawn;
            if (held == null)
                yield break;

            AcceptanceReport can = VoidAwake_GeneExtractionUtility.CanColonistPerformExtraction(selPawn, held);
            string label = "遺伝子を抽出（" + held.LabelShort + "）";

            if (!can.Accepted)
            {
                yield return new FloatMenuOption(
                    label + "（" + can.Reason + "）",
                    null);
                yield break;
            }

            yield return new FloatMenuOption(label, () =>
            {
                List<FloatMenuOption> geneOptions = new List<FloatMenuOption>();
                foreach (GeneDef gene in VoidAwake_GeneExtractionUtility.GetExtractableGenes(held))
                {
                    GeneDef localGene = gene;
                    geneOptions.Add(new FloatMenuOption(
                        localGene.LabelCap,
                        () => VoidAwake_GeneExtractionUtility.DesignateGeneExtraction(
                            held, localGene, orderedBy: selPawn),
                        localGene.Icon,
                        localGene.IconColor,
                        MenuOptionPriority.Default,
                        null,
                        held));
                }

                if (geneOptions.Count == 0)
                {
                    Messages.Message("抽出できる遺伝子がありません。",
                        held, MessageTypeDefOf.RejectInput, false);
                    return;
                }

                Find.WindowStack.Add(new FloatMenu(geneOptions));
            });
        }
    }
}
