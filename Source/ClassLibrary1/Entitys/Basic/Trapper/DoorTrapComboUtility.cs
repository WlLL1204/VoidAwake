using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidAwake
{
	public static class VoidAwake_DoorTrapComboUtility
	{
		public const int MinTrapsToTrigger = 3;

		private const float TripwireGlowWidth = 0.34f;

		private const float TripwireCoreWidth = 0.16f;

		private static bool triggering;

		private static Material tripwireGlowMaterial;

		private static Material tripwireCoreMaterial;

		/// <summary>Wide red halo. MoteGlow keeps it bright regardless of light level.</summary>
		public static Material TripwireGlowMaterial
		{
			get
			{
				if (tripwireGlowMaterial == null)
				{
					tripwireGlowMaterial = MaterialPool.MatFrom(
						GenDraw.LineTexPath,
						ShaderDatabase.MoteGlow,
						new Color(1f, 0.15f, 0.08f, 1f));
				}

				return tripwireGlowMaterial;
			}
		}

		public static Material TripwireCoreMaterial
		{
			get
			{
				if (tripwireCoreMaterial == null)
				{
					tripwireCoreMaterial = MaterialPool.MatFrom(
						GenDraw.LineTexPath,
						ShaderDatabase.MoteGlow,
						new Color(1f, 0.9f, 0.6f, 1f));
				}

				return tripwireCoreMaterial;
			}
		}

		public static bool IsFloatingDoor(Building_Door door)
		{
			if (door?.Map == null || !door.Spawned)
			{
				return false;
			}

			Map map = door.Map;
			IntVec3 doorCell = door.Position;
			int wallCount = 0;
			int standableCount = 0;

			for (int i = 0; i < 4; i++)
			{
				IntVec3 cell = doorCell + GenAdj.CardinalDirections[i];
				if (!cell.InBounds(map))
				{
					continue;
				}

				Building edifice = cell.GetEdifice(map);
				if (edifice != null && edifice.def.IsWall)
				{
					wallCount++;
				}

				if (cell.Standable(map))
				{
					standableCount++;
				}
			}

			return wallCount <= 1 || standableCount >= 3;
		}

		public static void CollectCardinalAdjacentTraps(Building_Door door, List<Building_VoidAwake_BearTrap> into)
		{
			into.Clear();
			if (door?.Map == null || !door.Spawned)
			{
				return;
			}

			Map map = door.Map;
			IntVec3 doorCell = door.Position;
			for (int i = 0; i < 4; i++)
			{
				IntVec3 cell = doorCell + GenAdj.CardinalDirections[i];
				if (!cell.InBounds(map))
				{
					continue;
				}

				Building_VoidAwake_BearTrap trap = FindBearTrapAt(map, cell);
				if (trap != null)
				{
					into.Add(trap);
				}
			}
		}

		public static Building_VoidAwake_BearTrap FindBearTrapAt(Map map, IntVec3 cell)
		{
			List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
			for (int i = 0; i < things.Count; i++)
			{
				if (things[i] is Building_VoidAwake_BearTrap trap)
				{
					return trap;
				}
			}

			return null;
		}

		public static IEnumerable<IntVec3> CellsInDoorBlast(IntVec3 doorCell)
		{
			for (int dx = -1; dx <= 1; dx++)
			{
				for (int dz = -1; dz <= 1; dz++)
				{
					yield return doorCell + new IntVec3(dx, 0, dz);
				}
			}
		}

		public static void TryTriggerOnDoorOpen(Building_Door door)
		{
			if (triggering || door == null || !door.Spawned || door.Map == null)
			{
				return;
			}

			if (door.Faction == null || !door.Faction.IsPlayer)
			{
				return;
			}

			if (!IsFloatingDoor(door))
			{
				return;
			}

			List<Building_VoidAwake_BearTrap> traps = new List<Building_VoidAwake_BearTrap>();
			CollectCardinalAdjacentTraps(door, traps);
			if (traps.Count < MinTrapsToTrigger)
			{
				return;
			}

			TriggerDoorTrapCombo(door, traps);
		}

		public static void TriggerDoorTrapCombo(Building_Door door, List<Building_VoidAwake_BearTrap> traps)
		{
			if (triggering || door == null || traps == null || traps.Count < MinTrapsToTrigger)
			{
				return;
			}

			Map map = door.Map;
			if (map == null)
			{
				return;
			}

			triggering = true;
			try
			{
				IntVec3 doorCell = door.Position;
				ThingDef trapDef = VoidAwake_TrapperDefOf.VoidAwake_BearTrap;
				float damage = trapDef.GetStatValueAbstract(StatDefOf.TrapMeleeDamage) * 2f;
				float armorPenetration = damage * VerbProperties.DefaultArmorPenetrationPerDamage;

				HashSet<Pawn> damaged = new HashSet<Pawn>();
				foreach (IntVec3 cell in CellsInDoorBlast(doorCell))
				{
					if (!cell.InBounds(map))
					{
						continue;
					}

					List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
					for (int i = 0; i < things.Count; i++)
					{
						if (!(things[i] is Pawn pawn) || pawn.Dead || !damaged.Add(pawn))
						{
							continue;
						}

						if (pawn.kindDef != null && pawn.kindDef.immuneToTraps)
						{
							continue;
						}

						BodyPartRecord hitPart = VoidAwake_BearTrapTargetingUtility.ChooseHitPart(pawn);
						DamageInfo dinfo = new DamageInfo(DamageDefOf.Stab, damage, armorPenetration, -1f, traps[0], hitPart);
						pawn.TakeDamage(dinfo);

						if (!pawn.Dead)
						{
							VoidAwake_BearTrapCaughtUtility.TryApplyCaught(pawn);
						}
					}
				}

				if (!door.Destroyed)
				{
					door.Destroy(DestroyMode.KillFinalize);
				}

				for (int i = 0; i < traps.Count; i++)
				{
					Building_VoidAwake_BearTrap trap = traps[i];
					if (trap != null && !trap.Destroyed)
					{
						trap.Destroy(DestroyMode.KillFinalize);
					}
				}

				VoidAwake_TrapperUtility.RevealAllTrappersOnMap(map);
			}
			finally
			{
				triggering = false;
			}
		}

		/// <summary>
		/// Draw tripwires from this trap to other cardinal traps sharing a floating door.
		/// Only the lower thingIDNumber draws each pair.
		/// </summary>
		public static void DrawTripwiresFrom(Building_VoidAwake_BearTrap trap)
		{
			if (trap?.Map == null || !trap.Spawned)
			{
				return;
			}

			Map map = trap.Map;
			IntVec3 trapCell = trap.Position;
			List<Building_VoidAwake_BearTrap> shared = new List<Building_VoidAwake_BearTrap>(4);
			float pulse = 1.075f + 0.175f * Mathf.Sin(Time.realtimeSinceStartup * 3f);
			Vector3 from = trap.TrueCenter();
			from.y = AltitudeLayer.MoteOverhead.AltitudeFor();

			for (int i = 0; i < 4; i++)
			{
				IntVec3 neighbor = trapCell + GenAdj.CardinalDirections[i];
				if (!neighbor.InBounds(map))
				{
					continue;
				}

				Building edifice = neighbor.GetEdifice(map);
				if (!(edifice is Building_Door door) || !IsFloatingDoor(door))
				{
					continue;
				}

				CollectCardinalAdjacentTraps(door, shared);
				if (shared.Count < 2)
				{
					continue;
				}

				for (int t = 0; t < shared.Count; t++)
				{
					Building_VoidAwake_BearTrap other = shared[t];
					if (other == null || other == trap || other.Destroyed || !other.Spawned)
					{
						continue;
					}

					if (trap.thingIDNumber > other.thingIDNumber)
					{
						continue;
					}

					Vector3 to = other.TrueCenter();
					to.y = from.y;
					GenDraw.DrawLineBetween(from, to, TripwireGlowMaterial, TripwireGlowWidth * pulse);

					// Slightly above the halo so the bright core is never z-fought away.
					Vector3 coreFrom = from;
					Vector3 coreTo = to;
					coreFrom.y += 0.03f;
					coreTo.y += 0.03f;
					GenDraw.DrawLineBetween(coreFrom, coreTo, TripwireCoreMaterial, TripwireCoreWidth * pulse);
				}
			}
		}
	}
}
