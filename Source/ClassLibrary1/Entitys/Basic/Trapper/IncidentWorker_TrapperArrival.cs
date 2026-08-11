using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VoidAwake
{
	public class IncidentWorker_TrapperArrival : IncidentWorker
	{
		protected override bool CanFireNowSub(IncidentParms parms)
		{
			if (!(parms.target is Map map))
			{
				return false;
			}

			return map.IsPlayerHome;
		}

		protected override bool TryExecuteWorker(IncidentParms parms)
		{
			Map map = (Map)parms.target;
			if (!RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 entryCell, map, CellFinder.EdgeRoadChance_Hostile))
			{
				return false;
			}

			int count = GetTrapperCount(parms.points);
			List<Pawn> spawned = new List<Pawn>();

			for (int i = 0; i < count; i++)
			{
				IntVec3 cell = CellFinder.RandomClosewalkCellNear(entryCell, map, 10);
				if (!cell.IsValid)
				{
					cell = entryCell;
				}

				Pawn trapper = PawnGenerator.GeneratePawn(VoidAwake_TrapperDefOf.Trapper, Faction.OfEntities);
				GenSpawn.Spawn(trapper, cell, map);
				EnsureStealth(trapper);
				spawned.Add(trapper);
			}

			// Intentionally no SendStandardLetter (Sightstealer-style silent arrival).
			return spawned.Count > 0;
		}

		private static int GetTrapperCount(float points)
		{
			if (points >= 800f)
			{
				return 3;
			}

			if (points >= 400f)
			{
				return 2;
			}

			return 1;
		}

		private static void EnsureStealth(Pawn pawn)
		{
			if (pawn?.health?.hediffSet == null)
			{
				return;
			}

			if (pawn.health.hediffSet.HasHediff(VoidAwake_TrapperDefOf.VoidAwake_TrapperStealth))
			{
				return;
			}

			pawn.health.AddHediff(VoidAwake_TrapperDefOf.VoidAwake_TrapperStealth);
		}
	}
}
