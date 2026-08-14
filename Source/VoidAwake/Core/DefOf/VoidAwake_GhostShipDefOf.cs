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
		public static ThingDef VoidAwake_GhostShipExit;
		public static ThingDef VoidAwake_GhostShipWall;
		public static MapGeneratorDef VoidAwake_GhostShipInterior;
		public static TerrainDef VoidAwake_GhostShipStairs;
		public static PawnKindDef VoidAwake_Ghost;
		public static PawnKindDef VoidAwake_GhostMusket;
		public static PawnKindDef VoidAwake_GhostAcid;
		public static MutantDef VoidAwake_GhostMutant;
		public static ThingDef MeleeWeapon_LongSword;
		public static ThingDef VoidAwake_Gun_GhostMusket;
		public static ThingDef VoidAwake_Weapon_AcidFlask;
		public static ThingDef VoidAwake_AcidSlick;
		public static ThingDef Bullet_Shell_HighExplosive;
		public static DutyDef VoidAwake_GhostHoldLanding;
		public static DutyDef VoidAwake_GhostShipWander;

		static VoidAwake_GhostShipDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VoidAwake_GhostShipDefOf));
	}
}
