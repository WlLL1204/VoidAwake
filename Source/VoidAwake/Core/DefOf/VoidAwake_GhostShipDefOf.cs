using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
	[DefOf]
	public static class VoidAwake_GhostShipDefOf
	{
		public static IncidentDef VoidAwake_GhostShipArrival;
		public static ThingDef VoidAwake_GhostShip;
		public static PawnKindDef VoidAwake_Ghost;
		public static MutantDef VoidAwake_GhostMutant;
		public static ThingDef MeleeWeapon_LongSword;
		public static ThingDef Bullet_Shell_HighExplosive;
		public static DutyDef VoidAwake_GhostHoldLanding;

		static VoidAwake_GhostShipDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VoidAwake_GhostShipDefOf));
	}
}
