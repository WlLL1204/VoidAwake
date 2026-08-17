using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace VoidAwake
{
	public static class VoidAwake_DebugActions_GhostShip
	{
		[DebugAction("VoidAwake", "GhostShip: flood ocean edge", false, false, false, false, false, 0, false,
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void FloodOceanEdge()
		{
			Map map = Find.CurrentMap;
			if (map == null)
			{
				return;
			}

			VoidAwake_MapComponent_GhostShip comp = map.GetComponent<VoidAwake_MapComponent_GhostShip>();
			if (comp == null)
			{
				Messages.Message("GhostShip map component missing.", MessageTypeDefOf.RejectInput, false);
				return;
			}

			if (!comp.TryStartOcean())
			{
				Messages.Message("Ocean edge already active.", MessageTypeDefOf.RejectInput, false);
				return;
			}

			Messages.Message("Started map-edge ocean flood.", MessageTypeDefOf.TaskCompletion, false);
		}

		[DebugAction("VoidAwake", "GhostShip: restore ocean edge", false, false, false, false, false, 0, false,
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void RestoreOceanEdge()
		{
			Map map = Find.CurrentMap;
			if (map == null)
			{
				return;
			}

			VoidAwake_MapComponent_GhostShip comp = map.GetComponent<VoidAwake_MapComponent_GhostShip>();
			if (comp == null)
			{
				Messages.Message("GhostShip map component missing.", MessageTypeDefOf.RejectInput, false);
				return;
			}

			if (!comp.IsOceanActive)
			{
				Messages.Message("Ocean edge is not active.", MessageTypeDefOf.RejectInput, false);
				return;
			}

			comp.RestoreOcean();
			Messages.Message("Restored flooded edge cells (temp ocean + natural rock).", MessageTypeDefOf.TaskCompletion, false);
		}

		[DebugAction("VoidAwake", "GhostShip arrival", false, false, false, false, false, 0, false,
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void GhostShipArrival()
		{
			Map map = Find.CurrentMap;
			if (map == null)
			{
				return;
			}

			IncidentDef def = VoidAwake_GhostShipDefOf.VoidAwake_GhostShipArrival;
			if (def?.Worker == null)
			{
				Messages.Message("VoidAwake_GhostShipArrival incident def missing.", MessageTypeDefOf.RejectInput, false);
				return;
			}

			IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, map);
			parms.forced = true;
			parms.target = map;
			def.Worker.TryExecute(parms);
		}

		[DebugAction("VoidAwake", "GhostShip: spawn ship", false, false, false, false, false, 0, false,
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void SpawnGhostShip()
		{
			Map map = Find.CurrentMap;
			if (map == null)
			{
				return;
			}

			VoidAwake_MapComponent_GhostShip comp = map.GetComponent<VoidAwake_MapComponent_GhostShip>();
			if (comp == null)
			{
				Messages.Message("GhostShip map component missing.", MessageTypeDefOf.RejectInput, false);
				return;
			}

			if (!comp.TryForceSpawnShip())
			{
				Messages.Message("Failed to spawn ghost ship (shore path empty?).", MessageTypeDefOf.RejectInput, false);
				return;
			}

			Messages.Message("Ghost ship spawned and wandering the sea.", MessageTypeDefOf.TaskCompletion, false);
		}

		[DebugAction("VoidAwake", "GhostShip: spawn ghost near", false, false, false, false, false, 0, false,
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void SpawnGhostNear()
		{
			Map map = Find.CurrentMap;
			if (map == null)
			{
				return;
			}

			IntVec3 cell = UI.MouseCell();
			if (!cell.InBounds(map))
			{
				cell = map.Center;
			}

			Pawn ghost = VoidAwake_GhostUtility.TrySpawnGhost(map, cell);
			if (ghost == null)
			{
				Messages.Message("Failed to spawn ghost.", MessageTypeDefOf.RejectInput, false);
				return;
			}

			Messages.Message("Spawned ghost: " + ghost.LabelShort, MessageTypeDefOf.TaskCompletion, false);
		}

		[DebugAction("VoidAwake", "GhostShip: jump to interior", false, false, false, false, false, 0, false,
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void JumpToInterior()
		{
			Map map = Find.CurrentMap;
			if (map == null)
			{
				return;
			}

			VoidAwake_Building_GhostShip ship = map.listerThings.ThingsOfDef(VoidAwake_GhostShipDefOf.VoidAwake_GhostShip)
				.FirstOrDefault() as VoidAwake_Building_GhostShip;
			if (ship == null)
			{
				Messages.Message("No ghost ship on this map.", MessageTypeDefOf.RejectInput, false);
				return;
			}

			ship.DevJumpToInterior();
		}
	}
}
