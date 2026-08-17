using UnityEngine;
using Verse;

namespace VoidAwake
{
	public class VoidAwake_Hediff_Mortal : Hediff
	{
		public const float AwakenedSeverity = 3f;
		public const int CorpseResurrectDelayTicks = 180;
		public const int InitialRefusalUses = 2;

		private int refusalUses = InitialRefusalUses;

		public bool IsAwakened => Severity >= AwakenedSeverity - 0.01f;

		public int RefusalUses
		{
			get => refusalUses;
			set => refusalUses = Mathf.Max(0, value);
		}

		public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
		{
			base.Notify_PawnDied(dinfo, culprit);
			if (def == null)
			{
				return;
			}

			if (!IsAwakened)
			{
				Severity = Mathf.Min(AwakenedSeverity, Severity + 1f);
				if (IsAwakened)
				{
					Awaken();
				}
			}
		}

		public override void Notify_Resurrected()
		{
			base.Notify_Resurrected();
			VoidAwake_GhostUtility.RestoreGhostUntilStanding(pawn);
		}

		public void Awaken()
		{
			Severity = AwakenedSeverity;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref refusalUses, "refusalUses", InitialRefusalUses);
		}
	}
}
