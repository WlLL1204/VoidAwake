using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace VoidAwake
{
	public static class VoidAwake_GhostShipOceanUtility
	{
		/// <summary>何 tick ごとに浸食を進めるか（Odyssey マグマは 5）。</summary>
		public const int TicksPerOceanPulse = 1;

		/// <summary>1 パルスあたりのセル数。一時地形の重ね置きなのでまとめて進める。</summary>
		public const int CellsPerPulse = 24;

		public static bool ShouldBecomeOcean(IntVec3 c, Map map)
		{
			if (map == null || !c.InBounds(map))
			{
				return false;
			}

			VoidAwake_MapComponent_GhostShip comp = map.GetComponent<VoidAwake_MapComponent_GhostShip>();
			return comp != null && comp.IsPlannedOcean(c);
		}

		/// <summary>
		/// 建物・岩などのエディフィスが無いマスだけ、一時地形の海を重ねられる。
		/// </summary>
		public static bool CanOverlayOcean(Map map, IntVec3 c)
		{
			if (map == null || !c.InBounds(map))
			{
				return false;
			}

			if (c.GetEdifice(map) != null)
			{
				return false;
			}

			if (IsOceanTerrain(map.terrainGrid.TerrainAt(c)))
			{
				return false;
			}

			TerrainDef existingTemp = map.terrainGrid.TempTerrainAt(c);
			if (existingTemp != null)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// 足場用。岩は壊さない（上陸は既存の通行可能マスを使う）。
		/// </summary>
		public static void ConvertFootingCell(Map map, IntVec3 c, List<VoidAwake_OceanRockRestore> rockRestores)
		{
		}

		/// <summary>外側の隣マスが既に海洋なら、この足場セルを作り始めてよい。</summary>
		public static bool HasAdjacentOcean(Map map, IntVec3 c)
		{
			for (int i = 0; i < 8; i++)
			{
				IntVec3 n = c + GenAdj.AdjacentCells[i];
				if (n.InBounds(map) && IsOceanTerrain(n.GetTerrain(map)))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// WaterOceanDeep は tags が Ocean のみで IsWater が false になるため、海判定はこれを使う。
		/// </summary>
		public static bool IsOceanTerrain(TerrainDef terrain)
		{
			if (terrain == null)
			{
				return false;
			}

			if (terrain.IsWater || terrain.HasTag("Ocean"))
			{
				return true;
			}

			if (terrain == TerrainDefOf.WaterOceanDeep)
			{
				return true;
			}

			return VoidAwake_GhostShipDefOf.VoidAwake_OceanFlood != null
				&& terrain == VoidAwake_GhostShipDefOf.VoidAwake_OceanFlood;
		}

		public static RoofDef FindRockRoof(IntVec3 c, Map map)
		{
			RoofDef roof = map.roofGrid.RoofAt(c);
			if (roof == null)
			{
				return null;
			}

			if (roof.isNatural || roof.isThickRoof || roof == RoofDefOf.RoofRockThin || roof == RoofDefOf.RoofRockThick)
			{
				return roof;
			}

			return null;
		}

		public static void TryRecordBedrock(Map map, IntVec3 c, ThingDef rockDef, bool restoreTerrain, List<VoidAwake_OceanRockRestore> rockRestores)
		{
			if (rockRestores == null)
			{
				return;
			}

			RoofDef roof = FindRockRoof(c, map);
			if (rockDef == null && roof == null)
			{
				return;
			}

			rockRestores.Add(new VoidAwake_OceanRockRestore
			{
				cell = c,
				terrain = map.terrainGrid.TerrainAt(c),
				underTerrain = map.terrainGrid.UnderTerrainAt(c),
				naturalRockDef = rockDef,
				rockRoof = roof,
				restoreTerrain = restoreTerrain,
			});
		}

		private static void ClearFootingCell(IntVec3 c, Map map)
		{
			List<Thing> things = c.GetThingList(map);
			for (int i = things.Count - 1; i >= 0; i--)
			{
				Thing t = things[i];
				if (t == null || t.Destroyed || t is Pawn)
				{
					continue;
				}

				if (IsNaturalOrResourceRock(t)
					|| (t.def.passability == Traversability.Impassable)
					|| (t.def.category == ThingCategory.Building && t.def.Fillage == FillCategory.Full))
				{
					t.Destroy(DestroyMode.Vanish);
				}
			}
		}

		/// <summary>岩屋根と、残った岩盤地形を除去する。</summary>
		public static void RemoveBedrock(Map map, IntVec3 c)
		{
			if (!c.InBounds(map))
			{
				return;
			}

			RoofDef roof = map.roofGrid.RoofAt(c);
			if (roof != null && (roof.isThickRoof || roof == RoofDefOf.RoofRockThin || roof == RoofDefOf.RoofRockThick))
			{
				map.roofGrid.SetRoof(c, null);
			}

			List<Thing> things = c.GetThingList(map);
			for (int i = things.Count - 1; i >= 0; i--)
			{
				Thing t = things[i];
				if (t == null || t.Destroyed || t is Pawn)
				{
					continue;
				}

				if (IsNaturalOrResourceRock(t))
				{
					t.Destroy(DestroyMode.Vanish);
				}
			}
		}

		/// <summary>船の論理位置（3×3 ならパスセルが中心になるよう SW へずらす）。</summary>
		public static IntVec3 ShipPositionForPathCell(IntVec3 pathCell, IntVec2 size, Map map)
		{
			IntVec3 pos = pathCell - new IntVec3((size.x - 1) / 2, 0, (size.z - 1) / 2);
			if (!pos.InBounds(map))
			{
				return pathCell;
			}

			return pos;
		}

		public static IntVec3 FindLandSpawnNear(Map map, IntVec3 near, int maxRadius = 48)
		{
			foreach (IntVec3 c in GenRadial.RadialCellsAround(near, maxRadius, true))
			{
				if (!c.InBounds(map) || ShouldBecomeOcean(c, map) || IsOceanTerrain(c.GetTerrain(map)))
				{
					continue;
				}

				if (c.Standable(map) && c.GetFirstPawn(map) == null)
				{
					return c;
				}
			}

			IntVec3 dest = map.Center;
			Vector3 from = near.ToVector3Shifted();
			Vector3 to = dest.ToVector3Shifted();
			Vector3 delta = to - from;
			int steps = Mathf.Max(map.Size.x, map.Size.z);
			if (delta.sqrMagnitude < 0.01f)
			{
				return IntVec3.Invalid;
			}

			delta.Normalize();
			for (int i = 1; i <= steps; i++)
			{
				IntVec3 c = (from + delta * i).ToIntVec3();
				if (!c.InBounds(map) || ShouldBecomeOcean(c, map) || IsOceanTerrain(c.GetTerrain(map)))
				{
					continue;
				}

				if (c.Standable(map) && c.GetFirstPawn(map) == null)
				{
					return c;
				}
			}

			return IntVec3.Invalid;
		}

		public static void ConvertCell(Map map, IntVec3 c, List<VoidAwake_OceanRockRestore> rockRestores)
		{
			if (!CanOverlayOcean(map, c))
			{
				return;
			}

			EvacuatePawnIfAny(c, map);

			TerrainDef flood = VoidAwake_GhostShipDefOf.VoidAwake_OceanFlood;
			if (flood == null || !flood.temporary)
			{
				return;
			}

			map.terrainGrid.SetTempTerrain(c, flood);
		}

		public static void Restore(Map map, List<IntVec3> floodCells, int convertedCount, List<VoidAwake_OceanRockRestore> rockRestores)
		{
			if (floodCells != null && convertedCount > 0)
			{
				int count = Mathf.Min(convertedCount, floodCells.Count);
				for (int i = 0; i < count; i++)
				{
					RestoreTempOceanCell(map, floodCells[i]);
				}
			}

			if (rockRestores == null || rockRestores.Count == 0)
			{
				return;
			}

			for (int i = 0; i < rockRestores.Count; i++)
			{
				VoidAwake_OceanRockRestore rock = rockRestores[i];
				if (rock == null)
				{
					continue;
				}

				if (rock.restoreTerrain)
				{
					RestoreRockCell(map, rock);
				}
				else
				{
					RestoreBedrock(map, rock);
				}
			}
		}

		public static void RestoreTempOceanCell(Map map, IntVec3 c)
		{
			if (!c.InBounds(map))
			{
				return;
			}

			if (map.terrainGrid.TempTerrainAt(c) != null)
			{
				map.terrainGrid.RemoveTempTerrain(c);
			}
		}

		public static void RestoreRockCell(Map map, VoidAwake_OceanRockRestore snap)
		{
			if (snap == null)
			{
				return;
			}

			IntVec3 c = snap.cell;
			if (!c.InBounds(map))
			{
				return;
			}

			if (map.terrainGrid.TempTerrainAt(c) != null)
			{
				map.terrainGrid.RemoveTempTerrain(c, doLeavings: false, preventDestroyEffects: true);
			}

			if (snap.restoreTerrain)
			{
				map.terrainGrid.SetTerrain(c, snap.terrain ?? TerrainDefOf.Soil);
				if (snap.underTerrain != null)
				{
					map.terrainGrid.SetUnderTerrain(c, snap.underTerrain);
				}
			}

			if (snap.naturalRockDef != null)
			{
				DestroyCellContents(c, map);
				GenSpawn.Spawn(snap.naturalRockDef, c, map);
			}

			if (snap.rockRoof != null)
			{
				map.roofGrid.SetRoof(c, snap.rockRoof);
			}
		}

		public static void RestoreBedrock(Map map, VoidAwake_OceanRockRestore snap)
		{
			if (snap == null || !snap.cell.InBounds(map))
			{
				return;
			}

			if (snap.naturalRockDef != null)
			{
				DestroyCellContents(snap.cell, map);
				GenSpawn.Spawn(snap.naturalRockDef, snap.cell, map);
			}

			if (snap.rockRoof != null)
			{
				map.roofGrid.SetRoof(snap.cell, snap.rockRoof);
			}
		}

		public static ThingDef FindNaturalRockDef(IntVec3 c, Map map)
		{
			List<Thing> things = c.GetThingList(map);
			for (int i = 0; i < things.Count; i++)
			{
				Thing t = things[i];
				if (IsNaturalOrResourceRock(t))
				{
					return t.def;
				}
			}

			return null;
		}

		public static bool IsNaturalOrResourceRock(Thing t)
		{
			if (t?.def?.building == null)
			{
				return false;
			}

			return t.def.building.isNaturalRock || t.def.building.isResourceRock;
		}

		private static void EvacuatePawnIfAny(IntVec3 c, Map map)
		{
			List<Thing> things = c.GetThingList(map);
			for (int i = things.Count - 1; i >= 0; i--)
			{
				if (!(things[i] is Pawn pawn) || pawn.Destroyed)
				{
					continue;
				}

				if (!TryFindPushToShoreCell(map, c, out IntVec3 dest))
				{
					dest = map.Center;
				}

				pawn.Position = dest;
				pawn.Notify_Teleported(true, false);
			}
		}

		private static bool TryFindPushToShoreCell(Map map, IntVec3 from, out IntVec3 result)
		{
			result = IntVec3.Invalid;
			if (map == null || !from.InBounds(map))
			{
				return false;
			}

			int n = map.cellIndices.NumGridCells;
			bool[] seen = new bool[n];
			var q = new Queue<IntVec3>();
			q.Enqueue(from);
			seen[map.cellIndices.CellToIndex(from)] = true;
			IntVec3 occupiedFallback = IntVec3.Invalid;

			while (q.Count > 0)
			{
				IntVec3 c = q.Dequeue();
				if (c != from && !ShouldBecomeOcean(c, map) && c.Standable(map))
				{
					if (c.GetFirstPawn(map) == null)
					{
						result = c;
						return true;
					}

					if (!occupiedFallback.IsValid)
					{
						occupiedFallback = c;
					}
				}

				for (int d = 0; d < 4; d++)
				{
					IntVec3 nb = c + GenAdj.CardinalDirections[d];
					if (!nb.InBounds(map))
					{
						continue;
					}

					int idx = map.cellIndices.CellToIndex(nb);
					if (seen[idx])
					{
						continue;
					}

					seen[idx] = true;
					q.Enqueue(nb);
				}
			}

			if (occupiedFallback.IsValid)
			{
				result = occupiedFallback;
				return true;
			}

			return CellFinder.TryFindRandomCellNear(
				from,
				map,
				40,
				cell => !ShouldBecomeOcean(cell, map) && cell.Standable(map),
				out result);
		}

		/// <summary>植物・建造物・落ち物などを破棄。岩の復元以外は記録しない。</summary>
		private static void DestroyCellContents(IntVec3 c, Map map)
		{
			List<Thing> things = c.GetThingList(map);
			for (int i = things.Count - 1; i >= 0; i--)
			{
				Thing t = things[i];
				if (t is Pawn || t.Destroyed)
				{
					continue;
				}

				t.Destroy();
			}
		}
	}

	/// <summary>岩・鉱石セルの復元用。通常の海洋セルは一時地形の下に元地形が残る。</summary>
	public class VoidAwake_OceanRockRestore : IExposable
	{
		public IntVec3 cell;
		public TerrainDef terrain;
		public TerrainDef underTerrain;
		public ThingDef naturalRockDef;
		public RoofDef rockRoof;
		/// <summary>true=海洋で地形ごと置き換えたセル。false=足場でブロックだけ消したセル。</summary>
		public bool restoreTerrain = true;

		public void ExposeData()
		{
			Scribe_Values.Look(ref cell, "cell");
			Scribe_Defs.Look(ref terrain, "terrain");
			Scribe_Defs.Look(ref underTerrain, "underTerrain");
			Scribe_Defs.Look(ref naturalRockDef, "naturalRockDef");
			Scribe_Defs.Look(ref rockRoof, "rockRoof");
			Scribe_Values.Look(ref restoreTerrain, "restoreTerrain", true);
		}
	}

	public class VoidAwake_MapComponent_GhostShip : MapComponent
	{
		public const int ShipMoveIntervalTicks = VoidAwake_GhostShipWanderUtility.TicksPerCell;
		public const int WaveIntervalTicks = GenDate.TicksPerHour * 6;
		public const int DepartShoreDelayTicks = GenDate.TicksPerHour * 3;
		public const int DepartShoreMinDistance = 24;
		public const int BombardIntervalTicks = 900;

		private bool oceanActive;
		private bool oceanFloodComplete;
		private List<IntVec3> floodCells;
		private List<IntVec3> footingCells;
		private List<VoidAwake_OceanRockRestore> rockRestores;
		private int nextCellIndex;
		private int nextFootingIndex;
		private int tickAccumulator;
		private HashSet<IntVec3> plannedOcean;

		private VoidAwake_GhostShipPhase phase = VoidAwake_GhostShipPhase.None;
		private VoidAwake_Building_GhostShip ship;
		private List<IntVec3> orbitPath;
		private int orbitIndex;
		private int shipMoveTicks;
		private int ghostSpawnTicks;
		private int bombardTicks;
		private int ghostsReleased;
		private List<Pawn> releasedGhosts;
		private Lord ghostLord;
		private bool startedGrayPall;
		private float raidPoints;
		private float spawnedPoints;
		private bool assaultAnnounced;
		private bool approachingShore;
		private bool departingShore;
		private int waitingUntilTick;
		private int waveDumpTick;
		private IntVec3 approachShoreCell = IntVec3.Invalid;

		public VoidAwake_MapComponent_GhostShip(Map map) : base(map)
		{
		}

		public bool IsOceanActive => oceanActive;

		public bool IsOceanFloodComplete => oceanActive && oceanFloodComplete;

		public bool IsOceanFlooding => oceanActive && !oceanFloodComplete;

		public bool CanEnterShip => ship != null && ship.CanEnter;

		public VoidAwake_GhostShipPhase Phase => phase;

		public bool IsPlannedOcean(IntVec3 c)
		{
			if (plannedOcean == null)
			{
				RebuildPlannedOcean();
			}

			return plannedOcean != null && plannedOcean.Contains(c);
		}

		private void RebuildPlannedOcean()
		{
			plannedOcean = new HashSet<IntVec3>();
			if (floodCells == null)
			{
				return;
			}

			for (int i = 0; i < floodCells.Count; i++)
			{
				plannedOcean.Add(floodCells[i]);
			}
		}

		public bool TryStartOcean(float points = -1f)
		{
			if (oceanActive)
			{
				return false;
			}

			if (!VoidAwake_GhostShipFloodPlanner.TryBuildPlan(map, out VoidAwake_GhostShipFloodPlan plan))
			{
				Log.Warning("[VoidAwake] Ghost ship flood plan failed.");
				return false;
			}

			floodCells = plan.FloodCells ?? new List<IntVec3>();
			footingCells = plan.FootingCells ?? new List<IntVec3>();
			orbitPath = new List<IntVec3>();
			RebuildPlannedOcean();
			rockRestores = new List<VoidAwake_OceanRockRestore>();
			nextCellIndex = 0;
			nextFootingIndex = 0;
			tickAccumulator = 0;
			orbitIndex = 0;
			raidPoints = points > 0f ? points : StorytellerUtility.DefaultThreatPointsNow(map);
			spawnedPoints = 0f;
			assaultAnnounced = false;
			approachingShore = false;
			departingShore = false;
			waitingUntilTick = 0;
			waveDumpTick = 0;
			approachShoreCell = IntVec3.Invalid;
			oceanFloodComplete = floodCells.Count == 0;
			oceanActive = true;
			phase = VoidAwake_GhostShipPhase.OceanFlooding;
			EnsureGrayPall();
			if (oceanFloodComplete)
			{
				OnOceanFloodCompleted();
			}

			return true;
		}

		public void RestoreOcean()
		{
			ClearShipAndGhosts();
			if (!oceanActive)
			{
				return;
			}

			VoidAwake_GhostShipOceanUtility.Restore(map, floodCells, nextCellIndex, rockRestores);
			floodCells = null;
			footingCells = null;
			rockRestores = null;
			plannedOcean = null;
			orbitPath = null;
			nextCellIndex = 0;
			nextFootingIndex = 0;
			tickAccumulator = 0;
			orbitIndex = 0;
			raidPoints = 0f;
			spawnedPoints = 0f;
			assaultAnnounced = false;
			approachingShore = false;
			departingShore = false;
			waitingUntilTick = 0;
			waveDumpTick = 0;
			approachShoreCell = IntVec3.Invalid;
			oceanFloodComplete = false;
			oceanActive = false;
			phase = VoidAwake_GhostShipPhase.None;
			EndGrayPallIfStarted();
		}

		/// <summary>海洋が既に完了している想定で船を出し、浸食境目への接近を始める（Dev 用）。</summary>
		public bool TryForceSpawnShip()
		{
			if (!oceanActive)
			{
				if (!TryStartOcean())
				{
					return false;
				}
			}

			if (!oceanFloodComplete)
			{
				while (floodCells != null && nextCellIndex < floodCells.Count)
				{
					VoidAwake_GhostShipOceanUtility.ConvertCell(map, floodCells[nextCellIndex], rockRestores);
					nextCellIndex++;
				}

				oceanFloodComplete = true;
			}

			if (phase == VoidAwake_GhostShipPhase.ShipOrbiting || phase == VoidAwake_GhostShipPhase.ShipUnlocked)
			{
				return ship != null && ship.Spawned;
			}

			return TrySpawnShipOnFloodedOcean(true);
		}

		public override void MapComponentTick()
		{
			if (oceanActive || phase != VoidAwake_GhostShipPhase.None)
			{
				EnsureGrayPall();
			}

			TickOceanFlood();
			if (phase == VoidAwake_GhostShipPhase.ShipOrbiting)
			{
				TickShipOrbit();
			}

			if (phase == VoidAwake_GhostShipPhase.ShipOrbiting || phase == VoidAwake_GhostShipPhase.ShipUnlocked)
			{
				if (Find.TickManager.TicksGame % 10 == 0)
				{
					List<Map> maps = Find.Maps;
					for (int i = 0; i < maps.Count; i++)
					{
						VoidAwake_GhostUtility.TickGhostCorpses(maps[i]);
					}

					if (releasedGhosts != null)
					{
						for (int i = 0; i < releasedGhosts.Count; i++)
						{
							VoidAwake_GhostUtility.TickGhostCorpse(releasedGhosts[i]);
						}
					}
				}
			}
		}

		private void TickOceanFlood()
		{
			if (!oceanActive || oceanFloodComplete)
			{
				return;
			}

			tickAccumulator++;
			if (tickAccumulator < VoidAwake_GhostShipOceanUtility.TicksPerOceanPulse)
			{
				return;
			}

			tickAccumulator = 0;

			bool floodDone = floodCells == null || nextCellIndex >= floodCells.Count;
			if (floodDone)
			{
				oceanFloodComplete = true;
				OnOceanFloodCompleted();
				return;
			}

			int pulse = VoidAwake_GhostShipOceanUtility.CellsPerPulse;
			int end = Mathf.Min(nextCellIndex + pulse, floodCells.Count);
			while (nextCellIndex < end)
			{
				VoidAwake_GhostShipOceanUtility.ConvertCell(map, floodCells[nextCellIndex], rockRestores);
				nextCellIndex++;
			}

			TrySpawnShipOnFloodedOcean(false);

			if (nextCellIndex >= floodCells.Count)
			{
				oceanFloodComplete = true;
				OnOceanFloodCompleted();
			}
		}

		private void OnOceanFloodCompleted()
		{
			if (phase != VoidAwake_GhostShipPhase.OceanFlooding && phase != VoidAwake_GhostShipPhase.None)
			{
				return;
			}

			TrySpawnShipOnFloodedOcean(true);
		}

		private bool TrySpawnShipOnFloodedOcean(bool startOrbit)
		{
			if (ship != null && ship.Spawned)
			{
				if (startOrbit)
				{
					StartShipOrbit();
				}

				return true;
			}

			IntVec2 shipSize = VoidAwake_GhostShipDefOf.VoidAwake_GhostShip.Size;
			int converted = oceanFloodComplete
				? (floodCells != null ? floodCells.Count : 0)
				: nextCellIndex;
			if (!VoidAwake_GhostShipWanderUtility.TryFindNavigableInConvertedFlood(
				map, shipSize, floodCells, converted, out IntVec3 spawnCenter))
			{
				if (startOrbit)
				{
					Log.Warning("[VoidAwake] Ghost ship has no eroded ocean cells; cannot spawn ship.");
				}

				return false;
			}

			IntVec3 spawnCell = VoidAwake_GhostShipOceanUtility.ShipPositionForPathCell(spawnCenter, shipSize, map);
			ship = (VoidAwake_Building_GhostShip)ThingMaker.MakeThing(VoidAwake_GhostShipDefOf.VoidAwake_GhostShip);
			ship.CanEnter = false;
			GenSpawn.Spawn(ship, spawnCell, map, Rot4.North, WipeMode.Vanish);
			ship.SetOrbitDraw(spawnCell.ToVector3Shifted(), Rot4.North);

			orbitPath = new List<IntVec3>();
			orbitIndex = 0;
			shipMoveTicks = 0;
			ghostSpawnTicks = 0;
			bombardTicks = 0;
			ghostsReleased = 0;
			spawnedPoints = 0f;
			assaultAnnounced = false;
			releasedGhosts = new List<Pawn>();
			ghostLord = null;
			waitingUntilTick = 0;
			waveDumpTick = 0;
			departingShore = false;
			approachingShore = false;

			if (startOrbit)
			{
				StartShipOrbit();
			}

			return true;
		}

		private void StartShipOrbit()
		{
			if (ship == null || !ship.Spawned)
			{
				return;
			}

			if (phase == VoidAwake_GhostShipPhase.ShipOrbiting)
			{
				return;
			}

			phase = VoidAwake_GhostShipPhase.ShipOrbiting;
			BeginApproachToRandomShore();
			Messages.Message("VoidAwake_GhostShip_OrbitStarted".Translate(), ship, MessageTypeDefOf.ThreatBig, false);
		}

		private void TickShipOrbit()
		{
			if (ship == null || !ship.Spawned)
			{
				phase = VoidAwake_GhostShipPhase.None;
				return;
			}

			PruneReleasedGhosts();
			TickShipMove();
			TickBombard();
		}

		private void TickShipMove()
		{
			if (ship == null || !ship.Spawned)
			{
				return;
			}

			int now = Find.TickManager.TicksGame;
			if (approachingShore || departingShore)
			{
				FollowShipPath();
				return;
			}

			if (waitingUntilTick > now)
			{
				return;
			}

			if (waveDumpTick > 0 && now < waveDumpTick + WaveIntervalTicks)
			{
				if (now >= waveDumpTick + DepartShoreDelayTicks)
				{
					if (!BeginDepartFromShore())
					{
						waitingUntilTick = waveDumpTick + WaveIntervalTicks;
					}

					return;
				}

				waitingUntilTick = waveDumpTick + DepartShoreDelayTicks;
				return;
			}

			BeginApproachToRandomShore();
		}

		private void FollowShipPath()
		{
			IntVec2 shipSize = ship.def.Size;
			if (orbitPath == null || orbitPath.Count == 0)
			{
				OnPathArrived();
				return;
			}

			if (orbitPath.Count < 2 || orbitIndex >= orbitPath.Count - 1)
			{
				OnPathArrived();
				return;
			}

			int next = orbitIndex + 1;
			IntVec3 nextCenter = orbitPath[next];
			if (!VoidAwake_GhostShipWanderUtility.IsShipNavigable(map, nextCenter, shipSize))
			{
				if (approachingShore)
				{
					BeginApproachToRandomShore();
				}
				else
				{
					BeginDepartFromShore();
				}

				return;
			}

			IntVec3 fromPos = ship.Position;
			IntVec3 toPos = VoidAwake_GhostShipOceanUtility.ShipPositionForPathCell(nextCenter, shipSize, map);
			Rot4 rot = Rot4.FromAngleFlat((orbitPath[next] - orbitPath[orbitIndex]).AngleFlat);

			shipMoveTicks++;
			float t = Mathf.Clamp01(shipMoveTicks / (float)ShipMoveIntervalTicks);
			Vector3 draw = Vector3.Lerp(fromPos.ToVector3Shifted(), toPos.ToVector3Shifted(), t);
			ship.SetOrbitDraw(draw, rot);

			if (t < 1f)
			{
				return;
			}

			shipMoveTicks = 0;
			orbitIndex = next;
			if (ship.Position != toPos)
			{
				ship.Position = toPos;
			}

			ship.Rotation = rot;
			ship.SetOrbitDraw(toPos.ToVector3Shifted(), rot);

			if (orbitIndex >= orbitPath.Count - 1)
			{
				OnPathArrived();
			}
		}

		private void OnPathArrived()
		{
			if (approachingShore)
			{
				OnArrivedAtShore();
			}
			else if (departingShore)
			{
				OnArrivedOffshore();
			}
		}

		private void BeginApproachToRandomShore()
		{
			if (ship == null || !ship.Spawned)
			{
				return;
			}

			departingShore = false;
			IntVec2 shipSize = ship.def.Size;
			IntVec3 from = ship.OccupiedRect().CenterCell;
			if (orbitPath == null)
			{
				orbitPath = new List<IntVec3>();
			}

			List<IntVec3> shores = CollectApproachShores();
			shores.Shuffle();
			int tries = Mathf.Min(12, shores.Count);
			for (int i = 0; i < tries; i++)
			{
				IntVec3 shore = shores[i];
				if (!VoidAwake_GhostShipWanderUtility.TryFindNavigableNear(map, shore, shipSize, out IntVec3 dest))
				{
					continue;
				}

				if (!VoidAwake_GhostShipWanderUtility.TryFindPath(map, from, dest, shipSize, orbitPath))
				{
					continue;
				}

				approachShoreCell = shore;
				orbitIndex = 0;
				shipMoveTicks = 0;
				approachingShore = true;
				return;
			}

			orbitPath.Clear();
			approachingShore = false;
		}

		private List<IntVec3> CollectApproachShores()
		{
			var shores = new List<IntVec3>();
			if (footingCells != null)
			{
				for (int i = 0; i < footingCells.Count; i++)
				{
					IntVec3 c = footingCells[i];
					if (c.InBounds(map) && c.Standable(map))
					{
						shores.Add(c);
					}
				}

				if (shores.Count > 0)
				{
					return shores;
				}

				for (int i = 0; i < footingCells.Count; i++)
				{
					IntVec3 c = footingCells[i];
					if (c.InBounds(map))
					{
						shores.Add(c);
					}
				}

				if (shores.Count > 0)
				{
					return shores;
				}
			}

			if (VoidAwake_GhostShipWanderUtility.TryFindRandomShoreCell(map, out IntVec3 fallback))
			{
				shores.Add(fallback);
			}

			return shores;
		}

		private bool BeginDepartFromShore()
		{
			if (ship == null || !ship.Spawned)
			{
				return false;
			}

			approachingShore = false;
			IntVec2 shipSize = ship.def.Size;
			IntVec3 from = ship.OccupiedRect().CenterCell;
			IntVec3 awayFrom = approachShoreCell.IsValid ? approachShoreCell : from;
			if (orbitPath == null)
			{
				orbitPath = new List<IntVec3>();
			}

			for (int i = 0; i < 8; i++)
			{
				if (!VoidAwake_GhostShipWanderUtility.TryFindRandomNavigableCell(
					map, shipSize, out IntVec3 dest, awayFrom, DepartShoreMinDistance))
				{
					break;
				}

				if (!VoidAwake_GhostShipWanderUtility.TryFindPath(map, from, dest, shipSize, orbitPath))
				{
					continue;
				}

				orbitIndex = 0;
				shipMoveTicks = 0;
				departingShore = true;
				return true;
			}

			orbitPath.Clear();
			departingShore = false;
			return false;
		}

		private void OnArrivedAtShore()
		{
			approachingShore = false;
			departingShore = false;
			DumpGhostWave();
			waveDumpTick = Find.TickManager.TicksGame;
			waitingUntilTick = waveDumpTick + DepartShoreDelayTicks;
		}

		private void OnArrivedOffshore()
		{
			departingShore = false;
			int nextWave = waveDumpTick + WaveIntervalTicks;
			waitingUntilTick = nextWave > Find.TickManager.TicksGame
				? nextWave
				: Find.TickManager.TicksGame;
		}

		private void DumpGhostWave()
		{
			if (ship == null || !ship.Spawned)
			{
				return;
			}

			if (releasedGhosts == null)
			{
				releasedGhosts = new List<Pawn>();
			}

			assaultAnnounced = false;
			float budget = raidPoints > 0f ? raidPoints : StorytellerUtility.DefaultThreatPointsNow(map);
			if (budget <= 0f)
			{
				budget = 350f;
			}

			IntVec3 anchor = approachShoreCell;
			if (!anchor.IsValid || !anchor.InBounds(map) || !anchor.Standable(map)
				|| VoidAwake_GhostShipOceanUtility.IsOceanTerrain(anchor.GetTerrain(map)))
			{
				anchor = VoidAwake_GhostShipOceanUtility.FindLandSpawnNear(map, ship.OccupiedRect().CenterCell, 24);
			}

			if (!anchor.IsValid)
			{
				VoidAwake_GhostShipWanderUtility.TryFindRandomShoreCell(map, out anchor);
			}

			if (!anchor.IsValid)
			{
				return;
			}

			float spent = 0f;
			int spawned = 0;
			const int maxGhosts = 40;
			while (spent < budget && spawned < maxGhosts)
			{
				IntVec3 land = CellFinder.RandomClosewalkCellNear(anchor, map, 10);
				if (!land.IsValid || VoidAwake_GhostShipOceanUtility.IsOceanTerrain(land.GetTerrain(map)))
				{
					land = VoidAwake_GhostShipOceanUtility.FindLandSpawnNear(map, anchor, 16);
				}

				if (!land.IsValid)
				{
					break;
				}

				Pawn ghost = VoidAwake_GhostUtility.TrySpawnGhost(map, land);
				if (ghost == null)
				{
					break;
				}

				VoidAwake_GhostUtility.LeapFrom(ghost, ship.DrawPos, land);
				releasedGhosts.Add(ghost);
				ghostsReleased++;
				spawned++;
				if (ghost.kindDef != null)
				{
					spent += ghost.kindDef.combatPower;
				}

				AddGhostToAssaultLord(ghost);
			}

			spawnedPoints += spent;
		}

		private void AddGhostToAssaultLord(Pawn ghost)
		{
			if (ghost == null || ghost.Destroyed)
			{
				return;
			}

			if (ghostLord == null || !map.lordManager.lords.Contains(ghostLord))
			{
				ghostLord = LordMaker.MakeNewLord(
					Faction.OfEntities,
					new LordJob_AssaultColony(Faction.OfEntities, false, false, false, true, false),
					map);
			}

			AssignGhostToLord(ghost, ghostLord);
			if (!assaultAnnounced)
			{
				assaultAnnounced = true;
				Messages.Message("VoidAwake_GhostShip_AssaultStarted".Translate(), ghost, MessageTypeDefOf.ThreatBig, false);
			}
		}

		private void AssignGhostToLord(Pawn ghost, Lord lord)
		{
			if (ghost == null || ghost.Destroyed || lord == null)
			{
				return;
			}

			Lord current = ghost.GetLord();
			if (current == lord)
			{
				return;
			}

			if (current != null)
			{
				current.RemovePawn(ghost);
				if (current.ownedPawns.Count == 0 && map.lordManager.lords.Contains(current))
				{
					map.lordManager.RemoveLord(current);
				}
			}

			if (!lord.ownedPawns.Contains(ghost))
			{
				lord.AddPawn(ghost);
			}
		}

		private void TickBombard()
		{
			bombardTicks++;
			if (bombardTicks < BombardIntervalTicks)
			{
				return;
			}

			bombardTicks = 0;
			TryFireMortar();
		}

		private void TryFireMortar()
		{
			List<Building> buildings = map.listerBuildings.allBuildingsColonist;
			if (buildings == null || buildings.Count == 0)
			{
				return;
			}

			Building target = buildings.RandomElement();
			if (target == null || !target.Spawned)
			{
				return;
			}

			ThingDef shellDef = VoidAwake_GhostShipDefOf.Bullet_Shell_HighExplosive;
			if (shellDef == null)
			{
				return;
			}

			IntVec3 launchCell = ship.OccupiedRect().CenterCell;
			if (!launchCell.InBounds(map))
			{
				launchCell = ship.Position;
			}

			IntVec3 aimCell = MortarAimCell(target);
			Projectile projectile = (Projectile)GenSpawn.Spawn(shellDef, launchCell, map);
			projectile.Launch(
				ship,
				ship.DrawPos,
				aimCell,
				aimCell,
				ProjectileHitFlags.None);

			SoundDef launchSound = DefDatabase<SoundDef>.GetNamedSilentFail("Mortar_LaunchA");
			if (launchSound != null)
			{
				launchSound.PlayOneShot(new TargetInfo(launchCell, map));
			}
		}

		/// <summary>バニラ迫撃砲に近い散布（forcedMissRadius ≒ 9）。</summary>
		private IntVec3 MortarAimCell(Building target)
		{
			IntVec3 center = target.OccupiedRect().RandomCell;
			const float missRadius = 9f;
			Vector3 offset = new Vector3(
				Rand.Gaussian(0f, missRadius * 0.4f),
				0f,
				Rand.Gaussian(0f, missRadius * 0.4f));
			IntVec3 cell = (center.ToVector3Shifted() + offset).ToIntVec3();
			if (!cell.InBounds(map))
			{
				return center;
			}

			return cell;
		}

		private void OnFirstOrbitComplete()
		{
			UnlockShip();
			VoidAwake_GhostUtility.ReleaseResurrectionWave(releasedGhosts);
			Messages.Message("VoidAwake_GhostShip_ResurrectionWave".Translate(), ship, MessageTypeDefOf.ThreatBig, false);
			StartGhostAssault();
		}

		private void UnlockShip()
		{
			phase = VoidAwake_GhostShipPhase.ShipUnlocked;
			if (ship != null)
			{
				ship.CanEnter = true;
				ship.ClearOrbitDraw();
			}

			Messages.Message("VoidAwake_GhostShip_Unlocked".Translate(), ship, MessageTypeDefOf.PositiveEvent, false);
			Log.Message("[VoidAwake] Ghost ship docked after one ocean ellipse.");
		}

		private void StartGhostAssault()
		{
			PruneReleasedGhosts();
			List<Pawn> attackers = new List<Pawn>();
			if (releasedGhosts != null)
			{
				for (int i = 0; i < releasedGhosts.Count; i++)
				{
					Pawn p = releasedGhosts[i];
					if (!VoidAwake_GhostUtility.IsGhostInactive(p))
					{
						attackers.Add(p);
					}
				}
			}

			if (ghostLord != null && map.lordManager.lords.Contains(ghostLord))
			{
				map.lordManager.RemoveLord(ghostLord);
			}

			ghostLord = null;
			if (attackers.Count == 0)
			{
				return;
			}

			ghostLord = LordMaker.MakeNewLord(
				Faction.OfEntities,
				new LordJob_AssaultColony(Faction.OfEntities, false, false, false, true, false),
				map,
				attackers);
			Messages.Message("VoidAwake_GhostShip_AssaultStarted".Translate(), attackers[0], MessageTypeDefOf.ThreatBig, false);
		}

		private int CountAliveReleasedGhosts()
		{
			if (releasedGhosts == null)
			{
				return 0;
			}

			int n = 0;
			for (int i = 0; i < releasedGhosts.Count; i++)
			{
				if (!VoidAwake_GhostUtility.IsGhostInactive(releasedGhosts[i]))
				{
					n++;
				}
			}

			return n;
		}

		private void PruneReleasedGhosts()
		{
			if (releasedGhosts == null)
			{
				return;
			}

			releasedGhosts.RemoveAll(p => p == null || p.Destroyed || (p.Dead && VoidAwake_GhostUtility.GetDeathRefusalUses(p) <= 0));
		}

		public void NotifyGhostReturned(Pawn pawn)
		{
			if (pawn == null || pawn.Destroyed || pawn.Dead)
			{
				return;
			}

			if (releasedGhosts == null)
			{
				releasedGhosts = new List<Pawn>();
			}

			if (!releasedGhosts.Contains(pawn))
			{
				releasedGhosts.Add(pawn);
			}

			if (phase == VoidAwake_GhostShipPhase.ShipOrbiting || phase == VoidAwake_GhostShipPhase.ShipUnlocked)
			{
				IntVec3 cell = pawn.Spawned ? pawn.Position : IntVec3.Invalid;
				if (!cell.IsValid || !cell.Standable(map) || VoidAwake_GhostShipOceanUtility.IsOceanTerrain(cell.GetTerrain(map)))
				{
					cell = VoidAwake_GhostShipOceanUtility.FindLandSpawnNear(map, cell.IsValid ? cell : map.Center);
					if (cell.IsValid && pawn.Spawned)
					{
						pawn.Position = cell;
						pawn.Notify_Teleported(true, false);
					}
				}

				AddGhostToAssaultLord(pawn);
			}
		}

		private void ClearShipAndGhosts()
		{
			if (ghostLord != null && map.lordManager.lords.Contains(ghostLord))
			{
				map.lordManager.RemoveLord(ghostLord);
			}

			ghostLord = null;
			if (releasedGhosts != null)
			{
				for (int i = 0; i < releasedGhosts.Count; i++)
				{
					Pawn p = releasedGhosts[i];
					if (p != null && !p.Destroyed && p.Spawned)
					{
						p.Destroy();
					}
				}

				releasedGhosts.Clear();
			}

			if (ship != null && !ship.Destroyed)
			{
				if (ship.Spawned)
				{
					ship.Destroy();
				}
				else
				{
					ship.Destroy();
				}
			}

			ship = null;
			orbitIndex = 0;
			shipMoveTicks = 0;
			ghostSpawnTicks = 0;
			bombardTicks = 0;
			ghostsReleased = 0;
			spawnedPoints = 0f;
		}

		private void EnsureGrayPall()
		{
			GameConditionDef def = DefDatabase<GameConditionDef>.GetNamedSilentFail("GrayPall");
			if (def == null || map?.GameConditionManager == null)
			{
				return;
			}

			if (map.GameConditionManager.ConditionIsActive(def))
			{
				return;
			}

			GameCondition condition = GameConditionMaker.MakeConditionPermanent(def);
			if (condition == null)
			{
				return;
			}

			map.GameConditionManager.RegisterCondition(condition);
			startedGrayPall = true;
		}

		private void EndGrayPallIfStarted()
		{
			if (!startedGrayPall)
			{
				return;
			}

			startedGrayPall = false;
			GameConditionDef def = DefDatabase<GameConditionDef>.GetNamedSilentFail("GrayPall");
			if (def == null || map?.GameConditionManager == null)
			{
				return;
			}

			GameCondition condition = map.GameConditionManager.GetActiveCondition(def);
			if (condition != null && !condition.Expired)
			{
				condition.End();
			}
		}

		public override void ExposeData()
		{
			Scribe_Values.Look(ref oceanActive, "oceanActive", false);
			Scribe_Values.Look(ref oceanFloodComplete, "oceanFloodComplete", false);
			Scribe_Values.Look(ref nextCellIndex, "nextCellIndex", 0);
			Scribe_Values.Look(ref nextFootingIndex, "nextFootingIndex", 0);
			Scribe_Values.Look(ref tickAccumulator, "tickAccumulator", 0);
			Scribe_Collections.Look(ref floodCells, "floodCells", LookMode.Value);
			Scribe_Collections.Look(ref footingCells, "footingCells", LookMode.Value);
			Scribe_Collections.Look(ref rockRestores, "rockRestores", LookMode.Deep);

			Scribe_Values.Look(ref phase, "phase", VoidAwake_GhostShipPhase.None);
			Scribe_References.Look(ref ship, "ship");
			Scribe_Collections.Look(ref orbitPath, "orbitPath", LookMode.Value);
			Scribe_Values.Look(ref orbitIndex, "orbitIndex", 0);
			Scribe_Values.Look(ref shipMoveTicks, "shipMoveTicks", 0);
			Scribe_Values.Look(ref ghostSpawnTicks, "ghostSpawnTicks", 0);
			Scribe_Values.Look(ref bombardTicks, "bombardTicks", 0);
			Scribe_Values.Look(ref ghostsReleased, "ghostsReleased", 0);
			Scribe_Values.Look(ref raidPoints, "raidPoints", 0f);
			Scribe_Values.Look(ref spawnedPoints, "spawnedPoints", 0f);
			Scribe_Values.Look(ref assaultAnnounced, "assaultAnnounced", false);
			Scribe_Values.Look(ref approachingShore, "approachingShore", false);
			Scribe_Values.Look(ref departingShore, "departingShore", false);
			Scribe_Values.Look(ref waitingUntilTick, "waitingUntilTick", 0);
			Scribe_Values.Look(ref waveDumpTick, "waveDumpTick", 0);
			Scribe_Values.Look(ref approachShoreCell, "approachShoreCell", IntVec3.Invalid);
			Scribe_Collections.Look(ref releasedGhosts, "releasedGhosts", LookMode.Reference);
			Scribe_References.Look(ref ghostLord, "ghostLord");
			Scribe_Values.Look(ref startedGrayPall, "startedGrayPall", false);

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (floodCells == null)
				{
					floodCells = new List<IntVec3>();
				}

				if (footingCells == null)
				{
					footingCells = new List<IntVec3>();
				}

				if (rockRestores == null)
				{
					rockRestores = new List<VoidAwake_OceanRockRestore>();
				}

				if (releasedGhosts == null)
				{
					releasedGhosts = new List<Pawn>();
				}

				if (orbitPath == null)
				{
					orbitPath = new List<IntVec3>();
				}

				RebuildPlannedOcean();
			}
		}
	}

	public enum VoidAwake_GhostShipPhase
	{
		None = 0,
		OceanFlooding = 1,
		ShipOrbiting = 2,
		ShipUnlocked = 3,
	}
}
