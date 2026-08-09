using RimWorld;
using Verse;

namespace VoidAwake
{
    public class VoidAwake_ExtractionSample : Designator
    {
        public VoidAwake_ExtractionSample()
        {
            defaultLabel = "サンプル抽出";
            defaultDesc = "ダウンしたアノマリーからサンプルを抽出する。対象は死亡する。抽出キットまたは抽出ベルトが必要。";
            icon = TexCommand.AttackMelee; // 仮。テクスチャ用意後に差し替え
            soundSucceeded = SoundDefOf.Designate_Hunt;
            useMouseIcon = true;
        }


        protected override DesignationDef Designation =>
        VoidAwake_DesignationDefOf.VoidAwake_ExtractSample;
        public override DrawStyleCategoryDef DrawStyleCategory =>
            DrawStyleCategoryDefOf.Orders;

        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            var p = t as Pawn;
            if (!VoidAwake_ExtractionUtility.IsExtractableAnomaly(p))
                return false;
            if (VoidAwake_ExtractionUtility.GetSampleDef(p) == null)
                return "抽出可能なサンプル階層がありません。";
            if (Map.designationManager.DesignationOn(t, Designation) != null)
                return false;
            return true;
        }

        public override void DesignateThing(Thing t)
        {
            Map.designationManager.AddDesignation(new Designation(t, Designation));
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(Map) || c.Fogged(Map))
                return false;
            foreach (var t in Map.thingGrid.ThingsAt(c))
                if (CanDesignateThing(t).Accepted)
                    return true;
            return "抽出可能な対象がありません。";
        }
        public override void DesignateSingleCell(IntVec3 c)
        {
            foreach (var t in Map.thingGrid.ThingsAt(c))
                if (CanDesignateThing(t).Accepted)
                    DesignateThing(t);
        }

    }
}