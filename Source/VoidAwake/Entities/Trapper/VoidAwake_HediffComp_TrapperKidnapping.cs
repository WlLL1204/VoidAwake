using Verse;

namespace VoidAwake
{
	public class VoidAwake_HediffCompProperties_TrapperKidnapping : HediffCompProperties
	{
		public VoidAwake_HediffCompProperties_TrapperKidnapping()
		{
			compClass = typeof(VoidAwake_HediffComp_TrapperKidnapping);
		}
	}

	public class VoidAwake_HediffComp_TrapperKidnapping : HediffComp
	{
		private Pawn targetPawn;

		public VoidAwake_HediffCompProperties_TrapperKidnapping Props =>
			(VoidAwake_HediffCompProperties_TrapperKidnapping)props;

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
