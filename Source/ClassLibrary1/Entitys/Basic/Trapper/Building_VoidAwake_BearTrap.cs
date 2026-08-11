using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidAwake
{
	public class Building_VoidAwake_BearTrap : Building_TrapDamager
	{
		private const float OverlayExtraY = 0.04f;

		/// <summary>Shared graphic data with no shadow so overlays don't interrupt soft shadows.</summary>
		private static readonly GraphicData NoShadowGraphicData = new GraphicData();

		private static Graphic haygrassGraphic;
		private static Graphic mossGraphic;
		private static readonly Dictionary<string, Graphic> terrainSurfaceGraphics = new Dictionary<string, Graphic>();

		private static Graphic HaygrassGraphic =>
			haygrassGraphic ?? (haygrassGraphic = GraphicDatabase.Get<Graphic_Random>(
				"Things/Plant/Haygrass",
				ShaderDatabase.CutoutPlant,
				Vector2.one,
				Color.white,
				Color.white,
				NoShadowGraphicData,
				null));

		private static Graphic MossGraphic =>
			mossGraphic ?? (mossGraphic = GraphicDatabase.Get<Graphic_Random>(
				"Things/Plant/Moss",
				ShaderDatabase.CutoutPlant,
				Vector2.one,
				Color.white,
				Color.white,
				NoShadowGraphicData,
				null));

		private static Graphic GetTerrainSurfaceGraphic(TerrainDef terrain)
		{
			if (terrain?.texturePath.NullOrEmpty() ?? true)
			{
				return null;
			}

			if (!terrainSurfaceGraphics.TryGetValue(terrain.texturePath, out Graphic graphic))
			{
				// Transparent: blends over the trap without opaque cutout punching holes in shadows.
				graphic = GraphicDatabase.Get<Graphic_Single>(
					terrain.texturePath,
					ShaderDatabase.Transparent,
					Vector2.one,
					new Color(1f, 1f, 1f, 0.92f),
					Color.white,
					NoShadowGraphicData,
					null);
				terrainSurfaceGraphics[terrain.texturePath] = graphic;
			}

			return graphic;
		}

		private static Graphic GetOverlayForTerrain(TerrainDef terrain)
		{
			if (terrain == null)
			{
				return null;
			}

			string defName = terrain.defName;
			if (defName == "Sand" || defName == "SoftSand" || defName == "Gravel" || defName == "Mud" || defName == "Ice")
			{
				return GetTerrainSurfaceGraphic(terrain);
			}

			if (defName == "MossyTerrain")
			{
				return MossGraphic;
			}

			if (terrain.HasTag("Soil"))
			{
				return HaygrassGraphic;
			}

			if (terrain.categoryType.ToString() == "Sand")
			{
				return GetTerrainSurfaceGraphic(terrain);
			}

			return null;
		}

		public override void Print(SectionLayer layer)
		{
			base.Print(layer);
			if (!Spawned)
			{
				return;
			}

			TerrainDef terrain = Position.GetTerrain(Map);
			Graphic overlay = GetOverlayForTerrain(terrain);
			overlay?.Print(layer, this, OverlayExtraY);
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			base.DrawAt(drawLoc, flip);
			VoidAwake_DoorTrapComboUtility.DrawTripwiresFrom(this);
		}

		protected override void SpringSub(Pawn p)
		{
			base.SpringSub(p);
			VoidAwake_TrapperUtility.RevealAllTrappersOnMap(Map);
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

		public MapComponent_VoidAwake_TrapperTraps(Map map) : base(map)
		{
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
}
