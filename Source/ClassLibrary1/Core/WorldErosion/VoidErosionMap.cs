using RimWorld;
using Verse;
using static VoidAwake.VoidAwake_VoidErosion;

namespace VoidAwake
{
    public class VoidAwake_VoidWeosionMap : MapComponent
    {
        // 任意: デバッグ用。セーブしなくても可（毎回タイルから再計算でよい）
        private VoidErosionLevel applied = VoidErosionLevel.None;

        public VoidAwake_VoidWeosionMap(Map map) : base(map) { }

        public override void FinalizeInit()
        {
            Sync(); // マップ読込直後に一度合わせる
        }

        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame % 250 != 0) return;
            Sync();
        }

        private void Sync()
        {
            var erosion = Find.World.GetComponent<VoidAwake_VoidErosion>();
            if (erosion == null) return;

            VoidErosionLevel desired = erosion.GetErosionLevel(map.Tile);
            GameConditionDef desiredDef = DefFor(desired);

            // 既に正しいものが付いていればスキップ
            if (desiredDef != null
                && map.gameConditionManager.ConditionIsActive(desiredDef))
            {
                applied = desired;
                return;
            }
            if (desiredDef == null && applied == VoidErosionLevel.None
                && !AnyErosionActive())
            {
                return;
            }

            EndAllErosionConditions();

            if (desiredDef != null)
            {
                GameCondition cond = GameConditionMaker.MakeCondition(desiredDef);
                cond.Permanent = true; // タイル帯にいる間ずっと
                map.gameConditionManager.RegisterCondition(cond);
            }

            applied = desired;
        }

        private static GameConditionDef DefFor(VoidErosionLevel level)
        {
            switch (level)
            {
                case VoidErosionLevel.Light:
                    return DefDatabase<GameConditionDef>.GetNamed("VoidAwake_Erosion_Light");
                case VoidErosionLevel.Medium:
                    return DefDatabase<GameConditionDef>.GetNamed("VoidAwake_Erosion_Medium");
                case VoidErosionLevel.Heavy:
                    return DefDatabase<GameConditionDef>.GetNamed("VoidAwake_Erosion_Heavy");
                case VoidErosionLevel.Extreme:
                    return DefDatabase<GameConditionDef>.GetNamed("VoidAwake_Erosion_Extreme");
                default:
                    return null;
            }
        }

        private static readonly string[] ErosionDefNames =
        {
            "VoidAwake_Erosion_Light",
            "VoidAwake_Erosion_Medium",
            "VoidAwake_Erosion_Heavy",
            "VoidAwake_Erosion_Extreme",
        };

        private bool AnyErosionActive()
        {
            foreach (string name in ErosionDefNames)
            {
                var def = DefDatabase<GameConditionDef>.GetNamedSilentFail(name);
                if (def != null && map.gameConditionManager.ConditionIsActive(def))
                    return true;
            }
            return false;
        }

        private void EndAllErosionConditions()
        {
            foreach (string name in ErosionDefNames)
            {
                var def = DefDatabase<GameConditionDef>.GetNamedSilentFail(name);
                if (def == null) continue;
                var active = map.gameConditionManager.GetActiveCondition(def);
                // GetActiveCondition はワールド側も見るので、
                // マップ固有だけ消したい場合は ActiveConditions を直接見る方が安全
                if (active != null && active.gameConditionManager == map.gameConditionManager)
                    active.End();
            }
        }
    }
}