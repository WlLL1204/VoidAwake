using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VoidAwake
{
    /// <summary>
    /// 収容強度が必要値を満たしている限り脱走判定に参加させない。
    /// InitiateEscapeMtbDays はここが false を返すと -1 を返し、
    /// 抽選・連鎖脱走の双方が成立しなくなる（ITab は "Never" 表示になる）。
    /// </summary>
    [HarmonyPatch(typeof(ContainmentUtility), nameof(ContainmentUtility.CanParticipateInEscape))]
    public static class VoidAwake_Patch_ContainmentUtility_CanParticipateInEscape
    {
        public static void Postfix(Pawn pawn, StringBuilder sb, ref bool __result)
        {
            if (!__result || !VoidAwake_ContainmentEscapeUtility.IsEscapeProof(pawn))
            {
                return;
            }

            __result = false;
            if (sb != null)
            {
                sb.AppendLineIfNotEmpty();
                sb.Append("  - " + "VoidAwake_FactorSafelyContained".Translate() + ": x0%");
            }
        }
    }

    /// <summary>
    /// 脱走したアノマリーごとの固有イベントを発火する。
    /// Escape は冒頭で EjectContents を呼ぶため、足場の情報は Prefix で退避しておく必要がある。
    /// </summary>
    [HarmonyPatch(typeof(CompHoldingPlatformTarget), nameof(CompHoldingPlatformTarget.Escape))]
    public static class VoidAwake_Patch_CompHoldingPlatformTarget_Escape
    {
        public static void Prefix(CompHoldingPlatformTarget __instance, bool initiator,
            out VoidAwake_EscapeContext __state)
        {
            __state = default(VoidAwake_EscapeContext);

            if (!(__instance.parent is Pawn pawn))
            {
                return;
            }

            Building_HoldingPlatform platform = __instance.HeldPlatform;
            __state.pawn = pawn;
            __state.platform = platform;
            __state.map = platform?.Map ?? pawn.MapHeld;
            __state.cell = platform?.Position ?? pawn.PositionHeld;
            __state.initiator = initiator;

            // VoidAwake 側でレターを送る場合、バニラ脱走レターのみ抑止する（連鎖脱走は維持）。
            VoidAwake_EscapeLetterPatchState.SuppressVanillaLetter =
                initiator && VoidAwake_ContainmentEscapeUtility.HasEscapeLetterFor(pawn);
        }

        public static void Postfix(VoidAwake_EscapeContext __state)
        {
            VoidAwake_ContainmentEscapeUtility.Notify_Escaped(__state);
            VoidAwake_EscapeLetterPatchState.SuppressVanillaLetter = false;
        }
    }

    /// <summary>Escape 処理中だけバニラ脱走レターを抑止するためのフラグ。</summary>
    internal static class VoidAwake_EscapeLetterPatchState
    {
        [System.ThreadStatic]
        public static bool SuppressVanillaLetter;
    }

    [HarmonyPatch(typeof(LetterStack), nameof(LetterStack.ReceiveLetter), new[] { typeof(Letter), typeof(string), typeof(int), typeof(bool) })]
    public static class VoidAwake_Patch_LetterStack_ReceiveLetter_SuppressVanillaEscape
    {
        public static bool Prefix(Letter let)
        {
            if (!VoidAwake_EscapeLetterPatchState.SuppressVanillaLetter || let == null)
            {
                return true;
            }

            return let.Label != "LetterLabelEscapingFromHoldingPlatform".Translate();
        }
    }
}
