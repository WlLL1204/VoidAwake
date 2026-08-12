using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VoidAwake
{
    public class VoidAwake_CompProperties_VoidMagic : CompProperties
    {
        public VoidAwake_CompProperties_VoidMagic()
        {
            compClass = typeof(VoidAwake_CompVoidMagic);
        }
    }

    /// <summary>入植者 1 人とアノマリー 1 種の繋がり。</summary>
    public class VoidAwake_VoidLink : IExposable
    {
        public ThingDef entityDef;
        public float connection;
        public int lastMeditatedTick = -99999;
        public int unlockedTierIndex = -1;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref entityDef, "entityDef");
            Scribe_Values.Look(ref connection, "connection", 0f);
            Scribe_Values.Look(ref lastMeditatedTick, "lastMeditatedTick", -99999);
            Scribe_Values.Look(ref unlockedTierIndex, "unlockedTierIndex", -1);
        }
    }

    public class VoidAwake_CompVoidMagic : ThingComp
    {
        private const int UpdateIntervalTicks = 250;

        private static readonly List<Pawn> tmpEntities = new List<Pawn>();

        private List<VoidAwake_VoidLink> links = new List<VoidAwake_VoidLink>();
        private int lastUpdateTick = -1;

        public Pawn Pawn => parent as Pawn;

        public List<VoidAwake_VoidLink> Links => links;

        public bool HasAnyLink => links.Count > 0;

        public VoidAwake_VoidLink GetLink(ThingDef entityDef)
        {
            if (entityDef == null)
            {
                return null;
            }

            for (int i = 0; i < links.Count; i++)
            {
                if (links[i].entityDef == entityDef)
                {
                    return links[i];
                }
            }
            return null;
        }

        public float ConnectionOn(ThingDef entityDef)
        {
            return GetLink(entityDef)?.connection ?? 0f;
        }

        public VoidAwake_VoidLink EnsureLink(ThingDef entityDef)
        {
            VoidAwake_VoidLink link = GetLink(entityDef);
            if (link != null)
            {
                return link;
            }

            link = new VoidAwake_VoidLink { entityDef = entityDef };
            links.Add(link);
            return link;
        }

        /// <summary>瞑想などで繋がりを伸ばす。段階の解放判定までまとめて行う。</summary>
        public void AddConnection(ThingDef entityDef, float amount)
        {
            if (entityDef == null || amount <= 0f)
            {
                return;
            }

            VoidAwake_VoidMagicDef magicDef = VoidAwake_VoidMagicUtility.DefFor(entityDef);
            if (magicDef == null)
            {
                return;
            }

            VoidAwake_VoidLink link = EnsureLink(entityDef);
            link.connection = Mathf.Min(link.connection + amount, magicDef.maxConnection);
            link.lastMeditatedTick = Find.TickManager.TicksGame;
            RefreshTier(link, magicDef);
        }

        public void SetConnection(ThingDef entityDef, float value)
        {
            VoidAwake_VoidMagicDef magicDef = VoidAwake_VoidMagicUtility.DefFor(entityDef);
            if (magicDef == null)
            {
                return;
            }

            VoidAwake_VoidLink link = EnsureLink(entityDef);
            link.connection = Mathf.Clamp(value, 0f, magicDef.maxConnection);
            RefreshTier(link, magicDef);
        }

        public void ClearAllLinks()
        {
            for (int i = links.Count - 1; i >= 0; i--)
            {
                VoidAwake_VoidLink link = links[i];
                VoidAwake_VoidMagicDef magicDef = VoidAwake_VoidMagicUtility.DefFor(link.entityDef);
                link.connection = 0f;
                if (magicDef != null)
                {
                    RefreshTier(link, magicDef);
                }
                links.RemoveAt(i);
            }
        }

        /// <summary>この繋がりが現在受けている 1 日あたりの減衰量。0 なら維持されている。</summary>
        public float DecayPerDayFor(VoidAwake_VoidLink link)
        {
            VoidAwake_VoidMagicDef magicDef = VoidAwake_VoidMagicUtility.DefFor(link?.entityDef);
            if (magicDef == null || link.connection <= 0f)
            {
                return 0f;
            }

            if (!VoidAwake_VoidMagicUtility.IsEntityDefContainedNow(link.entityDef))
            {
                return magicDef.decayPerDayLost;
            }

            if (IsIdle(link, magicDef))
            {
                return magicDef.decayPerDayIdle;
            }
            return 0f;
        }

        public bool IsLost(VoidAwake_VoidLink link)
        {
            return link?.entityDef != null && !VoidAwake_VoidMagicUtility.IsEntityDefContainedNow(link.entityDef);
        }

        private static bool IsIdle(VoidAwake_VoidLink link, VoidAwake_VoidMagicDef magicDef)
        {
            return Find.TickManager.TicksGame - link.lastMeditatedTick
                > magicDef.idleGraceDays * VoidAwake_VoidMagicUtility.TicksPerDay;
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (!VoidAwake_VoidMagicUtility.Active)
            {
                return;
            }

            Pawn pawn = Pawn;
            if (pawn == null || pawn.Dead)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            if (lastUpdateTick < 0 || lastUpdateTick > now)
            {
                lastUpdateTick = now;
                return;
            }

            int elapsed = now - lastUpdateTick;
            if (elapsed < UpdateIntervalTicks)
            {
                return;
            }

            lastUpdateTick = now;

            // 先に瞑想分を伸ばしてから減衰を見るので、瞑想中の繋がりは減らない
            TickMeditation(elapsed);
            if (links.Count > 0)
            {
                TickDecay(elapsed);
            }
        }

        /// <summary>
        /// 瞑想中なら、周囲に収容されているアノマリーとの繋がりを伸ばす。
        /// 捻じれた瞑想でもバニラの瞑想でも同じように伸びる。
        /// </summary>
        private void TickMeditation(int elapsedTicks)
        {
            Pawn pawn = Pawn;
            if (!VoidAwake_VoidMagicUtility.IsMeditatingNow(pawn))
            {
                return;
            }

            VoidAwake_CompMeditationAnchor anchor = VoidAwake_VoidMagicUtility.AnchorUnder(pawn);
            float radius = anchor?.Radius ?? VoidAwake_VoidMagicUtility.DefaultMeditationRadius;
            float gainMultiplier = anchor?.Props.gainMultiplier ?? 1f;

            VoidAwake_VoidMagicUtility.ContainedEntitiesNear(pawn.Map, pawn.Position, radius, tmpEntities);
            if (tmpEntities.Count == 0)
            {
                return;
            }

            // 範囲内の対象で配分するので、1体に絞った方が早く伸びる
            float share = 1f / tmpEntities.Count;
            for (int i = 0; i < tmpEntities.Count; i++)
            {
                ThingDef entityDef = tmpEntities[i].def;
                VoidAwake_VoidMagicDef magicDef = VoidAwake_VoidMagicUtility.DefFor(entityDef);
                if (magicDef == null)
                {
                    continue;
                }

                float perTick = magicDef.connectionPerHourMeditating / VoidAwake_VoidMagicUtility.TicksPerHour;
                AddConnection(entityDef, perTick * elapsedTicks * share * gainMultiplier);
            }
            tmpEntities.Clear();
        }

        private void TickDecay(int elapsedTicks)
        {
            float days = elapsedTicks / (float)VoidAwake_VoidMagicUtility.TicksPerDay;

            for (int i = links.Count - 1; i >= 0; i--)
            {
                VoidAwake_VoidLink link = links[i];
                VoidAwake_VoidMagicDef magicDef = VoidAwake_VoidMagicUtility.DefFor(link.entityDef);
                if (magicDef == null)
                {
                    continue;
                }

                float decayPerDay = DecayPerDayFor(link);
                if (decayPerDay <= 0f)
                {
                    continue;
                }

                link.connection = Mathf.Max(0f, link.connection - decayPerDay * days);
                RefreshTier(link, magicDef);

                if (link.connection <= 0f)
                {
                    links.RemoveAt(i);
                }
            }
        }

        private void RefreshTier(VoidAwake_VoidLink link, VoidAwake_VoidMagicDef magicDef)
        {
            int newIndex = magicDef.TierIndexFor(link.connection);
            if (newIndex == link.unlockedTierIndex)
            {
                return;
            }

            int oldIndex = link.unlockedTierIndex;
            link.unlockedTierIndex = newIndex;
            ApplyTierContent(link, magicDef);
            NotifyTierChanged(link, magicDef, oldIndex, newIndex);
        }

        /// <summary>
        /// 解放済み段階の能力 / hediff を付与し、失った段階のものを取り除く。
        /// 現状は段階に中身が無いため実質何もしないが、Def を埋めればそのまま機能する。
        /// </summary>
        private void ApplyTierContent(VoidAwake_VoidLink link, VoidAwake_VoidMagicDef magicDef)
        {
            Pawn pawn = Pawn;
            if (pawn == null || pawn.Dead)
            {
                return;
            }

            for (int i = 0; i < magicDef.TierCount; i++)
            {
                VoidAwake_VoidMagicTier tier = magicDef.TierAt(i);
                bool unlocked = i <= link.unlockedTierIndex;

                if (tier.abilities != null && pawn.abilities != null)
                {
                    for (int j = 0; j < tier.abilities.Count; j++)
                    {
                        AbilityDef abilityDef = tier.abilities[j];
                        bool has = pawn.abilities.GetAbility(abilityDef) != null;
                        if (unlocked && !has)
                        {
                            pawn.abilities.GainAbility(abilityDef);
                        }
                        else if (!unlocked && has)
                        {
                            pawn.abilities.RemoveAbility(abilityDef);
                        }
                    }
                }

                if (tier.hediff != null && pawn.health != null)
                {
                    Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(tier.hediff);
                    if (unlocked && existing == null)
                    {
                        pawn.health.AddHediff(tier.hediff);
                    }
                    else if (!unlocked && existing != null)
                    {
                        pawn.health.RemoveHediff(existing);
                    }
                }
            }
        }

        private void NotifyTierChanged(VoidAwake_VoidLink link, VoidAwake_VoidMagicDef magicDef,
            int oldIndex, int newIndex)
        {
            if (Current.ProgramState != ProgramState.Playing || Scribe.mode != LoadSaveMode.Inactive)
            {
                return;
            }

            Pawn pawn = Pawn;
            if (pawn == null || !pawn.IsColonistPlayerControlled)
            {
                return;
            }

            bool gained = newIndex > oldIndex;
            VoidAwake_VoidMagicTier tier = magicDef.TierAt(gained ? newIndex : oldIndex);
            if (tier == null)
            {
                return;
            }

            string key = gained ? "VoidAwake_VoidMagicTierGained" : "VoidAwake_VoidMagicTierLost";
            Messages.Message(
                key.Translate(pawn.LabelShortCap, VoidAwake_VoidMagicUtility.EntityLabel(link.entityDef), tier.LabelCap),
                pawn,
                gained ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NegativeEvent,
                false);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref links, "voidMagicLinks", LookMode.Deep);
            Scribe_Values.Look(ref lastUpdateTick, "voidMagicLastUpdateTick", -1);

            if (Scribe.mode != LoadSaveMode.PostLoadInit)
            {
                return;
            }

            if (links == null)
            {
                links = new List<VoidAwake_VoidLink>();
                return;
            }

            links.RemoveAll(l => l == null || l.entityDef == null);
        }
    }
}
