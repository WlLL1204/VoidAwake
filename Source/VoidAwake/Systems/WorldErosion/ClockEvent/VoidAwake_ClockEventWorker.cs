using RimWorld;
using Verse;

namespace VoidAwake
{
    public abstract class VoidAwake_ClockEventWorker
    {
        public VoidAwake_ClockEventDef def;

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

    public class VoidAwake_ClockEventWorker_Placeholder : VoidAwake_ClockEventWorker
    {
        public override void TryExecute(int hour)
        {
            SendEventLetter();
            Log.Message($"[VoidAwake] ClockEvent '{def.defName}' (Lv{def.level}) fired at hour {hour}");
        }
    }
}
