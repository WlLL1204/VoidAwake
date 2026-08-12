using RimWorld.Planet;
using Verse;

namespace VoidAwake
{
    public class VoidAwake_Mod : Mod
    {
        public static VoidAwake_Settings Settings;

        public VoidAwake_Mod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<VoidAwake_Settings>();
        }

        public override string SettingsCategory() => "VoidAwake";

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);

            list.CheckboxLabeled(
                "グラブシップの足跡を表示",
                ref Settings.showGravshipTrail,
                "ワールドマップ上に飛行軌跡を白く描画します。");

            list.End();

            // チェック変更直後に見た目を更新
            if (Find.World?.renderer != null)
            {
                PlanetLayer layer = Find.WorldGrid?.Surface;
                if (layer != null)
                    Find.World.renderer.SetDirty<VoidAwake_WorldDrawLayer_WorldProtect>(layer);
            }
        }
    }
}
