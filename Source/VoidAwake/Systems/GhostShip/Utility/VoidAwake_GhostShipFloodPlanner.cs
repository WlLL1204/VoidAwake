using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidAwake
{
	public class VoidAwake_GhostShipFloodPlan
	{
		public List<IntVec3> FloodCells;
		public List<IntVec3> FootingCells;
		public List<IntVec3> ShorePath;
	}

	/// <summary>
	/// 到達可能な辺 A と別辺 B を結ぶ線で陸と海の境目だけを決め、入植地を含まない側を海にする。
	/// 線自体は海にせず、浸食はマップ端の起点から円形に広がり、端ほど遅くなる。
	/// </summary>
	public static class VoidAwake_GhostShipFloodPlanner
	{
		public const int RiverHalfWidth = 2;
		public const int MinColonyClearance = 12;
		public const int MaxAttempts = 12;
		public const float MinFloodFraction = 0.15f;
		public const float MaxFloodFraction = 0.78f;
		public const int MinShorePathLength = 16;
		private const int DeepDetourClearance = 8;
		private const int EllipseSamples = 36;
		private const int EdgeCornerMargin = 8;
		private const int InlandClamp = 3;

		private enum MapEdge
		{
			North = 0,
			East = 1,
			South = 2,
			West = 3,
		}

		public static bool TryBuildPlan(Map map, out VoidAwake_GhostShipFloodPlan plan)
		{
			plan = null;
			if (map == null)
			{
				return false;
			}

			for (int i = 0; i < MaxAttempts; i++)
			{
				if (TryBuildPlanOnce(map, false, out plan) && Validate(map, plan, true))
				{
					return true;
				}
			}

			for (int i = 0; i < MaxAttempts; i++)
			{
				if (TryBuildPlanOnce(map, true, out plan) && Validate(map, plan, true))
				{
					return true;
				}
			}

			if (TryBuildSimpleAdjacentCut(map, out plan) && Validate(map, plan, false))
			{
				return true;
			}

			plan = null;
			return false;
		}

		private static bool TryBuildPlanOnce(Map map, bool preferAdjacent, out VoidAwake_GhostShipFloodPlan plan)
		{
			plan = null;
			IntVec3 colony = FindColonyCenter(map);
			if (!TryPickEndpoints(map, colony, preferAdjacent, out IntVec3 start, out IntVec3 end, out MapEdge edgeA, out MapEdge edgeB))
			{
				return false;
			}

			IntVec3 inland = MakeInlandWaypoint(map, start, end, edgeA, edgeB, colony);
			List<IntVec3> centerline = BuildMeanderPath(map, start, end, inland, colony);
			if (centerline == null || centerline.Count < 2)
			{
				return false;
			}

			return TryAssemblePlan(map, colony, centerline, out plan);
		}

		private static bool TryBuildSimpleAdjacentCut(Map map, out VoidAwake_GhostShipFloodPlan plan)
		{
			plan = null;
			IntVec3 colony = FindColonyCenter(map);
			MapEdge edgeA = colony.z < map.Size.z / 2 ? MapEdge.North : MapEdge.South;
			MapEdge edgeB = colony.x < map.Size.x / 2 ? MapEdge.East : MapEdge.West;
			IntVec3 corner = CornerOf(edgeA, edgeB, map);

			if (!TryPickCellOnEdgeFarFrom(map, edgeA, corner, true, out IntVec3 start)
				&& !TryPickCellOnEdgeFarFrom(map, edgeA, corner, false, out start))
			{
				return false;
			}

			if (!TryPickCellOnEdgeFarFrom(map, edgeB, corner, false, out IntVec3 end))
			{
				return false;
			}

			IntVec3 inland = MakeInlandWaypoint(map, start, end, edgeA, edgeB, colony);
			List<IntVec3> centerline = BuildMeanderPath(map, start, end, inland, colony);
			if (centerline == null || centerline.Count < 2)
			{
				centerline = RasterizeWaypoints(map, new List<IntVec3> { start, inland, end });
			}

			return TryAssemblePlan(map, colony, centerline, out plan);
		}

		private static bool TryAssemblePlan(Map map, IntVec3 colony, List<IntVec3> centerline, out VoidAwake_GhostShipFloodPlan plan)
		{
			plan = null;
			bool[] river = BuildRiverMask(map, centerline);
			if (river == null)
			{
				return false;
			}

			int seedIndex = map.cellIndices.CellToIndex(colony);
			if (river[seedIndex])
			{
				IntVec3 seed = FindLandSeed(map, river, colony);
				if (!seed.IsValid)
				{
					return false;
				}

				seedIndex = map.cellIndices.CellToIndex(seed);
			}

			bool[] land = FloodFillLand(map, river, seedIndex);
			List<IntVec3> floodCells = CollectFloodCells(map, river, land);
			if (floodCells.Count == 0)
			{
				return false;
			}

			List<IntVec3> footing = CollectRiverFooting(map, river, land);
			List<IntVec3> shore = BuildOceanEllipsePath(map, floodCells);
			if (shore.Count < 2)
			{
				return false;
			}

			plan = new VoidAwake_GhostShipFloodPlan
			{
				FloodCells = floodCells,
				FootingCells = footing,
				ShorePath = shore,
			};
			return true;
		}

		private static bool Validate(Map map, VoidAwake_GhostShipFloodPlan plan, bool strict)
		{
			if (plan == null || plan.FloodCells == null || plan.ShorePath == null)
			{
				return false;
			}

			if (plan.FloodCells.Count == 0 || plan.ShorePath.Count < 2)
			{
				return false;
			}

			if (strict && plan.ShorePath.Count < MinShorePathLength)
			{
				return false;
			}

			int mapCells = map.Size.x * map.Size.z;
			float frac = plan.FloodCells.Count / (float)mapCells;
			if (strict && (frac < MinFloodFraction || frac > MaxFloodFraction))
			{
				return false;
			}

			if (!strict && frac < 0.02f)
			{
				return false;
			}

			HashSet<IntVec3> ocean = new HashSet<IntVec3>(plan.FloodCells);
			List<Building> buildings = map.listerBuildings.allBuildingsColonist;
			if (buildings != null)
			{
				for (int i = 0; i < buildings.Count; i++)
				{
					Building b = buildings[i];
					if (b == null || !b.Spawned)
					{
						continue;
					}

					foreach (IntVec3 c in b.OccupiedRect())
					{
						if (ocean.Contains(c))
						{
							return false;
						}

						if (strict && OceanWithinClearance(c, ocean, MinColonyClearance))
						{
							return false;
						}
					}
				}
			}

			return true;
		}

		private static bool OceanWithinClearance(IntVec3 c, HashSet<IntVec3> ocean, int clearance)
		{
			for (int dx = -clearance + 1; dx < clearance; dx++)
			{
				for (int dz = -clearance + 1; dz < clearance; dz++)
				{
					if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) >= clearance)
					{
						continue;
					}

					if (ocean.Contains(new IntVec3(c.x + dx, 0, c.z + dz)))
					{
						return true;
					}
				}
			}

			return false;
		}

		private static bool TryPickEndpoints(
			Map map,
			IntVec3 colony,
			bool preferAdjacent,
			out IntVec3 start,
			out IntVec3 end,
			out MapEdge edgeA,
			out MapEdge edgeB)
		{
			start = IntVec3.Invalid;
			end = IntVec3.Invalid;
			edgeA = MapEdge.North;
			edgeB = MapEdge.East;

			List<IntVec3>[] reachable = CollectReachableEdgeCells(map);
			List<MapEdge> edgesWithReachable = new List<MapEdge>(4);
			for (int i = 0; i < 4; i++)
			{
				if (reachable[i].Count > 0)
				{
					edgesWithReachable.Add((MapEdge)i);
				}
			}

			if (edgesWithReachable.Count == 0)
			{
				if (!TryPickCellOnEdge(map, MapEdge.North, false, out start))
				{
					return false;
				}

				edgeA = EdgeOfCell(start, map);
				edgeB = AdjacentEdgeAwayFromColony(edgeA, colony, map);
				return TryPickCellOnEdge(map, edgeB, false, out end);
			}

			edgeA = edgesWithReachable.RandomElement();
			start = reachable[(int)edgeA].RandomElement();

			List<MapEdge> candidates = new List<MapEdge>(3);
			for (int i = 0; i < 4; i++)
			{
				MapEdge e = (MapEdge)i;
				if (e == edgeA)
				{
					continue;
				}

				if (preferAdjacent && !AreAdjacent(edgeA, e))
				{
					continue;
				}

				candidates.Add(e);
			}

			if (candidates.Count == 0)
			{
				for (int i = 0; i < 4; i++)
				{
					MapEdge e = (MapEdge)i;
					if (e != edgeA)
					{
						candidates.Add(e);
					}
				}
			}

			PreferEdgesAwayFromColony(candidates, colony, map);
			edgeB = candidates.RandomElement();
			IntVec3 corner = CornerOf(edgeA, edgeB, map);
			if (corner.IsValid)
			{
				start = PickFarthestFrom(reachable[(int)edgeA], corner, start);
			}

			if (!TryPickCellOnEdgeFarFrom(map, edgeB, corner, false, out end))
			{
				return false;
			}

			return start.IsValid && end.IsValid && start != end;
		}

		private static IntVec3 PickFarthestFrom(List<IntVec3> cells, IntVec3 from, IntVec3 fallback)
		{
			if (cells == null || cells.Count == 0)
			{
				return fallback;
			}

			var ranked = new List<IntVec3>(cells);
			ranked.Sort((a, b) => b.DistanceToSquared(from).CompareTo(a.DistanceToSquared(from)));
			int take = Mathf.Max(1, ranked.Count / 2);
			return ranked[Rand.Range(0, take)];
		}

		private static bool TryPickCellOnEdgeFarFrom(Map map, MapEdge edge, IntVec3 from, bool requireReachable, out IntVec3 cell)
		{
			if (!from.IsValid)
			{
				return TryPickCellOnEdge(map, edge, requireReachable, out cell);
			}

			int maxX = map.Size.x - 1;
			int maxZ = map.Size.z - 1;
			int bestDist = -1;
			cell = IntVec3.Invalid;
			for (int attempt = 0; attempt < 32; attempt++)
			{
				if (!TryPickCellOnEdge(map, edge, requireReachable, out IntVec3 candidate))
				{
					continue;
				}

				int dist = candidate.DistanceToSquared(from);
				if (dist > bestDist)
				{
					bestDist = dist;
					cell = candidate;
				}
			}

			if (cell.IsValid)
			{
				return true;
			}

			cell = CellOnEdge(edge, (edge == MapEdge.North || edge == MapEdge.South ? maxX : maxZ) / 2, maxX, maxZ);
			return cell.InBounds(map);
		}

		private static void PreferEdgesAwayFromColony(List<MapEdge> candidates, IntVec3 colony, Map map)
		{
			if (candidates.Count <= 1)
			{
				return;
			}

			MapEdge farA = colony.z < map.Size.z / 2 ? MapEdge.North : MapEdge.South;
			MapEdge farB = colony.x < map.Size.x / 2 ? MapEdge.East : MapEdge.West;
			var preferred = new List<MapEdge>();
			for (int i = 0; i < candidates.Count; i++)
			{
				if (candidates[i] == farA || candidates[i] == farB)
				{
					preferred.Add(candidates[i]);
				}
			}

			if (preferred.Count > 0 && Rand.Chance(0.7f))
			{
				candidates.Clear();
				candidates.AddRange(preferred);
			}
		}

		private static List<IntVec3>[] CollectReachableEdgeCells(Map map)
		{
			var lists = new List<IntVec3>[]
			{
				new List<IntVec3>(),
				new List<IntVec3>(),
				new List<IntVec3>(),
				new List<IntVec3>(),
			};

			int maxX = map.Size.x - 1;
			int maxZ = map.Size.z - 1;
			for (int x = EdgeCornerMargin; x <= maxX - EdgeCornerMargin; x++)
			{
				TryAddReachable(map, new IntVec3(x, 0, maxZ), MapEdge.North, lists);
				TryAddReachable(map, new IntVec3(x, 0, 0), MapEdge.South, lists);
			}

			for (int z = EdgeCornerMargin; z <= maxZ - EdgeCornerMargin; z++)
			{
				TryAddReachable(map, new IntVec3(maxX, 0, z), MapEdge.East, lists);
				TryAddReachable(map, new IntVec3(0, 0, z), MapEdge.West, lists);
			}

			return lists;
		}

		private static void TryAddReachable(Map map, IntVec3 c, MapEdge edge, List<IntVec3>[] lists)
		{
			if (!c.InBounds(map) || !c.Walkable(map))
			{
				return;
			}

			if (!map.reachability.CanReachColony(c))
			{
				return;
			}

			lists[(int)edge].Add(c);
		}

		private static bool TryPickCellOnEdge(Map map, MapEdge edge, bool requireReachable, out IntVec3 cell)
		{
			int maxX = map.Size.x - 1;
			int maxZ = map.Size.z - 1;
			int min = EdgeCornerMargin;
			int span = edge == MapEdge.North || edge == MapEdge.South
				? maxX - EdgeCornerMargin * 2
				: maxZ - EdgeCornerMargin * 2;
			if (span < 1)
			{
				min = 1;
				span = (edge == MapEdge.North || edge == MapEdge.South ? maxX : maxZ) - 2;
			}

			for (int attempt = 0; attempt < 24; attempt++)
			{
				int t = min + Rand.Range(0, Mathf.Max(1, span));
				cell = CellOnEdge(edge, t, maxX, maxZ);
				if (!cell.InBounds(map))
				{
					continue;
				}

				if (requireReachable)
				{
					if (cell.Walkable(map) && map.reachability.CanReachColony(cell))
					{
						return true;
					}
				}
				else
				{
					return true;
				}
			}

			cell = CellOnEdge(edge, (edge == MapEdge.North || edge == MapEdge.South ? maxX : maxZ) / 2, maxX, maxZ);
			return cell.InBounds(map);
		}

		private static IntVec3 CellOnEdge(MapEdge edge, int t, int maxX, int maxZ)
		{
			t = Mathf.Clamp(t, 1, (edge == MapEdge.North || edge == MapEdge.South ? maxX : maxZ) - 1);
			switch (edge)
			{
				case MapEdge.North:
					return new IntVec3(t, 0, maxZ);
				case MapEdge.East:
					return new IntVec3(maxX, 0, t);
				case MapEdge.South:
					return new IntVec3(t, 0, 0);
				default:
					return new IntVec3(0, 0, t);
			}
		}

		private static MapEdge EdgeOfCell(IntVec3 c, Map map)
		{
			if (c.z >= map.Size.z - 1)
			{
				return MapEdge.North;
			}

			if (c.x >= map.Size.x - 1)
			{
				return MapEdge.East;
			}

			if (c.z <= 0)
			{
				return MapEdge.South;
			}

			return MapEdge.West;
		}

		private static bool AreAdjacent(MapEdge a, MapEdge b)
		{
			int d = Mathf.Abs((int)a - (int)b);
			return d == 1 || d == 3;
		}

		private static MapEdge AdjacentEdgeAwayFromColony(MapEdge from, IntVec3 colony, Map map)
		{
			MapEdge farA = colony.z < map.Size.z / 2 ? MapEdge.North : MapEdge.South;
			MapEdge farB = colony.x < map.Size.x / 2 ? MapEdge.East : MapEdge.West;
			if (from != farA && AreAdjacent(from, farA))
			{
				return farA;
			}

			if (from != farB && AreAdjacent(from, farB))
			{
				return farB;
			}

			return (MapEdge)(((int)from + 1) % 4);
		}

		private static IntVec3 MakeInlandWaypoint(Map map, IntVec3 start, IntVec3 end, MapEdge edgeA, MapEdge edgeB, IntVec3 colony)
		{
			Vector3 colonyV = colony.ToVector3Shifted();
			IntVec3 corner = CornerOf(edgeA, edgeB, map);
			Vector3 far;
			if (corner.IsValid)
			{
				far = corner.ToVector3Shifted();
			}
			else
			{
				Vector3 mid = (start.ToVector3Shifted() + end.ToVector3Shifted()) * 0.5f;
				Vector3 ab = end.ToVector3Shifted() - start.ToVector3Shifted();
				Vector3 perp = new Vector3(-ab.z, 0f, ab.x);
				if (perp.sqrMagnitude < 0.01f)
				{
					perp = mid - colonyV;
				}

				if (Vector3.Dot(perp, mid - colonyV) < 0f)
				{
					perp = -perp;
				}

				perp.y = 0f;
				if (perp.sqrMagnitude > 0.01f)
				{
					perp.Normalize();
				}

				far = mid + perp * Mathf.Min(map.Size.x, map.Size.z) * 0.5f;
			}

			Vector3 dir = far - colonyV;
			dir.y = 0f;
			if (dir.sqrMagnitude < 1f)
			{
				dir = map.Center.ToVector3Shifted() - colonyV;
				dir.y = 0f;
			}

			if (dir.sqrMagnitude < 0.01f)
			{
				dir = Vector3.right;
			}

			dir.Normalize();
			Vector3 inland = colonyV + dir * (MinColonyClearance + DeepDetourClearance);
			return ClampInlandCell(map, inland.ToIntVec3(), colony);
		}

		private static IntVec3 CornerOf(MapEdge a, MapEdge b, Map map)
		{
			bool n = a == MapEdge.North || b == MapEdge.North;
			bool s = a == MapEdge.South || b == MapEdge.South;
			bool e = a == MapEdge.East || b == MapEdge.East;
			bool w = a == MapEdge.West || b == MapEdge.West;
			if (n && s || e && w)
			{
				return IntVec3.Invalid;
			}

			int x = e ? map.Size.x - 1 : 0;
			int z = n ? map.Size.z - 1 : 0;
			if (!n && !s)
			{
				z = map.Size.z / 2;
			}

			if (!e && !w)
			{
				x = map.Size.x / 2;
			}

			return new IntVec3(x, 0, z);
		}

		private static List<IntVec3> BuildMeanderPath(Map map, IntVec3 start, IntVec3 end, IntVec3 inland, IntVec3 colony)
		{
			var points = new List<Vector3>
			{
				start.ToVector3Shifted(),
				inland.ToVector3Shifted(),
				end.ToVector3Shifted(),
			};

			float offset = Mathf.Min(map.Size.x, map.Size.z) * 0.06f;
			for (int iter = 0; iter < 3; iter++)
			{
				var next = new List<Vector3>(points.Count * 2);
				next.Add(points[0]);
				for (int i = 0; i < points.Count - 1; i++)
				{
					Vector3 a = points[i];
					Vector3 b = points[i + 1];
					Vector3 mid = (a + b) * 0.5f;
					Vector3 dir = b - a;
					Vector3 perp = new Vector3(-dir.z, 0f, dir.x);
					if (perp.sqrMagnitude > 0.01f)
					{
						perp.Normalize();
						mid += perp * Rand.Range(-offset, offset);
					}

					mid = ClampInlandCell(map, mid.ToIntVec3(), colony).ToVector3Shifted();
					next.Add(mid);
					next.Add(b);
				}

				points = next;
				offset *= 0.5f;
			}

			var waypoints = new List<IntVec3>(points.Count);
			for (int i = 0; i < points.Count; i++)
			{
				waypoints.Add(points[i].ToIntVec3());
			}

			waypoints[0] = start;
			waypoints[waypoints.Count - 1] = end;
			return RasterizeWaypoints(map, waypoints);
		}

		private static List<IntVec3> RasterizeWaypoints(Map map, List<IntVec3> waypoints)
		{
			var path = new List<IntVec3>();
			for (int i = 0; i < waypoints.Count - 1; i++)
			{
				AppendLine(map, path, waypoints[i], waypoints[i + 1]);
			}

			if (path.Count == 0)
			{
				return path;
			}

			var deduped = new List<IntVec3>(path.Count);
			for (int i = 0; i < path.Count; i++)
			{
				if (deduped.Count == 0 || deduped[deduped.Count - 1] != path[i])
				{
					deduped.Add(path[i]);
				}
			}

			return deduped;
		}

		private static void AppendLine(Map map, List<IntVec3> path, IntVec3 from, IntVec3 to)
		{
			int x0 = from.x;
			int z0 = from.z;
			int x1 = to.x;
			int z1 = to.z;
			int dx = Mathf.Abs(x1 - x0);
			int dz = Mathf.Abs(z1 - z0);
			int sx = x0 < x1 ? 1 : -1;
			int sz = z0 < z1 ? 1 : -1;
			int err = dx - dz;
			int x = x0;
			int z = z0;
			int guard = dx + dz + 2;
			for (int i = 0; i < guard; i++)
			{
				IntVec3 c = new IntVec3(x, 0, z);
				if (c.InBounds(map))
				{
					path.Add(c);
				}

				if (x == x1 && z == z1)
				{
					break;
				}

				int e2 = 2 * err;
				if (e2 > -dz)
				{
					err -= dz;
					x += sx;
				}

				if (e2 < dx)
				{
					err += dx;
					z += sz;
				}
			}
		}

		private static bool[] BuildRiverMask(Map map, List<IntVec3> centerline)
		{
			int n = map.cellIndices.NumGridCells;
			bool[] river = new bool[n];
			int r = RiverHalfWidth;
			for (int i = 0; i < centerline.Count; i++)
			{
				IntVec3 c = centerline[i];
				for (int dx = -r; dx <= r; dx++)
				{
					for (int dz = -r; dz <= r; dz++)
					{
						if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) > r)
						{
							continue;
						}

						IntVec3 p = new IntVec3(c.x + dx, 0, c.z + dz);
						if (p.InBounds(map))
						{
							river[map.cellIndices.CellToIndex(p)] = true;
						}
					}
				}
			}

			return river;
		}

		private static bool[] FloodFillLand(Map map, bool[] river, int seedIndex)
		{
			int n = map.cellIndices.NumGridCells;
			bool[] land = new bool[n];
			if (seedIndex < 0 || seedIndex >= n || river[seedIndex])
			{
				return land;
			}

			var q = new Queue<int>();
			land[seedIndex] = true;
			q.Enqueue(seedIndex);
			while (q.Count > 0)
			{
				int idx = q.Dequeue();
				IntVec3 c = map.cellIndices.IndexToCell(idx);
				for (int i = 0; i < 4; i++)
				{
					IntVec3 nCell = c + GenAdj.CardinalDirections[i];
					if (!nCell.InBounds(map))
					{
						continue;
					}

					int nIdx = map.cellIndices.CellToIndex(nCell);
					if (land[nIdx] || river[nIdx])
					{
						continue;
					}

					land[nIdx] = true;
					q.Enqueue(nIdx);
				}
			}

			return land;
		}

		private static List<IntVec3> CollectFloodCells(Map map, bool[] river, bool[] land)
		{
			int n = map.cellIndices.NumGridCells;
			var flood = new List<IntVec3>();
			for (int i = 0; i < n; i++)
			{
				if (land[i] || river[i])
				{
					continue;
				}

				flood.Add(map.cellIndices.IndexToCell(i));
			}

			if (flood.Count == 0)
			{
				return flood;
			}

			int[] riverDist = DistanceFromRiver(map, river);
			IntVec3 origin = flood[0];
			int bestRiver = riverDist[map.cellIndices.CellToIndex(origin)];
			int bestEdge = EdgeDistance(origin, map);
			for (int i = 1; i < flood.Count; i++)
			{
				IntVec3 c = flood[i];
				int rd = riverDist[map.cellIndices.CellToIndex(c)];
				int ed = EdgeDistance(c, map);
				if (rd < bestRiver || (rd == bestRiver && ed >= bestEdge))
				{
					continue;
				}

				origin = c;
				bestRiver = rd;
				bestEdge = ed;
			}

			int ox = origin.x;
			int oz = origin.z;
			flood.Sort((a, b) =>
			{
				int ringA = CircleRing(a, ox, oz);
				int ringB = CircleRing(b, ox, oz);
				int cmp = ringA.CompareTo(ringB);
				if (cmp != 0)
				{
					return cmp;
				}

				int edgeA = EdgeDistance(a, map);
				int edgeB = EdgeDistance(b, map);
				cmp = edgeB.CompareTo(edgeA);
				if (cmp != 0)
				{
					return cmp;
				}

				cmp = a.x.CompareTo(b.x);
				if (cmp != 0)
				{
					return cmp;
				}

				return a.z.CompareTo(b.z);
			});

			return flood;
		}

		private static int CircleRing(IntVec3 c, int ox, int oz)
		{
			int dx = c.x - ox;
			int dz = c.z - oz;
			return Mathf.RoundToInt(Mathf.Sqrt(dx * dx + dz * dz));
		}

		private static int EdgeDistance(IntVec3 c, Map map)
		{
			return Mathf.Min(
				Mathf.Min(c.x, c.z),
				Mathf.Min(map.Size.x - 1 - c.x, map.Size.z - 1 - c.z));
		}

		private static int[] DistanceFromRiver(Map map, bool[] river)
		{
			int n = map.cellIndices.NumGridCells;
			int[] dist = new int[n];
			var q = new Queue<int>();
			for (int i = 0; i < n; i++)
			{
				if (river[i])
				{
					dist[i] = 0;
					q.Enqueue(i);
				}
				else
				{
					dist[i] = int.MaxValue;
				}
			}

			while (q.Count > 0)
			{
				int idx = q.Dequeue();
				IntVec3 c = map.cellIndices.IndexToCell(idx);
				int d = dist[idx] + 1;
				for (int i = 0; i < 4; i++)
				{
					IntVec3 nCell = c + GenAdj.CardinalDirections[i];
					if (!nCell.InBounds(map))
					{
						continue;
					}

					int nIdx = map.cellIndices.CellToIndex(nCell);
					if (d >= dist[nIdx])
					{
						continue;
					}

					dist[nIdx] = d;
					q.Enqueue(nIdx);
				}
			}

			return dist;
		}

		private static List<IntVec3> CollectRiverFooting(Map map, bool[] river, bool[] land)
		{
			var footing = new List<IntVec3>();
			var seen = new HashSet<IntVec3>();
			int n = map.cellIndices.NumGridCells;
			for (int i = 0; i < n; i++)
			{
				if (!river[i])
				{
					continue;
				}

				IntVec3 c = map.cellIndices.IndexToCell(i);
				for (int d = 0; d < 4; d++)
				{
					IntVec3 p = c + GenAdj.CardinalDirections[d];
					if (!p.InBounds(map))
					{
						continue;
					}

					int idx = map.cellIndices.CellToIndex(p);
					if (!land[idx] || river[idx] || !seen.Add(p))
					{
						continue;
					}

					footing.Add(p);
				}
			}

			return footing;
		}

		private static List<IntVec3> BuildOceanEllipsePath(Map map, List<IntVec3> floodCells)
		{
			var path = new List<IntVec3>();
			if (floodCells == null || floodCells.Count == 0)
			{
				return path;
			}

			var ocean = new HashSet<IntVec3>();
			for (int i = 0; i < floodCells.Count; i++)
			{
				IntVec3 c = floodCells[i];
				if (VoidAwake_GhostShipOceanUtility.CanOverlayOcean(map, c))
				{
					ocean.Add(c);
				}
			}

			if (ocean.Count == 0)
			{
				return path;
			}
			IntVec2 shipSize = VoidAwake_GhostShipDefOf.VoidAwake_GhostShip != null
				? VoidAwake_GhostShipDefOf.VoidAwake_GhostShip.Size
				: new IntVec2(3, 3);

			long sumX = 0;
			long sumZ = 0;
			int minX = int.MaxValue;
			int maxX = int.MinValue;
			int minZ = int.MaxValue;
			int maxZ = int.MinValue;
			for (int i = 0; i < floodCells.Count; i++)
			{
				IntVec3 c = floodCells[i];
				sumX += c.x;
				sumZ += c.z;
				if (c.x < minX)
				{
					minX = c.x;
				}

				if (c.x > maxX)
				{
					maxX = c.x;
				}

				if (c.z < minZ)
				{
					minZ = c.z;
				}

				if (c.z > maxZ)
				{
					maxZ = c.z;
				}
			}

			float cx = sumX / (float)floodCells.Count;
			float cz = sumZ / (float)floodCells.Count;
			float rx = Mathf.Max(5f, (maxX - minX + 1) * 0.22f);
			float rz = Mathf.Max(5f, (maxZ - minZ + 1) * 0.22f);

			for (int shrink = 0; shrink < 8; shrink++)
			{
				path.Clear();
				for (int i = 0; i < EllipseSamples; i++)
				{
					float ang = i * Mathf.PI * 2f / EllipseSamples;
					float x = cx + rx * Mathf.Cos(ang);
					float z = cz + rz * Mathf.Sin(ang);
					IntVec3 cell = SnapEllipsePointToOcean(map, ocean, shipSize, x, z, cx, cz);
					if (!cell.IsValid)
					{
						continue;
					}

					if (path.Count > 0 && path[path.Count - 1] == cell)
					{
						continue;
					}

					path.Add(cell);
				}

				if (path.Count >= MinShorePathLength)
				{
					if (path.Count > 1 && path[0] == path[path.Count - 1])
					{
						path.RemoveAt(path.Count - 1);
					}

					return path;
				}

				rx *= 0.82f;
				rz *= 0.82f;
			}

			return path;
		}

		private static IntVec3 SnapEllipsePointToOcean(
			Map map,
			HashSet<IntVec3> ocean,
			IntVec2 shipSize,
			float x,
			float z,
			float cx,
			float cz)
		{
			for (int step = 0; step < 24; step++)
			{
				float t = 1f - step / 24f;
				IntVec3 c = new IntVec3(Mathf.RoundToInt(cx + (x - cx) * t), 0, Mathf.RoundToInt(cz + (z - cz) * t));
				if (!c.InBounds(map) || !ocean.Contains(c))
				{
					continue;
				}

				if (ShipFitsInOcean(c, shipSize, map, ocean))
				{
					return c;
				}
			}

			return IntVec3.Invalid;
		}

		private static bool ShipFitsInOcean(IntVec3 pathCell, IntVec2 size, Map map, HashSet<IntVec3> ocean)
		{
			IntVec3 pos = VoidAwake_GhostShipOceanUtility.ShipPositionForPathCell(pathCell, size, map);
			CellRect rect = new CellRect(pos.x, pos.z, size.x, size.z);
			if (!rect.InBounds(map))
			{
				return false;
			}

			foreach (IntVec3 c in rect.Cells)
			{
				if (!ocean.Contains(c))
				{
					return false;
				}
			}

			return true;
		}

		private static IntVec3 FindColonyCenter(Map map)
		{
			Vector3 sum = Vector3.zero;
			int n = 0;
			List<Building> buildings = map.listerBuildings.allBuildingsColonist;
			if (buildings != null)
			{
				for (int i = 0; i < buildings.Count; i++)
				{
					Building b = buildings[i];
					if (b == null || !b.Spawned)
					{
						continue;
					}

					sum += b.Position.ToVector3Shifted();
					n++;
				}
			}

			if (n == 0)
			{
				List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
				if (colonists != null)
				{
					for (int i = 0; i < colonists.Count; i++)
					{
						Pawn p = colonists[i];
						if (p == null || !p.Spawned)
						{
							continue;
						}

						sum += p.Position.ToVector3Shifted();
						n++;
					}
				}
			}

			if (n == 0)
			{
				return map.Center;
			}

			return (sum / n).ToIntVec3();
		}

		private static IntVec3 FindLandSeed(Map map, bool[] river, IntVec3 colony)
		{
			if (colony.InBounds(map) && !river[map.cellIndices.CellToIndex(colony)])
			{
				return colony;
			}

			List<Building> buildings = map.listerBuildings.allBuildingsColonist;
			if (buildings != null)
			{
				for (int i = 0; i < buildings.Count; i++)
				{
					Building b = buildings[i];
					if (b == null || !b.Spawned)
					{
						continue;
					}

					IntVec3 c = b.Position;
					if (c.InBounds(map) && !river[map.cellIndices.CellToIndex(c)])
					{
						return c;
					}
				}
			}

			IntVec3 center = map.Center;
			if (center.InBounds(map) && !river[map.cellIndices.CellToIndex(center)])
			{
				return center;
			}

			foreach (IntVec3 c in map.AllCells)
			{
				if (!river[map.cellIndices.CellToIndex(c)])
				{
					return c;
				}
			}

			return IntVec3.Invalid;
		}

		private static IntVec3 ClampInlandCell(Map map, IntVec3 c, IntVec3 colony)
		{
			int minX = InlandClamp;
			int minZ = InlandClamp;
			int maxX = map.Size.x - 1 - InlandClamp;
			int maxZ = map.Size.z - 1 - InlandClamp;
			c = new IntVec3(Mathf.Clamp(c.x, minX, maxX), 0, Mathf.Clamp(c.z, minZ, maxZ));

			int dx = c.x - colony.x;
			int dz = c.z - colony.z;
			int cheb = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz));
			if (cheb < MinColonyClearance && cheb > 0)
			{
				float scale = MinColonyClearance / (float)cheb;
				c = new IntVec3(
					Mathf.Clamp(colony.x + Mathf.RoundToInt(dx * scale), minX, maxX),
					0,
					Mathf.Clamp(colony.z + Mathf.RoundToInt(dz * scale), minZ, maxZ));
			}

			return c;
		}
	}
}
