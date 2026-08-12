using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidAwake
{
	public static class VoidAwake_BearTrapDisarmGizmos
	{
		private static Texture2D cachedIcon;

		private static Texture2D Icon
		{
			get
			{
				if (cachedIcon == null)
				{
					cachedIcon = ContentFinder<Texture2D>.Get("UI/Commands/ReleasePrisoner", false);
					if (cachedIcon == null)
					{
						cachedIcon = ContentFinder<Texture2D>.Get("UI/Designators/Deconstruct", true);
					}
				}

				return cachedIcon;
			}
		}

		public static IEnumerable<Gizmo> GetGizmosFor(Pawn pawn)
		{
			if (pawn == null || !VoidAwake_BearTrapCaughtUtility.HasCaught(pawn))
			{
				yield break;
			}

			if (!pawn.IsColonistPlayerControlled)
			{
				yield break;
			}

			AcceptanceReport can = VoidAwake_BearTrapCaughtUtility.CanDisarm(pawn, pawn);
			yield return new Command_Action
			{
				defaultLabel = "罠の解除",
				defaultDesc = "足にかかった熊罠を自力で外す。一人では時間がかかる。",
				icon = Icon,
				action = () => VoidAwake_BearTrapCaughtUtility.StartDisarmJob(pawn, pawn),
				Disabled = !can.Accepted,
				disabledReason = can.Accepted ? null : can.Reason
			};
		}
	}

	[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
	public static class Patch_Pawn_GetGizmos_BearTrapDisarm
	{
		public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
		{
			foreach (Gizmo gizmo in __result)
			{
				yield return gizmo;
			}

			foreach (Gizmo gizmo in VoidAwake_BearTrapDisarmGizmos.GetGizmosFor(__instance))
			{
				yield return gizmo;
			}
		}
	}

	[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetFloatMenuOptions))]
	public static class Patch_Pawn_GetFloatMenuOptions_BearTrapDisarm
	{
		public static IEnumerable<FloatMenuOption> Postfix(
			IEnumerable<FloatMenuOption> __result,
			Pawn __instance,
			Pawn selPawn)
		{
			foreach (FloatMenuOption option in __result)
			{
				yield return option;
			}

			if (selPawn == null || __instance == null || selPawn == __instance)
			{
				yield break;
			}

			if (!VoidAwake_BearTrapCaughtUtility.HasCaught(__instance))
			{
				yield break;
			}

			AcceptanceReport can = VoidAwake_BearTrapCaughtUtility.CanDisarm(selPawn, __instance);
			string label = "罠を解除（" + __instance.LabelShort + "）";

			if (!can.Accepted)
			{
				yield return new FloatMenuOption(label + "（" + can.Reason + "）", null);
				yield break;
			}

			yield return new FloatMenuOption(label, () =>
				VoidAwake_BearTrapCaughtUtility.StartDisarmJob(selPawn, __instance));
		}
	}
}
