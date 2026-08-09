using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VoidAwake
{
	public class MainTabWindow_VoidServants : MainTabWindow_PawnTable
	{
		protected override PawnTableDef PawnTableDef =>
			DefDatabase<PawnTableDef>.GetNamed("VoidAwake_VoidServants");

		protected override IEnumerable<Pawn> Pawns
		{
			get
			{
				Map map = Find.CurrentMap;
				if (map == null)
				{
					return Enumerable.Empty<Pawn>();
				}
				return map.mapPawns.PawnsInFaction(Faction.OfPlayer).Where(VoidServantUtility.IsVoidServant);
			}
		}
	}
}
