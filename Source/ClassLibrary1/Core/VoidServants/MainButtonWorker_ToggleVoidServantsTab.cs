using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VoidAwake
{
	public class MainButtonWorker_ToggleVoidServantsTab : MainButtonWorker_ToggleTab
	{
		private const long DisabledCheckInterval = 180L;

		private long lastDisabledCheckTick = -10000000L;
		private int lastDisabledCheckMapId = int.MinValue;
		private bool lastDisabled;

		public override bool Disabled
		{
			get
			{
				if (base.Disabled)
				{
					return true;
				}
				Map currentMap = Find.CurrentMap;
				int mapId = currentMap?.uniqueID ?? int.MinValue;
				if (GenTicks.TicksGame - lastDisabledCheckTick < DisabledCheckInterval && lastDisabledCheckMapId == mapId)
				{
					return lastDisabled;
				}
				lastDisabledCheckMapId = mapId;
				lastDisabledCheckTick = GenTicks.TicksGame;
				if (currentMap != null)
				{
					List<Pawn> pawns = currentMap.mapPawns.PawnsInFaction(Faction.OfPlayer);
					for (int i = 0; i < pawns.Count; i++)
					{
						if (VoidServantUtility.IsVoidServant(pawns[i]))
						{
							lastDisabled = false;
							return false;
						}
					}
				}
				lastDisabled = true;
				return true;
			}
		}

		public override bool Visible => !Disabled;
	}
}
