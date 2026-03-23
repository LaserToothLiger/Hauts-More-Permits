using HautsFramework;
using HautsPermits;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HautsPermits_Ideology
{
    /*these permits all interact with permit authorizers
     * change the target item's quality. Relies on the following PermitMoreEffects fields:
     * -thingCategories: if specified, target item must have one of these categories
     * -forbiddenThingCategories: if specified, target item CAN'T have one of these categories
     * -extraNumber: target item's current quality level must be within this range (inclusive of min and max)*/
    public class RoyalTitlePermitWorker_AlterItemQuality : RoyalTitlePermitWorker_Targeted, ITargetingSource
    {
        public AcceptanceReport IsValidThing(LocalTargetInfo lti)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                TaggedString error = pme.invalidTargetMessage.Translate();
                if (!lti.IsValid)
                {
                    return new AcceptanceReport(error);
                } else {
                    if (pme.extraNumber != null)
                    {
                        if (lti.Thing != null)
                        {
                            Thing t = lti.Thing;
                            if (t.def.category == ThingCategory.Item && t.TryGetQuality(out QualityCategory qc) && t.def.thingCategories != null && (float)qc >= pme.extraNumber.min && (float)qc <= pme.extraNumber.max && (pme.thingCategories == null || t.def.thingCategories.ContainsAny((ThingCategoryDef tcd) => pme.thingCategories.Contains(tcd))) && (pme.forbiddenThingCategories == null || !t.def.thingCategories.ContainsAny((ThingCategoryDef tcd) => pme.forbiddenThingCategories.Contains(tcd))))
                            {
                                return AcceptanceReport.WasAccepted;
                            }
                        }
                    }
                }
                return new AcceptanceReport(error);
            }
            return new AcceptanceReport("Hauts_PMEMisconfig".Translate());
        }
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
            AcceptanceReport acceptanceReport = this.IsValidThing(target);
            if (!acceptanceReport.Accepted)
            {
                Messages.Message(acceptanceReport.Reason, new LookTargets(target.Cell, this.map), MessageTypeDefOf.RejectInput, false);
            }
            return acceptanceReport.Accepted;
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null && pme.extraNumber != null && target.Thing != null)
            {
                Thing t = target.Thing;
                if (t.def.category == ThingCategory.Item && t.TryGetQuality(out QualityCategory qc) && (float)qc >= pme.extraNumber.min && (float)qc <= pme.extraNumber.max && (pme.forbiddenThingCategories == null || !t.def.thingCategories.ContainsAny((ThingCategoryDef tcd) => pme.forbiddenThingCategories.Contains(tcd))))
                {
                    this.ImproveQuality(t, this.calledFaction);
                }
            }
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
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            if (PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out bool free))
            {
                action = delegate
                {
                    this.BeginImproveQuality(pawn, map, faction, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        private void BeginImproveQuality(Pawn pawn, Map map, Faction faction, bool free)
        {
            if (faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null)
            {
                return;
            }
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = false;
            this.targetingParameters.canTargetSelf = false;
            this.targetingParameters.canTargetPawns = false;
            this.targetingParameters.canTargetFires = false;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetItems = true;
            this.targetingParameters.mapObjectTargetsMustBeAutoAttackable = false;
            this.targetingParameters.validator = (TargetInfo target) => this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(this.caller.Position) <= this.def.royalAid.targetingRange;
            this.caller = pawn;
            this.map = map;
            this.calledFaction = faction;
            this.free = free;
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void ImproveQuality(Thing thing, Faction faction)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                if (Rand.Chance(pme.gambaFactorRange.RandomInRange))
                {
                    PermitGlowVFXUtility.ThrowQualityDestroyGlow(thing.PositionHeld.ToVector3(), this.map, 1f);
                    thing.Destroy();
                } else {
                    PermitGlowVFXUtility.ThrowQualityUpgradeGlow(thing.PositionHeld.ToVector3(), this.map, 1f);
                    MinifiedThing minifiedThing = thing as MinifiedThing;
                    CompQuality coq = ((minifiedThing != null) ? minifiedThing.InnerThing.TryGetComp<CompQuality>() : thing.TryGetComp<CompQuality>());
                    if (coq != null)
                    {
                        coq.SetQuality(coq.Quality + 1, new ArtGenerationContext?(ArtGenerationContext.Outsider));
                    }
                }
                Messages.Message(pme.onUseMessage.Translate(faction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
                this.caller.royalty.GetPermit(this.def, this.calledFaction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(this.calledFaction, this.def.royalAid.favorCost);
                }
                PermitAuthorizerUtility.DoPTargeterCooldown(this.calledFaction, this.caller, this);
            }
        }
        private Faction calledFaction;
    }
    /*restores hp to target building or item. Relies on the following PermitMoreEffects fields:
     * -thingCategories and forbiddenThingCategories, as AlterItemQuality
     * -extraNumber: add a random value from within this range to the target's hit points
     * also has an OtherEffects(Thing) virtual field that can be overriden to do other things to the target item*/
    public class RoyalTitlePermitWorker_RestoreItemHP : RoyalTitlePermitWorker_Targeted, ITargetingSource
    {
        public AcceptanceReport IsValidThing(LocalTargetInfo lti)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                TaggedString error = pme.invalidTargetMessage.Translate();
                if (!lti.IsValid)
                {
                    return new AcceptanceReport(error);
                } else {
                    if (pme.extraNumber != null)
                    {
                        if (lti.Thing != null)
                        {
                            Thing t = lti.Thing;
                            if (t.def.useHitPoints && (t.HitPoints < t.MaxHitPoints || this.OtherQualifiers(t)) && (t is Building || (t.def.thingCategories != null && (pme.thingCategories == null || t.def.thingCategories.ContainsAny((ThingCategoryDef tcd) => pme.thingCategories.Contains(tcd))) && (pme.forbiddenThingCategories == null || !t.def.thingCategories.ContainsAny((ThingCategoryDef tcd) => pme.forbiddenThingCategories.Contains(tcd))))))
                            {
                                return AcceptanceReport.WasAccepted;
                            }
                        }
                    }
                }
                return new AcceptanceReport(error);
            }
            return new AcceptanceReport("Hauts_PMEMisconfig".Translate());
        }
        public virtual bool OtherQualifiers(Thing t)
        {
            return true;
        }
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
            AcceptanceReport acceptanceReport = this.IsValidThing(target);
            if (!acceptanceReport.Accepted)
            {
                Messages.Message(acceptanceReport.Reason, new LookTargets(target.Cell, this.map), MessageTypeDefOf.RejectInput, false);
            }
            return acceptanceReport.Accepted;
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null && pme.extraNumber != null)
            {
                PermitGlowVFXUtility.ThrowRepairGlow(target.Cell.ToVector3(), this.map, 1.5f);
                if (target.Thing != null)
                {
                    Thing t = target.Thing;
                    if (t.def.useHitPoints && (t.HitPoints < t.MaxHitPoints || this.OtherQualifiers(t)) && (t is Building || (t.def.thingCategories != null && (pme.thingCategories == null || t.def.thingCategories.ContainsAny((ThingCategoryDef tcd) => pme.thingCategories.Contains(tcd))) && (pme.forbiddenThingCategories == null || !t.def.thingCategories.ContainsAny((ThingCategoryDef tcd) => pme.forbiddenThingCategories.Contains(tcd))))))
                    {
                        this.Heal(t, this.calledFaction);
                    }
                }
            }
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
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            if (PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out bool free))
            {
                action = delegate
                {
                    this.BeginHeal(pawn, map, faction, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        private void BeginHeal(Pawn pawn, Map map, Faction faction, bool free)
        {
            if (faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null)
            {
                return;
            }
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = false;
            this.targetingParameters.canTargetSelf = false;
            this.targetingParameters.canTargetPawns = false;
            this.targetingParameters.canTargetFires = false;
            this.targetingParameters.canTargetBuildings = true;
            this.targetingParameters.canTargetItems = true;
            this.targetingParameters.mapObjectTargetsMustBeAutoAttackable = false;
            this.targetingParameters.validator = (TargetInfo target) => this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(this.caller.Position) <= this.def.royalAid.targetingRange;
            this.caller = pawn;
            this.map = map;
            this.calledFaction = faction;
            this.free = free;
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void Heal(Thing thing, Faction faction)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null && pme.extraNumber != null)
            {
                thing.HitPoints += Math.Min((int)Math.Ceiling(pme.extraNumber.RandomInRange), thing.MaxHitPoints - thing.HitPoints);
                this.OtherEffects(thing);
                Messages.Message(pme.onUseMessage.Translate(faction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
                this.caller.royalty.GetPermit(this.def, this.calledFaction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(this.calledFaction, this.def.royalAid.favorCost);
                }
                PermitAuthorizerUtility.DoPTargeterCooldown(this.calledFaction, this.caller, this);
            }
        }
        public virtual void OtherEffects(Thing thing)
        {

        }
        private Faction calledFaction;
    }
    //derivative of RestoreItemHP, it overrides OtherEffects(Thing) to also untaint the target (if apparel) and replace broken components (if a breakdownable building whose components are broken down)
    public class RoyalTitlePermitWorker_RestoreItemHP_Perfect : RoyalTitlePermitWorker_RestoreItemHP
    {
        public override bool OtherQualifiers(Thing t)
        {
            return (t is Apparel a && a.WornByCorpse) || t.IsBrokenDown();
        }
        public override void OtherEffects(Thing thing)
        {
            if (thing is Apparel a)
            {
                a.WornByCorpse = false;
            }
            CompBreakdownable cbd = thing.TryGetComp<CompBreakdownable>();
            if (cbd != null && cbd.BrokenDown)
            {
                cbd.Notify_Repaired();
            }
        }
    }
    /*restores hp to all buildings and items (and restores broken down components) in radius of the target point (radius is one of the few fields actually IN royalAid, so it is not a PermitMoreEffects field)
     * amount of hp restored is random value within the extraNumber field of PermitMoreEffects
     * can do screenShake and play soundDef from PermitMoreEffects*/
    public class RoyalTitlePermitWorker_RestoreItemHP_AOE : RoyalTitlePermitWorker_Targeted
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
            this.DoAOE(target.Cell);
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
            bool free;
            if (PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free))
            {
                action = delegate
                {
                    this.BeginAOE(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        private void BeginAOE(Pawn caller, Faction faction, Map map, bool free)
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
        private void DoAOE(IntVec3 targetCell)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                foreach (Building building in GenRadial.RadialDistinctThingsAround(targetCell, this.map, this.def.royalAid.radius, true).OfType<Building>().Distinct<Building>())
                {
                    if (building.Faction == null || this.caller.Faction == null || this.caller.Faction == building.Faction || !this.caller.Faction.HostileTo(building.Faction))
                    {
                        bool throwGlow = false;
                        if (building.def.useHitPoints)
                        {
                            PermitGlowVFXUtility.ThrowRepairGlow(building.Position.ToVector3(), this.map, 1f);
                            throwGlow = true;
                            building.HitPoints += Math.Min((int)Math.Ceiling(pme.extraNumber.RandomInRange), building.MaxHitPoints - building.HitPoints);
                        }
                        CompBreakdownable cbd = building.TryGetComp<CompBreakdownable>();
                        if (cbd != null && cbd.BrokenDown)
                        {
                            if (!throwGlow)
                            {
                                PermitGlowVFXUtility.ThrowRepairGlow(building.Position.ToVector3(), this.map, 1f);
                            }
                            cbd.Notify_Repaired();
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
                Messages.Message(pme.onUseMessage.Translate(this.faction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
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
    //target item that is biocoded to an individual is no longer biocoded to that individual. It is still biocodable though, so picking it up will assign a new biocoding
    public class RoyalTitlePermitWorker_DecryptBiocoding : RoyalTitlePermitWorker_Targeted, ITargetingSource
    {
        public AcceptanceReport IsValidThing(LocalTargetInfo lti)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                TaggedString error = pme.invalidTargetMessage.Translate();
                if (!lti.IsValid)
                {
                    return new AcceptanceReport(error);
                } else {
                    if (lti.Thing != null)
                    {
                        CompBiocodable comp = lti.Thing.TryGetComp<CompBiocodable>();
                        if (comp != null && comp.Biocoded)
                        {
                            return AcceptanceReport.WasAccepted;
                        }
                    }
                }
                return new AcceptanceReport(error);
            }
            return new AcceptanceReport("Hauts_PMEMisconfig".Translate());
        }
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
            AcceptanceReport acceptanceReport = this.IsValidThing(target);
            if (!acceptanceReport.Accepted)
            {
                Messages.Message(acceptanceReport.Reason, new LookTargets(target.Cell, this.map), MessageTypeDefOf.RejectInput, false);
            }
            return acceptanceReport.Accepted;
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                PermitGlowVFXUtility.ThrowDecryptionGlow(target.Cell.ToVector3(), this.map, 1f);
                if (target.Thing != null)
                {
                    CompBiocodable comp = target.Thing.TryGetComp<CompBiocodable>();
                    if (comp != null && comp.Biocoded)
                    {
                        comp.UnCode();
                        Messages.Message(pme.onUseMessage.Translate(this.calledFaction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
                        this.caller.royalty.GetPermit(this.def, this.calledFaction).Notify_Used();
                        if (!this.free)
                        {
                            this.caller.royalty.TryRemoveFavor(this.calledFaction, this.def.royalAid.favorCost);
                        }
                        PermitAuthorizerUtility.DoPTargeterCooldown(this.calledFaction, this.caller, this);
                    }
                }
            }
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
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            if (PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out bool free))
            {
                action = delegate
                {
                    this.BeginHeal(pawn, map, faction, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        private void BeginHeal(Pawn pawn, Map map, Faction faction, bool free)
        {
            if (faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null)
            {
                return;
            }
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = false;
            this.targetingParameters.canTargetSelf = false;
            this.targetingParameters.canTargetPawns = false;
            this.targetingParameters.canTargetFires = false;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetItems = true;
            this.targetingParameters.mapObjectTargetsMustBeAutoAttackable = false;
            this.targetingParameters.validator = (TargetInfo target) => this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(this.caller.Position) <= this.def.royalAid.targetingRange;
            this.caller = pawn;
            this.map = map;
            this.calledFaction = faction;
            this.free = free;
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private Faction calledFaction;
    }
}
