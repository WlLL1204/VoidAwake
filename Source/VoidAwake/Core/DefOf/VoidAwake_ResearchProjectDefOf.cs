using RimWorld;
using Verse;

namespace VoidAwake
{
    [DefOf]
    public static class VoidAwake_ResearchProjectDefOf
    {
        public static ResearchProjectDef VoidAwake_GeneExtraction;
        static VoidAwake_ResearchProjectDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VoidAwake_ResearchProjectDefOf));
    }
}
