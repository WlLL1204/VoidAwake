using HarmonyLib;
using Verse;

namespace VoidAwake
{
	[HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
	public static class VoidAwake_Patch_Pawn_PreApplyDamage_GhostExplosion
	{
		private static void Prefix(Pawn __instance, ref DamageInfo dinfo)
		{
			if (!VoidAwake_GhostUtility.IsGhostPawn(__instance)
				|| !VoidAwake_GhostUtility.IsExplosiveDamage(dinfo.Def))
			{
				return;
			}

			dinfo.SetAmount(dinfo.Amount * VoidAwake_GhostUtility.ExplosiveIncomingDamageFactor);
		}
	}
}
