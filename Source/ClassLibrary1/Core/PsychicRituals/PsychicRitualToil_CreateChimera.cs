using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VoidAwake
{
	public class PsychicRitualToil_CreateChimera : PsychicRitualToil
	{
		private PsychicRitualRoleDef invokerRole;
		private PawnKindDef chimeraPawnKind;

		protected PsychicRitualToil_CreateChimera()
		{
		}

		public PsychicRitualToil_CreateChimera(PsychicRitualRoleDef invokerRole, PawnKindDef chimeraPawnKind)
		{
			this.invokerRole = invokerRole;
			this.chimeraPawnKind = chimeraPawnKind;
		}

		public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
		{
			base.Start(psychicRitual, parent);
			Pawn invoker = psychicRitual.assignments.FirstAssignedPawn(invokerRole);
			psychicRitual.ReleaseAllPawnsAndBuildings();
			if (invoker == null)
			{
				return;
			}

			ApplyOutcome(psychicRitual, invoker);
		}

		private void ApplyOutcome(PsychicRitual psychicRitual, Pawn invoker)
		{
			Map map = psychicRitual.Map;
			PawnKindDef kind = chimeraPawnKind ?? PawnKindDef.Named("VoidAwake_Chimera");
			IntVec3 center = psychicRitual.assignments.Target.IsValid
				? psychicRitual.assignments.Target.Cell
				: invoker.Position;

			if (!CellFinder.TryFindRandomCellNear(center, map, 6, (IntVec3 c) => c.Walkable(map) && c.Standable(map) && !c.Fogged(map), out IntVec3 spawnCell))
			{
				spawnCell = center;
			}

			Pawn chimera = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
				kind,
				Faction.OfPlayer,
				PawnGenerationContext.NonPlayer,
				forceGenerateNewPawn: true,
				canGeneratePawnRelations: false,
				allowAddictions: false));

			if (chimera.Name == null)
			{
				chimera.Name = PawnBioAndNameGenerator.GeneratePawnName(chimera, NameStyle.Numeric);
			}

			GenSpawn.Spawn(chimera, spawnCell, map);
			Find.LetterStack.ReceiveLetter(
				"PsychicRitualCompleteLabel".Translate(psychicRitual.def.label),
				"VoidAwake_CreateChimeraCompleteText".Translate(invoker, psychicRitual.def.Named("RITUAL")),
				LetterDefOf.PositiveEvent,
				chimera);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Defs.Look(ref invokerRole, "invokerRole");
			Scribe_Defs.Look(ref chimeraPawnKind, "chimeraPawnKind");
		}
	}
}
