using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VoidAwake
{
	public class VoidAwake_CompProperties_Chimera : CompProperties
	{
		public float rageEndHealthPercentThreshold = 0.98f;
		public float allyRageRadius = 8.9f;

		[MustTranslate]
		public string simpleAnimalLabel;

		public VoidAwake_CompProperties_Chimera()
		{
			compClass = typeof(VoidAwake_CompChimera);
		}
	}

	public class VoidAwake_CompChimera : ThingComp
	{
		private float totalDamageTaken;

		public VoidAwake_CompProperties_Chimera Props => (VoidAwake_CompProperties_Chimera)props;

		public Pawn Pawn => (Pawn)parent;

		public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
		{
			if (Pawn.Dead)
			{
				return;
			}

			totalDamageTaken += totalDamageDealt;
			if (Pawn.Dead || totalDamageTaken <= 0f || Pawn.health.Downed || Pawn.health.hediffSet.HasHediff(HediffDefOf.RageSpeed))
			{
				return;
			}

			Pawn.health.AddHediff(HediffMaker.MakeHediff(HediffDefOf.RageSpeed, Pawn));
			if (Pawn.Spawned)
			{
				EffecterDefOf.ChimeraRage.Spawn(Pawn.Position, Pawn.Map).Cleanup();
				ApplyRageToNearbyColonists();
			}
		}

		private void ApplyRageToNearbyColonists()
		{
			Map map = Pawn.Map;
			if (map == null)
			{
				return;
			}

			float radius = Props.allyRageRadius;
			List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
			for (int i = 0; i < colonists.Count; i++)
			{
				Pawn colonist = colonists[i];
				if (colonist.Dead || colonist.Downed)
				{
					continue;
				}
				if (!colonist.Position.InHorDistOf(Pawn.Position, radius))
				{
					continue;
				}
				if (colonist.health.hediffSet.HasHediff(HediffDefOf.RageSpeed))
				{
					continue;
				}
				colonist.health.AddHediff(HediffMaker.MakeHediff(HediffDefOf.RageSpeed, colonist));
			}
		}

		public override void Notify_Downed()
		{
			Hediff rage = Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.RageSpeed);
			if (rage != null)
			{
				Pawn.health.RemoveHediff(rage);
			}
		}

		public override void CompTickRare()
		{
			base.CompTickRare();
			if (Pawn.Dead || Pawn.health.summaryHealth.SummaryHealthPercent < Props.rageEndHealthPercentThreshold)
			{
				return;
			}

			Hediff rage = Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.RageSpeed);
			if (rage != null)
			{
				Pawn.health.RemoveHediff(rage);
				totalDamageTaken = 0f;
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref totalDamageTaken, "totalDamageTaken", 0f);
		}
	}
}
