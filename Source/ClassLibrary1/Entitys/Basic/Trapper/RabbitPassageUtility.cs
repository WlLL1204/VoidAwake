using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
	public static class RabbitPassageUtility
	{
		private const int MaxDoorsPerSearch = 4;

		private const int MaxFloodCells = 3000;

		/// <summary>How many stacked wall cells a single passage may tunnel under.</summary>
		private const int MaxWallThickness = 4;

		/// <summary>Free cells the trapper needs beside the exit hole to pop out at all.</summary>
		private const int MinExitOpenNeighbors = 2;

		/// <summary>Open cells behind the exit hole, so it never opens into a one-tile nook.</summary>
		private const int MinExitAreaCells = 6;

		/// <summary>
		/// Natural ground the trapper can burrow through: anything that is not a constructed floor.
		/// Soil, sand, gravel, mud, moss, packed dirt and mined rock floor all qualify.
		/// </summary>
		public static bool IsNaturalRabbitPassageTerrain(TerrainDef terrain)
		{
			if (terrain == null || terrain.IsWater)
			{
				return false;
			}

			if (terrain.layerable || terrain.BuildableByPlayer)
			{
				return false;
			}

			return terrain.passability != Traversability.Impassable;
		}

		public static bool IsValidPassageCell(Map map, IntVec3 cell)
		{
			if (map == null || !cell.InBounds(map))
			{
				return false;
			}

			if (!VoidAwake_TrapperUtility.StandableIgnoringClearables(map, cell))
			{
				return false;
			}

			// Walls, doors and existing passages all register as edifices.
			if (cell.GetEdifice(map) != null)
			{
				return false;
			}

			return IsNaturalRabbitPassageTerrain(cell.GetTerrain(map));
		}

		public static bool CanReachNormally(Pawn pawn, IntVec3 cell)
		{
			if (pawn?.Map == null || !cell.IsValid)
			{
				return false;
			}

			return pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly, false, false,
				TraverseMode.NoPassClosedDoors);
		}

		public static bool CanReachWithPassages(Pawn pawn, IntVec3 dest)
		{
			if (pawn?.Map == null || !dest.IsValid)
			{
				return false;
			}

			if (CanReachNormally(pawn, dest))
			{
				return true;
			}

			Map map = pawn.Map;
			List<Building_VoidAwake_RabbitPassage> passages = CollectPassages(map);
			if (passages.Count == 0)
			{
				return false;
			}

			return CanReachThroughPassages(map, passages, pawn.Position, dest, -1);
		}

		/// <summary>True if the pawn can reach the map edge without rabbit passages.</summary>
		public static bool CanReachOutsideNormally(Pawn pawn)
		{
			return pawn != null && pawn.Spawned && pawn.CanReachMapEdge();
		}

		public static bool HasUnreachableTrapDoor(Pawn pawn)
		{
			return CollectUnreachableTrapDoors(pawn).Count > 0;
		}

		/// <summary>
		/// Pick an entrance/exit pair straddling a wall between the trapper and a door that no
		/// existing passage already serves. Both ends must be valid before anything is dug.
		/// </summary>
		public static bool TryFindPassagePair(Pawn pawn, out IntVec3 entrance, out IntVec3 exit)
		{
			entrance = IntVec3.Invalid;
			exit = IntVec3.Invalid;
			if (pawn?.Map == null)
			{
				return false;
			}

			Map map = pawn.Map;
			List<IntVec3> doors = CollectDoorsNeedingNewPassage(pawn);
			if (doors.Count == 0)
			{
				return false;
			}

			doors.Sort((a, b) =>
				a.DistanceToSquared(pawn.Position).CompareTo(b.DistanceToSquared(pawn.Position)));

			int count = doors.Count < MaxDoorsPerSearch ? doors.Count : MaxDoorsPerSearch;
			HashSet<IntVec3> doorFlood = new HashSet<IntVec3>();
			for (int d = 0; d < count; d++)
			{
				FloodWalkableFromDoor(map, pawn, doors[d], doorFlood);
				if (TryFindPairOnFloodBoundary(pawn, doorFlood, out entrance, out exit))
				{
					return true;
				}
			}

			entrance = IntVec3.Invalid;
			exit = IntVec3.Invalid;
			return false;
		}

		public static bool TryFindUsePassageToward(Pawn pawn, IntVec3 goal, out Building_VoidAwake_RabbitPassage entrance)
		{
			entrance = null;
			if (pawn?.Map == null || !goal.IsValid || CanReachNormally(pawn, goal))
			{
				return false;
			}

			Map map = pawn.Map;
			List<Building_VoidAwake_RabbitPassage> passages = CollectPassages(map);
			if (passages.Count == 0)
			{
				return false;
			}

			Building_VoidAwake_RabbitPassage best = null;
			int bestDist = int.MaxValue;

			for (int i = 0; i < passages.Count; i++)
			{
				Building_VoidAwake_RabbitPassage passage = passages[i];
				Building_VoidAwake_RabbitPassage linked = passage?.LinkedPassage;
				if (passage == null || linked == null)
				{
					continue;
				}

				if (!CanTouchFrom(map, pawn.Position, passage.Position))
				{
					continue;
				}

				IntVec3 exitStand = FindStandableBeside(map, linked.Position, pawn.Position);
				if (!exitStand.IsValid)
				{
					continue;
				}

				if (!CanReachFrom(map, exitStand, goal)
					&& !CanReachThroughPassages(map, passages, exitStand, goal, passage.PairId))
				{
					continue;
				}

				int dist = passage.Position.DistanceToSquared(pawn.Position);
				if (dist < bestDist)
				{
					bestDist = dist;
					best = passage;
				}
			}

			entrance = best;
			return entrance != null;
		}

		public static bool TryFindUsePassageTowardOutside(Pawn pawn, out Building_VoidAwake_RabbitPassage entrance)
		{
			entrance = null;
			if (pawn?.Map == null || CanReachOutsideNormally(pawn))
			{
				return false;
			}

			Map map = pawn.Map;
			List<Building_VoidAwake_RabbitPassage> passages = CollectPassages(map);
			Building_VoidAwake_RabbitPassage best = null;
			int bestDist = int.MaxValue;

			for (int i = 0; i < passages.Count; i++)
			{
				Building_VoidAwake_RabbitPassage passage = passages[i];
				Building_VoidAwake_RabbitPassage linked = passage?.LinkedPassage;
				if (passage == null || linked == null)
				{
					continue;
				}

				if (!CanTouchFrom(map, pawn.Position, passage.Position))
				{
					continue;
				}

				IntVec3 exitStand = FindStandableBeside(map, linked.Position, pawn.Position);
				if (!exitStand.IsValid || !ExitLeadsOutside(map, passages, exitStand, passage.PairId))
				{
					continue;
				}

				int dist = passage.Position.DistanceToSquared(pawn.Position);
				if (dist < bestDist)
				{
					bestDist = dist;
					best = passage;
				}
			}

			entrance = best;
			return entrance != null;
		}

		public static IntVec3 FindStandableBeside(Map map, IntVec3 passageCell, IntVec3 preferAwayFrom)
		{
			IntVec3 best = IntVec3.Invalid;
			int bestScore = int.MinValue;
			for (int i = 0; i < 8; i++)
			{
				IntVec3 cell = passageCell + GenAdj.AdjacentCells[i];
				if (!cell.InBounds(map) || !cell.Standable(map) || cell.GetEdifice(map) != null)
				{
					continue;
				}

				int score = cell.DistanceToSquared(preferAwayFrom);
				if (score > bestScore)
				{
					bestScore = score;
					best = cell;
				}
			}

			return best;
		}

		/// <summary>
		/// Cell the trapper digs from. It must not be one of the two hole cells, otherwise the
		/// impassable hole would spawn underneath the digger and break its job.
		/// </summary>
		public static IntVec3 FindDigStandCell(Pawn pawn, IntVec3 entrance, IntVec3 exit)
		{
			if (pawn?.Map == null)
			{
				return IntVec3.Invalid;
			}

			Map map = pawn.Map;
			IntVec3 best = IntVec3.Invalid;
			int bestDist = int.MaxValue;
			for (int i = 0; i < 8; i++)
			{
				IntVec3 cell = entrance + GenAdj.AdjacentCells[i];
				if (cell == exit || !cell.InBounds(map) || !cell.Standable(map) || cell.GetEdifice(map) != null)
				{
					continue;
				}

				if (!CanReachNormally(pawn, cell))
				{
					continue;
				}

				int dist = cell.DistanceToSquared(pawn.Position);
				if (dist < bestDist)
				{
					bestDist = dist;
					best = cell;
				}
			}

			return best;
		}

		public static IntVec3 FindBestTrapGoalWithPassages(Pawn pawn)
		{
			if (pawn?.Map == null)
			{
				return IntVec3.Invalid;
			}

			Map map = pawn.Map;
			List<Building_VoidAwake_RabbitPassage> passages = CollectPassages(map);
			if (passages.Count == 0)
			{
				return IntVec3.Invalid;
			}

			IntVec3 best = IntVec3.Invalid;
			int bestDist = int.MaxValue;

			foreach (Building_Door door in map.listerBuildings.AllBuildingsColonistOfClass<Building_Door>())
			{
				if (door == null || !door.Spawned)
				{
					continue;
				}

				if (VoidAwake_TrapperUtility.IsDoorReservedByOther(map, door.Position, pawn))
				{
					continue;
				}

				foreach (IntVec3 cell in JobGiver_TrapperPlaceTrap.CellsAroundDoor(door.Position))
				{
					if (!JobGiver_TrapperPlaceTrap.IsValidTrapCellPhysical(pawn, cell))
					{
						continue;
					}

					if (CanReachNormally(pawn, cell))
					{
						continue;
					}

					if (!CanReachThroughPassages(map, passages, pawn.Position, cell, -1))
					{
						continue;
					}

					int dist = cell.DistanceToSquared(pawn.Position);
					if (dist < bestDist)
					{
						bestDist = dist;
						best = cell;
					}
				}
			}

			return best;
		}

		public static void SpawnPassagePair(Map map, IntVec3 entrance, IntVec3 exit, Pawn owner)
		{
			if (map == null || !IsValidPassageCell(map, entrance) || !IsValidPassageCell(map, exit))
			{
				return;
			}

			VoidAwake_TrapperUtility.ClearCellObstacles(map, entrance);
			VoidAwake_TrapperUtility.ClearCellObstacles(map, exit);

			// The holes are impassable edifices; anything left standing there would be sealed in.
			NudgePawnsOff(map, entrance);
			NudgePawnsOff(map, exit);

			int pairId = Find.UniqueIDsManager.GetNextThingID();
			Building_VoidAwake_RabbitPassage a = (Building_VoidAwake_RabbitPassage)ThingMaker.MakeThing(
				VoidAwake_TrapperDefOf.VoidAwake_RabbitPassage);
			Building_VoidAwake_RabbitPassage b = (Building_VoidAwake_RabbitPassage)ThingMaker.MakeThing(
				VoidAwake_TrapperDefOf.VoidAwake_RabbitPassage);
			a.SetFaction(Faction.OfEntities);
			b.SetFaction(Faction.OfEntities);
			a.ConfigurePair(pairId, exit, owner?.thingIDNumber ?? -1);
			b.ConfigurePair(pairId, entrance, owner?.thingIDNumber ?? -1);
			GenSpawn.Spawn(a, entrance, map, WipeMode.Vanish);
			GenSpawn.Spawn(b, exit, map, WipeMode.Vanish);
		}

		/// <summary>
		/// Removes passages that no longer earn their keep: leftovers whose twin is gone, pairs whose
		/// two ends can be walked between anyway, and later pairs linking the same two areas as an
		/// earlier one. Returns how many passages were destroyed.
		/// </summary>
		public static int PruneRedundantPassages(Map map)
		{
			if (map == null)
			{
				return 0;
			}

			List<Building_VoidAwake_RabbitPassage> passages = CollectPassages(map);
			if (passages.Count == 0)
			{
				return 0;
			}

			int removed = 0;
			List<Building_VoidAwake_RabbitPassage> pairHeads = new List<Building_VoidAwake_RabbitPassage>();
			HashSet<int> seenPairs = new HashSet<int>();

			for (int i = 0; i < passages.Count; i++)
			{
				Building_VoidAwake_RabbitPassage p = passages[i];
				if (p == null || p.Destroyed)
				{
					continue;
				}

				if (p.LinkedPassage == null)
				{
					if (!IsPassageInUse(map, p))
					{
						p.Destroy(DestroyMode.Vanish);
						removed++;
					}

					continue;
				}

				if (seenPairs.Add(p.PairId))
				{
					pairHeads.Add(p);
				}
			}

			// Oldest pair first, so a duplicate dug later is the one that goes.
			pairHeads.Sort((a, b) => a.PairId.CompareTo(b.PairId));

			List<IntVec3> keptNear = new List<IntVec3>();
			List<IntVec3> keptFar = new List<IntVec3>();

			for (int i = 0; i < pairHeads.Count; i++)
			{
				Building_VoidAwake_RabbitPassage head = pairHeads[i];
				Building_VoidAwake_RabbitPassage linked = head?.LinkedPassage;
				if (head == null || head.Destroyed || linked == null)
				{
					continue;
				}

				IntVec3 standA = FindStandableBeside(map, head.Position, linked.Position);
				IntVec3 standB = FindStandableBeside(map, linked.Position, head.Position);
				if (!standA.IsValid || !standB.IsValid)
				{
					continue;
				}

				if (IsPassageInUse(map, head))
				{
					keptNear.Add(standA);
					keptFar.Add(standB);
					continue;
				}

				// Pointless: both ends already sit on the same walkable side.
				if (CanReachFrom(map, standA, standB))
				{
					head.Destroy(DestroyMode.Vanish);
					removed += 2;
					continue;
				}

				if (LinksSameAreasAsKept(map, keptNear, keptFar, standA, standB))
				{
					head.Destroy(DestroyMode.Vanish);
					removed += 2;
					continue;
				}

				keptNear.Add(standA);
				keptFar.Add(standB);
			}

			return removed;
		}

		private static bool LinksSameAreasAsKept(
			Map map,
			List<IntVec3> keptNear,
			List<IntVec3> keptFar,
			IntVec3 standA,
			IntVec3 standB)
		{
			for (int i = 0; i < keptNear.Count; i++)
			{
				bool sameWay = CanReachFrom(map, standA, keptNear[i]) && CanReachFrom(map, standB, keptFar[i]);
				bool crossWay = CanReachFrom(map, standA, keptFar[i]) && CanReachFrom(map, standB, keptNear[i]);
				if (sameWay || crossWay)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>True if a pawn is currently crossing this passage, so pruning must leave it alone.</summary>
		private static bool IsPassageInUse(Map map, Building_VoidAwake_RabbitPassage passage)
		{
			Building_VoidAwake_RabbitPassage linked = passage?.LinkedPassage;
			IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
			for (int i = 0; i < pawns.Count; i++)
			{
				Job job = pawns[i]?.CurJob;
				if (job == null || job.def != VoidAwake_TrapperDefOf.VoidAwake_UseRabbitPassage)
				{
					continue;
				}

				Thing target = job.targetA.Thing;
				if (target != null && (target == passage || (linked != null && target == linked)))
				{
					return true;
				}
			}

			return false;
		}

		public static void DestroyAllPassagesOnMap(Map map)
		{
			if (map == null)
			{
				return;
			}

			List<Building_VoidAwake_RabbitPassage> passages = CollectPassages(map);
			for (int i = 0; i < passages.Count; i++)
			{
				if (passages[i] != null && !passages[i].Destroyed)
				{
					passages[i].Destroy(DestroyMode.Vanish);
				}
			}
		}

		/// <summary>
		/// True while this trapper still owns a living pair. One pair per trapper keeps a whole wall
		/// from filling up with holes; other trappers' passages are reused instead of duplicated.
		/// </summary>
		public static bool HasOwnPassage(Map map, int ownerId)
		{
			if (map == null || ownerId < 0)
			{
				return false;
			}

			List<Building_VoidAwake_RabbitPassage> passages = CollectPassages(map);
			for (int i = 0; i < passages.Count; i++)
			{
				Building_VoidAwake_RabbitPassage p = passages[i];
				if (p != null && p.OwnerId == ownerId && p.LinkedPassage != null)
				{
					return true;
				}
			}

			return false;
		}

		public static void DestroyPassagesOwnedBy(Map map, int ownerId)
		{
			if (map == null || ownerId < 0)
			{
				return;
			}

			List<Building_VoidAwake_RabbitPassage> passages = CollectPassages(map);
			for (int i = 0; i < passages.Count; i++)
			{
				Building_VoidAwake_RabbitPassage p = passages[i];
				if (p != null && !p.Destroyed && p.OwnerId == ownerId)
				{
					p.Destroy(DestroyMode.Vanish);
				}
			}
		}

		/// <summary>Colonist doors that have a usable trap cell but none the trapper can walk to.</summary>
		public static List<IntVec3> CollectUnreachableTrapDoors(Pawn pawn)
		{
			List<IntVec3> result = new List<IntVec3>();
			if (pawn?.Map == null)
			{
				return result;
			}

			Map map = pawn.Map;
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

				bool hasPhysical = false;
				bool hasNormalReach = false;
				foreach (IntVec3 cell in JobGiver_TrapperPlaceTrap.CellsAroundDoor(doorCell))
				{
					if (!JobGiver_TrapperPlaceTrap.IsValidTrapCellPhysical(pawn, cell))
					{
						continue;
					}

					hasPhysical = true;
					if (CanReachNormally(pawn, cell))
					{
						hasNormalReach = true;
						break;
					}
				}

				if (hasPhysical && !hasNormalReach)
				{
					result.Add(doorCell);
				}
			}

			return result;
		}

		/// <summary>
		/// Unreachable doors that no existing passage can already carry the trapper to. Digging a new
		/// pair is only worth it for these; anything else is reached by reusing what is already dug.
		/// </summary>
		public static List<IntVec3> CollectDoorsNeedingNewPassage(Pawn pawn)
		{
			List<IntVec3> doors = CollectUnreachableTrapDoors(pawn);
			if (doors.Count == 0 || pawn?.Map == null)
			{
				return doors;
			}

			List<Building_VoidAwake_RabbitPassage> passages = CollectPassages(pawn.Map);
			if (passages.Count == 0)
			{
				return doors;
			}

			List<IntVec3> result = new List<IntVec3>();
			for (int i = 0; i < doors.Count; i++)
			{
				if (!IsDoorServedByExistingPassages(pawn, passages, doors[i]))
				{
					result.Add(doors[i]);
				}
			}

			return result;
		}

		/// <summary>True if a trap cell around the door is reachable by hopping existing passages.</summary>
		public static bool IsDoorServedByExistingPassages(
			Pawn pawn,
			List<Building_VoidAwake_RabbitPassage> passages,
			IntVec3 doorCell)
		{
			if (pawn?.Map == null || passages == null || passages.Count == 0)
			{
				return false;
			}

			Map map = pawn.Map;
			foreach (IntVec3 cell in JobGiver_TrapperPlaceTrap.CellsAroundDoor(doorCell))
			{
				if (!JobGiver_TrapperPlaceTrap.IsValidTrapCellPhysical(pawn, cell))
				{
					continue;
				}

				if (CanReachThroughPassages(map, passages, pawn.Position, cell, -1))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>Walkable cells on the door's side, bounded so a huge base cannot stall the search.</summary>
		public static void FloodWalkableFromDoor(Map map, Pawn pawn, IntVec3 doorCell, HashSet<IntVec3> into)
		{
			into.Clear();
			Queue<IntVec3> queue = new Queue<IntVec3>();
			if (doorCell.InBounds(map) && into.Add(doorCell))
			{
				queue.Enqueue(doorCell);
			}

			foreach (IntVec3 cell in JobGiver_TrapperPlaceTrap.CellsAroundDoor(doorCell))
			{
				if (!cell.InBounds(map) || !IsFloodWalkable(map, cell) || CanReachNormally(pawn, cell))
				{
					continue;
				}

				if (into.Add(cell))
				{
					queue.Enqueue(cell);
				}
			}

			while (queue.Count > 0 && into.Count < MaxFloodCells)
			{
				IntVec3 cell = queue.Dequeue();
				for (int i = 0; i < 4; i++)
				{
					IntVec3 n = cell + GenAdj.CardinalDirections[i];
					if (!n.InBounds(map) || into.Contains(n) || !IsFloodWalkable(map, n))
					{
						continue;
					}

					into.Add(n);
					queue.Enqueue(n);
				}
			}
		}

		/// <summary>
		/// Walk the door-side flood looking for a natural cell whose neighbouring wall has a natural,
		/// pawn-reachable cell on the far side.
		/// </summary>
		public static bool TryFindPairOnFloodBoundary(
			Pawn pawn,
			HashSet<IntVec3> doorFlood,
			out IntVec3 entrance,
			out IntVec3 exit)
		{
			entrance = IntVec3.Invalid;
			exit = IntVec3.Invalid;
			Map map = pawn.Map;
			int bestScore = int.MaxValue;

			foreach (IntVec3 exitCell in doorFlood)
			{
				if (!IsValidPassageCell(map, exitCell) || !HasUsableExitSpace(map, exitCell))
				{
					continue;
				}

				for (int i = 0; i < 4; i++)
				{
					IntVec3 dir = GenAdj.CardinalDirections[i];
					IntVec3 entranceCell = FindCellPastWall(map, exitCell, dir);
					if (!entranceCell.IsValid || doorFlood.Contains(entranceCell))
					{
						continue;
					}

					if (!IsValidPassageCell(map, entranceCell) || !CanReachNormally(pawn, entranceCell))
					{
						continue;
					}

					if (!HasUsableExitSpace(map, entranceCell)
						|| !FindDigStandCell(pawn, entranceCell, exitCell).IsValid)
					{
						continue;
					}

					int score = entranceCell.DistanceToSquared(pawn.Position);
					if (score < bestScore)
					{
						bestScore = score;
						entrance = entranceCell;
						exit = exitCell;
					}
				}
			}

			return entrance.IsValid && exit.IsValid;
		}

		/// <summary>
		/// Steps across up to <see cref="MaxWallThickness"/> stacked wall cells and returns the first
		/// open cell behind them, so double and triple walls can still be tunnelled under.
		/// Invalid when there is no wall at all or the stack is thicker than a passage can span.
		/// </summary>
		public static IntVec3 FindCellPastWall(Map map, IntVec3 from, IntVec3 dir)
		{
			IntVec3 cell = from + dir;
			for (int thickness = 0; thickness < MaxWallThickness; thickness++)
			{
				if (!cell.InBounds(map))
				{
					return IntVec3.Invalid;
				}

				Building edifice = cell.GetEdifice(map);
				if (edifice == null || !edifice.def.IsWall)
				{
					return thickness == 0 ? IntVec3.Invalid : cell;
				}

				cell += dir;
			}

			return IntVec3.Invalid;
		}

		/// <summary>
		/// True when the hole opens into real space instead of a one-tile nook. The hole itself becomes
		/// impassable, so the trapper needs free neighbours to pop out into plus room behind them.
		/// </summary>
		public static bool HasUsableExitSpace(Map map, IntVec3 holeCell)
		{
			HashSet<IntVec3> seen = new HashSet<IntVec3> { holeCell };
			Queue<IntVec3> queue = new Queue<IntVec3>();
			int openNeighbors = 0;

			for (int i = 0; i < 8; i++)
			{
				IntVec3 cell = holeCell + GenAdj.AdjacentCells[i];
				if (!cell.InBounds(map) || !cell.Standable(map) || cell.GetEdifice(map) != null)
				{
					continue;
				}

				openNeighbors++;
				if (seen.Add(cell))
				{
					queue.Enqueue(cell);
				}
			}

			if (openNeighbors < MinExitOpenNeighbors)
			{
				return false;
			}

			int area = 0;
			while (queue.Count > 0 && area < MinExitAreaCells)
			{
				IntVec3 cur = queue.Dequeue();
				area++;
				for (int i = 0; i < 4; i++)
				{
					IntVec3 n = cur + GenAdj.CardinalDirections[i];
					if (!n.InBounds(map) || seen.Contains(n) || !IsFloodWalkable(map, n))
					{
						continue;
					}

					seen.Add(n);
					queue.Enqueue(n);
				}
			}

			return area >= MinExitAreaCells;
		}

		public static List<Building_VoidAwake_RabbitPassage> CollectPassages(Map map)
		{
			List<Building_VoidAwake_RabbitPassage> result = new List<Building_VoidAwake_RabbitPassage>();
			if (map == null)
			{
				return result;
			}

			List<Thing> things = map.listerThings.ThingsOfDef(VoidAwake_TrapperDefOf.VoidAwake_RabbitPassage);
			if (things == null)
			{
				return result;
			}

			for (int i = 0; i < things.Count; i++)
			{
				if (things[i] is Building_VoidAwake_RabbitPassage passage && passage.Spawned)
				{
					result.Add(passage);
				}
			}

			return result;
		}

		private static void NudgePawnsOff(Map map, IntVec3 cell)
		{
			List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
			List<Pawn> occupants = null;
			for (int i = 0; i < things.Count; i++)
			{
				if (things[i] is Pawn p && p.Spawned)
				{
					if (occupants == null)
					{
						occupants = new List<Pawn>();
					}

					occupants.Add(p);
				}
			}

			if (occupants == null)
			{
				return;
			}

			for (int i = 0; i < occupants.Count; i++)
			{
				Pawn p = occupants[i];
				IntVec3 dest = FindStandableBeside(map, cell, cell);
				if (!dest.IsValid)
				{
					continue;
				}

				p.Position = dest;
				p.Notify_Teleported(false, true);
			}
		}

		private static bool IsFloodWalkable(Map map, IntVec3 cell)
		{
			if (!cell.Walkable(map))
			{
				return false;
			}

			Building edifice = cell.GetEdifice(map);
			if (edifice is Building_Door door && !door.Open)
			{
				return false;
			}

			return !(edifice is Building_VoidAwake_RabbitPassage);
		}

		private static bool CanReachFrom(Map map, IntVec3 origin, IntVec3 dest)
		{
			if (!origin.InBounds(map) || !dest.InBounds(map))
			{
				return false;
			}

			if (origin == dest)
			{
				return true;
			}

			return map.reachability.CanReach(origin, dest, PathEndMode.OnCell,
				TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly));
		}

		private static bool CanTouchFrom(Map map, IntVec3 origin, IntVec3 target)
		{
			if (!origin.InBounds(map) || !target.InBounds(map))
			{
				return false;
			}

			if (origin.AdjacentTo8WayOrInside(target))
			{
				return true;
			}

			return map.reachability.CanReach(origin, target, PathEndMode.Touch,
				TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly));
		}

		private static bool CanReachThroughPassages(
			Map map,
			List<Building_VoidAwake_RabbitPassage> passages,
			IntVec3 origin,
			IntVec3 dest,
			int skipPairId)
		{
			HashSet<int> visited = new HashSet<int>();
			if (skipPairId >= 0)
			{
				visited.Add(skipPairId);
			}

			Queue<IntVec3> queue = new Queue<IntVec3>();
			queue.Enqueue(origin);

			while (queue.Count > 0)
			{
				IntVec3 cur = queue.Dequeue();
				if (CanReachFrom(map, cur, dest))
				{
					return true;
				}

				for (int i = 0; i < passages.Count; i++)
				{
					Building_VoidAwake_RabbitPassage passage = passages[i];
					Building_VoidAwake_RabbitPassage linked = passage?.LinkedPassage;
					if (passage == null || linked == null || !visited.Add(passage.PairId))
					{
						continue;
					}

					if (!CanTouchFrom(map, cur, passage.Position))
					{
						visited.Remove(passage.PairId);
						continue;
					}

					IntVec3 exitStand = FindStandableBeside(map, linked.Position, cur);
					if (exitStand.IsValid)
					{
						queue.Enqueue(exitStand);
					}
				}
			}

			return false;
		}

		private static bool ExitLeadsOutside(
			Map map,
			List<Building_VoidAwake_RabbitPassage> passages,
			IntVec3 exitStand,
			int usedPairId)
		{
			HashSet<int> visited = new HashSet<int> { usedPairId };
			Queue<IntVec3> queue = new Queue<IntVec3>();
			queue.Enqueue(exitStand);

			while (queue.Count > 0)
			{
				IntVec3 cur = queue.Dequeue();
				if (map.reachability.CanReachMapEdge(cur,
					TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly)))
				{
					return true;
				}

				for (int i = 0; i < passages.Count; i++)
				{
					Building_VoidAwake_RabbitPassage passage = passages[i];
					Building_VoidAwake_RabbitPassage linked = passage?.LinkedPassage;
					if (passage == null || linked == null || !visited.Add(passage.PairId))
					{
						continue;
					}

					if (!CanTouchFrom(map, cur, passage.Position))
					{
						visited.Remove(passage.PairId);
						continue;
					}

					IntVec3 next = FindStandableBeside(map, linked.Position, cur);
					if (next.IsValid)
					{
						queue.Enqueue(next);
					}
				}
			}

			return false;
		}
	}
}
