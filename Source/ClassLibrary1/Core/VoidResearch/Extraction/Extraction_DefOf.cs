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
        static VoidAwake_ThingDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VoidAwake_ThingDefOf));
    }

    [DefOf]
    public static class VA_JobDefOf
    {
        public static JobDef VoidAwake_ExtractSample;
        public static JobDef VoidAwake_RefuelBasicSample;
        public static JobDef VoidAwake_RefuelAdvancedSample;

        static VA_JobDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VA_JobDefOf));
    }

    [DefOf]
    public static class VoidAwake_DesignationDefOf
    {
        public static DesignationDef VoidAwake_ExtractSample;
        static VoidAwake_DesignationDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VoidAwake_DesignationDefOf));
    }
}