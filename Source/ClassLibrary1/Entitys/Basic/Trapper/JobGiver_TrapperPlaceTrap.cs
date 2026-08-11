using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
	public class JobGiver_TrapperPlaceTrap : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn?.Map == null || pawn.Downed || pawn.Dead)
			{
				return null;
			}

			VoidAwake_TrapperComp comp = pawn.TryGetComp<VoidAwake_TrapperComp>();
			if (comp == null || !comp.IsStealth || !comp.CanPlaceTrapNow)
			{
				return null;
			}

			IntVec3 cell = FindTrapCell(pawn, comp);
			if (!cell.IsValid)
			{
				if (comp.ChainPlacing)
				{
					comp.Notify_ChainEnded();
				}

				return null;
			}

			return MakePlaceTrapJob(cell);
		}

		public static Job MakePlaceTrapJob(IntVec3 cell)
		{
			Job job = JobMaker.MakeJob(VoidAwake_TrapperDefOf.VoidAwake_PlaceBearTrap, cell);
			job.canBashDoors = false;
			job.canBashFences = false;
			return job;
		}

		private static IntVec3 FindTrapCell(Pawn pawn, VoidAwake_TrapperComp comp)
		{
			if (comp.ChainPlacing && comp.ChainDoorCell.IsValid)
			{
				IntVec3 chained = FindNextCellAroundDoor(pawn, comp.ChainDoorCell, comp.LastTrapCell);
				if (chained.IsValid)
				{
					return chained;
				}

				comp.Notify_ChainEnded();
			}

			return FindDoorNeighborhoodCell(pawn, comp);
		}

		/// <summary>
		/// Next cell inside the door's 3x3 (9 cells). Prefer cells adjacent to the last trap.
		/// </summary>
		public static IntVec3 FindNextCellAroundDoor(Pawn pawn, IntVec3 doorCell, IntVec3 lastTrapCell)
		{
			List<IntVec3> inNeighborhood = CollectValidCellsAroundDoor(pawn, doorCell);
			if (inNeighborhood.Count == 0)
			{
				return IntVec3.Invalid;
			}

			if (lastTrapCell.IsValid)
			{
				List<IntVec3> adjacent = new List<IntVec3>();
				for (int i = 0; i < inNeighborhood.Count; i++)
				{
					IntVec3 cell = inNeighborhood[i];
					if (IsCardinalAdjacent(lastTrapCell, cell))
					{
						adjacent.Add(cell);
					}
				}

				if (adjacent.Count > 0)
				{
					adjacent.Sort((a, b) =>
						a.DistanceToSquared(pawn.Position).CompareTo(b.DistanceToSquared(pawn.Position)));
					return adjacent[0];
				}
			}

			inNeighborhood.Sort((a, b) =>
				a.DistanceToSquared(pawn.Position).CompareTo(b.DistanceToSquared(pawn.Position)));
			return inNeighborhood[0];
		}

		private static IntVec3 FindDoorNeighborhoodCell(Pawn pawn, VoidAwake_TrapperComp comp)
		{
			Map map = pawn.Map;
			List<(IntVec3 cell, IntVec3 door)> candidates = new List<(IntVec3, IntVec3)>();

			foreach (Building_Door door in map.listerBuildings.AllBuildingsColonistOfClass<Building_Door>())
			{
				if (door == null || !door.Spawned)
				{
					continue;
				}

				IntVec3 doorCell = door.Position;
				if (VoidAwake_TrapperUtility.IsDoorReservedByOther(map, doorCell, pawn))
				{
					continue;
				}

				foreach (IntVec3 cell in CellsAroundDoor(doorCell))
				{
					if (IsValidTrapCell(pawn, cell))
					{
						candidates.Add((cell, doorCell));
					}
				}
			}

			if (candidates.Count == 0)
			{
				return IntVec3.Invalid;
			}

			candidates.Sort((a, b) =>
				a.cell.DistanceToSquared(pawn.Position).CompareTo(b.cell.DistanceToSquared(pawn.Position)));

			int take = candidates.Count < 8 ? candidates.Count : 8;

			// Prefer a door we can actually reserve; try nearby candidates until one sticks.
			List<IntVec3> triedDoors = new List<IntVec3>();
			for (int attempt = 0; attempt < take; attempt++)
			{
				(IntVec3 cell, IntVec3 door) pick = candidates[Rand.Range(0, take)];
				if (triedDoors.Contains(pick.door))
				{
					continue;
				}

				triedDoors.Add(pick.door);
				comp.BeginDoorChain(pick.door);
				if (comp.ChainPlacing && comp.ChainDoorCell == pick.door)
				{
					return pick.cell;
				}
			}

			// Deterministic fallback over nearest doors.
			for (int i = 0; i < candidates.Count; i++)
			{
				(IntVec3 cell, IntVec3 door) pick = candidates[i];
				if (triedDoors.Contains(pick.door))
				{
					continue;
				}

				triedDoors.Add(pick.door);
				comp.BeginDoorChain(pick.door);
				if (comp.ChainPlacing && comp.ChainDoorCell == pick.door)
				{
					return pick.cell;
				}
			}

			return IntVec3.Invalid;
		}

		public static IEnumerable<IntVec3> CellsAroundDoor(IntVec3 doorCell)
		{
			// 3x3 centered on door (9 cells), excluding the door cell itself.
			for (int dx = -1; dx <= 1; dx++)
			{
				for (int dz = -1; dz <= 1; dz++)
				{
					if (dx == 0 && dz == 0)
					{
						continue;
					}

					yield return doorCell + new IntVec3(dx, 0, dz);
				}
			}
		}

		public static bool IsInDoorNeighborhood(IntVec3 doorCell, IntVec3 cell)
		{
			int dx = cell.x - doorCell.x;
			int dz = cell.z - doorCell.z;
			return dx >= -1 && dx <= 1 && dz >= -1 && dz <= 1 && !(dx == 0 && dz == 0);
		}

		private static List<IntVec3> CollectValidCellsAroundDoor(Pawn pawn, IntVec3 doorCell)
		{
			List<IntVec3> result = new List<IntVec3>();
			foreach (IntVec3 cell in CellsAroundDoor(doorCell))
			{
				if (IsValidTrapCell(pawn, cell))
				{
					result.Add(cell);
				}
			}

			return result;
		}

		private static bool IsCardinalAdjacent(IntVec3 a, IntVec3 b)
		{
			int dx = a.x - b.x;
			int dz = a.z - b.z;
			return (dx == 0 && (dz == 1 || dz == -1)) || (dz == 0 && (dx == 1 || dx == -1));
		}

		public static bool IsValidTrapCell(Pawn pawn, IntVec3 cell)
		{
			Map map = pawn.Map;
			if (!cell.InBounds(map) || !cell.Standable(map))
			{
				return false;
			}

			if (cell.GetEdifice(map) != null)
			{
				return false;
			}

			List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
			for (int i = 0; i < things.Count; i++)
			{
				Thing t = things[i];
				if (t.def.building != null && t.def.building.isTrap)
				{
					return false;
				}
			}

			if (!pawn.CanReserve(cell))
			{
				return false;
			}

			// Reachable without bashing walls/doors — only cells the trapper can already walk to.
			if (!pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.NoPassClosedDoors))
			{
				return false;
			}

			return true;
		}
	}
}
