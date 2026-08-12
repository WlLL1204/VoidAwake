using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace VoidAwake
{
	public class VoidAwake_MapComponent_TrapperTraps : MapComponent
	{
		public const int DestroyedTrapsToEnterCombat = 5;

		private int destroyedTrapCount;

		/// <summary>Door cell → reserving trapper thingIDNumber. One trapper per door.</summary>
		private Dictionary<IntVec3, int> doorReservations = new Dictionary<IntVec3, int>();

		public const int PassagePruneIntervalTicks = 2000;

		public VoidAwake_MapComponent_TrapperTraps(Map map) : base(map)
		{
		}

		public override void MapComponentTick()
		{
			if (Find.TickManager.TicksGame % PassagePruneIntervalTicks != 0)
			{
				return;
			}

			VoidAwake_RabbitPassageUtility.PruneRedundantPassages(map);
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
					&& p.kindDef == VoidAwake_TrapperDefOf.VoidAwake_Trapper)
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
}
