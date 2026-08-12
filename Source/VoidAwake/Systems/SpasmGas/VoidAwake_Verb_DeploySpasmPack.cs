using RimWorld;
using Verse;

namespace VoidAwake
{
    public class VoidAwake_Verb_DeploySpasmPack : Verb
    {
        protected override bool TryCastShot()
        {
            CompApparelReloadable reloadable = ReloadableCompSource;
            if (reloadable == null || !reloadable.CanBeUsed(out _))
                return false;

            VoidAwake_CompSpasmGasRelease gas = reloadable.parent.TryGetComp<VoidAwake_CompSpasmGasRelease>();
            if (gas == null)
                return false;

            gas.StartRelease();
            reloadable.UsedOnce();
            return true;
        }
    }
}
