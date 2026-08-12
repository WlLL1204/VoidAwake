using RimWorld;
using Verse;

namespace VoidAwake
{
    [DefOf]
    public static class VoidAwake_DesignationDefOf
    {
        public static DesignationDef VoidAwake_ExtractSample;
        public static DesignationDef VoidAwake_ExtractGene;
        static VoidAwake_DesignationDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VoidAwake_DesignationDefOf));
    }
}
