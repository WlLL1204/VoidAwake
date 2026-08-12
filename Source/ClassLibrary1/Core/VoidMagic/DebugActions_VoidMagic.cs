using System;
using System.Collections.Generic;
using System.Text;
using LudeonTK;
using RimWorld;
using Verse;

namespace VoidAwake
{
    public static class DebugActions_VoidMagic
    {
        private const float DebugConnectionStep = 25f;

        [DebugAction("VoidAwake", "VoidMagic: add connection +25", false, false, false, false, false, 0, false,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void AddConnection()
        {
            WithSelectedColonists(pawns => ChooseEntity(entityDef =>
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    VoidMagicUtility.GetComp(pawns[i]).AddConnection(entityDef, DebugConnectionStep);
                }
            }));
        }

        [DebugAction("VoidAwake", "VoidMagic: fill connection", false, false, false, false, false, 0, false,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FillConnection()
        {
            WithSelectedColonists(pawns => ChooseEntity(entityDef =>
            {
                VoidAwake_VoidMagicDef magicDef = VoidMagicUtility.DefFor(entityDef);
                if (magicDef == null)
                {
                    return;
                }

                for (int i = 0; i < pawns.Count; i++)
                {
                    VoidMagicUtility.GetComp(pawns[i]).SetConnection(entityDef, magicDef.maxConnection);
                }
            }));
        }

        [DebugAction("VoidAwake", "VoidMagic: clear connections", false, false, false, false, false, 0, false,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ClearConnections()
        {
            WithSelectedColonists(pawns =>
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    VoidMagicUtility.GetComp(pawns[i]).ClearAllLinks();
                }
                Messages.Message($"Cleared void magic links on {pawns.Count} colonist(s).",
                    MessageTypeDefOf.TaskCompletion, false);
            });
        }

        [DebugAction("VoidAwake", "VoidMagic: dump links", false, false, false, false, false, 0, false,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void DumpLinks()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("[VoidAwake] VoidMagic links");
            sb.AppendLine($"linkable anomaly defs: {VoidMagicUtility.LinkableEntityDefs.Count}, "
                + $"contained right now: {VoidMagicUtility.ContainedEntityDefsNow().Count}");

            foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
            {
                VoidAwake_VoidMagicComp comp = VoidMagicUtility.GetComp(colonist);
                if (comp == null)
                {
                    continue;
                }

                sb.AppendLine($"{colonist.LabelShort}: {comp.Links.Count} link(s)");
                for (int i = 0; i < comp.Links.Count; i++)
                {
                    VoidAwake_VoidLink link = comp.Links[i];
                    VoidAwake_VoidMagicDef magicDef = VoidMagicUtility.DefFor(link.entityDef);
                    VoidMagicTier tier = magicDef?.TierAt(link.unlockedTierIndex);
                    sb.AppendLine($"  {link.entityDef.defName} {link.connection:F1}"
                        + $" tier={link.unlockedTierIndex}({tier?.LabelCap ?? "none"})"
                        + $" decay={comp.DecayPerDayFor(link):F1}/day"
                        + $" lost={comp.IsLost(link)}");
                }
            }

            Log.Message(sb.ToString());
            Messages.Message("VoidMagic links written to log.", MessageTypeDefOf.TaskCompletion, false);
        }

        private static void WithSelectedColonists(Action<List<Pawn>> action)
        {
            var pawns = new List<Pawn>();
            foreach (object selected in Find.Selector.SelectedObjects)
            {
                if (selected is Pawn pawn && VoidMagicUtility.GetComp(pawn) != null)
                {
                    pawns.Add(pawn);
                }
            }

            if (pawns.Count == 0)
            {
                Messages.Message("Select a colonist first.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            action(pawns);
        }

        /// <summary>収容中のアノマリーを先頭に並べて選ばせる。</summary>
        private static void ChooseEntity(Action<ThingDef> action)
        {
            var options = new List<FloatMenuOption>();
            HashSet<ThingDef> contained = VoidMagicUtility.ContainedEntityDefsNow();

            foreach (ThingDef entityDef in contained)
            {
                ThingDef local = entityDef;
                options.Add(new FloatMenuOption($"* {local.LabelCap}", () => action(local)));
            }

            List<ThingDef> linkable = VoidMagicUtility.LinkableEntityDefs;
            for (int i = 0; i < linkable.Count; i++)
            {
                ThingDef local = linkable[i];
                if (contained.Contains(local))
                {
                    continue;
                }
                options.Add(new FloatMenuOption(local.LabelCap, () => action(local)));
            }

            if (options.Count == 0)
            {
                Messages.Message("No linkable anomaly def found.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}
