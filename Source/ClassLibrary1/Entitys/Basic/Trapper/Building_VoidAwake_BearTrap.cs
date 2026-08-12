using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace VoidAwake
{
	public class Building_VoidAwake_BearTrap : Building_TrapDamager
	{
		private const int TrapHitCount = 5;

		private static readonly FloatRange TrapDamageRandomFactorRange = new FloatRange(0.8f, 1.2f);

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			base.DrawAt(drawLoc, flip);
			VoidAwake_DoorTrapComboUtility.DrawTripwiresFrom(this);
		}

		protected override void SpringSub(Pawn p)
		{
			SoundDefOf.TrapSpring.PlayOneShot(new TargetInfo(Position, Map, false));
			if (p == null)
			{
				return;
			}

			float totalDamage = this.GetStatValue(StatDefOf.TrapMeleeDamage, true) * TrapDamageRandomFactorRange.RandomInRange;
			float damagePerHit = totalDamage / TrapHitCount;
			float armorPenetration = damagePerHit * 0.015f;

			for (int i = 0; i < TrapHitCount; i++)
			{
				BodyPartRecord hitPart = VoidAwake_BearTrapTargetingUtility.ChooseHitPart(p);
				DamageInfo dinfo = new DamageInfo(DamageDefOf.Stab, damagePerHit, armorPenetration, -1f, this, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown, null);
				DamageWorker.DamageResult damageResult = p.TakeDamage(dinfo);
				if (i == 0)
				{
					BattleLogEntry_DamageTaken battleLogEntry = new BattleLogEntry_DamageTaken(p, RulePackDefOf.DamageEvent_TrapSpike, null);
					Find.BattleLog.Add(battleLogEntry);
					damageResult.AssociateWithLog(battleLogEntry);
				}
			}

			VoidAwake_TrapperUtility.RevealAllTrappersOnMap(Map);

			if (p != null && !p.Dead && p.kindDef?.immuneToTraps != true)
			{
				VoidAwake_BearTrapCaughtUtility.TryApplyCaught(p);
			}
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			Map map = Map;
			base.Destroy(mode);
			if (map != null)
			{
				VoidAwake_TrapperUtility.NotifyTrapDestroyed(map);
			}
		}
	}

	public class MapComponent_VoidAwake_TrapperTraps : MapComponent
	{
		public const int DestroyedTrapsToEnterCombat = 5;

		private int destroyedTrapCount;

		/// <summary>Door cell → reserving trapper thingIDNumber. One trapper per door.</summary>
		private Dictionary<IntVec3, int> doorReservations = new Dictionary<IntVec3, int>();

		public const int PassagePruneIntervalTicks = 2000;

		public MapComponent_VoidAwake_TrapperTraps(Map map) : base(map)
		{
		}

		public override void MapComponentTick()
		{
			if (Find.TickManager.TicksGame % PassagePruneIntervalTicks != 0)
			{
				return;
			}

			RabbitPassageUtility.PruneRedundantPassages(map);
		}

		public void NotifyTrapDestroyed()
		{
			destroyedTrapCount++;
			if (destroyedTrapCount < DestroyedTrapsToEnterCombat)
			{
				return;
			}

			destroyedTrapCount = 0;
			VoidAwake_TrapperUtility.RevealAllTrappersOnMap(map);
		}

		public bool IsDoorReservedByOther(IntVec3 doorCell, Pawn pawn)
		{
			if (pawn == null || !doorCell.IsValid)
			{
				return false;
			}

			if (!doorReservations.TryGetValue(doorCell, out int holderId))
			{
				return false;
			}

			if (holderId == pawn.thingIDNumber)
			{
				return false;
			}

			if (!IsReservationHolderValid(holderId))
			{
				doorReservations.Remove(doorCell);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Claim a door for this trapper. Releases this pawn's previous door claims first.
		/// Fails if another living trapper already holds the door.
		/// </summary>
		public bool TryReserveDoor(IntVec3 doorCell, Pawn pawn)
		{
			if (pawn == null || !doorCell.IsValid)
			{
				return false;
			}

			if (IsDoorReservedByOther(doorCell, pawn))
			{
				return false;
			}

			ReleaseAllDoorsFor(pawn);
			doorReservations[doorCell] = pawn.thingIDNumber;
			return true;
		}

		public void ReleaseDoor(IntVec3 doorCell, Pawn pawn)
		{
			if (pawn == null || !doorCell.IsValid)
			{
				return;
			}

			if (doorReservations.TryGetValue(doorCell, out int holderId) && holderId == pawn.thingIDNumber)
			{
				doorReservations.Remove(doorCell);
			}
		}

		public void ReleaseAllDoorsFor(Pawn pawn)
		{
			if (pawn == null || doorReservations.Count == 0)
			{
				return;
			}

			int id = pawn.thingIDNumber;
			List<IntVec3> toRemove = null;
			foreach (KeyValuePair<IntVec3, int> kv in doorReservations)
			{
				if (kv.Value == id)
				{
					if (toRemove == null)
					{
						toRemove = new List<IntVec3>();
					}

					toRemove.Add(kv.Key);
				}
			}

			if (toRemove == null)
			{
				return;
			}

			for (int i = 0; i < toRemove.Count; i++)
			{
				doorReservations.Remove(toRemove[i]);
			}
		}

		private bool IsReservationHolderValid(int holderId)
		{
			IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
			for (int i = 0; i < pawns.Count; i++)
			{
				Pawn p = pawns[i];
				if (p != null && p.thingIDNumber == holderId && !p.Dead && p.Spawned
					&& p.kindDef == VoidAwake_TrapperDefOf.Trapper)
				{
					return true;
				}
			}

			return false;
		}

		public override void ExposeData()
		{
			Scribe_Values.Look(ref destroyedTrapCount, "destroyedTrapCount", 0);
			Scribe_Collections.Look(ref doorReservations, "doorReservations", LookMode.Value, LookMode.Value);
			if (Scribe.mode == LoadSaveMode.PostLoadInit && doorReservations == null)
			{
				doorReservations = new Dictionary<IntVec3, int>();
			}
		}
	}

	public static class VoidAwake_TrapperUtility
	{
		/// <summary>Plants and loose items (rock chunks) the trapper simply removes to free a cell.</summary>
		public static bool IsClearableObstacle(Thing t)
		{
			if (t == null || t is Pawn || t is Corpse)
			{
				return false;
			}

			if (t.def.category == ThingCategory.Plant)
			{
				return true;
			}

			return t.def.category == ThingCategory.Item;
		}

		/// <summary>Standable check that treats plants and chunks as removable rather than blocking.</summary>
		public static bool StandableIgnoringClearables(Map map, IntVec3 cell)
		{
			if (map == null || !cell.InBounds(map))
			{
				return false;
			}

			TerrainDef terrain = cell.GetTerrain(map);
			if (terrain == null || terrain.passability == Traversability.Impassable)
			{
				return false;
			}

			List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
			for (int i = 0; i < things.Count; i++)
			{
				Thing t = things[i];
				if (t.def.passability == Traversability.Standable)
				{
					continue;
				}

				if (!IsClearableObstacle(t))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>Remove every plant plus any blocking loose item so a trap or passage fits.</summary>
		public static void ClearCellObstacles(Map map, IntVec3 cell)
		{
			if (map == null || !cell.InBounds(map))
			{
				return;
			}

			List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
			List<Thing> toRemove = null;
			for (int i = 0; i < things.Count; i++)
			{
				Thing t = things[i];
				if (!IsClearableObstacle(t))
				{
					continue;
				}

				bool blocks = t.def.passability != Traversability.Standable;
				if (!blocks && t.def.category != ThingCategory.Plant)
				{
					continue;
				}

				if (toRemove == null)
				{
					toRemove = new List<Thing>();
				}

				toRemove.Add(t);
			}

			if (toRemove == null)
			{
				return;
			}

			for (int i = 0; i < toRemove.Count; i++)
			{
				if (!toRemove[i].Destroyed)
				{
					toRemove[i].Destroy(DestroyMode.Vanish);
				}
			}
		}

		public static void NotifyTrapDestroyed(Map map)
		{
			if (map == null)
			{
				return;
			}

			map.GetComponent<MapComponent_VoidAwake_TrapperTraps>()?.NotifyTrapDestroyed();
		}

		public static MapComponent_VoidAwake_TrapperTraps GetTrapMapComp(Map map)
		{
			return map?.GetComponent<MapComponent_VoidAwake_TrapperTraps>();
		}

		public static bool TryReserveDoor(Map map, IntVec3 doorCell, Pawn pawn)
		{
			return GetTrapMapComp(map)?.TryReserveDoor(doorCell, pawn) ?? false;
		}

		public static bool IsDoorReservedByOther(Map map, IntVec3 doorCell, Pawn pawn)
		{
			return GetTrapMapComp(map)?.IsDoorReservedByOther(doorCell, pawn) ?? false;
		}

		public static void ReleaseAllDoorsFor(Pawn pawn)
		{
			if (pawn?.Map == null)
			{
				return;
			}

			GetTrapMapComp(pawn.Map)?.ReleaseAllDoorsFor(pawn);
		}

		private static bool revealingAll;

		public static void RevealAllTrappersOnMap(Map map)
		{
			if (map == null || revealingAll)
			{
				return;
			}

			revealingAll = true;
			try
			{
				IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
				for (int i = 0; i < pawns.Count; i++)
				{
					Pawn pawn = pawns[i];
					if (pawn?.kindDef == VoidAwake_TrapperDefOf.Trapper)
					{
						pawn.TryGetComp<VoidAwake_TrapperComp>()?.ApplyEnterCombat();
					}
				}
			}
			finally
			{
				revealingAll = false;
			}
		}
	}

	public static class VoidAwake_BearTrapTargetingUtility
	{
		public static BodyPartRecord ChooseHitPart(Pawn pawn)
		{
			if (pawn == null || pawn.Dead)
			{
				return null;
			}

			HediffSet hediffSet = pawn.health.hediffSet;
			if (pawn.Downed)
			{
				BodyPartRecord head = ChooseHead(hediffSet);
				if (head != null)
				{
					return head;
				}
			}
			else
			{
				BodyPartRecord leg = ChooseLeg(hediffSet);
				if (leg != null)
				{
					return leg;
				}
			}

			return hediffSet.GetRandomNotMissingPart(DamageDefOf.Stab);
		}

		private static BodyPartRecord ChooseHead(HediffSet hediffSet)
		{
			foreach (BodyPartRecord part in hediffSet.GetNotMissingParts())
			{
				if (part.def == BodyPartDefOf.Head)
				{
					return part;
				}
			}

			return ChooseFromGroup(hediffSet, BodyPartGroupDefOf.FullHead)
				?? ChooseFromGroup(hediffSet, BodyPartGroupDefOf.UpperHead);
		}

		private static BodyPartRecord ChooseLeg(HediffSet hediffSet)
		{
			List<BodyPartRecord> candidates = null;
			foreach (BodyPartRecord part in hediffSet.GetNotMissingParts())
			{
				if (!IsLegPart(part))
				{
					continue;
				}

				if (candidates == null)
				{
					candidates = new List<BodyPartRecord>();
				}

				candidates.Add(part);
			}

			if (candidates == null || candidates.Count == 0)
			{
				return null;
			}

			if (candidates.TryRandomElementByWeight(p => p.coverageAbs * p.def.GetHitChanceFactorFor(DamageDefOf.Stab), out BodyPartRecord result))
			{
				return result;
			}

			return candidates.RandomElement();
		}

		private static bool IsLegPart(BodyPartRecord part)
		{
			if (part.IsInGroup(BodyPartGroupDefOf.Legs))
			{
				return true;
			}

			List<BodyPartTagDef> tags = part.def.tags;
			return tags.Contains(BodyPartTagDefOf.MovingLimbCore)
				|| tags.Contains(BodyPartTagDefOf.MovingLimbSegment)
				|| tags.Contains(BodyPartTagDefOf.MovingLimbDigit);
		}

		private static BodyPartRecord ChooseFromGroup(HediffSet hediffSet, BodyPartGroupDef group)
		{
			List<BodyPartRecord> candidates = null;
			foreach (BodyPartRecord part in hediffSet.GetNotMissingParts())
			{
				if (!part.IsInGroup(group))
				{
					continue;
				}

				if (candidates == null)
				{
					candidates = new List<BodyPartRecord>();
				}

				candidates.Add(part);
			}

			if (candidates == null || candidates.Count == 0)
			{
				return null;
			}

			if (candidates.TryRandomElementByWeight(p => p.coverageAbs * p.def.GetHitChanceFactorFor(DamageDefOf.Stab), out BodyPartRecord result))
			{
				return result;
			}

			return candidates.RandomElement();
		}
	}
}
