using Verse;

namespace VoidAwake
{
	public class VoidAwake_Projectile_AcidFlask : Projectile_Explosive
	{
		protected override void Impact(Thing hitThing, bool blockedByShield)
		{
			Map map = Map;
			IntVec3 cell = Position;
			if (blockedByShield)
			{
				base.Impact(hitThing, true);
				return;
			}

			Destroy();
			if (map == null || VoidAwake_GhostShipDefOf.VoidAwake_AcidSlick == null || !cell.InBounds(map))
			{
				return;
			}

			if (cell.GetFirstThing(map, VoidAwake_GhostShipDefOf.VoidAwake_AcidSlick) != null)
			{
				return;
			}

			Thing slick = ThingMaker.MakeThing(VoidAwake_GhostShipDefOf.VoidAwake_AcidSlick);
			GenSpawn.Spawn(slick, cell, map);
		}
	}
}
