using RimWorld;
using Verse;

namespace VoidAwake
{
    public class VoidAwake_ThoughtWorker_TwistedBondStrain : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p?.needs?.mood == null)
            {
                return ThoughtState.Inactive;
            }

            VoidAwake_CompVoidMagic comp = VoidAwake_VoidMagicUtility.GetComp(p);
            if (comp == null || comp.BondStrainStacks() <= 0)
            {
                return ThoughtState.Inactive;
            }

            return ThoughtState.ActiveAtStage(0);
        }
    }

    public class VoidAwake_Thought_TwistedBondStrain : Thought_Situational
    {
        public override float MoodOffset()
        {
            VoidAwake_CompVoidMagic comp = VoidAwake_VoidMagicUtility.GetComp(pawn);
            int stacks = comp != null ? comp.BondStrainStacks() : 0;
            if (stacks <= 0)
            {
                return 0f;
            }

            return VoidAwake_VoidMagicUtility.BondStrainMoodPerStack * stacks;
        }

        public override string LabelCap
        {
            get
            {
                VoidAwake_CompVoidMagic comp = VoidAwake_VoidMagicUtility.GetComp(pawn);
                int stacks = comp != null ? comp.BondStrainStacks() : 0;
                return "VoidAwake_TwistedBondStrainLabel".Translate(stacks).CapitalizeFirst();
            }
        }
    }
}
