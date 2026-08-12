using Verse;

namespace VoidAwake
{
	public class VoidAwake_TrapperKidnapRecord : IExposable
	{
		public Pawn pawn;
		public int kidnapTick;
		public int originMapTile;
		public int kidnapperThingId;

		public VoidAwake_TrapperKidnapRecord()
		{
		}

		public VoidAwake_TrapperKidnapRecord(Pawn pawn, int kidnapTick, int originMapTile, int kidnapperThingId)
		{
			this.pawn = pawn;
			this.kidnapTick = kidnapTick;
			this.originMapTile = originMapTile;
			this.kidnapperThingId = kidnapperThingId;
		}

		public void ExposeData()
		{
			Scribe_References.Look(ref pawn, "pawn");
			Scribe_Values.Look(ref kidnapTick, "kidnapTick", 0);
			Scribe_Values.Look(ref originMapTile, "originMapTile", 0);
			Scribe_Values.Look(ref kidnapperThingId, "kidnapperThingId", 0);
		}
	}
}
