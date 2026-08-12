using HarmonyLib;
using Verse;

namespace VoidAwake
{
    [StaticConstructorOnStartup]
    public static class VoidAwake_HarmonyInit
    {
        static VoidAwake_HarmonyInit()
        {
            new Harmony("Will.VoidAwake").PatchAll();
        }
    }
}
