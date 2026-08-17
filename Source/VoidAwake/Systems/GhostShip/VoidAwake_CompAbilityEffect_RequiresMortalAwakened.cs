using RimWorld;
using Verse;

namespace VoidAwake
{
	public class VoidAwake_CompProperties_AbilityRequiresMortalAwakened : CompProperties_AbilityEffect
	{
		public VoidAwake_CompProperties_AbilityRequiresMortalAwakened()
		{
			compClass = typeof(VoidAwake_CompAbilityEffect_RequiresMortalAwakened);
		}
	}

	public class VoidAwake_CompAbilityEffect_RequiresMortalAwakened : CompAbilityEffect
	{
		public override bool CanCast
		{
			get
			{
				VoidAwake_Hediff_Mortal mortal = VoidAwake_GhostUtility.GetMortal(parent.pawn);
				return mortal != null && mortal.IsAwakened;
			}
		}

		public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
		{
			if (!CanCast)
			{
				return false;
			}

			return base.Valid(target, throwMessages);
		}

		public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
		{
			return CanCast && base.CanApplyOn(target, dest);
		}

		public override bool AICanTargetNow(LocalTargetInfo target)
		{
			return CanCast;
		}
	}
}
