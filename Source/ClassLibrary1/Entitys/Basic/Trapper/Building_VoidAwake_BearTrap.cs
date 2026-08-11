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

		public override void ExposeData()
		{
			Scribe_Values.Look(ref destroyedTrapCount, "destroyedTrapCount", 0);
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

		public static void RevealAllTrappersOnMap(Map map)
		{
			if (map == null)
			{
				return;
			}

			IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
			for (int i = 0; i < pawns.Count; i++)
			{
				Pawn pawn = pawns[i];
				if (pawn?.kindDef == VoidAwake_TrapperDefOf.Trapper)
				{
					pawn.TryGetComp<VoidAwake_TrapperComp>()?.EnterCombat();
				}
			}
		}
	}
}
