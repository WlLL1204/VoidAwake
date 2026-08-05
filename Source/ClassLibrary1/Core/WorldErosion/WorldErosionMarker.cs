using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VoidAwake
{
    public class VoidAwake_WorldErosionMarker : WorldObject
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
                yield return g;

            yield return new Command_Action
            {
                defaultLabel = "基点を見る",
                defaultDesc = "この浸食基点へカメラを移動します。",
                action = () => CameraJumper.TryJump(this),
                // icon は省略可（仮）
            };
        }
    }
}