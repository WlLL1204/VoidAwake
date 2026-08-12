using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VoidAwake
{
	[HarmonyPatch(typeof(MainTabWindow_Animals), "get_Pawns")]
	public static class VoidAwake_Patch_MainTabWindow_Animals_Pawns
	{
		public static void Postfix(ref IEnumerable<Pawn> __result)
		{
			__result = __result.Where(p => !VoidAwake_VoidServantUtility.IsVoidServant(p));
		}
	}
}
