using Verse;

namespace VoidAwake
{
    /// <summary>固定枠の共通処理（発生通知）。各時の中身はサブクラスで後から拡張する。</summary>
    public abstract class VoidAwake_ClockEventWorker_FixedHourBase : VoidAwake_ClockEventWorker
    {
        public override void TryExecute(int hour)
        {
            SendEventLetter();
            Log.Message($"[VoidAwake] FixedClockEvent '{def.defName}' at hour {hour}");
            ExecuteFixedEvent(hour);
        }

        /// <summary>時ごとのゲームプレイ効果。現状は空。</summary>
        protected virtual void ExecuteFixedEvent(int hour)
        {
        }
    }

    /// <summary>時計の時1で必ず発火する固定イベント枠。</summary>
    public class VoidAwake_ClockEventWorker_FixedHour1 : VoidAwake_ClockEventWorker_FixedHourBase
    {
    }

    /// <summary>時計の時4で必ず発火する固定イベント枠。</summary>
    //public class VoidAwake_ClockEventWorker_FixedHour4 : VoidAwake_ClockEventWorker_FixedHourBase
    //{
    //}
// このクラスは不要なので削除

    /// <summary>時計の時7で必ず発火する固定イベント枠。</summary>
    public class VoidAwake_ClockEventWorker_FixedHour7 : VoidAwake_ClockEventWorker_FixedHourBase
    {

    }

    /// <summary>時計の時10で必ず発火する固定イベント枠。</summary>
    public class VoidAwake_ClockEventWorker_FixedHour10 : VoidAwake_ClockEventWorker_FixedHourBase
    {
    }

    /// <summary>時計の時12で必ず発火する固定イベント枠。</summary>
    public class VoidAwake_ClockEventWorker_FixedHour12 : VoidAwake_ClockEventWorker_FixedHourBase
    {
    }
}
