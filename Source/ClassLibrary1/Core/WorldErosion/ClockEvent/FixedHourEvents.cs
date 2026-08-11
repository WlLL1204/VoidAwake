using Verse;

namespace VoidAwake
{
    /// <summary>固定枠の共通処理（発生通知）。各時の中身はサブクラスで後から拡張する。</summary>
    public abstract class VoidClockEventWorker_FixedHourBase : VoidClockEventWorker
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
    public class VoidClockEventWorker_FixedHour1 : VoidClockEventWorker_FixedHourBase
    {
    }

    /// <summary>時計の時4で必ず発火する固定イベント枠。</summary>
    public class VoidClockEventWorker_FixedHour4 : VoidClockEventWorker_FixedHourBase
    {
    }

    /// <summary>時計の時7で必ず発火する固定イベント枠。</summary>
    public class VoidClockEventWorker_FixedHour7 : VoidClockEventWorker_FixedHourBase
    {
    }

    /// <summary>時計の時10で必ず発火する固定イベント枠。</summary>
    public class VoidClockEventWorker_FixedHour10 : VoidClockEventWorker_FixedHourBase
    {
    }

    /// <summary>時計の時12で必ず発火する固定イベント枠。</summary>
    public class VoidClockEventWorker_FixedHour12 : VoidClockEventWorker_FixedHourBase
    {
    }
}
