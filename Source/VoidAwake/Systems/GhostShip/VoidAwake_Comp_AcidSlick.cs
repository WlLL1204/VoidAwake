using RimWorld;
using Verse;

namespace VoidAwake
{
	public class VoidAwake_CompProperties_AcidSlick : CompProperties
	{
		public int durationTicks = 480;
		public int damageIntervalTicks = 30;
		public int damageAmount = 4;
		public int chebyshevRadius = 1;

		public VoidAwake_CompProperties_AcidSlick()
		{
			compClass = typeof(VoidAwake_Comp_AcidSlick);
		}
	}

	public class VoidAwake_Comp_AcidSlick : ThingComp
	{
		private int ticksLeft;
		private int damageTick;

		private VoidAwake_CompProperties_AcidSlick Props => (VoidAwake_CompProperties_AcidSlick)props;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			if (!respawningAfterLoad)
			{
				ticksLeft = Props.durationTicks;
				damageTick = 0;
			}
		}

		public override void CompTick()
		{
			ticksLeft--;
			if (ticksLeft <= 0)
			{
				if (!parent.Destroyed)
				{
					parent.Destroy();
				}

				return;
			}

			damageTick++;
			if (damageTick < Props.damageIntervalTicks)
			{
				return;
			}

			damageTick = 0;
			ApplyDamage();
		}

		private void ApplyDamage()
		{
			Map map = parent.Map;
			if (map == null)
			{
				return;
			}

			IntVec3 center = parent.Position;
			int r = Props.chebyshevRadius;
			DamageDef dmg = DamageDefOf.AcidBurn;
			for (int dx = -r; dx <= r; dx++)
			{
				for (int dz = -r; dz <= r; dz++)
				{
					IntVec3 c = new IntVec3(center.x + dx, 0, center.z + dz);
					if (!c.InBounds(map))
					{
						continue;
					}

					Pawn pawn = c.GetFirstPawn(map);
					if (pawn == null || VoidAwake_GhostUtility.IsGhostPawn(pawn))
					{
						continue;
					}

					pawn.TakeDamage(new DamageInfo(dmg, Props.damageAmount, 0.15f, -1f, parent));
				}
			}
		}

		public override void PostExposeData()
		{
			Scribe_Values.Look(ref ticksLeft, "ticksLeft", 0);
			Scribe_Values.Look(ref damageTick, "damageTick", 0);
		}
	}
}
