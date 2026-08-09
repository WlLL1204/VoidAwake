using System.Linq;
using RimWorld;
using Verse;

namespace VoidAwake
{
    public static class VoidAwake_ExtractionUtility
    {
        public static bool HasExtractionGear(Pawn pawn)
        {
            if (pawn?.equipment?.Primary?.def == VoidAwake_ThingDefOf.VoidAwake_SampleExtractionKit)
                return true;
            return pawn?.apparel != null
                && pawn.apparel.WornApparel.Any(a =>
                    a.def == VoidAwake_ThingDefOf.VoidAwake_SampleExtractionKitBelt);
        }

        public static bool IsExtractableAnomaly(Pawn p)
        {
            if (p == null || p.Dead || !p.Downed || p.RaceProps.Humanlike)
                return false;
            // 収容可能なエンティティ＝アノマリー判定
            return p.TryGetComp<CompHoldingPlatformTarget>() != null;
        }

        public static ThingDef GetSampleDef(Pawn target)
        {
            var studiable = target.TryGetComp<CompStudiable>();
            var cat = studiable?.KnowledgeCategory;
            if (cat == KnowledgeCategoryDefOf.Basic)
                return VoidAwake_ThingDefOf.VoidAwake_BasicSample;
            if (cat == KnowledgeCategoryDefOf.Advanced)
                return VoidAwake_ThingDefOf.VoidAwake_AdvancedSample;
            if (cat != null)
                return VoidAwake_ThingDefOf.VoidAwake_AdvancedSample;
            return null;
        }
    }
}