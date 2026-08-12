using Verse;

namespace VoidAwake
{
	public static class TrapperLoadoutUtility
	{
		public static void EnsureRabbitShotEquipped(Pawn pawn)
		{
			if (pawn?.equipment == null || pawn.equipment.Primary != null)
			{
				return;
			}

			ThingDef gunDef = VoidAwake_TrapperDefOf.VoidAwake_Gun_RabbitShot;
			if (gunDef == null)
			{
				return;
			}

			ThingWithComps gun = (ThingWithComps)ThingMaker.MakeThing(gunDef);
			pawn.equipment.AddEquipment(gun);
		}
	}
}
