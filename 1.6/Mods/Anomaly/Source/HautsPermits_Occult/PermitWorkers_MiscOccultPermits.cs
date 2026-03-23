using HautsFramework;
using HautsPermits;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HautsPermits_Occult
{
    //teleport permit-user to target location
    public class RoyalTitlePermitWorker_SelfSkip : RoyalTitlePermitWorker_Targeted
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            FleckCreationData dataAttachedOverlay = FleckMaker.GetDataAttachedOverlay(this.caller, FleckDefOf.PsycastSkipFlashEntry, new Vector3(-0.5f, 0f, -0.5f), 1f, -1f);
            dataAttachedOverlay.link.detachAfterTicks = 5;
            this.caller.Map.flecks.CreateFleck(dataAttachedOverlay);
            FleckMaker.Static(target.Cell, this.caller.Map, FleckDefOf.PsycastSkipInnerExit, 1f);
            SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(this.caller.Position, this.caller.Map, false));
            SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(target.Cell, this.caller.Map, false));
            CompCanBeDormant compCanBeDormant = this.caller.TryGetComp<CompCanBeDormant>();
            if (compCanBeDormant != null)
            {
                compCanBeDormant.WakeUp();
            }
            this.caller.Position = target.Cell;
            if ((this.caller.Faction == Faction.OfPlayer || this.caller.IsPlayerControlled) && this.caller.Position.Fogged(this.caller.Map))
            {
                FloodFillerFog.FloodUnfog(this.caller.Position, this.caller.Map);
            }
            this.caller.stances.stunner.StunFor(60, this.caller, false, false, false);
            this.caller.Notify_Teleported(true, true);
            CompAbilityEffect_Teleport.SendSkipUsedSignal(this.caller.Position, this.caller);
            GenClamor.DoClamor(this.caller, target.Cell, 10f, ClamorDefOf.Ability);
            this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
            }
            PermitAuthorizerUtility.DoPTargeterCooldown(this.faction, this.caller, this);
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (map.generatorDef.isUnderground)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            string text = this.def.LabelCap + ": ";
            Action action = null;
            if (PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out bool free))
            {
                action = delegate
                {
                    this.BeginSelfSkip(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        private void BeginSelfSkip(Pawn caller, Faction faction, Map map, bool free)
        {
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetSelf = false;
            this.targetingParameters.canTargetFires = false;
            this.targetingParameters.canTargetItems = false;
            this.caller = caller;
            this.map = map;
            this.faction = faction;
            this.free = free;
            float rangeActual = this.def.royalAid.targetingRange;
            this.targetingParameters.validator = (TargetInfo target) => (rangeActual <= 0f || target.Cell.DistanceTo(caller.Position) <= rangeActual && !target.Cell.Fogged(map) && target.Cell.Standable(map));
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private Faction faction;
    }
    //only targets an unnatural entity or anomalous mutant. In addition to giving the hediffs from PermitMoreEffects, it also stuns for a random number of ticks in the PME's extraNumber*/
    public class RoyalTitlePermitWorker_EntityStunner : RoyalTitlePermitWorker_GiveHediffs
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return (pawn.IsMutant || pawn.IsEntity) && base.IsGoodPawn(pawn);
        }
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
            base.AffectPawnInner(pme, pawn, faction);
            if (pawn.stances.stunner != null)
            {
                pawn.stances.stunner.StunFor((int)pme.extraNumber.RandomInRange, null);
            }
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            PermitAuthorizerUtility.DoPTargeterCooldown(faction, caller, this);
        }
    }
}
