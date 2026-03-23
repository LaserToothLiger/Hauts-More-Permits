using HautsF_Ideology;
using HautsFramework;
using HautsPermits;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HautsPermits_Ideology
{
    //as DropIdeoBookForCaller, but, y'know, having an off-cooldown permit authorizer of the correct faction allows the permit's use even when hostile to the permit's faction
    public class RoyalTitlePermitWorker_DropIdeoBook_PTargFriendly : RoyalTitlePermitWorker_DropIdeoBookForCaller
    {
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            PermitAuthorizerUtility.DoPTargeterCooldown(faction, caller, this);
        }
        public override int ItemStackCount(PermitMoreEffects pme, Pawn caller)
        {
            int result = base.ItemStackCount(pme, caller);
            if (this.faction != null)
            {
                float curSeniority = HVMP_Mod.settings.permitsScaleBySeniority ? caller.royalty.GetCurrentTitleInFaction(this.faction).def.seniority : this.def.minTitle.seniority;
                float divisor = (pme != null && pme.minPetness > 0) ? pme.minPetness : 100f;
                int seniority = Math.Max((int)(curSeniority / divisor), 1);
                result *= seniority;
            }
            return result;
        }
    }
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_DiscoverRelic : RoyalTitlePermitWorker
    {
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            bool free;
            if (PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free))
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
            string text;
            bool flag;
            if (!base.FillCaravanAidOption(pawn, faction, out text, out this.free, out flag))
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
            if (faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null)
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
                PermitAuthorizerUtility.DoPTargeterCooldown(faction, caller, this);
            }
        }
        protected bool free;
        private static readonly Texture2D CommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CallAid", true);
    }
}
