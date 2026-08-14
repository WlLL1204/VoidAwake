using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace VoidAwake
{
	public class VoidAwake_GenStep_GhostShipInterior : GenStep
	{
		private const int GhostMinCount = 12;
		private const int GhostMaxCount = 18;
		private const int GhostMinSpacing = 4;
		private const int GhostExitClearance = 6;
		private const int DeckMortarCount = 3;
		private const int DeckMortarGap = 1;
		private const int DeckShellStackCount = 20;

		public override int SeedPart => 184720331;

		public override void Generate(Map map, GenStepParams parms)
		{
			if (map == null)
			{
				return;
			}

			foreach (IntVec3 cell in map.AllCells)
			{
				map.terrainGrid.SetTerrain(cell, TerrainDefOf.WaterOceanDeep);
			}

			if (!VoidAwake_GhostShipMapUtility.TryLoadLayout(out Color32[] pixels, out int width, out int height))
			{
				MapGenerator.PlayerStartSpot = map.Center;
				return;
			}

			int originX = (map.Size.x - width) / 2;
			int originZ = (map.Size.z - height) / 2;
			List<IntVec3> exitCells = new List<IntVec3>();
			List<IntVec3> floorCells = new List<IntVec3>();
			ThingDef wallDef = VoidAwake_GhostShipDefOf.VoidAwake_GhostShipWall ?? ThingDefOf.Wall;
			ThingDef doorDef = ThingDefOf.Door;
			TerrainDef stairs = VoidAwake_GhostShipDefOf.VoidAwake_GhostShipStairs;

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					Color32 color = pixels[y * width + x];
					IntVec3 cell = new IntVec3(originX + x, 0, originZ + y);
					if (!cell.InBounds(map))
					{
						continue;
					}

					if (VoidAwake_GhostShipMapUtility.Matches(color, VoidAwake_GhostShipMapUtility.ColorSea))
					{
						continue;
					}

					bool isStairs = VoidAwake_GhostShipMapUtility.Matches(color, VoidAwake_GhostShipMapUtility.ColorStairs);
					map.terrainGrid.SetTerrain(cell, isStairs && stairs != null ? stairs : TerrainDefOf.WoodPlankFloor);
					map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);

					if (VoidAwake_GhostShipMapUtility.Matches(color, VoidAwake_GhostShipMapUtility.ColorWall))
					{
						SpawnStuffable(wallDef, cell, map);
					}
					else if (VoidAwake_GhostShipMapUtility.Matches(color, VoidAwake_GhostShipMapUtility.ColorDoor))
					{
						SpawnStuffable(doorDef, cell, map);
					}
					else if (VoidAwake_GhostShipMapUtility.Matches(color, VoidAwake_GhostShipMapUtility.ColorExit))
					{
						exitCells.Add(cell);
					}
					else if (VoidAwake_GhostShipMapUtility.Matches(color, VoidAwake_GhostShipMapUtility.ColorWood))
					{
						floorCells.Add(cell);
					}
				}
			}

			ThingDef exitDef = ResolveExitDef();
			if (exitDef != null && exitCells.Count > 0)
			{
				IntVec3 exitPos = exitCells[0];
				for (int i = 1; i < exitCells.Count; i++)
				{
					IntVec3 c = exitCells[i];
					if (c.z < exitPos.z || (c.z == exitPos.z && c.x < exitPos.x))
					{
						exitPos = c;
					}
				}

				Thing exit = ThingMaker.MakeThing(exitDef);
				GenSpawn.Spawn(exit, exitPos, map, WipeMode.Vanish);
				MapGenerator.PlayerStartSpot = exit.Position;
			}
			else
			{
				MapGenerator.PlayerStartSpot = map.Center;
			}

			SpawnRoomFurniture(map, MapGenerator.PlayerStartSpot);
			SpawnInteriorGhosts(map, floorCells, MapGenerator.PlayerStartSpot);
		}

		private static void SpawnInteriorGhosts(Map map, List<IntVec3> floors, IntVec3 exitPos)
		{
			if (floors == null || floors.Count == 0)
			{
				return;
			}

			List<IntVec3> candidates = new List<IntVec3>();
			for (int i = 0; i < floors.Count; i++)
			{
				IntVec3 cell = floors[i];
				if (!cell.Standable(map) || cell.GetEdifice(map) != null)
				{
					continue;
				}

				if (cell.DistanceTo(exitPos) < GhostExitClearance)
				{
					continue;
				}

				candidates.Add(cell);
			}

			candidates.Shuffle();
			int target = Mathf.Clamp(candidates.Count / 90, GhostMinCount, GhostMaxCount);
			List<IntVec3> used = new List<IntVec3>();
			for (int i = 0; i < candidates.Count && used.Count < target; i++)
			{
				IntVec3 cell = candidates[i];
				bool tooClose = false;
				for (int j = 0; j < used.Count; j++)
				{
					IntVec3 placed = used[j];
					int dx = placed.x - cell.x;
					if (dx < 0)
					{
						dx = -dx;
					}

					int dz = placed.z - cell.z;
					if (dz < 0)
					{
						dz = -dz;
					}

					if ((dx > dz ? dx : dz) < GhostMinSpacing)
					{
						tooClose = true;
						break;
					}
				}

				if (tooClose)
				{
					continue;
				}

				if (VoidAwake_GhostUtility.TrySpawnGhost(map, cell) != null)
				{
					used.Add(cell);
				}
			}

			if (used.Count == 0)
			{
				return;
			}

			List<Pawn> ghosts = new List<Pawn>();
			foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
			{
				if (VoidAwake_GhostUtility.IsGhostPawn(pawn))
				{
					ghosts.Add(pawn);
				}
			}

			if (ghosts.Count > 0 && ghosts[0].Faction != null)
			{
				LordMaker.MakeNewLord(ghosts[0].Faction, new VoidAwake_LordJob_GhostShipWander(), map, ghosts);
			}
		}

		private static void SpawnRoomFurniture(Map map, IntVec3 exitPos)
		{
			map.regionAndRoomUpdater.Enabled = true;
			map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();

			ThingDef bed = Def("Bed");
			ThingDef doubleBed = Def("DoubleBed");
			ThingDef table1 = Def("Table1x2c");
			ThingDef table2 = Def("Table2x2c");
			ThingDef chair = Def("DiningChair");
			ThingDef stool = Def("Stool");
			ThingDef shelf = Def("Shelf");
			ThingDef dresser = Def("Dresser");
			ThingDef endTable = Def("EndTable");
			ThingDef lamp = Def("TorchLamp");
			ThingDef armchair = Def("Armchair");
			ThingDef pot = Def("PlantPot");
			ThingDef tailor = Def("HandTailoringBench");
			ThingDef stove = Def("FueledStove");
			ThingDef research = Def("SimpleResearchBench");

			foreach (Room room in map.regionGrid.AllRooms)
			{
				if (room == null || room.PsychologicallyOutdoors || room.IsDoorway || room.CellCount < 8)
				{
					continue;
				}

				int minX = int.MaxValue;
				int maxX = int.MinValue;
				int minZ = int.MaxValue;
				int maxZ = int.MinValue;
				foreach (IntVec3 c in room.Cells)
				{
					if (c.x < minX) minX = c.x;
					if (c.x > maxX) maxX = c.x;
					if (c.z < minZ) minZ = c.z;
					if (c.z > maxZ) maxZ = c.z;
				}

				int width = maxX - minX + 1;
				int height = maxZ - minZ + 1;
				if (width < 3 || height < 3)
				{
					continue;
				}

				int cells = room.CellCount;
				int area = width * height;
				float fill = area > 0 ? (float)cells / area : 0f;
				if (room == exitPos.GetRoom(map))
				{
					PlaceDeckBattery(room, map, exitPos);
					continue;
				}

				// Corridors are long and 1-3 tiles wide. Cabins are filled rectangles
				// reached through a door (min side >= 4, most cells inside the bounding box).
				bool rectangularCabin = width >= 4 && height >= 4 && fill >= 0.75f;
				if (!rectangularCabin)
				{
					continue;
				}

				TryPlaceInRoom(lamp, room, map, exitPos, true);
				if (cells < 16)
				{
					TryPlaceInRoom(bed, room, map, exitPos, true);
					TryPlaceInRoom(endTable ?? stool, room, map, exitPos, true);
					continue;
				}

				if (cells < 28)
				{
					TryPlaceInRoom(bed, room, map, exitPos, true);
					TryPlaceInRoom(endTable ?? dresser, room, map, exitPos, true);
					TryPlaceInRoom(table1 ?? table2, room, map, exitPos, false);
					TryPlaceInRoom(chair ?? stool, room, map, exitPos, false);
					TryPlaceInRoom(chair ?? stool, room, map, exitPos, false);
					continue;
				}

				TryPlaceInRoom(doubleBed ?? bed, room, map, exitPos, true);

				TryPlaceInRoom(dresser, room, map, exitPos, true);
				TryPlaceInRoom(table2 ?? table1, room, map, exitPos, false);
				TryPlaceInRoom(chair ?? armchair, room, map, exitPos, false);
				TryPlaceInRoom(chair ?? stool, room, map, exitPos, false);
				TryPlaceInRoom(shelf, room, map, exitPos, true);
				TryPlaceInRoom(pot, room, map, exitPos, false);

				int roll = Rand.RangeInclusive(0, 2);
				if (roll == 0)
				{
					TryPlaceInRoom(tailor, room, map, exitPos, true);
				}
				else if (roll == 1)
				{
					TryPlaceInRoom(stove, room, map, exitPos, true);
				}
				else
				{
					TryPlaceInRoom(research, room, map, exitPos, true);
				}
			}
		}

		private static ThingDef Def(string defName)
		{
			return DefDatabase<ThingDef>.GetNamedSilentFail(defName);
		}

		private static void PlaceDeckBattery(Room room, Map map, IntVec3 exitPos)
		{
			ThingDef mortar = Def("Turret_Mortar");
			if (mortar == null || room == null)
			{
				return;
			}

			Rot4 mortarRot = Rot4.North;
			int mortarW = mortar.Size.x;
			int stride = mortarW + DeckMortarGap;
			int rowWidth = mortarW * DeckMortarCount + DeckMortarGap * (DeckMortarCount - 1);
			int roomCenterX = 0;
			int count = 0;
			foreach (IntVec3 c in room.Cells)
			{
				roomCenterX += c.x;
				count++;
			}

			if (count > 0)
			{
				roomCenterX /= count;
			}

			IntVec3 best = IntVec3.Invalid;
			int bestScore = int.MinValue;
			foreach (IntVec3 cell in room.Cells)
			{
				if (cell.x + rowWidth - 1 > map.Size.x)
				{
					continue;
				}

				bool rowOk = true;
				for (int i = 0; i < DeckMortarCount; i++)
				{
					IntVec3 origin = new IntVec3(cell.x + i * stride, 0, cell.z);
					if (!CanPlaceBuilding(mortar, origin, mortarRot, room, map, exitPos, 4f))
					{
						rowOk = false;
						break;
					}
				}

				if (!rowOk)
				{
					continue;
				}

				int score = cell.z - exitPos.z;
				score -= Abs(cell.x + rowWidth / 2 - roomCenterX);
				if (LockerRowClear(cell, rowWidth, mortar.Size.z, room, map))
				{
					score += 20;
				}

				if (score > bestScore)
				{
					bestScore = score;
					best = cell;
				}
			}

			if (!best.IsValid)
			{
				return;
			}

			for (int i = 0; i < DeckMortarCount; i++)
			{
				IntVec3 origin = new IntVec3(best.x + i * stride, 0, best.z);
				TrySpawnFurniture(mortar, origin, mortarRot, room, map);
			}

			ThingDef locker = Def("AncientLockerBank") ?? Def("Shelf");
			Rot4 lockerRot = Rot4.South;
			int lockerZ = best.z + mortar.Size.z;
			if (locker != null && LockerRowClear(best, rowWidth, mortar.Size.z, room, map))
			{
				IntVec3 left = OriginForMinCorner(locker, lockerRot, best.x, lockerZ);
				IntVec3 right = OriginForMinCorner(locker, lockerRot, best.x + rowWidth - locker.Size.x, lockerZ);
				if (left.IsValid)
				{
					TrySpawnFurniture(locker, left, lockerRot, room, map);
				}

				if (right.IsValid)
				{
					TrySpawnFurniture(locker, right, lockerRot, room, map);
				}
			}

			int shellLeft = best.x + mortarW + DeckMortarGap;
			int shellRight = shellLeft + 1;
			if (!SpawnShellStack(Def("Shell_HighExplosive"), new IntVec3(shellLeft, 0, lockerZ), room, map))
			{
				SpawnShellStack(Def("Shell_HighExplosive"), new IntVec3(best.x + mortarW, 0, best.z), room, map);
			}

			if (!SpawnShellStack(Def("Shell_Incendiary") ?? Def("Shell_HighExplosive"), new IntVec3(shellRight, 0, lockerZ), room, map))
			{
				SpawnShellStack(Def("Shell_Incendiary") ?? Def("Shell_HighExplosive"), new IntVec3(best.x + stride + mortarW, 0, best.z), room, map);
			}
		}

		private static IntVec3 OriginForMinCorner(ThingDef def, Rot4 rot, int minX, int minZ)
		{
			for (int x = minX - 2; x <= minX + 2; x++)
			{
				IntVec3 cell = new IntVec3(x, 0, minZ);
				CellRect occupied = GenAdj.OccupiedRect(cell, rot, def.Size);
				if (occupied.minX == minX && occupied.minZ == minZ)
				{
					return cell;
				}
			}

			return IntVec3.Invalid;
		}

		private static bool LockerRowClear(IntVec3 mortarOrigin, int rowWidth, int mortarHeight, Room room, Map map)
		{
			int z = mortarOrigin.z + mortarHeight;
			for (int x = mortarOrigin.x; x < mortarOrigin.x + rowWidth; x++)
			{
				IntVec3 c = new IntVec3(x, 0, z);
				if (!c.InBounds(map) || c.GetRoom(map) != room || !c.Standable(map) || c.GetEdifice(map) != null || c.GetDoor(map) != null)
				{
					return false;
				}
			}

			return true;
		}

		private static bool CanPlaceBuilding(ThingDef def, IntVec3 cell, Rot4 rot, Room room, Map map, IntVec3 exitPos, float minExitDist)
		{
			if (def == null || cell.DistanceTo(exitPos) < minExitDist)
			{
				return false;
			}

			CellRect occupied = GenAdj.OccupiedRect(cell, rot, def.Size);
			if (!occupied.InBounds(map))
			{
				return false;
			}

			foreach (IntVec3 c in occupied)
			{
				if (c.GetRoom(map) != room || !c.Standable(map) || c.GetEdifice(map) != null || c.GetDoor(map) != null)
				{
					return false;
				}

				if (c.GetTerrain(map) == VoidAwake_GhostShipDefOf.VoidAwake_GhostShipStairs)
				{
					return false;
				}
			}

			if (!def.hasInteractionCell)
			{
				return true;
			}

			IntVec3 interact = ThingUtility.InteractionCellWhenAt(def, cell, rot, map);
			return interact.InBounds(map) && interact.Standable(map) && interact.GetEdifice(map) == null && interact.GetRoom(map) == room;
		}

		private static bool SpawnShellStack(ThingDef def, IntVec3 cell, Room room, Map map)
		{
			if (def == null || !cell.InBounds(map) || cell.GetRoom(map) != room || !cell.Standable(map) || cell.GetEdifice(map) != null)
			{
				return false;
			}

			Thing shell = ThingMaker.MakeThing(def);
			shell.stackCount = Mathf.Min(DeckShellStackCount, def.stackLimit);
			return GenPlace.TryPlaceThing(shell, cell, map, ThingPlaceMode.Direct);
		}

		private static int Abs(int value)
		{
			return value < 0 ? -value : value;
		}

		private static bool TryPlaceInRoom(ThingDef def, Room room, Map map, IntVec3 exitPos, bool preferWall)
		{
			if (def == null || room == null)
			{
				return false;
			}

			List<IntVec3> cells = new List<IntVec3>(room.CellCount);
			foreach (IntVec3 c in room.Cells)
			{
				if (c.DistanceTo(exitPos) < 3f)
				{
					continue;
				}

				if (preferWall && !TouchesWall(c, map))
				{
					continue;
				}

				cells.Add(c);
			}

			if (cells.Count == 0 && preferWall)
			{
				return TryPlaceInRoom(def, room, map, exitPos, false);
			}

			cells.Shuffle();
			for (int i = 0; i < cells.Count; i++)
			{
				for (int r = 0; r < 4; r++)
				{
					if (TrySpawnFurniture(def, cells[i], new Rot4(r), room, map))
					{
						return true;
					}
				}
			}

			return false;
		}

		private static bool TouchesWall(IntVec3 cell, Map map)
		{
			for (int i = 0; i < 4; i++)
			{
				IntVec3 n = cell + GenAdj.CardinalDirections[i];
				if (!n.InBounds(map))
				{
					continue;
				}

				Building edifice = n.GetEdifice(map);
				if (edifice != null && edifice.def.Fillage == FillCategory.Full)
				{
					return true;
				}
			}

			return false;
		}

		private static bool TrySpawnFurniture(ThingDef def, IntVec3 cell, Rot4 rot, Room room, Map map)
		{
			CellRect occupied = GenAdj.OccupiedRect(cell, rot, def.Size);
			if (!occupied.InBounds(map))
			{
				return false;
			}

			foreach (IntVec3 c in occupied)
			{
				if (c.GetRoom(map) != room || !c.Standable(map) || c.GetEdifice(map) != null || c.GetDoor(map) != null)
				{
					return false;
				}
			}

			if (def.hasInteractionCell)
			{
				IntVec3 interact = ThingUtility.InteractionCellWhenAt(def, cell, rot, map);
				if (!interact.InBounds(map) || !interact.Standable(map) || interact.GetEdifice(map) != null || interact.GetRoom(map) != room)
				{
					return false;
				}
			}

			ThingDef stuff = def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null;
			Thing thing = ThingMaker.MakeThing(def, stuff);
			GenSpawn.Spawn(thing, cell, map, rot, WipeMode.Vanish);
			return true;
		}

		private static void SpawnStuffable(ThingDef def, IntVec3 cell, Map map)
		{
			if (def == null)
			{
				return;
			}

			ThingDef stuff = def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null;
			Thing thing = ThingMaker.MakeThing(def, stuff);
			GenSpawn.Spawn(thing, cell, map, WipeMode.Vanish);
		}

		private static ThingDef ResolveExitDef()
		{
			MapPortal portal = PocketMapUtility.currentlyGeneratingPortal;
			ThingDef fromPortal = portal?.def?.portal?.exitDef;
			if (fromPortal != null)
			{
				return fromPortal;
			}

			return VoidAwake_GhostShipDefOf.VoidAwake_GhostShipExit;
		}
	}
}
