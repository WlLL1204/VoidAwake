using System.Linq;
using Verse;

namespace VoidAwake
{
    public static class VoidAwake_ClockEventUtility
    {
        /// <summary>
        /// 針 → イベントレベル（ClockPhase と同じ区切り）。
        /// 1-3 → Lv1 / 4-6 → Lv2 / 7-9 → Lv3 / 10-12 → Lv4
        /// hour 0 はイベントなし。
        /// </summary>
        public static int HourToEventLevel(int hour)
        {
            return (int)VoidAwake_ClockPhaseUtility.HourToPhase(hour);
        }

        public static void TryFireRandomEvent(int hour)
        {
            // 別枠: 固定イベント（該当時のみ。ランダムと併発）
            TryFireFixedEvent(hour);

            int eventLevel = HourToEventLevel(hour);
            if (eventLevel <= 0)
                return;

            var candidates = DefDatabase<VoidAwake_ClockEventDef>.AllDefsListForReading
                .Where(d => d.IsAllowedAtLevel(eventLevel) && d.Worker.CanFire(hour));

            if (!candidates.TryRandomElementByWeight(d => d.weight, out var chosen))
            {
                Log.Warning($"[VoidAwake] No clock events available for level {eventLevel} (hour {hour})");
                return;
            }

            chosen.Worker.TryExecute(hour);
        }

        /// <summary>固定枠。時 1/4/7/10/12 などで必ず発火。ランダムとは独立。</summary>
        public static void TryFireFixedEvent(int hour)
        {
            VoidAwake_ClockEventDef fixedDef = DefDatabase<VoidAwake_ClockEventDef>.AllDefsListForReading
                .FirstOrDefault(d => d.fixedHour == hour && d.Worker.CanFire(hour));
            if (fixedDef == null)
                return;

            fixedDef.Worker.TryExecute(hour);
        }
    }
}
