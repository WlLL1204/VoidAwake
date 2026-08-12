using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
	public static class VoidAwake_TrapperKidnapUtility
	{
		private const float KidnapCarryingCapacityBonus = 500f;

		public static bool HasKidnapTargets(Pawn trapper)
		{
			return TryFindKidnapTarget(trapper, out _);
		}

		public static bool TryFindKidnapTarget(Pawn trapper, out Pawn victim)
		{
			victim = null;
			if (trapper?.Map == null || trapper.Dead)
			{
				return false;
			}

			if (trapper.carryTracker?.CarriedThing != null)
			{
				return false;
			}

			if (trapper.jobs?.curJob?.def == VoidAwake_TrapperDefOf.VoidAwake_TrapperKidnap)
			{
				return false;
			}

			Map map = trapper.Map;
			IReadOnlyList<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
			int bestDist = int.MaxValue;

			for (int i = 0; i < colonists.Count; i++)
			{
				Pawn candidate = colonists[i];
				if (!IsValidKidnapCandidate(trapper, candidate))
				{
					continue;
				}

				int dist = trapper.Position.DistanceToSquared(candidate.Position);
				if (dist < bestDist)
				{
					bestDist = dist;
					victim = candidate;
				}
			}

			return victim != null;
		}

		private static bool IsValidKidnapCandidate(Pawn trapper, Pawn candidate)
		{
			if (candidate == null || candidate.Dead || !candidate.Downed || !candidate.RaceProps.Humanlike)
			{
				return false;
			}

			if (!trapper.CanReserve(candidate, 1, -1, null, false))
			{
				return false;
			}

			if (!CanCarryVictim(trapper, candidate))
			{
				return false;
			}

			if (!RabbitPassageUtility.CanReachWithPassages(trapper, candidate.Position))
			{
				return false;
			}

			if (!RabbitPassageUtility.CanReachNormally(trapper, candidate.Position))
			{
				return false;
			}

			return true;
		}

		private static bool CanCarryVictim(Pawn trapper, Pawn candidate)
		{
			float capacity = trapper.GetStatValue(StatDefOf.CarryingCapacity) + KidnapCarryingCapacityBonus;
			return capacity >= candidate.GetStatValue(StatDefOf.Mass);
		}
	}
}
