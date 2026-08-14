using RimWorld;
using UnityEngine;
using Verse;

namespace VoidAwake
{
	public static class VoidAwake_GhostUtility
	{
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

			PawnGenerationRequest request = new PawnGenerationRequest(
				VoidAwake_GhostShipDefOf.VoidAwake_Ghost,
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
			EquipLongSword(pawn);

			GenSpawn.Spawn(pawn, cell, map, WipeMode.Vanish);
			pawn.Drawer?.renderer?.EnsureGraphicsInitialized();
			return pawn;
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

		public static void EquipLongSword(Pawn pawn)
		{
			if (pawn?.equipment == null)
			{
				return;
			}

			ThingDef swordDef = VoidAwake_GhostShipDefOf.MeleeWeapon_LongSword;
			if (swordDef == null)
			{
				return;
			}

			if (pawn.equipment.Primary != null)
			{
				pawn.equipment.DestroyEquipment(pawn.equipment.Primary);
			}

			ThingWithComps sword = (ThingWithComps)ThingMaker.MakeThing(swordDef, GenStuff.DefaultStuffFor(swordDef));
			pawn.equipment.AddEquipment(sword);
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

			// ジャンプ中は flyer が保持しているので生存扱い
			return !(pawn.ParentHolder is PawnFlyer);
		}
	}
}
