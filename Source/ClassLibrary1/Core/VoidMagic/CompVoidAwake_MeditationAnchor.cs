using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace VoidAwake
{
    public class CompProperties_VoidAwake_MeditationAnchor : CompProperties
    {
        /// <summary>収容されたアノマリーを探す半径。</summary>
        public float radius = 9.9f;

        public float gainMultiplier = 1f;

        public CompProperties_VoidAwake_MeditationAnchor()
        {
            compClass = typeof(CompVoidAwake_MeditationAnchor);
        }
    }

    /// <summary>
    /// 瞑想スポットに付く。半径内の収容アノマリーを探し、捻じれた瞑想の指示を出せるようにする。
    /// 自前のスポットにも Royalty の瞑想スポットにも同じ comp を付けて使う。
    /// </summary>
    public class CompVoidAwake_MeditationAnchor : ThingComp
    {
        private static readonly List<Pawn> tmpEntities = new List<Pawn>();

        public CompProperties_VoidAwake_MeditationAnchor Props =>
            (CompProperties_VoidAwake_MeditationAnchor)props;

        public float Radius => Props.radius;

        public void GetEntitiesInRange(List<Pawn> outEntities)
        {
            VoidMagicUtility.ContainedEntitiesNear(parent.Map, parent.Position, Props.radius, outEntities);
        }

        public bool AnyEntityInRange()
        {
            GetEntitiesInRange(tmpEntities);
            bool any = tmpEntities.Count > 0;
            tmpEntities.Clear();
            return any;
        }

        public AcceptanceReport CanMeditateHere(Pawn pawn)
        {
            if (!VoidMagicUtility.Active)
            {
                return "VoidAwake_VoidMeditateRequiresAnomaly".Translate();
            }
            if (!VoidMagicUtility.CanMeditateForVoidMagic(pawn))
            {
                return "VoidAwake_VoidMeditateCannotMeditate".Translate();
            }
            if (!AnyEntityInRange())
            {
                return "VoidAwake_VoidMeditateNoAnomalyNear".Translate();
            }
            if (!pawn.CanReach(parent, PathEndMode.OnCell, Danger.Deadly))
            {
                return "NoPath".Translate();
            }
            if (!pawn.CanReserve(parent))
            {
                return "Reserved".Translate();
            }
            return AcceptanceReport.WasAccepted;
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (!VoidMagicUtility.Active || parent.Map == null)
            {
                yield break;
            }
            if (selPawn == null || !selPawn.IsColonistPlayerControlled)
            {
                yield break;
            }
            if (VoidMagicUtility.GetComp(selPawn) == null)
            {
                yield break;
            }

            string label = "VoidAwake_VoidMeditateFloatMenu".Translate();
            AcceptanceReport report = CanMeditateHere(selPawn);
            if (!report.Accepted)
            {
                yield return new FloatMenuOption(label + ": " + report.Reason, null);
                yield break;
            }

            yield return new FloatMenuOption(label, () => StartMeditation(selPawn));
        }

        private void StartMeditation(Pawn pawn)
        {
            Job job = JobMaker.MakeJob(VoidAwake_VoidMagicDefOf.VoidAwake_VoidMeditate, parent);
            job.playerForced = true;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        public override string CompInspectStringExtra()
        {
            if (!VoidMagicUtility.Active || parent.Map == null)
            {
                return null;
            }

            GetEntitiesInRange(tmpEntities);
            string result = tmpEntities.Count == 0
                ? "VoidAwake_VoidMeditationSpotNoAnomaly".Translate().ToString()
                : "VoidAwake_VoidMeditationSpotAnomalies".Translate(
                    tmpEntities.Select(e => e.LabelShortCap.ToString()).ToCommaList(true)).ToString();
            tmpEntities.Clear();
            return result;
        }
    }
}
