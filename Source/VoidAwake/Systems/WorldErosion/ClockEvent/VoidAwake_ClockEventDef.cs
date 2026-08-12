using System;
using Verse;

namespace VoidAwake
{
    public class VoidAwake_ClockEventDef : Def
    {
        public float weight = 1f;

        /// <summary>1〜4。時計の四分円ごとのイベントテーブル。</summary>
        public int level = 1;

        /// <summary>-1 = ランダム枠。0以上 = その時に必ず発火する固定枠（ランダム候補外）。</summary>
        public int fixedHour = -1;

        public Type workerClass = typeof(VoidAwake_ClockEventWorker_Placeholder);

        [Unsaved] private VoidAwake_ClockEventWorker workerInt;

        public VoidAwake_ClockEventWorker Worker
        {
            get
            {
                if (workerInt == null)
                {
                    workerInt = (VoidAwake_ClockEventWorker)Activator.CreateInstance(workerClass);
                    workerInt.def = this;
                }
                return workerInt;
            }
        }

        public bool IsFixedEvent => fixedHour >= 0;

        public bool IsAllowedAtLevel(int eventLevel)
        {
            // 固定枠はランダム抽選に混ぜない
            return !IsFixedEvent && level == eventLevel && weight > 0f;
        }
    }
}
