using RimWorld;
using Verse;

namespace VoidAwake
{
    [DefOf]
    public static class VoidAwake_ThingDefOf
    {
        public static ThingDef VoidAwake_SampleExtractionKit;
        public static ThingDef VoidAwake_SampleExtractionKitBelt;
        public static ThingDef VoidAwake_BasicSample;
        public static ThingDef VoidAwake_AdvancedSample;
        public static ThingDef VoidAwake_SampleAnalysisBench;
        public static ThingDef VoidAwake_GeneExtractionKit;
        static VoidAwake_ThingDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VoidAwake_ThingDefOf));
    }
}
