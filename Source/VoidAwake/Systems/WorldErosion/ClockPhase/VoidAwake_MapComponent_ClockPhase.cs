using Verse;

namespace VoidAwake
{
    /// <summary>新規コロニー・キャラバンマップ生成時に、現在の時計フェーズ効果を適用する。</summary>
    public class VoidAwake_MapComponent_ClockPhase : MapComponent
    {
        public VoidAwake_MapComponent_ClockPhase(Map map) : base(map)
        {
        }

        public override void FinalizeInit()
        {
            if (map.IsPocketMap || !map.Tile.Valid)
                return;

            var erosion = Find.World.GetComponent<VoidAwake_WorldComponent_VoidErosion>();
            erosion?.NotifyMapReadyForClockPhase(map);
        }
    }
}
