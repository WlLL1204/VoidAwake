using Verse;

namespace VoidAwake
{
    /// <summary>
    /// WorldClock フェーズ滞在中の効果。単発の ClockEvent とは別。
    /// セーブが必要な状態は Worker に持たせず、WorldComponent か GameCondition の ExposeData に置く。
    /// </summary>
    public abstract class VoidAwake_ClockPhaseEffectWorker
    {
        /// <summary>フェーズ開始。<paramref name="fromLoad"/> が true ならレター等の単発処理をスキップする。</summary>
        public virtual void OnEntered(int hour, bool fromLoad)
        {
        }

        /// <summary>次フェーズへ移る前の掃除。</summary>
        public virtual void OnExited(int hour)
        {
        }

        /// <summary>このフェーズにいる間、毎 WorldComponentTick。</summary>
        public virtual void Tick()
        {
        }

        /// <summary>既存マップ・新規マップへ現在フェーズを適用する。冪等であること。</summary>
        public virtual void ApplyToMap(Map map)
        {
        }
    }

    /// <summary>時 0。ヴォイドはまだ眠っている。</summary>
    public class VoidAwake_ClockPhaseEffectWorker_Phase0 : VoidAwake_ClockPhaseEffectWorker
    {
    }

    /// <summary>時 1–3。浸食の開始。</summary>
    public class VoidAwake_ClockPhaseEffectWorker_Phase1 : VoidAwake_ClockPhaseEffectWorker
    {
    }

    /// <summary>時 4–6。</summary>
    public class VoidAwake_ClockPhaseEffectWorker_Phase2 : VoidAwake_ClockPhaseEffectWorker
    {
    }

    /// <summary>時 7–9。</summary>
    public class VoidAwake_ClockPhaseEffectWorker_Phase3 : VoidAwake_ClockPhaseEffectWorker
    {
    }

    /// <summary>時 10–12。</summary>
    public class VoidAwake_ClockPhaseEffectWorker_Phase4 : VoidAwake_ClockPhaseEffectWorker
    {
    }
}
