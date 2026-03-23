using HautsFramework;
using HautsPermits;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HautsPermits_Biotech
{
    //unpollute the terrain in radius of the target point. Also destroy all wastepacks (and anything with a thing category in the PermitMoreEffects' thingCategories, if any) in that radius
    public class RoyalTitlePermitWorker_PollutionScoop : RoyalTitlePermitWorker_Targeted
    {
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.CanHitTarget(target))
            {
                if (target.IsValid && showMessages)
                {
                    Messages.Message(this.def.LabelCap + ": " + "AbilityCannotHitTarget".Translate(), MessageTypeDefOf.RejectInput, true);
                }
                return false;
            }
            return true;
        }
        public override void DrawHighlight(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(this.caller.Position, this.def.royalAid.targetingRange, Color.white, null);
            GenDraw.DrawRadiusRing(target.Cell, this.def.royalAid.radius, Color.white, null);
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.ScoopPollution(target.Cell);
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
                    this.BeginScoop(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        private void BeginScoop(Pawn caller, Faction faction, Map map, bool free)
        {
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetSelf = true;
            this.targetingParameters.canTargetFires = true;
            this.targetingParameters.canTargetItems = true;
            this.caller = caller;
            this.map = map;
            this.faction = faction;
            this.free = free;
            this.targetingParameters.validator = delegate (TargetInfo target)
            {
                if (this.def.royalAid.targetingRange > 0f && target.Cell.DistanceTo(caller.Position) > this.def.royalAid.targetingRange)
                {
                    return false;
                }
                if (target.Cell.Fogged(map))
                {
                    return false;
                }
                RoofDef roof = target.Cell.GetRoof(map);
                return roof == null || !roof.isThickRoof;
            };
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void ScoopPollution(IntVec3 targetCell)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                List<Thing> thingsToDestroy = new List<Thing>();
                foreach (Thing thing in GenRadial.RadialDistinctThingsAround(targetCell, this.map, 6, true))
                {
                    if (thing.def == ThingDefOf.Wastepack || (!thing.def.thingCategories.NullOrEmpty() && !pme.thingCategories.NullOrEmpty() && thing.def.thingCategories.ContainsAny((ThingCategoryDef tcd) => pme.thingCategories.Contains(tcd))))
                    {
                        thingsToDestroy.Add(thing);
                    }
                }
                for (int i = thingsToDestroy.Count - 1; i >= 0; i--)
                {
                    thingsToDestroy[i].Destroy();
                }
                int cells = GenRadial.NumCellsInRadius(this.def.royalAid.radius);
                for (int i = 0; i < cells; i++)
                {
                    IntVec3 c = targetCell + GenRadial.RadialPattern[i];
                    if (c.InBounds(this.map) && c.IsValid)
                    {
                        if (c.CanUnpollute(this.map))
                        {
                            this.map.pollutionGrid.SetPolluted(c, false, false);
                        }
                    }
                }
                if (pme.screenShake && this.map == Find.CurrentMap)
                {
                    Find.CameraDriver.shaker.DoShake(1f);
                }
                if (pme.soundDef != null)
                {
                    pme.soundDef.PlayOneShot(new TargetInfo(targetCell, this.map, false));
                }
                GenExplosion.DoExplosion(targetCell, this.map, this.def.royalAid.radius * 0.67f, DamageDefOf.Smoke, null, -1, -1f, null, null, null, null, null, 0f, 1, new GasType?(GasType.BlindSmoke), null, 255, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f, null, null);
                this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
                }
                PermitAuthorizerUtility.DoPTargeterCooldown(this.faction, this.caller, this);
            }
        }
        private Faction faction;
    }
}
