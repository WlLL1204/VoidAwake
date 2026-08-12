using Verse;

namespace VoidAwake
{
    public class VoidAwake_HediffCompProperties_SpasmGasExposure : HediffCompProperties
    {
        /// <summary>ガス外での1秒あたりseverity減少。重度(3)から消失まで約 3 / この値 秒。</summary>
        public float severityLossPerSecond = 0.035f;
        /// <summary>離脱後、減衰を始めるまでの猶予（tick）。</summary>
        public int leaveGraceTicks = 60;
        public float removeBelowSeverity = 0.05f;

        public VoidAwake_HediffCompProperties_SpasmGasExposure()
        {
            compClass = typeof(VoidAwake_HediffComp_SpasmGasExposure);
        }
    }

    public class VoidAwake_HediffComp_SpasmGasExposure : HediffComp
    {
        private int lastExposedTick = -99999;

        public VoidAwake_HediffCompProperties_SpasmGasExposure Props => (VoidAwake_HediffCompProperties_SpasmGasExposure)props;

        public void NotifyExposed()
        {
            lastExposedTick = Find.TickManager.TicksGame;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            int since = Find.TickManager.TicksGame - lastExposedTick;
            if (since <= Props.leaveGraceTicks)
                return;

            severityAdjustment -= Props.severityLossPerSecond / GenTicks.TicksPerRealSecond;
        }

        public override bool CompShouldRemove => parent.Severity <= Props.removeBelowSeverity;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref lastExposedTick, "lastExposedTick", -99999);
        }
    }
}
