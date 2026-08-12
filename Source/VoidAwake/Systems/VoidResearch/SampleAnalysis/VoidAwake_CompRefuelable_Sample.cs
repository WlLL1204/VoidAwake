using RimWorld;

namespace VoidAwake
{
    // 分析台は燃料槽を2つ持つため、TryGetComp で区別できるよう派生型に分ける
    public abstract class VoidAwake_CompRefuelable_SampleBase : CompRefuelable { }

    public class VoidAwake_CompRefuelable_BasicSample : VoidAwake_CompRefuelable_SampleBase { }
    public class VoidAwake_CompRefuelable_AdvancedSample : VoidAwake_CompRefuelable_SampleBase { }
}
