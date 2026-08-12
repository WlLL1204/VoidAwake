using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace VoidAwake
{
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

			map.GetComponent<VoidAwake_MapComponent_TrapperTraps>()?.NotifyTrapDestroyed();
		}

		public static VoidAwake_MapComponent_TrapperTraps GetTrapMapComp(Map map)
		{
			return map?.GetComponent<VoidAwake_MapComponent_TrapperTraps>();
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
					if (pawn?.kindDef == VoidAwake_TrapperDefOf.VoidAwake_Trapper)
					{
						pawn.TryGetComp<VoidAwake_CompTrapper>()?.ApplyEnterCombat();
					}
				}
			}
			finally
			{
				revealingAll = false;
			}
		}
	}
}
