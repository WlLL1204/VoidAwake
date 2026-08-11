using RimWorld;
using Verse;

namespace VoidAwake
{
	[DefOf]
	public static class VoidAwake_TrapperDefOf
	{
		public static JobDef VoidAwake_PlaceBearTrap;
		public static ThingDef VoidAwake_BearTrap;
		public static HediffDef VoidAwake_TrapperStealth;
		public static IncidentDef VoidAwake_TrapperArrival;
		public static PawnKindDef Trapper;
		public static FleckDef VoidAwake_TrapperFootstep;

		static VoidAwake_TrapperDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VoidAwake_TrapperDefOf));
	}
}
