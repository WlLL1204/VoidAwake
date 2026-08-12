using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace VoidAwake
{
	public class VoidAwake_Building_BearTrap : Building_TrapDamager
	{
		private const int TrapHitCount = 5;

		private static readonly FloatRange TrapDamageRandomFactorRange = new FloatRange(0.8f, 1.2f);

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			base.DrawAt(drawLoc, flip);
			VoidAwake_DoorTrapComboUtility.DrawTripwiresFrom(this);
		}

		protected override void SpringSub(Pawn p)
		{
			SoundDefOf.TrapSpring.PlayOneShot(new TargetInfo(Position, Map, false));
			if (p == null)
			{
				return;
			}

			float totalDamage = this.GetStatValue(StatDefOf.TrapMeleeDamage, true) * TrapDamageRandomFactorRange.RandomInRange;
			float damagePerHit = totalDamage / TrapHitCount;
			float armorPenetration = damagePerHit * 0.015f;

			for (int i = 0; i < TrapHitCount; i++)
			{
				BodyPartRecord hitPart = VoidAwake_BearTrapTargetingUtility.ChooseHitPart(p);
				DamageInfo dinfo = new DamageInfo(DamageDefOf.Stab, damagePerHit, armorPenetration, -1f, this, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown, null);
				DamageWorker.DamageResult damageResult = p.TakeDamage(dinfo);
				if (i == 0)
				{
					BattleLogEntry_DamageTaken battleLogEntry = new BattleLogEntry_DamageTaken(p, RulePackDefOf.DamageEvent_TrapSpike, null);
					Find.BattleLog.Add(battleLogEntry);
					damageResult.AssociateWithLog(battleLogEntry);
				}
			}

			VoidAwake_TrapperUtility.RevealAllTrappersOnMap(Map);

			if (p != null && !p.Dead && p.kindDef?.immuneToTraps != true)
			{
				VoidAwake_BearTrapCaughtUtility.TryApplyCaught(p);
			}
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			Map map = Map;
			base.Destroy(mode);
			if (map != null)
			{
				VoidAwake_TrapperUtility.NotifyTrapDestroyed(map);
			}
		}
	}
}
