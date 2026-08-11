using HarmonyLib;
using RimWorld;

namespace VoidAwake
{
	[HarmonyPatch(typeof(Building_Door), "DoorOpen")]
	public static class Patch_Building_Door_DoorOpen
	{
		private static void Postfix(Building_Door __instance)
		{
			VoidAwake_DoorTrapComboUtility.TryTriggerOnDoorOpen(__instance);
		}
	}
}
