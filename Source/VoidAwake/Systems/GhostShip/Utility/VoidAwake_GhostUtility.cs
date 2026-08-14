using RimWorld;
using UnityEngine;
using Verse;

namespace VoidAwake
{
	public static class VoidAwake_GhostUtility
	{
		public const int SpawnWeightMelee = 5;
		public const int SpawnWeightMusket = 3;
		public const int SpawnWeightAcid = 2;

		public static Pawn TrySpawnGhost(Map map, IntVec3 cell, Faction faction = null)
		{
			if (map == null || !cell.InBounds(map))
			{
				return null;
			}

			Faction fac = faction ?? Faction.OfEntities;
			if (fac == null)
			{
				return null;
			}

			PawnKindDef kind = RollGhostKind();
			PawnGenerationRequest request = new PawnGenerationRequest(
				kind,
				fac,
				PawnGenerationContext.NonPlayer,
				forceGenerateNewPawn: true,
				canGeneratePawnRelations: false,
				allowFood: false,
				allowAddictions: false,
				colonistRelationChanceFactor: 0f,
				relationWithExtraPawnChanceFactor: 0f,
				allowGay: false,
				dontGiveWeapon: true,
				forceNoGear: true,
				forcedXenotype: ModsConfig.BiotechActive ? XenotypeDefOf.Baseliner : null,
				forceBaselinerChance: 1f);

			Pawn pawn = PawnGenerator.GeneratePawn(request);
			if (pawn == null)
			{
				return null;
			}

			if (!pawn.IsMutant && VoidAwake_GhostShipDefOf.VoidAwake_GhostMutant != null)
			{
				MutantUtility.SetFreshPawnAsMutant(pawn, VoidAwake_GhostShipDefOf.VoidAwake_GhostMutant);
			}

			PrepareGhostAppearance(pawn);
			EquipLoadout(pawn);

			GenSpawn.Spawn(pawn, cell, map, WipeMode.Vanish);
			pawn.Drawer?.renderer?.EnsureGraphicsInitialized();
			return pawn;
		}

		public static PawnKindDef RollGhostKind()
		{
			int roll = Rand.Range(0, SpawnWeightMelee + SpawnWeightMusket + SpawnWeightAcid);
			if (roll < SpawnWeightMelee)
			{
				return VoidAwake_GhostShipDefOf.VoidAwake_Ghost;
			}

			if (roll < SpawnWeightMelee + SpawnWeightMusket)
			{
				return VoidAwake_GhostShipDefOf.VoidAwake_GhostMusket ?? VoidAwake_GhostShipDefOf.VoidAwake_Ghost;
			}

			return VoidAwake_GhostShipDefOf.VoidAwake_GhostAcid ?? VoidAwake_GhostShipDefOf.VoidAwake_Ghost;
		}

		public static bool IsGhostPawn(Pawn pawn)
		{
			if (pawn == null)
			{
				return false;
			}

			if (pawn.mutant?.Def == VoidAwake_GhostShipDefOf.VoidAwake_GhostMutant)
			{
				return true;
			}

			PawnKindDef kind = pawn.kindDef;
			return kind == VoidAwake_GhostShipDefOf.VoidAwake_Ghost
				|| kind == VoidAwake_GhostShipDefOf.VoidAwake_GhostMusket
				|| kind == VoidAwake_GhostShipDefOf.VoidAwake_GhostAcid;
		}

		private static void PrepareGhostAppearance(Pawn pawn)
		{
			if (pawn.story != null)
			{
				pawn.story.hairDef = HairDefOf.Bald;
				pawn.story.headType = HeadTypeDefOf.Skull;
			}

			if (pawn.style != null)
			{
				pawn.style.beardDef = BeardDefOf.NoBeard;
			}

			if (ModsConfig.BiotechActive && pawn.genes != null && XenotypeDefOf.Baseliner != null)
			{
				pawn.genes.SetXenotype(XenotypeDefOf.Baseliner);
			}

			pawn.Drawer?.renderer?.SetAllGraphicsDirty();
		}

		public static void EquipLoadout(Pawn pawn)
		{
			if (pawn?.equipment == null)
			{
				return;
			}

			ThingDef weaponDef = WeaponDefFor(pawn.kindDef);
			if (weaponDef == null)
			{
				return;
			}

			if (pawn.equipment.Primary != null)
			{
				pawn.equipment.DestroyEquipment(pawn.equipment.Primary);
			}

			ThingDef stuff = weaponDef.MadeFromStuff ? GenStuff.DefaultStuffFor(weaponDef) : null;
			ThingWithComps weapon = (ThingWithComps)ThingMaker.MakeThing(weaponDef, stuff);
			pawn.equipment.AddEquipment(weapon);
		}

		private static ThingDef WeaponDefFor(PawnKindDef kind)
		{
			if (kind == VoidAwake_GhostShipDefOf.VoidAwake_GhostMusket)
			{
				return VoidAwake_GhostShipDefOf.VoidAwake_Gun_GhostMusket;
			}

			if (kind == VoidAwake_GhostShipDefOf.VoidAwake_GhostAcid)
			{
				return VoidAwake_GhostShipDefOf.VoidAwake_Weapon_AcidFlask;
			}

			return VoidAwake_GhostShipDefOf.MeleeWeapon_LongSword;
		}

		public static void LeapFrom(Pawn pawn, Vector3 start, IntVec3 dest)
		{
			if (pawn == null || !pawn.Spawned || !dest.InBounds(pawn.Map))
			{
				return;
			}

			Map map = pawn.Map;
			EffecterDef flight = DefDatabase<EffecterDef>.GetNamedSilentFail("JumpFlightEffect");
			SoundDef land = DefDatabase<SoundDef>.GetNamedSilentFail("JumpPackLand");
			PawnFlyer flyer = PawnFlyer.MakeFlyer(ThingDefOf.PawnFlyer, pawn, dest, flight, land, false, start);
			if (flyer != null)
			{
				GenSpawn.Spawn(flyer, dest, map);
			}
		}

		public static bool IsGhostInactive(Pawn pawn)
		{
			if (pawn == null || pawn.Destroyed || pawn.Dead || pawn.Downed)
			{
				return true;
			}

			if (pawn.Spawned)
			{
				return false;
			}

			return !(pawn.ParentHolder is PawnFlyer);
		}
	}
}
