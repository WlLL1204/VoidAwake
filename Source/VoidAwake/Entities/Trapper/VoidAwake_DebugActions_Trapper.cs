using System.Collections.Generic;
using System.Text;
using LudeonTK;
using RimWorld;
using Verse;

namespace VoidAwake
{
	public static class VoidAwake_DebugActions_Trapper
	{
		[DebugAction("VoidAwake", "Trapper arrival", false, false, false, false, false, 0, false,
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void TrapperArrival()
		{
			Map map = Find.CurrentMap;
			if (map == null)
			{
				return;
			}

			IncidentDef def = VoidAwake_TrapperDefOf.VoidAwake_TrapperArrival;
			if (def?.Worker == null)
			{
				Messages.Message("VoidAwake_TrapperArrival incident def missing.", MessageTypeDefOf.RejectInput, false);
				return;
			}

			IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, map);
			parms.forced = true;
			parms.target = map;
			def.Worker.TryExecute(parms);
		}

		[DebugAction("VoidAwake", "Trapper: prune rabbit passages", false, false, false, false, false, 0, false,
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void PruneRabbitPassages()
		{
			Map map = Find.CurrentMap;
			if (map == null)
			{
				return;
			}

			int before = VoidAwake_RabbitPassageUtility.CollectPassages(map).Count;
			int removed = VoidAwake_RabbitPassageUtility.PruneRedundantPassages(map);
			int after = VoidAwake_RabbitPassageUtility.CollectPassages(map).Count;
			Log.Message($"[VoidAwake] Pruned {removed} rabbit passages ({before} -> {after}).");
			Messages.Message($"Pruned {removed} rabbit passages.", MessageTypeDefOf.TaskCompletion, false);
		}

		[DebugAction("VoidAwake", "Trapper: rabbit passage debug", false, false, false, false, false, 0, false,
			allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void RabbitPassageDebug()
		{
			Map map = Find.CurrentMap;
			if (map == null)
			{
				return;
			}

			Pawn trapper = null;
			IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
			for (int i = 0; i < pawns.Count; i++)
			{
				if (pawns[i]?.kindDef == VoidAwake_TrapperDefOf.VoidAwake_Trapper && !pawns[i].Dead)
				{
					trapper = pawns[i];
					break;
				}
			}

			if (trapper == null)
			{
				Messages.Message("No trapper on this map.", MessageTypeDefOf.RejectInput, false);
				return;
			}

			StringBuilder sb = new StringBuilder();
			sb.AppendLine($"[VoidAwake] Rabbit passage debug for {trapper.LabelShort} at {trapper.Position}");

			int doorCount = 0;
			foreach (Building_Door door in map.listerBuildings.AllBuildingsColonistOfClass<Building_Door>())
			{
				if (door != null && door.Spawned)
				{
					doorCount++;
				}
			}

			List<IntVec3> unreachable = VoidAwake_RabbitPassageUtility.CollectUnreachableTrapDoors(trapper);
			List<IntVec3> needDig = VoidAwake_RabbitPassageUtility.CollectDoorsNeedingNewPassage(trapper);
			sb.AppendLine($"colonist doors: {doorCount}, unreachable trap doors: {unreachable.Count}");
			sb.AppendLine($"existing passages: {VoidAwake_RabbitPassageUtility.CollectPassages(map).Count}, "
				+ $"doors already served by them: {unreachable.Count - needDig.Count}, "
				+ $"doors needing a new passage: {needDig.Count}");

			bool ownsPair = VoidAwake_RabbitPassageUtility.HasOwnPassage(map, trapper.thingIDNumber);
			sb.AppendLine(ownsPair
				? "owns a living pair already, so it will not dig another one."
				: "owns no pair, so it may dig one.");

			if (unreachable.Count == 0)
			{
				sb.AppendLine("No unreachable door, so no passage is needed.");
				Log.Message(sb.ToString());
				Messages.Message("Rabbit passage debug written to log.", MessageTypeDefOf.TaskCompletion, false);
				return;
			}

			if (needDig.Count == 0)
			{
				sb.AppendLine("Every unreachable door is already served by an existing passage; reuse instead of digging.");
				Log.Message(sb.ToString());
				Messages.Message("Rabbit passage debug written to log.", MessageTypeDefOf.TaskCompletion, false);
				return;
			}

			needDig.Sort((a, b) =>
				a.DistanceToSquared(trapper.Position).CompareTo(b.DistanceToSquared(trapper.Position)));

			IntVec3 target = needDig[0];
			HashSet<IntVec3> flood = new HashSet<IntVec3>();
			VoidAwake_RabbitPassageUtility.FloodWalkableFromDoor(map, trapper, target, flood);
			sb.AppendLine($"nearest door needing a new passage {target}, door-side flood cells: {flood.Count}");

			int naturalExits = 0;
			int crampedExits = 0;
			int wallAdjacentExits = 0;
			Dictionary<string, int> rejectedTerrain = new Dictionary<string, int>();
			foreach (IntVec3 cell in flood)
			{
				if (!VoidAwake_RabbitPassageUtility.IsValidPassageCell(map, cell))
				{
					TerrainDef terrain = cell.GetTerrain(map);
					string key = terrain?.defName ?? "null";
					rejectedTerrain.TryGetValue(key, out int n);
					rejectedTerrain[key] = n + 1;
					continue;
				}

				naturalExits++;
				if (!VoidAwake_RabbitPassageUtility.HasUsableExitSpace(map, cell))
				{
					crampedExits++;
					continue;
				}

				for (int i = 0; i < 4; i++)
				{
					if (VoidAwake_RabbitPassageUtility.FindCellPastWall(map, cell, GenAdj.CardinalDirections[i]).IsValid)
					{
						wallAdjacentExits++;
						break;
					}
				}
			}

			sb.AppendLine($"natural exit candidates: {naturalExits} "
				+ $"(too cramped: {crampedExits}, wall-adjacent with open far side: {wallAdjacentExits})");
			foreach (KeyValuePair<string, int> kv in rejectedTerrain)
			{
				sb.AppendLine($"  rejected exit terrain {kv.Key} x{kv.Value}");
			}

			if (VoidAwake_RabbitPassageUtility.TryFindPairOnFloodBoundary(trapper, flood, out IntVec3 entrance, out IntVec3 exit))
			{
				sb.AppendLine($"PAIR FOUND entrance {entrance} exit {exit}");
			}
			else
			{
				sb.AppendLine("No pair: every wall lacks a natural, pawn-reachable cell on the outer side.");
			}

			Log.Message(sb.ToString());
			Messages.Message("Rabbit passage debug written to log.", MessageTypeDefOf.TaskCompletion, false);
		}
	}
}
