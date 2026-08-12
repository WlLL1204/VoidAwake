using RimWorld;
using Verse;

namespace VoidAwake
{
	public class VoidAwake_Building_RabbitPassage : Building
	{
		private int pairId = -1;
		private IntVec3 linkedCell = IntVec3.Invalid;
		private int ownerId = -1;
		private bool destroyingPair;

		public int PairId => pairId;

		public int OwnerId => ownerId;

		public IntVec3 LinkedCell => linkedCell;

		public VoidAwake_Building_RabbitPassage LinkedPassage
		{
			get
			{
				if (!Spawned || !linkedCell.IsValid)
				{
					return null;
				}

				return linkedCell.GetFirstThing<VoidAwake_Building_RabbitPassage>(Map);
			}
		}

		public void ConfigurePair(int id, IntVec3 linked, int owner)
		{
			pairId = id;
			linkedCell = linked;
			ownerId = owner;
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			Map map = Map;
			IntVec3 otherCell = linkedCell;
			base.Destroy(mode);

			if (destroyingPair || map == null || !otherCell.IsValid)
			{
				return;
			}

			VoidAwake_Building_RabbitPassage other = otherCell.GetFirstThing<VoidAwake_Building_RabbitPassage>(map);
			if (other != null && !other.Destroyed)
			{
				other.destroyingPair = true;
				other.Destroy(DestroyMode.Vanish);
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref pairId, "pairId", -1);
			Scribe_Values.Look(ref linkedCell, "linkedCell", IntVec3.Invalid);
			Scribe_Values.Look(ref ownerId, "ownerId", -1);
		}
	}
}
