using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VoidAwake
{
	public class GameComponent_VoidAwake_TrapperKidnaps : GameComponent
	{
		private List<VoidAwake_TrapperKidnapRecord> kidnapped = new List<VoidAwake_TrapperKidnapRecord>();

		public GameComponent_VoidAwake_TrapperKidnaps(Game game)
		{
		}

		public static GameComponent_VoidAwake_TrapperKidnaps Get()
		{
			return Current.Game?.GetComponent<GameComponent_VoidAwake_TrapperKidnaps>();
		}

		public IReadOnlyList<VoidAwake_TrapperKidnapRecord> GetAllKidnapped()
		{
			PruneInvalid();
			return kidnapped;
		}

		public void RegisterKidnap(Pawn victim, Pawn kidnapper, Map map)
		{
			if (victim == null || map == null)
			{
				return;
			}

			PruneInvalid();

			if (kidnapper?.carryTracker?.CarriedThing == victim)
			{
				kidnapper.carryTracker.TryDropCarriedThing(kidnapper.Position, ThingPlaceMode.Direct, out _);
			}

			if (victim.Spawned)
			{
				victim.DeSpawn(DestroyMode.Vanish);
			}

			if (!Find.WorldPawns.Contains(victim))
			{
				Find.WorldPawns.PassToWorld(victim, PawnDiscardDecideMode.KeepForever);
			}

			kidnapped.Add(new VoidAwake_TrapperKidnapRecord(
				victim,
				GenTicks.TicksGame,
				map.Tile,
				kidnapper?.thingIDNumber ?? -1));

			Find.LetterStack.ReceiveLetter(
				"拉致",
				(victim.LabelShort ?? victim.Label) + " がトラッパーに拉致された。",
				LetterDefOf.NegativeEvent,
				new GlobalTargetInfo(map.Tile));
		}

		private void PruneInvalid()
		{
			for (int i = kidnapped.Count - 1; i >= 0; i--)
			{
				if (kidnapped[i]?.pawn == null || kidnapped[i].pawn.Destroyed)
				{
					kidnapped.RemoveAt(i);
				}
			}
		}

		public override void ExposeData()
		{
			Scribe_Collections.Look(ref kidnapped, "kidnapped", LookMode.Deep);
			if (Scribe.mode == LoadSaveMode.PostLoadInit && kidnapped == null)
			{
				kidnapped = new List<VoidAwake_TrapperKidnapRecord>();
			}
		}
	}
}
