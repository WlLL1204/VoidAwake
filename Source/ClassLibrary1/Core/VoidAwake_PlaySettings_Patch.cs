using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidAwake
{
    [StaticConstructorOnStartup]
    public static class VoidAwake_HarmonyInit
    {
        static VoidAwake_HarmonyInit()
        {
            new Harmony("Will.VoidAwake").PatchAll();
        }
    }

    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    public static class VoidErosion_PlaySettings_Patch
    {
        private static readonly Texture2D Icon =
            ContentFinder<Texture2D>.Get("UI/Buttons/ShowLearningHelper", true);
        // 後で専用アイコンに差し替え可（例: UI/Buttons/VoidErosionRate）

        public static void Postfix(WidgetRow row, bool worldView)
        {
            if (VoidAwakeMod.Settings == null) return;

            bool before = VoidAwakeMod.Settings.showErosionRateUI;
            row.ToggleableIcon(
                ref VoidAwakeMod.Settings.showErosionRateUI,
                Icon,
                "世界浸食率の表示を切り替えます。",
                SoundDefOf.Mouseover_ButtonToggle);

            if (before != VoidAwakeMod.Settings.showErosionRateUI)
                LoadedModManager.GetMod<VoidAwakeMod>().WriteSettings();
        }
    }
}