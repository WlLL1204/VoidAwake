using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VoidAwake
{
    /// <summary>
    /// 繋がりの段階。abilities / hediff が空でも成立し、その場合は解放表示だけを行う。
    /// </summary>
    public class VoidMagicTier
    {
        [MustTranslate]
        public string label;

        public float threshold = 25f;

        public List<AbilityDef> abilities;

        public HediffDef hediff;

        public string LabelCap =>
            label.NullOrEmpty() ? threshold.ToString("F0") : label.CapitalizeFirst();

        public bool HasContent => (abilities != null && abilities.Count > 0) || hediff != null;
    }

    public class VoidAwake_VoidMagicDef : Def
    {
        /// <summary>
        /// null の場合は収容可能なアノマリー全体に適用される既定テンプレートになる。
        /// </summary>
        public ThingDef entityDef;

        public float maxConnection = 100f;

        /// <summary>瞑想 1 時間（2500 tick）あたりの獲得量。</summary>
        public float connectionPerHourMeditating = 5f;

        /// <summary>対象を収容していない間の 1 日あたり減衰量。</summary>
        public float decayPerDayLost = 6f;

        /// <summary>収容中でも瞑想を放置した場合の 1 日あたり減衰量。</summary>
        public float decayPerDayIdle = 1f;

        /// <summary>放置減衰が始まるまでの猶予日数。</summary>
        public float idleGraceDays = 3f;

        public List<VoidMagicTier> tiers = new List<VoidMagicTier>();

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            tiers?.SortBy(t => t.threshold);
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (maxConnection <= 0f)
            {
                yield return "maxConnection must be greater than 0.";
            }

            if (tiers.NullOrEmpty())
            {
                yield return "no tiers defined.";
            }
        }

        public int TierCount => tiers?.Count ?? 0;

        /// <summary>繋がり値に対応する最上位の段階。未達なら -1。</summary>
        public int TierIndexFor(float connection)
        {
            int result = -1;
            if (tiers == null)
            {
                return result;
            }

            for (int i = 0; i < tiers.Count; i++)
            {
                if (connection < tiers[i].threshold)
                {
                    break;
                }
                result = i;
            }
            return result;
        }

        public VoidMagicTier TierAt(int index)
        {
            if (tiers == null || index < 0 || index >= tiers.Count)
            {
                return null;
            }
            return tiers[index];
        }
    }
}
