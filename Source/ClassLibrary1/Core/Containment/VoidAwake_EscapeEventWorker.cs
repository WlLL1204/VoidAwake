using RimWorld;
using Verse;

namespace VoidAwake
{
    /// <summary>
    /// 脱走時の状況。足場は既に EjectContents 済みなので、位置は脱走直前の値を保持する。
    /// </summary>
    public struct VoidAwake_EscapeContext
    {
        public Pawn pawn;

        public Building_HoldingPlatform platform;

        public Map map;

        public IntVec3 cell;

        /// <summary>連鎖脱走に巻き込まれた側は false。</summary>
        public bool initiator;

        public bool IsValid => pawn != null && map != null;
    }

    /// <summary>
    /// 脱走イベントの基底ワーカー。Def にレターが設定されていれば送信する。
    /// 個別の演出はこれを継承し、VoidAwake_EscapeEventDef の workerClass に指定する。
    /// </summary>
    public class VoidAwake_EscapeEventWorker
    {
        public VoidAwake_EscapeEventDef def;

        public virtual void DoEscapeEvent(VoidAwake_EscapeContext ctx)
        {
            ContainmentEscapeUtility.TrySendEscapeLetter(def, ctx);
        }
    }
}
