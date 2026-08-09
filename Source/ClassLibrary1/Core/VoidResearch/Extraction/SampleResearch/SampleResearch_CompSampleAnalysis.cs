using RimWorld;
using Verse;

namespace VoidAwake
{
    public class VoidAwake_CompProperties_SampleAnalysis : CompProperties
    {
        public float basicKnowledgePerDay = 1f;
        public float advancedKnowledgePerDay = 1f;
        public int ticksPerCycle = 60000; // 1日
        public float fuelPerCycle = 1f;

        public VoidAwake_CompProperties_SampleAnalysis()
        {
            compClass = typeof(VoidAwake_CompSampleAnalysis);
        }
    }

    public class VoidAwake_CompSampleAnalysis : ThingComp
    {
        public VoidAwake_CompProperties_SampleAnalysis Props =>
            (VoidAwake_CompProperties_SampleAnalysis)props;

        public override void CompTick()
        {
            if (!parent.IsHashIntervalTick(Props.ticksPerCycle))
                return;

            var power = parent.TryGetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
                return;

            var basic = parent.TryGetComp<CompRefuelable_BasicSample>();
            if (basic != null && basic.HasFuel)
            {
                basic.ConsumeFuel(Props.fuelPerCycle);
                Find.ResearchManager.ApplyKnowledge(
                    KnowledgeCategoryDefOf.Basic, Props.basicKnowledgePerDay);
            }

            var advanced = parent.TryGetComp<CompRefuelable_AdvancedSample>();
            if (advanced != null && advanced.HasFuel)
            {
                advanced.ConsumeFuel(Props.fuelPerCycle);
                Find.ResearchManager.ApplyKnowledge(
                    KnowledgeCategoryDefOf.Advanced, Props.advancedKnowledgePerDay);
            }
        }

        public override string CompInspectStringExtra()
        {
            return "通電中、サンプルがある側の知識を毎日付与します。";
        }
    }
}