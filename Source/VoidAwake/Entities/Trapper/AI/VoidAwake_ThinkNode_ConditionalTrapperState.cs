using Verse;
using Verse.AI;

namespace VoidAwake
{
	public class VoidAwake_ThinkNode_ConditionalTrapperStealth : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn pawn)
		{
			VoidAwake_CompTrapper comp = pawn.TryGetComp<VoidAwake_CompTrapper>();
			return comp != null && comp.IsStealth;
		}
	}

	public class VoidAwake_ThinkNode_ConditionalTrapperCombat : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn pawn)
		{
			VoidAwake_CompTrapper comp = pawn.TryGetComp<VoidAwake_CompTrapper>();
			return comp != null && comp.IsCombat;
		}
	}

	public class VoidAwake_ThinkNode_ConditionalTrapperKidnap : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn pawn)
		{
			VoidAwake_CompTrapper comp = pawn.TryGetComp<VoidAwake_CompTrapper>();
			if (comp == null)
			{
				return false;
			}

			return comp.IsKidnap
				|| (comp.IsStealth && VoidAwake_TrapperKidnapUtility.HasKidnapTargets(pawn));
		}
	}
}
