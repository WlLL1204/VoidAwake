using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VoidAwake
{
    /// <summary>
    /// 脱走時に起こる 1 イベント。複数のアノマリーから使い回せる。
    /// イベント固有のパラメータが必要になったらこの Def を継承し、XML の Class= で指定する。
    /// </summary>
    public class VoidAwake_EscapeEventDef : Def
    {
        public Type workerClass = typeof(VoidAwake_EscapeEventWorker);

        /// <summary>発火確率。</summary>
        public float chance = 1f;

        /// <summary>true なら連鎖脱走に巻き込まれた側では発火しない。</summary>
        public bool onlyWhenInitiator;

        [MustTranslate]
        public string letterLabel;

        [MustTranslate]
        public string letterText;

        public LetterDef letterDef;

        /// <summary>true ならレターは initiator の脱走時だけ送る（バニラと同じ）。</summary>
        public bool letterOnlyWhenInitiator = true;

        public bool HasLetter => !letterLabel.NullOrEmpty() && !letterText.NullOrEmpty();

        private VoidAwake_EscapeEventWorker workerInt;

        public VoidAwake_EscapeEventWorker Worker
        {
            get
            {
                if (workerInt == null)
                {
                    workerInt = (VoidAwake_EscapeEventWorker)Activator.CreateInstance(
                        workerClass ?? typeof(VoidAwake_EscapeEventWorker));
                    workerInt.def = this;
                }
                return workerInt;
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (workerClass == null)
            {
                yield return "workerClass is null.";
            }
            else if (!typeof(VoidAwake_EscapeEventWorker).IsAssignableFrom(workerClass))
            {
                yield return "workerClass " + workerClass.Name + " does not derive from "
                    + nameof(VoidAwake_EscapeEventWorker) + ".";
            }

            if (chance <= 0f)
            {
                yield return "chance must be greater than 0.";
            }
        }
    }
}
