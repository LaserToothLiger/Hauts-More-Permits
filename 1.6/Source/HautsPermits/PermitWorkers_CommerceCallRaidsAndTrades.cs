using HautsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace HautsPermits
{
    //generates a raid from a random faction. This is a GenerateQuest derivative, so has all its options
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_CallRaid : RoyalTitlePermitWorker_GenerateQuest
    {
        public override bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
        {
            return !f.IsPlayer && !f.defeated && !f.temporary && (desperate || (map != null && f.def.allowedArrivalTemperatureRange.Includes(map.mapTemperature.OutdoorTemp) && f.def.allowedArrivalTemperatureRange.Includes(map.mapTemperature.SeasonalTemp))) && !f.Hidden && f.HostileTo(Faction.OfPlayer);
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (!this.CandidateFactions(map, false).Any<Faction>())
            {
                yield return new FloatMenuOption("HVMP_NoFactionCanSendRaids".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            if (PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out bool free))
            {
                action = delegate
                {
                    this.GiveQuest(pawn, faction, new IncidentParms(), this.free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            PermitAuthorizerUtility.DoPTargeterCooldown(faction, caller, this);
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            yield break;
        }
    }
    //also GenerateQuest derivative. Cannot be used if you have a Mastermind quest effect blocking traders, since otherwise this permit would just, uh, solve that quest.

    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_CallTrader : RoyalTitlePermitWorker_GenerateQuest
    {
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
            if (wcbs != null && wcbs.tradeBlockages > 0)
            {
                yield return new FloatMenuOption("HVMP_CommandCallRoyalAidTradersBlocked".Translate(wcbs.tradeBlockages, faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            if (PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out bool free))
            {
                action = delegate
                {
                    this.GiveQuest(pawn, faction, new IncidentParms(), this.free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            PermitAuthorizerUtility.DoPTargeterCooldown(faction, caller, this);
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            string text;
            bool flag;
            if (!base.FillCaravanAidOption(pawn, faction, out text, out this.free, out flag))
            {
                yield break;
            }
            WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
            if (wcbs != null && wcbs.tradeBlockages > 0)
            {
                yield break;
            }
            Command_Action command_Action = new Command_Action
            {
                defaultLabel = this.def.LabelCap + " (" + pawn.LabelShort + ")",
                defaultDesc = text,
                icon = RoyalTitlePermitWorker_CallTrader.CommandTex,
                action = delegate
                {
                    this.GiveQuest(pawn, faction, new IncidentParms(), this.free);
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
        private static readonly Texture2D CommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CallAid", true);
    }
    //derivative of CallTrader, which requires there to be a faction capable of sending a trader caravan. Because duh

    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_CallTraderCaravan : RoyalTitlePermitWorker_GenerateQuest
    {
        public override bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
        {
            return !f.IsPlayer && !f.defeated && !f.temporary && (desperate || (map != null && f.def.allowedArrivalTemperatureRange.Includes(map.mapTemperature.OutdoorTemp) && f.def.allowedArrivalTemperatureRange.Includes(map.mapTemperature.SeasonalTemp))) && !f.Hidden && !f.HostileTo(Faction.OfPlayer) && f.def.pawnGroupMakers != null && f.def.pawnGroupMakers.Any((PawnGroupMaker x) => x.kindDef == PawnGroupKindDefOf.Trader) && !NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, f) && f.def.caravanTraderKinds.Count != 0 && f.def.caravanTraderKinds.Any((TraderKindDef t) => t.requestable && this.TraderKindCommonality(t, map, f) > 0f);
        }
        public float TraderKindCommonality(TraderKindDef traderKind, Map map, Faction faction)
        {
            if (traderKind.faction != null && faction.def != traderKind.faction)
            {
                return 0f;
            }
            if (ModsConfig.IdeologyActive && faction.ideos != null && traderKind.category == "Slaver")
            {
                using (IEnumerator<Ideo> enumerator = faction.ideos.AllIdeos.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (!enumerator.Current.IdeoApprovesOfSlavery())
                        {
                            return 0f;
                        }
                    }
                }
            }
            if (traderKind.permitRequiredForTrading != null && !map.mapPawns.FreeColonists.Any((Pawn p) => p.royalty != null && p.royalty.HasPermit(traderKind.permitRequiredForTrading, faction)))
            {
                return 0f;
            }
            return traderKind.CalculatedCommonality;
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
            if (wcbs != null && wcbs.tradeBlockages > 0)
            {
                yield return new FloatMenuOption("HVMP_CommandCallRoyalAidTradersBlocked".Translate(wcbs.tradeBlockages, faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (!this.CandidateFactions(map, false).Any<Faction>())
            {
                yield return new FloatMenuOption("HVMP_NoFactionCanSendTraders".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            if (PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out bool free))
            {
                action = delegate
                {
                    this.GiveQuest(pawn, faction, new IncidentParms(), this.free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            PermitAuthorizerUtility.DoPTargeterCooldown(faction, caller, this);
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            yield break;
        }
    }
}
