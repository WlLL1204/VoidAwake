using RimWorld;
using Verse;

namespace VoidAwake
{
    [DefOf]
    public static class VoidAwake_ThoughtDefOf
    {
        public static ThoughtDef VoidAwake_EntityEscaped;

        static VoidAwake_ThoughtDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VoidAwake_ThoughtDefOf));
    }
}
