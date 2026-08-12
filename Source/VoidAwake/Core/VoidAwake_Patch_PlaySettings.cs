using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidAwake
{
    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    public static class VoidAwake_Patch_PlaySettings
    {
        private static readonly Texture2D Icon =
            ContentFinder<Texture2D>.Get("UI/Buttons/ShowLearningHelper", true);
        // 後で専用アイコンに差し替え可（例: UI/Buttons/VoidErosionRate）

        public static void Postfix(WidgetRow row, bool worldView)
        {
            if (VoidAwake_Mod.Settings == null) return;

            bool before = VoidAwake_Mod.Settings.showErosionRateUI;
            row.ToggleableIcon(
                ref VoidAwake_Mod.Settings.showErosionRateUI,
                Icon,
                "世界浸食率の表示を切り替えます。",
                SoundDefOf.Mouseover_ButtonToggle);

            if (before != VoidAwake_Mod.Settings.showErosionRateUI)
                LoadedModManager.GetMod<VoidAwake_Mod>().WriteSettings();
        }
    }
}
