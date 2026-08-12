using Verse;
using Verse.AI;

namespace VoidAwake
{
	public class ThinkNode_ConditionalTrapperStealth : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn pawn)
		{
			VoidAwake_TrapperComp comp = pawn.TryGetComp<VoidAwake_TrapperComp>();
			return comp != null && comp.IsStealth;
		}
	}

	public class ThinkNode_ConditionalTrapperCombat : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn pawn)
		{
			VoidAwake_TrapperComp comp = pawn.TryGetComp<VoidAwake_TrapperComp>();
			return comp != null && comp.IsCombat;
		}
	}

	public class ThinkNode_ConditionalTrapperKidnap : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn pawn)
		{
			VoidAwake_TrapperComp comp = pawn.TryGetComp<VoidAwake_TrapperComp>();
			if (comp == null)
			{
				return false;
			}

			return comp.IsKidnap
				|| ((comp.IsStealth || comp.IsCombat) && VoidAwake_TrapperKidnapUtility.HasKidnapTargets(pawn));
		}
	}
}
