using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VoidAwake
{
	[HarmonyPatch(typeof(MainTabWindow_Animals), "get_Pawns")]
	public static class MainTabWindow_Animals_Pawns_Patch
	{
		public static void Postfix(ref IEnumerable<Pawn> __result)
		{
			__result = __result.Where(p => !VoidServantUtility.IsVoidServant(p));
		}
	}
}
