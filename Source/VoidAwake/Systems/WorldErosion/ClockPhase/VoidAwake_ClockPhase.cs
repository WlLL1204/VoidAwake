namespace VoidAwake
{
    /// <summary>
    /// WorldClock の時 0–12 を 5 フェーズにまとめた区分。
    /// 時 0 → Phase0 / 1–3 → Phase1 / 4–6 → Phase2 / 7–9 → Phase3 / 10–12 → Phase4
    /// </summary>
    public enum VoidAwake_ClockPhase
    {
        Phase0 = 0,
        Phase1 = 1,
        Phase2 = 2,
        Phase3 = 3,
        Phase4 = 4,
    }

    public static class VoidAwake_ClockPhaseUtility
    {
        private static readonly VoidAwake_ClockPhaseEffectWorker[] workers =
        {
            new VoidAwake_ClockPhaseEffectWorker_Phase0(),
            new VoidAwake_ClockPhaseEffectWorker_Phase1(),
            new VoidAwake_ClockPhaseEffectWorker_Phase2(),
            new VoidAwake_ClockPhaseEffectWorker_Phase3(),
            new VoidAwake_ClockPhaseEffectWorker_Phase4(),
        };

        public static VoidAwake_ClockPhase HourToPhase(int hour)
        {
            if (hour <= 0) return VoidAwake_ClockPhase.Phase0;
            if (hour <= 3) return VoidAwake_ClockPhase.Phase1;
            if (hour <= 6) return VoidAwake_ClockPhase.Phase2;
            if (hour <= 9) return VoidAwake_ClockPhase.Phase3;
            return VoidAwake_ClockPhase.Phase4;
        }

        public static VoidAwake_ClockPhaseEffectWorker WorkerFor(VoidAwake_ClockPhase phase)
        {
            int i = (int)phase;
            if (i < 0 || i >= workers.Length)
                return workers[0];
            return workers[i];
        }
    }
}
