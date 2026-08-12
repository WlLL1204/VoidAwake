using Verse;

namespace VoidAwake
{
    public class VoidAwake_Building_VoidMeditationSpot : Building
    {
        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();

            VoidAwake_CompMeditationAnchor anchor = this.TryGetComp<VoidAwake_CompMeditationAnchor>();
            if (anchor != null)
            {
                GenDraw.DrawRadiusRing(Position, anchor.Radius);
            }
        }
    }
}
