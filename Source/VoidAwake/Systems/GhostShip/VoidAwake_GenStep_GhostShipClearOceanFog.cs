using RimWorld;
using Verse;

namespace VoidAwake
{
	public class VoidAwake_GenStep_GhostShipClearOceanFog : GenStep
	{
		public override int SeedPart => 184720332;

		public override void Generate(Map map, GenStepParams parms)
		{
			if (map == null)
			{
				return;
			}

			foreach (IntVec3 cell in map.AllCells)
			{
				TerrainDef terrain = cell.GetTerrain(map);
				if (terrain != null && terrain.IsWater)
				{
					map.fogGrid.Unfog(cell);
				}
			}
		}
	}
}
