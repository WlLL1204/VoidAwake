using HarmonyLib;
using UnityEngine;
using Verse;

namespace VoidAwake
{
	[HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
	public static class VoidAwake_Patch_Pawn_Kill_GhostCorpse
	{
		private static bool Prefix(Pawn __instance)
		{
			if (!VoidAwake_GhostUtility.SuppressGhostDeathOnDowned
				|| __instance == null
				|| __instance.Dead
				|| !VoidAwake_GhostUtility.IsGhostPawn(__instance))
			{
				return true;
			}

			VoidAwake_GhostUtility.RestoreGhostUntilStanding(__instance);
			return false;
		}

		private static void Postfix(Pawn __instance)
		{
			if (!__instance.Dead || !VoidAwake_GhostUtility.IsGhostPawn(__instance))
			{
				return;
			}

			Corpse corpse = __instance.Corpse;
			if (corpse == null || corpse.Destroyed)
			{
				return;
			}

			int reduced = corpse.MaxHitPoints - (corpse.MaxHitPoints / 3);
			corpse.HitPoints = Mathf.Max(1, reduced);
		}
	}
}
