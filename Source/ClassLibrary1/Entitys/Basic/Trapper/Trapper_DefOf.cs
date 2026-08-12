using RimWorld;
using Verse;

namespace VoidAwake
{
	[DefOf]
	public static class VoidAwake_TrapperDefOf
	{
		public static JobDef VoidAwake_PlaceBearTrap;
		public static JobDef VoidAwake_CreateRabbitPassage;
		public static JobDef VoidAwake_UseRabbitPassage;
		public static JobDef VoidAwake_DisarmBearTrap;
		public static JobDef VoidAwake_TrapperKidnap;
		public static ThingDef VoidAwake_BearTrap;
		public static ThingDef VoidAwake_RabbitPassage;
		public static ThingDef VoidAwake_Gun_RabbitShot;
		public static HediffDef VoidAwake_TrapperStealth;
		public static HediffDef VoidAwake_CaughtInTrap;
		public static HediffDef VoidAwake_TrapperKidnapping;
		public static IncidentDef VoidAwake_TrapperArrival;
		public static PawnKindDef Trapper;
		public static FleckDef VoidAwake_TrapperFootstep;

		static VoidAwake_TrapperDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VoidAwake_TrapperDefOf));
	}
}
