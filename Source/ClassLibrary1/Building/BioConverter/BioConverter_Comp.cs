using RimWorld;
using Verse;

namespace VoidAwake
{
    public class VoidAwake_CompProperties_BioConverter : CompProperties
    {
        public int bioferritePerCycle = 5;
        public float fuelPerCycle = 1f;
        public int ticksPerCycle = 15000;

        public VoidAwake_CompProperties_BioConverter()
        {
            compClass = typeof(VoidAwake_CompBioConverter);
        }
    }

    public class VoidAwake_CompBioConverter : ThingComp
    {
        public VoidAwake_CompProperties_BioConverter Props =>
            (VoidAwake_CompProperties_BioConverter)props;

        public override void CompTick()
        {
            if (!parent.IsHashIntervalTick(Props.ticksPerCycle))
                return;

            var power = parent.TryGetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
                return;

            var meat = parent.TryGetComp<CompRefuelable_TwistedMeat>();
            var leather = parent.TryGetComp<CompRefuelable_DreadLeather>();

            bool hasMeat = meat != null && meat.HasFuel;
            bool hasLeather = leather != null && leather.HasFuel;
            if (!hasMeat && !hasLeather)
                return;

            int amount = Props.bioferritePerCycle;
            if (hasMeat && hasLeather)
                amount *= 2; // 両方で効率2倍

            if (hasMeat)
                meat.ConsumeFuel(Props.fuelPerCycle);
            if (hasLeather)
                leather.ConsumeFuel(Props.fuelPerCycle);

            Thing product = ThingMaker.MakeThing(ThingDef.Named("Bioferrite"));
            product.stackCount = amount;
            GenPlace.TryPlaceThing(product, parent.InteractionCell, parent.Map, ThingPlaceMode.Near);
        }

        public override string CompInspectStringExtra()
        {
            var meat = parent.TryGetComp<CompRefuelable_TwistedMeat>();
            var leather = parent.TryGetComp<CompRefuelable_DreadLeather>();
            bool both = meat != null && meat.HasFuel && leather != null && leather.HasFuel;
            return both
                ? "両方装填中: 生産効率 x2"
                : "片側装填: 通常効率（両方で x2）";
        }
    }
}