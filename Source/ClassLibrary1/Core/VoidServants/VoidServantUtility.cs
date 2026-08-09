using RimWorld;
using Verse;

namespace VoidAwake
{
	public class ModExtension_VoidServant : DefModExtension
	{
	}

	public static class VoidServantUtility
	{
		public static bool IsVoidServant(ThingDef def)
		{
			return def?.GetModExtension<ModExtension_VoidServant>() != null;
		}

		public static bool IsVoidServant(Pawn pawn)
		{
			return pawn != null && IsVoidServant(pawn.def);
		}

		public static bool IsPlayerVoidServant(Pawn pawn)
		{
			return IsVoidServant(pawn) && pawn.Faction == Faction.OfPlayer;
		}
	}
}
