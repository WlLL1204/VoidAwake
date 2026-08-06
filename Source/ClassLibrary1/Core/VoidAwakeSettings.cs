using RimWorld.Planet;
using Verse;

namespace VoidAwake
{
    public class VoidAwakeSettings : ModSettings
    {
        public bool showGravshipTrail = true; // デフォルトON

        public override void ExposeData()
        {
            Scribe_Values.Look(ref showGravshipTrail, "showGravshipTrail", true);
        }
    }

    public class VoidAwakeMod : Mod
    {
        public static VoidAwakeSettings Settings;

        public VoidAwakeMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<VoidAwakeSettings>();
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
                    Find.World.renderer.SetDirty<VoidAwake_WorldProtect>(layer);
            }
        }
    }
}