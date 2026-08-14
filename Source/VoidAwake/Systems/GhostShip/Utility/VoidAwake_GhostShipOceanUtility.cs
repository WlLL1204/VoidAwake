using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace VoidAwake
{
	public static class VoidAwake_GhostShipOceanUtility
	{
		/// <summary>確認用の速さ。1 tick に 1 セル。</summary>
		public const int TicksPerOceanCell = 1;

		/// <summary>見た目更新の間隔（セル数）。毎回 WholeMapChanged は重い。</summary>
		public const int MeshRefreshEveryCells = 64;

		private static FieldInfo fogGridField;

		public static List<VoidAwake_OceanCellSnapshot> CreateSnapshots(Map map)
		{
			List<IntVec3> cells = CollectEdgeCellsOuterFirst(map);
			var snaps = new List<VoidAwake_OceanCellSnapshot>(cells.Count);
			for (int i = 0; i < cells.Count; i++)
			{
				IntVec3 c = cells[i];
				snaps.Add(new VoidAwake_OceanCellSnapshot
				{
					cell = c,
					terrain = map.terrainGrid.TerrainAt(c),
					underTerrain = map.terrainGrid.UnderTerrainAt(c),
					naturalRockDef = FindNaturalRockDef(c, map),
					wasFogged = map.fogGrid.IsFogged(c),
				});
			}

			return snaps;
		}

		/// <summary>中心から遠いセルを先に（外側から浸食）。角は丸めて岸線を曲線にする。</summary>
		public static List<IntVec3> CollectEdgeCellsOuterFirst(Map map)
		{
			IntVec3 center = map.Center;
			int edge = GetNoBuildEdgeWidth(map);
			var cells = new List<IntVec3>();
			foreach (IntVec3 c in map.AllCells)
			{
				if (ShouldBecomeOcean(c, map, edge))
				{
					cells.Add(c);
				}
			}

			cells.Sort((a, b) =>
			{
				int da = a.DistanceToSquared(center);
				int db = b.DistanceToSquared(center);
				int cmp = db.CompareTo(da);
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
			return cells;
		}

		public static bool ShouldBecomeOcean(IntVec3 c, Map map)
		{
			return ShouldBecomeOcean(c, map, GetNoBuildEdgeWidth(map));
		}

		public static bool ShouldBecomeOcean(IntVec3 c, Map map, int edge)
		{
			// 島＝角を丸めた矩形。その外を海洋にする（角で両辺が滑らかにつながる）
			return !IsInsideRoundedLandIsland(c, map, edge, edge);
		}

		public static int GetNoBuildEdgeWidth(Map map)
		{
			int z = map.Size.z / 2;
			int w = 0;
			int max = map.Size.x / 2;
			while (w < max && new IntVec3(w, 0, z).InNoBuildEdgeArea(map))
			{
				w++;
			}

			return Mathf.Max(1, w);
		}

		/// <summary>
		/// 海洋帯の中央リング（幅 5 なら外側から 3 マス目）を、中心まわりの角度順に並べた周回パス。
		/// </summary>
		public static List<IntVec3> BuildShoreOrbitPath(Map map)
		{
			int edge = GetNoBuildEdgeWidth(map);
			int targetDist = Mathf.Max(1, (edge + 1) / 2);
			IntVec3 center = map.Center;
			var ring = new List<IntVec3>();
			foreach (IntVec3 c in map.AllCells)
			{
				if (!ShouldBecomeOcean(c, map, edge))
				{
					continue;
				}

				if (ChebyshevDistToLand(c, map, edge) == targetDist)
				{
					ring.Add(c);
				}
			}

			if (ring.Count == 0)
			{
				foreach (IntVec3 c in map.AllCells)
				{
					if (!ShouldBecomeOcean(c, map, edge))
					{
						continue;
					}

					if (ChebyshevDistToLand(c, map, edge) == 1)
					{
						ring.Add(c);
					}
				}
			}

			ring.Sort((a, b) =>
			{
				float aa = Mathf.Atan2(a.z - center.z, a.x - center.x);
				float ba = Mathf.Atan2(b.z - center.z, b.x - center.x);
				int cmp = aa.CompareTo(ba);
				if (cmp != 0)
				{
					return cmp;
				}

				return a.DistanceToSquared(center).CompareTo(b.DistanceToSquared(center));
			});
			return ThinOrbitPath(ring);
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

		private static List<IntVec3> ThinOrbitPath(List<IntVec3> ring)
		{
			if (ring.Count < 3)
			{
				return ring;
			}

			var thinned = new List<IntVec3>(ring.Count);
			for (int i = 0; i < ring.Count; i++)
			{
				IntVec3 c = ring[i];
				if (thinned.Count > 0 && thinned[thinned.Count - 1] == c)
				{
					continue;
				}

				thinned.Add(c);
			}

			if (thinned.Count > 1 && thinned[0] == thinned[thinned.Count - 1])
			{
				thinned.RemoveAt(thinned.Count - 1);
			}

			return thinned.Count >= 3 ? thinned : ring;
		}

		private static int ChebyshevDistToLand(IntVec3 c, Map map, int edge)
		{
			int max = edge + 2;
			int best = max + 1;
			for (int dx = -max; dx <= max; dx++)
			{
				for (int dz = -max; dz <= max; dz++)
				{
					int cheb = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz));
					if (cheb == 0 || cheb >= best)
					{
						continue;
					}

					IntVec3 n = new IntVec3(c.x + dx, 0, c.z + dz);
					if (n.InBounds(map) && !ShouldBecomeOcean(n, map, edge))
					{
						best = cheb;
					}
				}
			}

			return best;
		}

		public static IntVec3 FindLandSpawnNear(Map map, IntVec3 near, int maxRadius = 14)
		{
			foreach (IntVec3 c in GenRadial.RadialCellsAround(near, maxRadius, true))
			{
				if (!c.InBounds(map) || ShouldBecomeOcean(c, map))
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

		/// <summary>
		/// 角丸矩形の陸地（島）。直線の岸と、角では半径 radius の四分円でつなぐ。
		/// 円を角に食い込ませるのではなく、直角の岸同士を外側へそらせて接続する。
		/// </summary>
		private static bool IsInsideRoundedLandIsland(IntVec3 c, Map map, int inset, int radius)
		{
			int sx = map.Size.x;
			int sz = map.Size.z;
			float x = c.x + 0.5f;
			float z = c.z + 0.5f;
			float left = inset;
			float right = sx - inset;
			float bottom = inset;
			float top = sz - inset;
			float r = radius;

			// 外枠より外 → 海
			if (x < left || x >= right || z < bottom || z >= top)
			{
				return false;
			}

			// 角を除く帯 → 陸
			if (x >= left + r && x < right - r)
			{
				return true;
			}

			if (z >= bottom + r && z < top - r)
			{
				return true;
			}

			float rSq = r * r;

			// SW: 中心 (left+r, bottom+r)
			if (x < left + r && z < bottom + r)
			{
				float dx = x - (left + r);
				float dz = z - (bottom + r);
				return dx * dx + dz * dz <= rSq;
			}

			// SE: 中心 (right-r, bottom+r)
			if (x >= right - r && z < bottom + r)
			{
				float dx = x - (right - r);
				float dz = z - (bottom + r);
				return dx * dx + dz * dz <= rSq;
			}

			// NW: 中心 (left+r, top-r)
			if (x < left + r && z >= top - r)
			{
				float dx = x - (left + r);
				float dz = z - (top - r);
				return dx * dx + dz * dz <= rSq;
			}

			// NE: 中心 (right-r, top-r)
			if (x >= right - r && z >= top - r)
			{
				float dx = x - (right - r);
				float dz = z - (top - r);
				return dx * dx + dz * dz <= rSq;
			}

			return true;
		}

		public static void ConvertCell(Map map, VoidAwake_OceanCellSnapshot snap)
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

			EvacuatePawnIfAny(c, map);
			DestroyCellContents(c, map);
			map.terrainGrid.SetTerrain(c, TerrainDefOf.WaterOceanDeep);
		}

		public static void Restore(Map map, List<VoidAwake_OceanCellSnapshot> snaps, int convertedCount)
		{
			if (snaps == null || convertedCount <= 0)
			{
				return;
			}

			int count = Mathf.Min(convertedCount, snaps.Count);
			for (int i = 0; i < count; i++)
			{
				RestoreCell(map, snaps[i]);
			}

			map.mapDrawer.WholeMapChanged(MapMeshFlagDefOf.Terrain);
			map.mapDrawer.WholeMapChanged(MapMeshFlagDefOf.Things);
			map.mapDrawer.WholeMapChanged(MapMeshFlagDefOf.FogOfWar);
		}

		public static void RestoreCell(Map map, VoidAwake_OceanCellSnapshot snap)
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

			map.terrainGrid.SetTerrain(c, snap.terrain ?? TerrainDefOf.Soil);
			if (snap.underTerrain != null)
			{
				map.terrainGrid.SetUnderTerrain(c, snap.underTerrain);
			}

			if (snap.naturalRockDef != null)
			{
				DestroyCellContents(c, map);
				GenSpawn.Spawn(snap.naturalRockDef, c, map);
			}

			SetFogged(map, c, snap.wasFogged);
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

		public static void SetFogged(Map map, IntVec3 c, bool fogged)
		{
			if (!c.InBounds(map))
			{
				return;
			}

			bool currentlyFogged = map.fogGrid.IsFogged(c);
			if (currentlyFogged == fogged)
			{
				return;
			}

			if (!fogged)
			{
				map.fogGrid.Unfog(c);
				return;
			}

			// FogGrid に公開の再霧 API が無いため、内部配列を直接戻す
			if (fogGridField == null)
			{
				fogGridField = typeof(FogGrid).GetField("fogGrid", BindingFlags.Instance | BindingFlags.NonPublic);
			}

			if (fogGridField == null)
			{
				return;
			}

			if (!(fogGridField.GetValue(map.fogGrid) is bool[] grid))
			{
				return;
			}

			int index = map.cellIndices.CellToIndex(c);
			if (index < 0 || index >= grid.Length)
			{
				return;
			}

			grid[index] = true;
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

				if (!TryFindEvacuateCell(map, c, out IntVec3 dest))
				{
					dest = map.Center;
				}

				pawn.Position = dest;
				pawn.Notify_Teleported(true, false);
			}
		}

		private static bool TryFindEvacuateCell(Map map, IntVec3 from, out IntVec3 result)
		{
			return CellFinder.TryFindRandomCellNear(
				from,
				map,
				40,
				cell => !ShouldBecomeOcean(cell, map) && cell.Standable(map),
				out result);
		}

		/// <summary>植物・天然岩・遺跡などの建造物・その他非ポーンを破壊（復元しない）。</summary>
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

	public class VoidAwake_OceanCellSnapshot : IExposable
	{
		public IntVec3 cell;
		public TerrainDef terrain;
		public TerrainDef underTerrain;
		public ThingDef naturalRockDef;
		public bool wasFogged;

		public void ExposeData()
		{
			Scribe_Values.Look(ref cell, "cell");
			Scribe_Defs.Look(ref terrain, "terrain");
			Scribe_Defs.Look(ref underTerrain, "underTerrain");
			Scribe_Defs.Look(ref naturalRockDef, "naturalRockDef");
			Scribe_Values.Look(ref wasFogged, "wasFogged", false);
		}
	}

	public class VoidAwake_MapComponent_GhostShip : MapComponent
	{
		public const int ShipMoveIntervalTicks = 30;
		public const int GhostSpawnIntervalTicks = 240;
		public const int BombardIntervalTicks = 900;

		private bool oceanActive;
		private bool oceanFloodComplete;
		private List<VoidAwake_OceanCellSnapshot> oceanSnapshots;
		private int nextCellIndex;
		private int tickAccumulator;

		private VoidAwake_GhostShipPhase phase = VoidAwake_GhostShipPhase.None;
		private VoidAwake_Building_GhostShip ship;
		private List<IntVec3> orbitPath;
		private int orbitIndex;
		private int shipMoveTicks;
		private int ghostSpawnTicks;
		private int bombardTicks;
		private int ghostsReleased;
		private List<Pawn> releasedGhosts;
		private int orbitCellsAdvanced;
		private Lord ghostLord;

		public VoidAwake_MapComponent_GhostShip(Map map) : base(map)
		{
		}

		public bool IsOceanActive => oceanActive;

		public bool IsOceanFloodComplete => oceanActive && oceanFloodComplete;

		public bool IsOceanFlooding => oceanActive && !oceanFloodComplete;

		public bool CanEnterShip => ship != null && ship.CanEnter;

		public VoidAwake_GhostShipPhase Phase => phase;

		public bool TryStartOcean()
		{
			if (oceanActive)
			{
				return false;
			}

			oceanSnapshots = VoidAwake_GhostShipOceanUtility.CreateSnapshots(map);
			nextCellIndex = 0;
			tickAccumulator = 0;
			oceanFloodComplete = oceanSnapshots.Count == 0;
			oceanActive = true;
			phase = VoidAwake_GhostShipPhase.OceanFlooding;
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

			VoidAwake_GhostShipOceanUtility.Restore(map, oceanSnapshots, nextCellIndex);
			oceanSnapshots = null;
			nextCellIndex = 0;
			tickAccumulator = 0;
			oceanFloodComplete = false;
			oceanActive = false;
			phase = VoidAwake_GhostShipPhase.None;
		}

		/// <summary>海洋が既に完了している想定で船を出し周回を始める（Dev 用）。</summary>
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
				while (oceanSnapshots != null && nextCellIndex < oceanSnapshots.Count)
				{
					VoidAwake_GhostShipOceanUtility.ConvertCell(map, oceanSnapshots[nextCellIndex]);
					nextCellIndex++;
				}

				oceanFloodComplete = true;
				RefreshMapMeshes();
			}

			if (phase == VoidAwake_GhostShipPhase.ShipOrbiting || phase == VoidAwake_GhostShipPhase.ShipUnlocked)
			{
				return ship != null && ship.Spawned;
			}

			return TrySpawnShipAndStartOrbit();
		}

		public override void MapComponentTick()
		{
			TickOceanFlood();
			if (phase == VoidAwake_GhostShipPhase.ShipOrbiting)
			{
				TickShipOrbit();
			}
		}

		private void TickOceanFlood()
		{
			if (!oceanActive || oceanFloodComplete || oceanSnapshots == null)
			{
				return;
			}

			tickAccumulator++;
			if (tickAccumulator < VoidAwake_GhostShipOceanUtility.TicksPerOceanCell)
			{
				return;
			}

			tickAccumulator = 0;
			if (nextCellIndex >= oceanSnapshots.Count)
			{
				oceanFloodComplete = true;
				RefreshMapMeshes();
				OnOceanFloodCompleted();
				return;
			}

			VoidAwake_GhostShipOceanUtility.ConvertCell(map, oceanSnapshots[nextCellIndex]);
			nextCellIndex++;
			if (nextCellIndex >= oceanSnapshots.Count)
			{
				oceanFloodComplete = true;
				RefreshMapMeshes();
				OnOceanFloodCompleted();
			}
			else if (nextCellIndex % VoidAwake_GhostShipOceanUtility.MeshRefreshEveryCells == 0)
			{
				RefreshMapMeshes();
			}
		}

		private void OnOceanFloodCompleted()
		{
			if (phase != VoidAwake_GhostShipPhase.OceanFlooding && phase != VoidAwake_GhostShipPhase.None)
			{
				return;
			}

			TrySpawnShipAndStartOrbit();
		}

		private bool TrySpawnShipAndStartOrbit()
		{
			orbitPath = VoidAwake_GhostShipOceanUtility.BuildShoreOrbitPath(map);
			if (orbitPath == null || orbitPath.Count == 0)
			{
				Log.Warning("[VoidAwake] Ghost ship orbit path empty; cannot spawn ship.");
				return false;
			}

			if (ship != null && ship.Spawned)
			{
				ship.Destroy();
			}

			orbitIndex = 0;
			IntVec2 shipSize = VoidAwake_GhostShipDefOf.VoidAwake_GhostShip.Size;
			IntVec3 spawnCell = VoidAwake_GhostShipOceanUtility.ShipPositionForPathCell(orbitPath[0], shipSize, map);
			ship = (VoidAwake_Building_GhostShip)ThingMaker.MakeThing(VoidAwake_GhostShipDefOf.VoidAwake_GhostShip);
			ship.CanEnter = false;
			Rot4 spawnRot = OrbitRotationAt(0);
			GenSpawn.Spawn(ship, spawnCell, map, spawnRot, WipeMode.Vanish);
			ship.SetOrbitDraw(spawnCell.ToVector3Shifted(), spawnRot);

			shipMoveTicks = 0;
			ghostSpawnTicks = 0;
			bombardTicks = 0;
			ghostsReleased = 0;
			orbitCellsAdvanced = 0;
			releasedGhosts = new List<Pawn>();
			ghostLord = null;
			phase = VoidAwake_GhostShipPhase.ShipOrbiting;
			Messages.Message("VoidAwake_GhostShip_OrbitStarted".Translate(), ship, MessageTypeDefOf.ThreatBig, false);
			return true;
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
			TickGhostRelease();
			TickBombard();
		}

		private void TickShipMove()
		{
			if (orbitPath == null || orbitPath.Count < 2)
			{
				return;
			}

			int next = (orbitIndex + 1) % orbitPath.Count;
			IntVec2 shipSize = ship.def.Size;
			IntVec3 fromPos = VoidAwake_GhostShipOceanUtility.ShipPositionForPathCell(orbitPath[orbitIndex], shipSize, map);
			IntVec3 toPos = VoidAwake_GhostShipOceanUtility.ShipPositionForPathCell(orbitPath[next], shipSize, map);
			Rot4 rot = OrbitRotationAt(orbitIndex);

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
			orbitCellsAdvanced++;
			if (ship.Position != toPos)
			{
				ship.Position = toPos;
			}

			ship.Rotation = rot;
			ship.SetOrbitDraw(toPos.ToVector3Shifted(), rot);

			if (orbitCellsAdvanced >= orbitPath.Count)
			{
				OnFirstOrbitComplete();
			}
		}

		private Rot4 OrbitRotationAt(int index)
		{
			if (orbitPath == null || orbitPath.Count < 2)
			{
				return Rot4.North;
			}

			IntVec3 from = orbitPath[index];
			IntVec3 to = orbitPath[(index + 1) % orbitPath.Count];
			return Rot4.FromAngleFlat((to - from).AngleFlat);
		}

		private void TickGhostRelease()
		{
			ghostSpawnTicks++;
			if (ghostSpawnTicks < GhostSpawnIntervalTicks)
			{
				return;
			}

			ghostSpawnTicks = 0;
			IntVec3 land = VoidAwake_GhostShipOceanUtility.FindLandSpawnNear(map, ship.OccupiedRect().CenterCell);
			if (!land.IsValid)
			{
				return;
			}

			Pawn ghost = VoidAwake_GhostUtility.TrySpawnGhost(map, land);
			if (ghost == null)
			{
				return;
			}

			VoidAwake_GhostUtility.LeapFrom(ghost, ship.DrawPos, land);
			releasedGhosts.Add(ghost);
			ghostsReleased++;
			AddGhostToStagingLord(ghost, land);
		}

		private void AddGhostToStagingLord(Pawn ghost, IntVec3 stagingCell)
		{
			if (ghost == null || ghost.Destroyed)
			{
				return;
			}

			if (ghostLord == null || !map.lordManager.lords.Contains(ghostLord))
			{
				ghostLord = LordMaker.MakeNewLord(
					Faction.OfEntities,
					new VoidAwake_LordJob_GhostHoldLanding(),
					map);
			}

			VoidAwake_LordJob_GhostHoldLanding holdJob = ghostLord.LordJob as VoidAwake_LordJob_GhostHoldLanding;
			holdJob?.SetLanding(ghost, stagingCell);

			if (!ghostLord.ownedPawns.Contains(ghost))
			{
				ghostLord.AddPawn(ghost);
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
			Log.Message("[VoidAwake] Ghost ship docked after one orbit (portal stub).");
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

			releasedGhosts.RemoveAll(VoidAwake_GhostUtility.IsGhostInactive);
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
			orbitPath = null;
			orbitIndex = 0;
			shipMoveTicks = 0;
			ghostSpawnTicks = 0;
			bombardTicks = 0;
			ghostsReleased = 0;
			orbitCellsAdvanced = 0;
		}

		private void RefreshMapMeshes()
		{
			map.mapDrawer.WholeMapChanged(MapMeshFlagDefOf.Terrain);
			map.mapDrawer.WholeMapChanged(MapMeshFlagDefOf.Things);
			map.mapDrawer.WholeMapChanged(MapMeshFlagDefOf.FogOfWar);
		}

		public override void ExposeData()
		{
			Scribe_Values.Look(ref oceanActive, "oceanActive", false);
			Scribe_Values.Look(ref oceanFloodComplete, "oceanFloodComplete", false);
			Scribe_Values.Look(ref nextCellIndex, "nextCellIndex", 0);
			Scribe_Values.Look(ref tickAccumulator, "tickAccumulator", 0);
			Scribe_Collections.Look(ref oceanSnapshots, "oceanSnapshots", LookMode.Deep);

			Scribe_Values.Look(ref phase, "phase", VoidAwake_GhostShipPhase.None);
			Scribe_References.Look(ref ship, "ship");
			Scribe_Collections.Look(ref orbitPath, "orbitPath", LookMode.Value);
			Scribe_Values.Look(ref orbitIndex, "orbitIndex", 0);
			Scribe_Values.Look(ref shipMoveTicks, "shipMoveTicks", 0);
			Scribe_Values.Look(ref ghostSpawnTicks, "ghostSpawnTicks", 0);
			Scribe_Values.Look(ref bombardTicks, "bombardTicks", 0);
			Scribe_Values.Look(ref ghostsReleased, "ghostsReleased", 0);
			Scribe_Values.Look(ref orbitCellsAdvanced, "orbitCellsAdvanced", 0);
			Scribe_Collections.Look(ref releasedGhosts, "releasedGhosts", LookMode.Reference);
			Scribe_References.Look(ref ghostLord, "ghostLord");

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (oceanSnapshots == null)
				{
					oceanSnapshots = new List<VoidAwake_OceanCellSnapshot>();
				}

				if (releasedGhosts == null)
				{
					releasedGhosts = new List<Pawn>();
				}

				if (orbitPath == null)
				{
					orbitPath = new List<IntVec3>();
				}
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
