using Verse;

namespace VoidAwake
{
	public class HediffCompProperties_VoidAwake_TrapperKidnapping : HediffCompProperties
	{
		public float carryingCapacityOffset = 500f;

		public HediffCompProperties_VoidAwake_TrapperKidnapping()
		{
			compClass = typeof(HediffComp_VoidAwake_TrapperKidnapping);
		}
	}

	public class HediffComp_VoidAwake_TrapperKidnapping : HediffComp
	{
		private Pawn targetPawn;

		public HediffCompProperties_VoidAwake_TrapperKidnapping Props =>
			(HediffCompProperties_VoidAwake_TrapperKidnapping)props;

		public Pawn TargetPawn => targetPawn;

		public void SetTarget(Pawn target)
		{
			targetPawn = target;
		}

		public override void CompExposeData()
		{
			base.CompExposeData();
			Scribe_References.Look(ref targetPawn, "targetPawn");
		}
	}
}
