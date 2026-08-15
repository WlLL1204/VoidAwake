using System.Collections.Generic;
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

		private static readonly List<Pawn> tmpGhosts = new List<Pawn>();

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
			GiveMortalCurse(pawn);

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
			if (kind == VoidAwake_GhostShipDefOf.VoidAwake_Ghost
				|| kind == VoidAwake_GhostShipDefOf.VoidAwake_GhostMusket
				|| kind == VoidAwake_GhostShipDefOf.VoidAwake_GhostAcid)
			{
				return true;
			}

			HediffDef mortalDef = VoidAwake_GhostShipDefOf.VoidAwake_Mortal;
			return mortalDef != null && pawn.health?.hediffSet != null && pawn.health.hediffSet.HasHediff(mortalDef);
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

		public static void GiveMortalCurse(Pawn pawn)
		{
			if (pawn?.health?.hediffSet == null)
			{
				return;
			}

			HediffDef mortalDef = VoidAwake_GhostShipDefOf.VoidAwake_Mortal;
			if (mortalDef != null && !pawn.health.hediffSet.HasHediff(mortalDef))
			{
				pawn.health.AddHediff(mortalDef);
			}

			VoidAwake_Hediff_Mortal mortal = GetMortal(pawn);
			if (mortal != null)
			{
				mortal.RefusalUses = VoidAwake_Hediff_Mortal.InitialRefusalUses;
			}

			SyncDeathRefusalHediff(pawn, VoidAwake_Hediff_Mortal.InitialRefusalUses);
		}

		public static VoidAwake_Hediff_Mortal GetMortal(Pawn pawn)
		{
			HediffDef mortalDef = VoidAwake_GhostShipDefOf.VoidAwake_Mortal;
			if (pawn?.health?.hediffSet == null || mortalDef == null)
			{
				return null;
			}

			return pawn.health.hediffSet.GetFirstHediffOfDef(mortalDef) as VoidAwake_Hediff_Mortal;
		}

		public static int GetDeathRefusalUses(Pawn pawn)
		{
			Hediff_DeathRefusal refusal = pawn?.health?.hediffSet?.GetFirstHediff<Hediff_DeathRefusal>();
			if (refusal != null)
			{
				return refusal.UsesLeft;
			}

			VoidAwake_Hediff_Mortal mortal = GetMortal(pawn);
			return mortal?.RefusalUses ?? 0;
		}

		public static void ConsumeOneDeathRefusal(Pawn pawn)
		{
			Hediff_DeathRefusal refusal = pawn?.health?.hediffSet?.GetFirstHediff<Hediff_DeathRefusal>();
			if (refusal != null)
			{
				refusal.AIEnabled = true;
				refusal.SetUseAmountDirect(Mathf.Max(0, refusal.UsesLeft - 1), true);
			}

			VoidAwake_Hediff_Mortal mortal = GetMortal(pawn);
			if (mortal != null)
			{
				mortal.RefusalUses = mortal.RefusalUses - 1;
			}
		}

		public static void AddOneDeathRefusal(Pawn pawn)
		{
			if (pawn?.health?.hediffSet == null)
			{
				return;
			}

			VoidAwake_Hediff_Mortal mortal = GetMortal(pawn);
			if (mortal == null)
			{
				HediffDef mortalDef = VoidAwake_GhostShipDefOf.VoidAwake_Mortal;
				if (mortalDef != null)
				{
					pawn.health.AddHediff(mortalDef);
					mortal = GetMortal(pawn);
				}
			}

			int uses = (mortal != null ? mortal.RefusalUses : GetDeathRefusalUses(pawn)) + 1;
			if (mortal != null)
			{
				mortal.RefusalUses = uses;
			}

			SyncDeathRefusalHediff(pawn, uses);
		}

		public static void SyncDeathRefusalHediff(Pawn pawn, int uses)
		{
			if (pawn?.health?.hediffSet == null)
			{
				return;
			}

			HediffDef refusalDef = DefDatabase<HediffDef>.GetNamedSilentFail("DeathRefusal");
			if (refusalDef == null)
			{
				return;
			}

			Hediff_DeathRefusal refusal = pawn.health.hediffSet.GetFirstHediff<Hediff_DeathRefusal>();
			if (uses <= 0)
			{
				if (refusal != null)
				{
					pawn.health.RemoveHediff(refusal);
				}

				return;
			}

			if (refusal == null)
			{
				Hediff added = pawn.health.AddHediff(refusalDef);
				refusal = added as Hediff_DeathRefusal;
			}

			if (refusal != null)
			{
				refusal.AIEnabled = true;
				refusal.SetUseAmountDirect(uses, true);
			}
		}

		public static bool CanGhostCorpseResurrect(Pawn pawn)
		{
			if (pawn == null || pawn.Destroyed || !pawn.Dead)
			{
				return false;
			}

			if (GetDeathRefusalUses(pawn) <= 0)
			{
				return false;
			}

			if (pawn.ParentHolder is PawnFlyer)
			{
				return true;
			}

			Corpse corpse = pawn.ParentHolder as Corpse ?? pawn.Corpse;
			return corpse != null && !corpse.Destroyed;
		}

		public static bool IsHeldByFlyer(Pawn pawn)
		{
			if (pawn == null)
			{
				return false;
			}

			if (pawn.ParentHolder is PawnFlyer)
			{
				return true;
			}

			Corpse corpse = pawn.ParentHolder as Corpse ?? pawn.Corpse;
			return corpse?.ParentHolder is PawnFlyer;
		}

		public static IntVec3 FindGhostResurrectCell(Map map, IntVec3 near)
		{
			if (map == null)
			{
				return IntVec3.Invalid;
			}

			if (near.IsValid && near.InBounds(map) && near.Standable(map)
				&& !VoidAwake_GhostShipOceanUtility.IsOceanTerrain(near.GetTerrain(map)))
			{
				return near;
			}

			return VoidAwake_GhostShipOceanUtility.FindLandSpawnNear(map, near.IsValid ? near : map.Center, 20);
		}

		public static bool TryResurrectGhostFromCorpse(Pawn pawn, bool consumeDeathRefusal)
		{
			if (pawn == null || pawn.Destroyed || !pawn.Dead)
			{
				return false;
			}

			if (IsHeldByFlyer(pawn))
			{
				return false;
			}

			Corpse corpse = pawn.ParentHolder as Corpse ?? pawn.Corpse;
			if (corpse == null || corpse.Destroyed)
			{
				return false;
			}

			if (pawn.Discarded)
			{
				return false;
			}

			Map mapHeld = corpse.MapHeld;
			IntVec3 posHeld = corpse.PositionHeld;
			IntVec3 spawnCell = FindGhostResurrectCell(mapHeld, posHeld);
			if (!spawnCell.IsValid)
			{
				spawnCell = posHeld;
			}

			BeginGhostResurrectGuard();
			bool ok;
			try
			{
				ok = ResurrectionUtility.TryResurrect(pawn, new ResurrectionParams
				{
					canKidnap = false,
					canTimeoutOrFlee = false,
					useAvoidGridSmart = true,
					canSteal = false,
					noLord = true,
					dontSpawn = true,
					restoreMissingParts = true,
				});
			}
			finally
			{
				EndGhostResurrectGuard();
			}

			if (!ok)
			{
				return false;
			}

			if (mapHeld != null && spawnCell.IsValid && spawnCell.InBounds(mapHeld))
			{
				GenSpawn.Spawn(pawn, spawnCell, mapHeld);
			}

			if (pawn.Dead || pawn.Destroyed)
			{
				return false;
			}

			RestoreGhostUntilStanding(pawn);

			if (consumeDeathRefusal)
			{
				ConsumeOneDeathRefusal(pawn);
			}

			Hediff sickness = pawn.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.DeathRefusalSickness);
			if (sickness != null)
			{
				pawn.health.RemoveHediff(sickness);
			}

			if (pawn.equipment != null && pawn.equipment.Primary == null)
			{
				EquipLoadout(pawn);
			}

			NotifyGhostReturnedToFight(pawn);

			if (mapHeld != null && spawnCell.IsValid)
			{
				EffecterDef effect = DefDatabase<EffecterDef>.GetNamedSilentFail("DeathRefusalUse");
				effect?.Spawn(spawnCell, mapHeld).Cleanup();
			}

			return true;
		}

		public static void NotifyGhostReturnedToFight(Pawn pawn)
		{
			if (pawn == null || pawn.Destroyed || pawn.Dead)
			{
				return;
			}

			Map map = pawn.Map ?? pawn.MapHeld;
			VoidAwake_MapComponent_GhostShip comp = map?.GetComponent<VoidAwake_MapComponent_GhostShip>();
			if (comp == null)
			{
				List<Map> maps = Find.Maps;
				for (int i = 0; i < maps.Count; i++)
				{
					comp = maps[i].GetComponent<VoidAwake_MapComponent_GhostShip>();
					if (comp != null)
					{
						break;
					}
				}
			}

			comp?.NotifyGhostReturned(pawn);
		}

		public static void ApplyResurrectionWave(Pawn pawn)
		{
			if (pawn == null || pawn.Destroyed)
			{
				return;
			}

			if (pawn.Dead)
			{
				TryResurrectGhostFromCorpse(pawn, false);
				return;
			}

			AddOneDeathRefusal(pawn);
		}

		public static void ReleaseResurrectionWave(List<Pawn> extraGhosts = null)
		{
			tmpGhosts.Clear();
			List<Map> maps = Find.Maps;
			for (int i = 0; i < maps.Count; i++)
			{
				CollectGhostsOnMap(maps[i], tmpGhosts);
			}

			if (extraGhosts != null)
			{
				for (int i = 0; i < extraGhosts.Count; i++)
				{
					Pawn pawn = extraGhosts[i];
					if (IsGhostPawn(pawn) && !tmpGhosts.Contains(pawn))
					{
						tmpGhosts.Add(pawn);
					}
				}
			}

			for (int i = 0; i < tmpGhosts.Count; i++)
			{
				ApplyResurrectionWave(tmpGhosts[i]);
			}

			tmpGhosts.Clear();
		}

		public static void CollectGhostsOnMap(Map map, List<Pawn> dest)
		{
			if (map == null || dest == null)
			{
				return;
			}

			List<Pawn> pawns = map.mapPawns.AllPawns;
			for (int i = 0; i < pawns.Count; i++)
			{
				Pawn pawn = pawns[i];
				if (IsGhostPawn(pawn) && !dest.Contains(pawn))
				{
					dest.Add(pawn);
				}
			}

			List<Thing> all = map.listerThings.AllThings;
			if (all != null)
			{
				for (int i = 0; i < all.Count; i++)
				{
					Corpse corpse = all[i] as Corpse;
					Pawn inner = corpse?.InnerPawn;
					if (IsGhostPawn(inner) && !dest.Contains(inner))
					{
						dest.Add(inner);
					}
				}
			}
		}

		public static bool SuppressGhostDeathOnDowned { get; private set; }

		public static void BeginGhostResurrectGuard()
		{
			SuppressGhostDeathOnDowned = true;
		}

		public static void EndGhostResurrectGuard()
		{
			SuppressGhostDeathOnDowned = false;
		}

		public static void RestoreGhostUntilStanding(Pawn pawn)
		{
			if (pawn?.health?.hediffSet == null || pawn.Dead)
			{
				return;
			}

			List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
			int guard = 0;
			while (guard++ < 400 && pawn.health.ShouldBeDowned())
			{
				Hediff_Injury injury = null;
				for (int i = 0; i < hediffs.Count; i++)
				{
					injury = hediffs[i] as Hediff_Injury;
					if (injury != null)
					{
						break;
					}
				}

				if (injury != null)
				{
					injury.Severity -= 1f;
					if (injury.Severity <= 0f)
					{
						pawn.health.RemoveHediff(injury);
					}

					continue;
				}

				MutantUtility.RestoreBodyParts(pawn);
				if (pawn.health.ShouldBeDowned())
				{
					MutantUtility.RegenerateHealth(pawn);
				}

				break;
			}

			pawn.health.hediffSet.DirtyCache();
		}

		public static void KickGhostDeathRefusal(Pawn pawn)
		{
			if (pawn == null || pawn.Destroyed || !pawn.Dead || IsHeldByFlyer(pawn))
			{
				return;
			}

			Hediff_DeathRefusal refusal = pawn.health?.hediffSet?.GetFirstHediff<Hediff_DeathRefusal>();
			if (refusal == null || refusal.UsesLeft <= 0)
			{
				return;
			}

			refusal.AIEnabled = true;
			if (!refusal.InProgress)
			{
				refusal.Notify_PawnDied(null);
			}

			refusal.TickRare();
		}

		public static void AfterGhostDeathRefusalResurrect(Pawn pawn)
		{
			if (pawn == null || pawn.Destroyed || pawn.Dead)
			{
				return;
			}

			RestoreGhostUntilStanding(pawn);

			if (pawn.Spawned && VoidAwake_GhostShipOceanUtility.IsOceanTerrain(pawn.Position.GetTerrain(pawn.Map)))
			{
				IntVec3 cell = FindGhostResurrectCell(pawn.Map, pawn.Position);
				if (cell.IsValid && cell != pawn.Position)
				{
					pawn.Position = cell;
				}
			}

			Hediff sickness = pawn.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.DeathRefusalSickness);
			if (sickness != null)
			{
				pawn.health.RemoveHediff(sickness);
			}

			if (pawn.equipment != null && pawn.equipment.Primary == null)
			{
				EquipLoadout(pawn);
			}

			Hediff_DeathRefusal refusal = pawn.health?.hediffSet?.GetFirstHediff<Hediff_DeathRefusal>();
			VoidAwake_Hediff_Mortal mortal = GetMortal(pawn);
			if (mortal != null)
			{
				mortal.RefusalUses = refusal != null ? refusal.UsesLeft : 0;
			}

			NotifyGhostReturnedToFight(pawn);
		}

		public static void TickGhostCorpse(Pawn pawn)
		{
			if (pawn == null || pawn.Destroyed || pawn.health?.hediffSet == null)
			{
				return;
			}

			if (pawn.Dead)
			{
				KickGhostDeathRefusal(pawn);
			}

			GetMortal(pawn)?.TryVanish();
		}

		public static void TickGhostCorpses(Map map)
		{
			if (map == null)
			{
				return;
			}

			tmpGhosts.Clear();
			CollectGhostsOnMap(map, tmpGhosts);
			for (int i = 0; i < tmpGhosts.Count; i++)
			{
				TickGhostCorpse(tmpGhosts[i]);
			}

			tmpGhosts.Clear();
		}

		public static void VanishGhostWithCorpse(Pawn pawn)
		{
			if (pawn == null || pawn.Destroyed)
			{
				return;
			}

			Corpse corpse = pawn.ParentHolder as Corpse ?? pawn.Corpse;
			if (pawn.Spawned)
			{
				pawn.Destroy();
				return;
			}

			if (corpse != null && !corpse.Destroyed)
			{
				corpse.Destroy();
				return;
			}

			if (!pawn.Destroyed)
			{
				pawn.Destroy();
			}
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
