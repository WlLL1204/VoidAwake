using RimWorld;
using Verse;

namespace VoidAwake
{
    [DefOf]
    public static class VoidAwake_JobDefOf
    {
        public static JobDef VoidAwake_ExtractSample;
        public static JobDef VoidAwake_ExtractGene;
        public static JobDef VoidAwake_RefuelBasicSample;
        public static JobDef VoidAwake_RefuelAdvancedSample;
        public static JobDef VoidAwake_RefuelTwistedMeat;
        public static JobDef VoidAwake_RefuelDreadLeather;
        static VoidAwake_JobDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VoidAwake_JobDefOf));
    }
}
