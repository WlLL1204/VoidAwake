using HarmonyLib;
using Verse;

namespace VoidAwake
{
	[HarmonyPatch(typeof(HediffComp_Invisibility), nameof(HediffComp_Invisibility.DisruptInvisibility))]
	public static class Patch_TrapperStealth_DisruptInvisibility
	{
		private static void Postfix(HediffComp_Invisibility __instance)
		{
			if (__instance?.parent?.def != VoidAwake_TrapperDefOf.VoidAwake_TrapperStealth)
			{
				return;
			}

			__instance.Pawn?.TryGetComp<VoidAwake_TrapperComp>()?.EnterCombat();
		}
	}
}
