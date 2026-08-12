using Verse;

namespace VoidAwake
{
    public class Building_VoidAwake_VoidMeditationSpot : Building
    {
        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();

            CompVoidAwake_MeditationAnchor anchor = this.TryGetComp<CompVoidAwake_MeditationAnchor>();
            if (anchor != null)
            {
                GenDraw.DrawRadiusRing(Position, anchor.Radius);
            }
        }
    }
}
