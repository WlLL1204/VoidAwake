using RimWorld;
using Verse;

namespace VoidAwake
{
    [DefOf]
    public static class VoidAwake_HediffDefOf
    {
        public static HediffDef VoidAwake_SpasmGasExposure;

        static VoidAwake_HediffDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VoidAwake_HediffDefOf));
    }
}
