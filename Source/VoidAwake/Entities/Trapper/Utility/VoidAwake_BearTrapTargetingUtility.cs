using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace VoidAwake
{
	public static class VoidAwake_BearTrapTargetingUtility
	{
		public static BodyPartRecord ChooseHitPart(Pawn pawn)
		{
			if (pawn == null || pawn.Dead)
			{
				return null;
			}

			HediffSet hediffSet = pawn.health.hediffSet;
			if (pawn.Downed)
			{
				BodyPartRecord head = ChooseHead(hediffSet);
				if (head != null)
				{
					return head;
				}
			}
			else
			{
				BodyPartRecord leg = ChooseLeg(hediffSet);
				if (leg != null)
				{
					return leg;
				}
			}

			return hediffSet.GetRandomNotMissingPart(DamageDefOf.Stab);
		}

		private static BodyPartRecord ChooseHead(HediffSet hediffSet)
		{
			foreach (BodyPartRecord part in hediffSet.GetNotMissingParts())
			{
				if (part.def == BodyPartDefOf.Head)
				{
					return part;
				}
			}

			return ChooseFromGroup(hediffSet, BodyPartGroupDefOf.FullHead)
				?? ChooseFromGroup(hediffSet, BodyPartGroupDefOf.UpperHead);
		}

		private static BodyPartRecord ChooseLeg(HediffSet hediffSet)
		{
			List<BodyPartRecord> candidates = null;
			foreach (BodyPartRecord part in hediffSet.GetNotMissingParts())
			{
				if (!IsLegPart(part))
				{
					continue;
				}

				if (candidates == null)
				{
					candidates = new List<BodyPartRecord>();
				}

				candidates.Add(part);
			}

			if (candidates == null || candidates.Count == 0)
			{
				return null;
			}

			if (candidates.TryRandomElementByWeight(p => p.coverageAbs * p.def.GetHitChanceFactorFor(DamageDefOf.Stab), out BodyPartRecord result))
			{
				return result;
			}

			return candidates.RandomElement();
		}

		private static bool IsLegPart(BodyPartRecord part)
		{
			if (part.IsInGroup(BodyPartGroupDefOf.Legs))
			{
				return true;
			}

			List<BodyPartTagDef> tags = part.def.tags;
			return tags.Contains(BodyPartTagDefOf.MovingLimbCore)
				|| tags.Contains(BodyPartTagDefOf.MovingLimbSegment)
				|| tags.Contains(BodyPartTagDefOf.MovingLimbDigit);
		}

		private static BodyPartRecord ChooseFromGroup(HediffSet hediffSet, BodyPartGroupDef group)
		{
			List<BodyPartRecord> candidates = null;
			foreach (BodyPartRecord part in hediffSet.GetNotMissingParts())
			{
				if (!part.IsInGroup(group))
				{
					continue;
				}

				if (candidates == null)
				{
					candidates = new List<BodyPartRecord>();
				}

				candidates.Add(part);
			}

			if (candidates == null || candidates.Count == 0)
			{
				return null;
			}

			if (candidates.TryRandomElementByWeight(p => p.coverageAbs * p.def.GetHitChanceFactorFor(DamageDefOf.Stab), out BodyPartRecord result))
			{
				return result;
			}

			return candidates.RandomElement();
		}
	}
}
