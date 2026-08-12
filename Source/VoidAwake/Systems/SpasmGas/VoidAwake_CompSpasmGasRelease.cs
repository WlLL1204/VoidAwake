using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidAwake
{
    public class VoidAwake_CompProperties_SpasmGasRelease : CompProperties
    {
        public float radius = 4.9f;
        public float durationSeconds = 10f;
        public HediffDef hediff;
        public bool autoStart;
        public EffecterDef effecterReleasing;
        public float severityAtCenter = 3f;
        public float severityAtEdge = 1f;
        /// <summary>中心曝露時の1秒あたりseverity上昇量。3到達まで約 severityAtCenter / この値 秒。</summary>
        public float severityGainPerSecond = 0.75f;
        /// <summary>初回付与時に即時乗せる最低severity（軽度をすぐ出す）。</summary>
        public float initialSeverityBurst = 0.5f;
        public int fleckIntervalTicks = 10;

        public VoidAwake_CompProperties_SpasmGasRelease()
        {
            compClass = typeof(VoidAwake_CompSpasmGasRelease);
        }
    }

    public class VoidAwake_CompSpasmGasRelease : ThingComp
    {
        private bool releasing;
        private int ticksRemaining;
        private Effecter effecter;
        private int fleckTick;

        public VoidAwake_CompProperties_SpasmGasRelease Props => (VoidAwake_CompProperties_SpasmGasRelease)props;

        private IntVec3 SourceCell
        {
            get
            {
                if (parent is Apparel apparel && apparel.Wearer != null)
                    return apparel.Wearer.Position;
                return parent.Position;
            }
        }

        private Map SourceMap
        {
            get
            {
                if (parent is Apparel apparel && apparel.Wearer != null)
                    return apparel.Wearer.Map;
                return parent.Map;
            }
        }

        public bool IsReleasing => releasing;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad && Props.autoStart)
                StartRelease();
        }

        public void StartRelease()
        {
            if (releasing)
                return;
            releasing = true;
            ticksRemaining = Props.durationSeconds.SecondsToTicks();
            fleckTick = 0;
            if (Props.effecterReleasing != null && SourceMap != null)
            {
                effecter = Props.effecterReleasing.Spawn();
                effecter.Trigger(new TargetInfo(SourceCell, SourceMap), TargetInfo.Invalid);
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!releasing)
                return;

            Map map = SourceMap;
            if (map == null)
            {
                StopRelease();
                return;
            }

            IntVec3 center = SourceCell;
            effecter?.EffectTick(new TargetInfo(center, map), TargetInfo.Invalid);

            ApplyToNearbyPawns(map, center);
            MaybeSpawnFlecks(map, center);

            ticksRemaining--;
            if (ticksRemaining <= 0)
                StopRelease();
        }

        private void ApplyToNearbyPawns(Map map, IntVec3 center)
        {
            float radius = Props.radius;
            float radiusSq = radius * radius;
            HediffDef hediffDef = Props.hediff ?? VoidAwake_HediffDefOf.VoidAwake_SpasmGasExposure;
            if (hediffDef == null)
                return;

            float gainPerTick = Props.severityGainPerSecond / GenTicks.TicksPerRealSecond;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn.Dead || !pawn.RaceProps.IsFlesh)
                    continue;
                if (IsImmuneToSpasmGas(pawn))
                    continue;
                if (!pawn.Position.InHorDistOf(center, radius))
                    continue;

                float distSq = pawn.Position.DistanceToSquared(center);
                if (distSq > radiusSq)
                    continue;

                float t = radius <= 0.01f ? 0f : Mathf.Clamp01(Mathf.Sqrt(distSq) / radius);
                float severityCap = Mathf.Lerp(Props.severityAtCenter, Props.severityAtEdge, t);
                severityCap = Mathf.Clamp(severityCap, 0.01f, hediffDef.maxSeverity);

                // 中心ほど速く進行（端はキャップも上昇量も低い）
                float gainScale = Mathf.Lerp(1f, 0.35f, t);
                float gain = gainPerTick * gainScale;

                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                if (hediff == null)
                {
                    hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                    float burst = Mathf.Min(Props.initialSeverityBurst, severityCap);
                    hediff.Severity = Mathf.Max(burst, Mathf.Min(gain, severityCap));
                    pawn.health.AddHediff(hediff);
                }
                else if (hediff.Severity < severityCap)
                {
                    hediff.Severity = Mathf.Min(hediff.Severity + gain, severityCap);
                }

                hediff.TryGetComp<VoidAwake_HediffComp_SpasmGasExposure>()?.NotifyExposed();
            }
        }

        /// <summary>
        /// ガスマスク等の immuneToToxGasExposure アパレル／遺伝子で痙縮ガスを無効化。
        /// </summary>
        private static bool IsImmuneToSpasmGas(Pawn pawn)
        {
            if (pawn.apparel != null)
            {
                List<Apparel> worn = pawn.apparel.WornApparel;
                for (int i = 0; i < worn.Count; i++)
                {
                    ApparelProperties apparelProps = worn[i].def.apparel;
                    if (apparelProps != null && apparelProps.immuneToToxGasExposure)
                        return true;
                }
            }

            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                var genesListForReading = pawn.genes.GenesListForReading;
                for (int i = 0; i < genesListForReading.Count; i++)
                {
                    Gene gene = genesListForReading[i];
                    if (gene.Active && gene.def.immuneToToxGasExposure)
                        return true;
                }
            }

            return false;
        }

        private void MaybeSpawnFlecks(Map map, IntVec3 center)
        {
            fleckTick++;
            if (fleckTick < Props.fleckIntervalTicks)
                return;
            fleckTick = 0;

            int count = Mathf.Max(1, Mathf.RoundToInt(Props.radius));
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell = center + GenRadial.RadialPattern[Rand.Range(0, GenRadial.NumCellsInRadius(Props.radius))];
                if (!cell.InBounds(map))
                    continue;
                FleckMaker.ThrowAirPuffUp(cell.ToVector3Shifted(), map);
            }
        }

        private void StopRelease()
        {
            CleanupRelease();

            // パック以外の放出源は終了時に破棄（残骸を残さない）
            if (!(parent is Apparel) && parent.Spawned && !parent.Destroyed)
                parent.Destroy(DestroyMode.Vanish);
        }

        private void CleanupRelease()
        {
            releasing = false;
            ticksRemaining = 0;
            if (effecter != null)
            {
                effecter.Cleanup();
                effecter = null;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref releasing, "releasing", false);
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", 0);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            CleanupRelease();
            base.PostDestroy(mode, previousMap);
        }
    }
}
