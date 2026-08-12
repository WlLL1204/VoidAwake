using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VoidAwake
{
    /// <summary>
    /// アノマリー 1 体分の脱走設定。脱走時に発火させるイベント Def をまとめて持つ。
    /// </summary>
    public class VoidAwake_ContainmentEscapeDef : Def
    {
        /// <summary>
        /// 対象エンティティの ThingDef。null の場合は他に一致する定義が無いときのフォールバックになる。
        /// </summary>
        public ThingDef entityDef;

        /// <summary>
        /// シャンブラー・グール・目覚めた死体は Human などの通常 ThingDef を持つため、
        /// これらを区別するには MutantDef で指定する。
        /// </summary>
        public MutantDef mutantDef;

        public List<VoidAwake_EscapeEventDef> events = new List<VoidAwake_EscapeEventDef>();

        public bool IsFallback => entityDef == null && mutantDef == null;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (events == null)
            {
                yield break;
            }

            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] == null)
                {
                    yield return "events contains a null entry at index " + i + ".";
                }
            }
        }
    }
}
