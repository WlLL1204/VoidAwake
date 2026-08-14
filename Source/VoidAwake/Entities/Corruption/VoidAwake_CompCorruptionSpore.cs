using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidAwake
{
    public class VoidAwake_CompProperties_CorruptionSpore : CompProperties
    {
        /// <summary>1回の排出量。実際にはこの値に pawn の bodySize を掛けた量を撒く。</summary>
        public int amountPerEmission = 100;
        public int intervalTicks = 60;
        public EffecterDef effecterEmitting;

        /// <summary>胞子に接触した者へ付ける hediff。null なら寄生させない。</summary>
        public HediffDef parasiteHediff;
        /// <summary>bodySize 1 のときの胞子の到達半径。実効半径は sqrt(bodySize) 倍。</summary>
        public float parasiteRadius = 3.5f;
        /// <summary>胞子の中に留まり続けたとき、寄生が致死（severity 1）に達するまでの時間。</summary>
        public float parasiteHoursToLethal = 3f;

        public VoidAwake_CompProperties_CorruptionSpore()
        {
            compClass = typeof(VoidAwake_CompCorruptionSpore);
        }
    }

    /// <summary>
    /// 腐敗の胞子。親の足元にバニラの腐敗臭ガスを撒き続けつつ、
    /// 胞子の届く範囲にいる者へ胞子寄生を進行させる。
    /// 肺腐敗病への進行はバニラの LungRotExposure 側が処理するので触らない。
    /// </summary>
    public class VoidAwake_CompCorruptionSpore : ThingComp
    {
        private static readonly List<Pawn> tmpPawns = new List<Pawn>();

        private Effecter effecter;

        public VoidAwake_CompProperties_CorruptionSpore Props => (VoidAwake_CompProperties_CorruptionSpore)props;

        private bool CanEmit
        {
            get
            {
                if (!parent.Spawned || parent.Map == null)
                    return false;
                return !(parent is Pawn pawn) || !pawn.Dead;
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!CanEmit)
            {
                CleanupEffecter();
                return;
            }

            Map map = parent.Map;
            IntVec3 center = parent.Position;

            if (Props.effecterEmitting != null)
            {
                if (effecter == null)
                {
                    effecter = Props.effecterEmitting.Spawn();
                    effecter.Trigger(new TargetInfo(center, map), TargetInfo.Invalid);
                }
                effecter.EffectTick(new TargetInfo(center, map), TargetInfo.Invalid);
            }

            if (!parent.IsHashIntervalTick(Props.intervalTicks))
                return;

            float scale = parent is Pawn p ? p.BodySize : 1f;
            int amount = Mathf.Max(1, Mathf.RoundToInt(Props.amountPerEmission * scale));
            GasUtility.AddGas(center, map, GasType.RotStink, amount);

            ProgressParasite(map, center, scale);
        }

        private void ProgressParasite(Map map, IntVec3 center, float bodySize)
        {
            HediffDef hediffDef = Props.parasiteHediff;
            if (hediffDef == null || Props.parasiteHoursToLethal <= 0f)
                return;

            // 撒く量が bodySize 比例なので、それが広がる面積とみなして半径は sqrt を取る
            float radius = Props.parasiteRadius * Mathf.Sqrt(bodySize);
            float gainPerCheck = Props.intervalTicks / (Props.parasiteHoursToLethal * GenDate.TicksPerHour);

            // severity が致死に達した pawn はその場で死んで AllPawnsSpawned から外れるため、
            // 列挙中の変更を避けてスナップショットを取る
            tmpPawns.Clear();
            tmpPawns.AddRange(map.mapPawns.AllPawnsSpawned);

            for (int i = 0; i < tmpPawns.Count; i++)
            {
                Pawn pawn = tmpPawns[i];
                if (pawn == parent || pawn.Dead || !pawn.Spawned || !pawn.RaceProps.IsFlesh)
                    continue;
                if (!pawn.Position.InHorDistOf(center, radius))
                    continue;

                float gain = gainPerCheck * ExposureFactor(pawn);
                if (gain <= 0f)
                    continue;

                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                if (hediff == null)
                {
                    hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                    hediff.Severity = gain;
                    pawn.health.AddHediff(hediff);
                }
                else
                {
                    hediff.Severity += gain;
                }

                hediff.TryGetComp<VoidAwake_HediffComp_SporeParasite>()?.NotifyExposed();
            }

            tmpPawns.Clear();
        }

        /// <summary>
        /// ガスマスク等の immuneToToxGasExposure 装備・遺伝子なら完全無効、
        /// それ以外は毒環境耐性の分だけ進行が遅くなる（布マスク 0.5 なら半減）。
        /// </summary>
        private static float ExposureFactor(Pawn pawn)
        {
            if (pawn.apparel != null)
            {
                List<Apparel> worn = pawn.apparel.WornApparel;
                for (int i = 0; i < worn.Count; i++)
                {
                    ApparelProperties apparelProps = worn[i].def.apparel;
                    if (apparelProps != null && apparelProps.immuneToToxGasExposure)
                        return 0f;
                }
            }

            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                List<Gene> genes = pawn.genes.GenesListForReading;
                for (int i = 0; i < genes.Count; i++)
                {
                    if (genes[i].Active && genes[i].def.immuneToToxGasExposure)
                        return 0f;
                }
            }

            return Mathf.Clamp01(1f - pawn.GetStatValue(StatDefOf.ToxicEnvironmentResistance));
        }

        private void CleanupEffecter()
        {
            if (effecter == null)
                return;
            effecter.Cleanup();
            effecter = null;
        }

        public override void PostDeSpawn(Map map, DestroyMode mode)
        {
            CleanupEffecter();
            base.PostDeSpawn(map, mode);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            CleanupEffecter();
            base.PostDestroy(mode, previousMap);
        }
    }
}
