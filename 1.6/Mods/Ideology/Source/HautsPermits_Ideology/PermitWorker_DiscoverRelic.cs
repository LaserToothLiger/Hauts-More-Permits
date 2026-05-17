using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HautsPermits_Ideology
{
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_DiscoverRelic : RoyalTitlePermitWorker
    {
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (faction.HostileTo(Faction.OfPlayer))
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            if (base.FillAidOption(pawn, faction, ref text, out bool free))
            {
                action = delegate
                {
                    this.MakeRelic(pawn, faction, this.free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            if (!base.FillCaravanAidOption(pawn, faction, out string text, out this.free, out bool flag))
            {
                yield break;
            }
            Command_Action command_Action = new Command_Action
            {
                defaultLabel = this.def.LabelCap + " (" + pawn.LabelShort + ")",
                defaultDesc = text,
                icon = RoyalTitlePermitWorker_DiscoverRelic.CommandTex,
                action = delegate
                {
                    this.MakeRelic(pawn, faction, this.free);
                }
            };
            if (faction.HostileTo(Faction.OfPlayer))
            {
                command_Action.Disable("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")));
            }
            if (flag)
            {
                command_Action.Disable("CommandCallRoyalAidNotEnoughFavor".Translate());
            }
            yield return command_Action;
            yield break;
        }
        protected virtual void MakeRelic(Pawn caller, Faction faction, bool free)
        {
            if (caller.Ideo != null)
            {
                Precept_Relic precept = (Precept_Relic)PreceptMaker.MakePrecept(PreceptDefOf.IdeoRelic);
                caller.Ideo.AddPrecept(precept, true, caller.Faction.def);
                Messages.Message("HVMP_RelicDiscovered".Translate(faction.Name, caller.Ideo.name), null, MessageTypeDefOf.NeutralEvent, true);
                caller.royalty.GetPermit(this.def, faction).Notify_Used();
                if (!free)
                {
                    caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
                }
            }
        }
        protected bool free;
        private static readonly Texture2D CommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CallAid", true);
    }
}
