using RimWorld;
using Verse;

namespace VoidAwake
{
    public class VoidAwake_HediffCompProperties_SporeParasite : HediffCompProperties
    {
        /// <summary>胞子の外にいる間の1時間あたりseverity減少。</summary>
        public float severityLossPerHour = 0.1f;
        /// <summary>離脱後、減衰を始めるまでの猶予（tick）。</summary>
        public int leaveGraceTicks = 120;
        public float removeBelowSeverity = 0.0001f;

        public VoidAwake_HediffCompProperties_SporeParasite()
        {
            compClass = typeof(VoidAwake_HediffComp_SporeParasite);
        }
    }

    /// <summary>
    /// 胞子寄生の減衰側。進行は胞子源の VoidAwake_CompCorruptionSpore が
    /// NotifyExposed とともに severity を積むので、ここでは離脱後の後退だけを扱う。
    /// </summary>
    public class VoidAwake_HediffComp_SporeParasite : HediffComp
    {
        private int lastExposedTick = -99999;

        public VoidAwake_HediffCompProperties_SporeParasite Props => (VoidAwake_HediffCompProperties_SporeParasite)props;

        public void NotifyExposed()
        {
            lastExposedTick = Find.TickManager.TicksGame;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Find.TickManager.TicksGame - lastExposedTick <= Props.leaveGraceTicks)
                return;

            severityAdjustment -= Props.severityLossPerHour / GenDate.TicksPerHour;
        }

        public override bool CompShouldRemove => parent.Severity <= Props.removeBelowSeverity;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref lastExposedTick, "lastExposedTick", -99999);
        }
    }
}
