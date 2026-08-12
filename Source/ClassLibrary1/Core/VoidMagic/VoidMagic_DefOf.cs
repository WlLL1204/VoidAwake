using RimWorld;
using Verse;

namespace VoidAwake
{
    [DefOf]
    public static class VoidAwake_VoidMagicDefOf
    {
        public static JobDef VoidAwake_VoidMeditate;
        public static ThingDef VoidAwake_VoidMeditationSpot;
        public static VoidAwake_VoidMagicDef VoidAwake_VoidMagicDefault;

        static VoidAwake_VoidMagicDefOf() =>
            DefOfHelper.EnsureInitializedInCtor(typeof(VoidAwake_VoidMagicDefOf));
    }
}
