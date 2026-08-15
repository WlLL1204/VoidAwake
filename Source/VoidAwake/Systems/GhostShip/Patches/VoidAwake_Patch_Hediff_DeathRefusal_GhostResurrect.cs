using HarmonyLib;
using RimWorld;
using Verse;

namespace VoidAwake
{
	[HarmonyPatch(typeof(Hediff_DeathRefusal), "Resurrect")]
	public static class VoidAwake_Patch_Hediff_DeathRefusal_GhostResurrect
	{
		private static bool Prefix(Hediff_DeathRefusal __instance)
		{
			Pawn pawn = __instance?.pawn;
			if (pawn != null && VoidAwake_GhostUtility.IsGhostPawn(pawn))
			{
				if (!pawn.Dead)
				{
					return false;
				}

				VoidAwake_GhostUtility.BeginGhostResurrectGuard();
			}

			return true;
		}

		private static void Postfix(Hediff_DeathRefusal __instance)
		{
			try
			{
				Pawn pawn = __instance?.pawn;
				if (!VoidAwake_GhostUtility.IsGhostPawn(pawn))
				{
					return;
				}

				VoidAwake_GhostUtility.AfterGhostDeathRefusalResurrect(pawn);
			}
			finally
			{
				VoidAwake_GhostUtility.EndGhostResurrectGuard();
			}
		}
	}

	[HarmonyPatch(typeof(ResurrectionUtility), nameof(ResurrectionUtility.TryResurrect))]
	public static class VoidAwake_Patch_ResurrectionUtility_GhostNoLord
	{
		private static void Prefix(Pawn pawn, ResurrectionParams parms)
		{
			if (parms == null || !VoidAwake_GhostUtility.IsGhostPawn(pawn))
			{
				return;
			}

			parms.noLord = true;
		}
	}
}
