using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VoidAwake
{
    public class VoidAwake_MapComponent_GeneExtraction : MapComponent
    {
        private Dictionary<int, GeneDef> pendingByThingId = new Dictionary<int, GeneDef>();

        public VoidAwake_MapComponent_GeneExtraction(Map map) : base(map) { }

        public void SetPending(Pawn pawn, GeneDef geneDef)
        {
            if (pawn == null || geneDef == null)
                return;
            pendingByThingId[pawn.thingIDNumber] = geneDef;
        }

        public GeneDef GetPending(Pawn pawn)
        {
            if (pawn == null)
                return null;

            if (!pendingByThingId.TryGetValue(pawn.thingIDNumber, out GeneDef gene))
                return null;

            if (map.designationManager.DesignationOn(
                    pawn, VoidAwake_DesignationDefOf.VoidAwake_ExtractGene) == null)
            {
                pendingByThingId.Remove(pawn.thingIDNumber);
                return null;
            }

            return gene;
        }

        public void ClearPending(Pawn pawn)
        {
            if (pawn == null)
                return;
            pendingByThingId.Remove(pawn.thingIDNumber);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pendingByThingId, "pendingByThingId",
                LookMode.Value, LookMode.Def);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && pendingByThingId == null)
                pendingByThingId = new Dictionary<int, GeneDef>();
        }
    }
}
