using System;
using System.Linq;
using RimWorld;
using Verse;

namespace VoidAwake
{
    public class VoidClockEventDef : Def
    {
        public float weight = 1f;

        /// <summary>1〜4。時計の四分円ごとのイベントテーブル。</summary>
        public int level = 1;

        public Type workerClass = typeof(VoidClockEventWorker_Placeholder);

        [Unsaved] private VoidClockEventWorker workerInt;

        public VoidClockEventWorker Worker
        {
            get
            {
                if (workerInt == null)
                {
                    workerInt = (VoidClockEventWorker)Activator.CreateInstance(workerClass);
                    workerInt.def = this;
                }
                return workerInt;
            }
        }

        public bool IsAllowedAtLevel(int eventLevel)
        {
            return level == eventLevel && weight > 0f;
        }
    }

    public abstract class VoidClockEventWorker
    {
        public VoidClockEventDef def;

        public virtual bool CanFire(int hour) => true;

        public abstract void TryExecute(int hour);

        /// <summary>発生したイベントをレターで表示する。</summary>
        protected void SendEventLetter(string body = null)
        {
            Find.LetterStack.ReceiveLetter(
                def.LabelCap,
                body ?? (def.description.NullOrEmpty()
                    ? $"ヴォイドの時計の針が進み、「{def.label}」が発生しました。"
                    : def.description),
                LetterDefOf.NeutralEvent);
        }
    }

    public class VoidClockEventWorker_Placeholder : VoidClockEventWorker
    {
        public override void TryExecute(int hour)
        {
            SendEventLetter();
            Log.Message($"[VoidAwake] ClockEvent '{def.defName}' (Lv{def.level}) fired at hour {hour}");
        }
    }

    public static class VoidClockEventUtility
    {
        /// <summary>
        /// 針 → イベントレベル。
        /// 1-3 → Lv1 / 4-6 → Lv2 / 7-9 → Lv3 / 10-12 → Lv4
        /// hour 0 はイベントなし。
        /// </summary>
        public static int HourToEventLevel(int hour)
        {
            if (hour <= 0) return 0;
            if (hour <= 3) return 1;
            if (hour <= 6) return 2;
            if (hour <= 9) return 3;
            return 4;
        }

        public static void TryFireRandomEvent(int hour)
        {
            int eventLevel = HourToEventLevel(hour);
            if (eventLevel <= 0)
                return;

            var candidates = DefDatabase<VoidClockEventDef>.AllDefsListForReading
                .Where(d => d.IsAllowedAtLevel(eventLevel) && d.Worker.CanFire(hour));

            if (!candidates.TryRandomElementByWeight(d => d.weight, out var chosen))
            {
                Log.Warning($"[VoidAwake] No clock events available for level {eventLevel} (hour {hour})");
                return;
            }

            chosen.Worker.TryExecute(hour);
        }
    }
}