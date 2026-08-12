using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VoidAwake
{
	public class VoidAwake_PawnTable_VoidServants : PawnTable
	{
		public VoidAwake_PawnTable_VoidServants(PawnTableDef def, Func<IEnumerable<Pawn>> pawnsGetter, int uiWidth, int uiHeight)
			: base(def, pawnsGetter, uiWidth, uiHeight)
		{
		}

		protected override IEnumerable<Pawn> LabelSortFunction(IEnumerable<Pawn> input)
		{
			return from p in input
				orderby p.Name == null || p.Name.Numerical, p.def.label, (p.Name is NameSingle name) ? name.Number : 0
				select p;
		}
	}
}
