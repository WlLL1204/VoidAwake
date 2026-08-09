using HarmonyLib;
using RimWorld;
using Verse;

namespace VoidAwake
{
	[HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
	public static class Patch_VoidServantEnsureName
	{
		public static void Postfix(Pawn __instance)
		{
			if (__instance.Name != null)
			{
				return;
			}
			if (!VoidServantUtility.IsPlayerVoidServant(__instance))
			{
				return;
			}
			__instance.Name = PawnBioAndNameGenerator.GeneratePawnName(__instance, NameStyle.Numeric);
		}
	}
}
