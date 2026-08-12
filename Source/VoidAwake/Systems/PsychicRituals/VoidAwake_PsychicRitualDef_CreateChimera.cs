using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VoidAwake
{
	public class VoidAwake_PsychicRitualDef_CreateChimera : PsychicRitualDef_InvocationCircle
	{
		public PawnKindDef chimeraPawnKind;

		public override List<PsychicRitualToil> CreateToils(PsychicRitual psychicRitual, PsychicRitualGraph graph)
		{
			List<PsychicRitualToil> list = base.CreateToils(psychicRitual, graph);
			list.Add(new VoidAwake_PsychicRitualToil_CreateChimera(InvokerRole, chimeraPawnKind));
			return list;
		}

		public override TaggedString OutcomeDescription(FloatRange qualityRange, string qualityNumber, PsychicRitualRoleAssignments assignments)
		{
			return outcomeDescription;
		}
	}
}
