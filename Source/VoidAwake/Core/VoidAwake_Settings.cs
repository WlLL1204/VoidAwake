using Verse;

namespace VoidAwake
{
    public class VoidAwake_Settings : ModSettings
    {
        public bool showGravshipTrail = true; // デフォルトON
        public bool showErosionRateUI = true;
        public float erosionUiX = -1f;
        public float erosionUiY = -1f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref showGravshipTrail, "showGravshipTrail", true);
            Scribe_Values.Look(ref showErosionRateUI, "showErosionRateUI", true);
            Scribe_Values.Look(ref erosionUiX, "erosionUiX", -1f);
            Scribe_Values.Look(ref erosionUiY, "erosionUiY", -1f);
        }

    }
}
