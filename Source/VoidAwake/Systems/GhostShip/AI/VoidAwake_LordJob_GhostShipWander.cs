using Verse;
using Verse.AI.Group;

namespace VoidAwake
{
	public class VoidAwake_LordJob_GhostShipWander : LordJob
	{
		public override bool AddFleeToil => false;

		public override StateGraph CreateGraph()
		{
			StateGraph graph = new StateGraph();
			graph.StartingToil = new VoidAwake_LordToil_GhostShipWander();
			return graph;
		}
	}
}
