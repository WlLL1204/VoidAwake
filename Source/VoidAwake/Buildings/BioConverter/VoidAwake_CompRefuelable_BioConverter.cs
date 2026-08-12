using RimWorld;

namespace VoidAwake
{
    // バイオコンバータは燃料槽を2つ持つため、TryGetComp で区別できるよう派生型に分ける
    public abstract class VoidAwake_CompRefuelable_BioConverterBase : CompRefuelable { }

    public class VoidAwake_CompRefuelable_TwistedMeat : VoidAwake_CompRefuelable_BioConverterBase { }
    public class VoidAwake_CompRefuelable_DreadLeather : VoidAwake_CompRefuelable_BioConverterBase { }
}
