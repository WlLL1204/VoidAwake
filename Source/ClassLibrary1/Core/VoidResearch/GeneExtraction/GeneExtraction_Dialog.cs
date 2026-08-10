using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidAwake
{
    public class Dialog_VoidAwakeGeneExtraction : Window
    {
        private readonly Pawn target;
        private readonly List<GeneDef> genes;
        private Vector2 scrollPos;
        private const float RowHeight = 36f;

        public override Vector2 InitialSize => new Vector2(420f, 480f);

        public Dialog_VoidAwakeGeneExtraction(Pawn target, List<GeneDef> genes)
        {
            this.target = target;
            this.genes = genes ?? new List<GeneDef>();
            forcePause = true;
            doCloseX = true;
            doCloseButton = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f),
                "遺伝子を抽出: " + target.LabelShortCap);
            Text.Font = GameFont.Small;

            Rect listRect = new Rect(0f, 40f, inRect.width,
                inRect.height - 40f - CloseButSize.y - 10f);
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, genes.Count * RowHeight);

            GeneDef selected = null;

            Widgets.BeginScrollView(listRect, ref scrollPos, viewRect);
            for (int i = 0; i < genes.Count; i++)
            {
                GeneDef gene = genes[i];
                Rect row = new Rect(0f, i * RowHeight, viewRect.width, RowHeight - 2f);

                if (Widgets.ButtonInvisible(row))
                    selected = gene;

                if (Mouse.IsOver(row))
                    Widgets.DrawHighlight(row);

                Rect iconRect = new Rect(row.x + 4f, row.y + 2f, 28f, 28f);
                if (gene.Icon != null)
                {
                    GUI.color = gene.IconColor;
                    Widgets.DrawTextureFitted(iconRect, gene.Icon, 1f);
                    GUI.color = Color.white;
                }

                Widgets.Label(
                    new Rect(iconRect.xMax + 8f, row.y, row.width - iconRect.width - 16f, row.height),
                    gene.LabelCap);
            }
            Widgets.EndScrollView();

            if (selected != null)
            {
                VoidAwake_GeneExtractionUtility.DesignateGeneExtraction(target, selected);
                Close();
            }
        }
    }
}
