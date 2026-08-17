using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace VoidAwake
{
	/// <summary>
	/// 幽霊船が海洋上だけを壁にぶつからず移動するための経路。
	/// </summary>
	public static class VoidAwake_GhostShipWanderUtility
	{
		public const float MuffaloMoveSpeed = 4.5f;
		public const int TicksPerCell = 13;
		public const int MinWanderDistance = 16;

		public static IntVec2 ShipSize
		{
			get
			{
				if (VoidAwake_GhostShipDefOf.VoidAwake_GhostShip != null)
				{
					return VoidAwake_GhostShipDefOf.VoidAwake_GhostShip.Size;
				}

				return new IntVec2(3, 3);
			}
		}

		public static bool IsShipNavigable(Map map, IntVec3 pathCell, IntVec2 shipSize)
		{
			if (map == null || !pathCell.InBounds(map))
			{
				return false;
			}

			IntVec3 pos = VoidAwake_GhostShipOceanUtility.ShipPositionForPathCell(pathCell, shipSize, map);
			CellRect rect = new CellRect(pos.x, pos.z, shipSize.x, shipSize.z);
			if (!rect.InBounds(map))
			{
				return false;
			}

			foreach (IntVec3 c in rect.Cells)
			{
				if (c.GetEdifice(map) != null)
				{
					return false;
				}

				if (!VoidAwake_GhostShipOceanUtility.IsOceanTerrain(c.GetTerrain(map)))
				{
					return false;
				}
			}

			return true;
		}

		public static bool TryFindRandomNavigableCell(Map map, IntVec2 shipSize, out IntVec3 cell, IntVec3 near, int minDist)
		{
			cell = IntVec3.Invalid;
			List<IntVec3> cells = CollectNavigableCells(map, shipSize);
			if (cells.Count == 0)
			{
				return false;
			}

			if (!near.IsValid)
			{
				cell = cells.RandomElement();
				return true;
			}

			var far = new List<IntVec3>();
			int minSq = minDist * minDist;
			for (int i = 0; i < cells.Count; i++)
			{
				if (cells[i].DistanceToSquared(near) >= minSq)
				{
					far.Add(cells[i]);
				}
			}

			if (far.Count > 0)
			{
				cell = far.RandomElement();
				return true;
			}

			cell = cells.RandomElement();
			return true;
		}

		public static bool TryFindNavigableNear(Map map, IntVec3 near, IntVec2 shipSize, out IntVec3 cell, int maxRadius = 24)
		{
			cell = IntVec3.Invalid;
			if (map == null || !near.IsValid)
			{
				return false;
			}

			int bestDist = int.MaxValue;
			foreach (IntVec3 c in GenRadial.RadialCellsAround(near, maxRadius, true))
			{
				if (!IsShipNavigable(map, c, shipSize))
				{
					continue;
				}

				int dist = c.DistanceToSquared(near);
				if (dist < bestDist)
				{
					bestDist = dist;
					cell = c;
				}
			}

			if (cell.IsValid)
			{
				return true;
			}

			List<IntVec3> all = CollectNavigableCells(map, shipSize);
			if (all.Count == 0)
			{
				return false;
			}

			cell = all[0];
			bestDist = cell.DistanceToSquared(near);
			for (int i = 1; i < all.Count; i++)
			{
				int dist = all[i].DistanceToSquared(near);
				if (dist < bestDist)
				{
					bestDist = dist;
					cell = all[i];
				}
			}

			return cell.IsValid;
		}

		public static bool TryFindNavigableInConvertedFlood(
			Map map,
			IntVec2 shipSize,
			List<IntVec3> floodCells,
			int convertedCount,
			out IntVec3 cell)
		{
			cell = IntVec3.Invalid;
			if (map == null || floodCells == null || convertedCount <= 0)
			{
				return false;
			}

			int count = Mathf.Min(convertedCount, floodCells.Count);
			for (int i = 0; i < count; i++)
			{
				IntVec3 c = floodCells[i];
				if (!c.InBounds(map) || !IsShipNavigable(map, c, shipSize))
				{
					continue;
				}

				cell = c;
				return true;
			}

			return false;
		}

		public static List<IntVec3> CollectNavigableCells(Map map, IntVec2 shipSize)
		{
			var cells = new List<IntVec3>();
			foreach (IntVec3 c in map.AllCells)
			{
				if (IsShipNavigable(map, c, shipSize))
				{
					cells.Add(c);
				}
			}

			return cells;
		}

		public static bool TryFindPath(Map map, IntVec3 start, IntVec3 dest, IntVec2 shipSize, List<IntVec3> path)
		{
			path.Clear();
			if (!IsShipNavigable(map, start, shipSize) || !IsShipNavigable(map, dest, shipSize))
			{
				return false;
			}

			if (start == dest)
			{
				path.Add(start);
				return true;
			}

			int n = map.cellIndices.NumGridCells;
			int[] cameFrom = new int[n];
			int[] gScore = new int[n];
			bool[] closed = new bool[n];
			bool[] inOpen = new bool[n];
			for (int i = 0; i < n; i++)
			{
				cameFrom[i] = -1;
				gScore[i] = int.MaxValue;
			}

			int startIdx = map.cellIndices.CellToIndex(start);
			int destIdx = map.cellIndices.CellToIndex(dest);
			gScore[startIdx] = 0;
			var open = new List<int>();
			open.Add(startIdx);
			inOpen[startIdx] = true;

			while (open.Count > 0)
			{
				int bestI = 0;
				int bestF = gScore[open[0]] + Heuristic(map, open[0], dest);
				for (int i = 1; i < open.Count; i++)
				{
					int f = gScore[open[i]] + Heuristic(map, open[i], dest);
					if (f < bestF)
					{
						bestF = f;
						bestI = i;
					}
				}

				int current = open[bestI];
				open.RemoveAt(bestI);
				inOpen[current] = false;
				if (current == destIdx)
				{
					Reconstruct(map, cameFrom, destIdx, path);
					return true;
				}

				closed[current] = true;
				IntVec3 c = map.cellIndices.IndexToCell(current);
				for (int d = 0; d < 4; d++)
				{
					IntVec3 nb = c + GenAdj.CardinalDirections[d];
					if (!nb.InBounds(map) || !IsShipNavigable(map, nb, shipSize))
					{
						continue;
					}

					int ni = map.cellIndices.CellToIndex(nb);
					if (closed[ni])
					{
						continue;
					}

					int tentative = gScore[current] + 1;
					if (tentative >= gScore[ni])
					{
						continue;
					}

					cameFrom[ni] = current;
					gScore[ni] = tentative;
					if (!inOpen[ni])
					{
						open.Add(ni);
						inOpen[ni] = true;
					}
				}
			}

			return false;
		}

		public static bool TryFindRandomShoreCell(Map map, out IntVec3 cell)
		{
			cell = IntVec3.Invalid;
			var candidates = new List<IntVec3>();
			foreach (IntVec3 c in map.AllCells)
			{
				if (!c.Standable(map) || VoidAwake_GhostShipOceanUtility.IsOceanTerrain(c.GetTerrain(map)))
				{
					continue;
				}

				if (c.GetFirstPawn(map) != null)
				{
					continue;
				}

				if (!VoidAwake_GhostShipOceanUtility.HasAdjacentOcean(map, c))
				{
					continue;
				}

				candidates.Add(c);
			}

			if (candidates.Count == 0)
			{
				return false;
			}

			cell = candidates.RandomElement();
			return true;
		}

		private static int Heuristic(Map map, int idx, IntVec3 dest)
		{
			IntVec3 c = map.cellIndices.IndexToCell(idx);
			return Mathf.Abs(c.x - dest.x) + Mathf.Abs(c.z - dest.z);
		}

		private static void Reconstruct(Map map, int[] cameFrom, int destIdx, List<IntVec3> path)
		{
			var rev = new List<IntVec3>();
			int cur = destIdx;
			while (cur >= 0)
			{
				rev.Add(map.cellIndices.IndexToCell(cur));
				cur = cameFrom[cur];
			}

			for (int i = rev.Count - 1; i >= 0; i--)
			{
				path.Add(rev[i]);
			}
		}
	}
}
