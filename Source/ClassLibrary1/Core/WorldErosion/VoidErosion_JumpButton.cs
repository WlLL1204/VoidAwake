using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VoidAwake
{
    public class VoidAwake_JumpButtom : MainButtonWorker
    {
        public override void Activate()
        {
            var erosion = Find.World?.GetComponent<VoidAwake_VoidErosion>();
            if (erosion == null || !erosion.originTile.Valid)
            {
                Messages.Message("浸食基点がまだありません。", MessageTypeDefOf.RejectInput, false);
                return;
            }

            CameraJumper.TryJump(erosion.originTile, CameraJumper.MovementMode.Pan);
        }
    }
}