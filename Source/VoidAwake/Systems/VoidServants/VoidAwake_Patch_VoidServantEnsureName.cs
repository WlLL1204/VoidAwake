using HarmonyLib;
using RimWorld;
using Verse;

namespace VoidAwake
{
	[HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
	public static class VoidAwake_Patch_VoidServantEnsureName
	{
		public static void Postfix(Pawn __instance)
		{
			if (!VoidAwake_VoidServantUtility.IsPlayerVoidServant(__instance))
			{
				return;
			}

			if (__instance.Name == null)
			{
				__instance.Name = PawnBioAndNameGenerator.GeneratePawnName(__instance, NameStyle.Numeric);
			}

			VoidAwake_VoidServantUtility.EnsureTamenessComplete(__instance);
		}
	}
}
