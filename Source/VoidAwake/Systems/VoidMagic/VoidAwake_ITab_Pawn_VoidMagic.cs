using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidAwake
{
    public class VoidAwake_ITab_Pawn_VoidMagic : ITab
    {
        private const float RowHeight = 64f;
        private const float IconSize = 40f;
        private const float BarHeight = 20f;
        private const float StatusWidth = 150f;
        private const float DebugButtonSize = 22f;

        // テクスチャ生成はメインスレッド限定。このタブは Def 解決中にロードスレッドから
        // インスタンス化されるため、描画時まで生成を遅延させる。
        private static Texture2D barFillTex;
        private static Texture2D barBackgroundTex;
        private static Texture2D thresholdTex;
        private static Texture2D thresholdReachedTex;

        private static Texture2D BarFillTex =>
            barFillTex ?? (barFillTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.42f, 0.19f, 0.55f)));

        private static Texture2D BarBackgroundTex =>
            barBackgroundTex ?? (barBackgroundTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.12f, 0.12f, 0.14f)));

        private static Texture2D ThresholdTex =>
            thresholdTex ?? (thresholdTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.75f, 0.75f, 0.78f, 0.7f)));

        private static Texture2D ThresholdReachedTex =>
            thresholdReachedTex ?? (thresholdReachedTex = SolidColorMaterials.NewSolidColorTexture(new Color(1f, 0.85f, 0.35f, 0.9f)));

        private static readonly List<ThingDef> tmpRows = new List<ThingDef>();

        private Vector2 scrollPosition;

        public VoidAwake_ITab_Pawn_VoidMagic()
        {
            size = new Vector2(540f, 420f);
            labelKey = "VoidAwake_VoidMagicTab";
        }

        private Pawn PawnToShow => SelThing as Pawn;

        public override bool IsVisible
        {
            get
            {
                Pawn pawn = PawnToShow;
                return VoidAwake_VoidMagicUtility.Active
                    && pawn != null
                    && pawn.IsColonistPlayerControlled
                    && VoidAwake_VoidMagicUtility.GetComp(pawn) != null;
            }
        }

        protected override void FillTab()
        {
            Pawn pawn = PawnToShow;
            VoidAwake_CompVoidMagic comp = VoidAwake_VoidMagicUtility.GetComp(pawn);
            if (comp == null)
            {
                return;
            }

            Rect outer = new Rect(0f, 20f, size.x, size.y - 20f).ContractedBy(12f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            BuildRows(comp);

            if (tmpRows.Count == 0)
            {
                GUI.color = new Color(0.75f, 0.75f, 0.75f);
                Widgets.Label(outer, "VoidAwake_VoidMagicNoLinks".Translate());
                GUI.color = Color.white;
                return;
            }

            Rect headerRect = new Rect(outer.x, outer.y, outer.width, 24f);
            Widgets.Label(headerRect, "VoidAwake_VoidMagicHeader".Translate(comp.Links.Count));
            Widgets.DrawLineHorizontal(outer.x, headerRect.yMax + 2f, outer.width);

            Rect listRect = new Rect(
                outer.x,
                headerRect.yMax + 8f,
                outer.width,
                outer.height - headerRect.height - 8f);
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, tmpRows.Count * RowHeight);

            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);
            float y = 0f;
            for (int i = 0; i < tmpRows.Count; i++)
            {
                DrawRow(new Rect(0f, y, viewRect.width, RowHeight), comp, tmpRows[i], i);
                y += RowHeight;
            }
            Widgets.EndScrollView();

            Text.Anchor = TextAnchor.UpperLeft;
        }

        /// <summary>繋がりを持つアノマリーと、現在収容中のアノマリーを合わせて一覧にする。</summary>
        private static void BuildRows(VoidAwake_CompVoidMagic comp)
        {
            tmpRows.Clear();

            List<VoidAwake_VoidLink> links = comp.Links;
            for (int i = 0; i < links.Count; i++)
            {
                if (!tmpRows.Contains(links[i].entityDef))
                {
                    tmpRows.Add(links[i].entityDef);
                }
            }

            foreach (ThingDef entityDef in VoidAwake_VoidMagicUtility.ContainedEntityDefsNow())
            {
                if (!tmpRows.Contains(entityDef))
                {
                    tmpRows.Add(entityDef);
                }
            }

            tmpRows.SortByDescending(d => comp.ConnectionOn(d));
        }

        private void DrawRow(Rect rect, VoidAwake_CompVoidMagic comp, ThingDef entityDef, int index)
        {
            VoidAwake_VoidMagicDef magicDef = VoidAwake_VoidMagicUtility.DefFor(entityDef);
            if (magicDef == null)
            {
                return;
            }

            if (index % 2 == 1)
            {
                Widgets.DrawLightHighlight(rect);
            }
            Widgets.DrawHighlightIfMouseover(rect);

            VoidAwake_VoidLink link = comp.GetLink(entityDef);
            float connection = link?.connection ?? 0f;
            int tierIndex = magicDef.TierIndexFor(connection);

            Rect inner = rect.ContractedBy(4f);
            Rect iconRect = new Rect(inner.x, inner.y + (inner.height - IconSize) / 2f, IconSize, IconSize);
            Widgets.ThingIcon(iconRect, entityDef);

            float textX = iconRect.xMax + 8f;
            float debugButtonsWidth = Prefs.DevMode ? (DebugButtonSize * 2f + 6f) : 0f;
            float textWidth = inner.xMax - StatusWidth - 8f - textX - debugButtonsWidth;

            Rect labelRect = new Rect(textX, inner.y, textWidth, 22f);
            Widgets.Label(labelRect, VoidAwake_VoidMagicUtility.EntityLabel(entityDef));

            Rect barRect = new Rect(textX, labelRect.yMax + 2f, textWidth, BarHeight);
            DrawConnectionBar(barRect, magicDef, connection, tierIndex);

            Rect statusRect = new Rect(
                inner.xMax - StatusWidth - debugButtonsWidth,
                inner.y,
                StatusWidth,
                inner.height);
            DrawStatus(statusRect, comp, link, magicDef, tierIndex);

            if (Prefs.DevMode)
            {
                DrawDebugTierButtons(inner, comp, entityDef, magicDef, tierIndex);
            }

            TooltipHandler.TipRegion(rect, () => BuildTooltip(magicDef, connection, tierIndex), entityDef.shortHash + 91337);
        }

        private static void DrawConnectionBar(Rect rect, VoidAwake_VoidMagicDef magicDef,
            float connection, int tierIndex)
        {
            float fillPercent = Mathf.Clamp01(connection / magicDef.maxConnection);
            Widgets.FillableBar(rect, fillPercent, BarFillTex, BarBackgroundTex, false);

            for (int i = 0; i < magicDef.TierCount; i++)
            {
                VoidAwake_VoidMagicTier tier = magicDef.TierAt(i);
                float pct = Mathf.Clamp01(tier.threshold / magicDef.maxConnection);
                float x = rect.x + (rect.width * pct) - 1f;
                x = Mathf.Min(x, rect.xMax - 2f);
                GUI.DrawTexture(
                    new Rect(x, rect.y, 2f, rect.height),
                    i <= tierIndex ? ThresholdReachedTex : ThresholdTex);
            }

            Widgets.DrawBox(rect);

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect, connection.ToString("F1") + " / " + magicDef.maxConnection.ToString("F0"));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DrawStatus(Rect rect, VoidAwake_CompVoidMagic comp, VoidAwake_VoidLink link,
            VoidAwake_VoidMagicDef magicDef, int tierIndex)
        {
            VoidAwake_VoidMagicTier tier = magicDef.TierAt(tierIndex);
            string tierLabel = tier != null
                ? tier.LabelCap
                : "VoidAwake_VoidMagicTierNone".Translate().ToString();

            Rect tierRect = new Rect(rect.x, rect.y, rect.width, 22f);
            Text.Anchor = TextAnchor.UpperRight;
            GUI.color = tier != null ? new Color(1f, 0.85f, 0.35f) : new Color(0.7f, 0.7f, 0.7f);
            Widgets.Label(tierRect, tierLabel);
            GUI.color = Color.white;

            float decayPerDay = link != null ? comp.DecayPerDayFor(link) : 0f;
            string status;
            Color color;
            if (link == null || link.connection <= 0f)
            {
                status = "VoidAwake_VoidMagicStatusNew".Translate();
                color = new Color(0.7f, 0.7f, 0.7f);
            }
            else if (decayPerDay > 0f)
            {
                status = comp.IsLost(link)
                    ? "VoidAwake_VoidMagicStatusLost".Translate(decayPerDay.ToString("F1"))
                    : "VoidAwake_VoidMagicStatusIdle".Translate(decayPerDay.ToString("F1"));
                color = new Color(0.9f, 0.45f, 0.45f);
            }
            else
            {
                status = "VoidAwake_VoidMagicStatusStable".Translate();
                color = new Color(0.6f, 0.85f, 0.6f);
            }

            Rect statusRect = new Rect(rect.x, tierRect.yMax + 2f, rect.width, 22f);
            Text.Font = GameFont.Tiny;
            GUI.color = color;
            Widgets.Label(statusRect, status);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DrawDebugTierButtons(Rect inner, VoidAwake_CompVoidMagic comp,
            ThingDef entityDef, VoidAwake_VoidMagicDef magicDef, int tierIndex)
        {
            float y = inner.y + (inner.height - DebugButtonSize) / 2f;
            Rect plusRect = new Rect(inner.xMax - DebugButtonSize, y, DebugButtonSize, DebugButtonSize);
            Rect minusRect = new Rect(plusRect.x - DebugButtonSize - 2f, y, DebugButtonSize, DebugButtonSize);

            bool canLower = tierIndex >= 0;
            bool canRaise = tierIndex < magicDef.TierCount - 1;

            GUI.color = canLower ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            if (Widgets.ButtonText(minusRect, "-") && canLower)
            {
                comp.StepTier(entityDef, -1);
            }

            GUI.color = canRaise ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            if (Widgets.ButtonText(plusRect, "+") && canRaise)
            {
                comp.StepTier(entityDef, 1);
            }

            GUI.color = Color.white;
            TooltipHandler.TipRegion(minusRect, "VoidAwake_VoidMagicDebugLower".Translate());
            TooltipHandler.TipRegion(plusRect, "VoidAwake_VoidMagicDebugRaise".Translate());
        }

        private static string BuildTooltip(VoidAwake_VoidMagicDef magicDef, float connection, int tierIndex)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < magicDef.TierCount; i++)
            {
                VoidAwake_VoidMagicTier tier = magicDef.TierAt(i);
                string mark = i <= tierIndex ? "+" : "-";
                sb.Append(mark).Append(" ").Append(tier.LabelCap)
                    .Append(" (").Append(tier.threshold.ToString("F0")).Append(")");
                if (!tier.HasContent)
                {
                    sb.Append(" - ").Append("VoidAwake_VoidMagicAbilityPending".Translate());
                }
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("VoidAwake_VoidMagicTooltipGain".Translate(
                magicDef.connectionPerHourMeditating.ToString("F1")));
            sb.AppendLine("VoidAwake_VoidMagicTooltipDecay".Translate(
                magicDef.decayPerDayLost.ToString("F1"),
                magicDef.idleGraceDays.ToString("F0"),
                magicDef.decayPerDayIdle.ToString("F1")));
            return sb.ToString().TrimEndNewlines();
        }
    }
}
