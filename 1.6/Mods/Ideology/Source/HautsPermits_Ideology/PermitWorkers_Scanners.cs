using HautsFramework;
using HautsPermits;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace HautsPermits_Ideology
{
    //unfogs the target cell (plus adjacent cells, which means if you hit a wall of a room, it'll still unfog adjacent rooms
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_ScannerSweep : RoyalTitlePermitWorker_Targeted
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.Reveal(target.Cell, this.caller.Map);
        }
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
                    this.BeginReveal(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            yield break;
        }
        private void BeginReveal(Pawn caller, Faction faction, Map map, bool free)
        {
            this.targetingParameters = new TargetingParameters
            {
                canTargetLocations = true,
                canTargetBuildings = false,
                canTargetPawns = false
            };
            this.caller = caller;
            this.map = map;
            this.faction = faction;
            this.free = free;
            this.targetingParameters.validator = (TargetInfo target) => (this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(caller.Position) <= this.def.royalAid.targetingRange);
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void Reveal(IntVec3 cell, Map map)
        {
            FloodFillerFog.FloodUnfog(cell, map);
            FogGrid fg = map.fogGrid;
            foreach (IntVec3 iv3 in GenRadial.RadialCellsAround(cell, 1.9f, true))
            {
                fg.Unfog(iv3);
            }
            Messages.Message("HVMP_ScannerSweep".Translate(this.faction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
            this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
            }
        }
        private Faction faction;
    }
    //Revealing Scan instantiates this condition, which disrupts all invisibility (and reduces HediffComp_Disappears timer to 0, if applicable) every 60s
    public class GameCondition_RevealingScan : GameCondition
    {
        public override void Init()
        {
            base.Init();
            this.RevealAll();
            this.ticks = 60;
        }
        public override void GameConditionTick()
        {
            this.ticks--;
            if (this.ticks <= 0)
            {
                this.RevealAll();
                this.ticks = 60;
            }
        }
        public void RevealAll()
        {
            foreach (Map m in base.AffectedMaps)
            {
                foreach (Pawn p in m.mapPawns.AllPawnsSpawned)
                {
                    foreach (Hediff h in p.health.hediffSet.hediffs)
                    {
                        HediffComp_Invisibility hci = h.TryGetComp<HediffComp_Invisibility>();
                        if (hci != null)
                        {
                            hci.DisruptInvisibility();
                            HediffComp_Disappears hcd = h.TryGetComp<HediffComp_Disappears>();
                            if (hcd != null)
                            {
                                hcd.ticksToDisappear = 0;
                            }
                        }
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.ticks, "ticks", 0, false);
        }
        public int ticks;
    }
    //Scanner Upgrade inflicts a stat boost by toggling a given comp
    public class CompProperties_UpgradeableScanner : CompProperties
    {
        public CompProperties_UpgradeableScanner()
        {
            this.compClass = typeof(CompUpgradeableScanner);
        }
    }
    public class CompUpgradeableScanner : ThingComp
    {
        public CompProperties_UpgradeableScanner Props
        {
            get
            {
                return (CompProperties_UpgradeableScanner)this.props;
            }
        }
        public override float GetStatOffset(StatDef stat)
        {
            return stat == HautsDefOf.Hauts_SurveySpeedFactor ? this.statOffset : 0f;
        }
        public override void GetStatsExplanation(StatDef stat, StringBuilder sb, string whitespace = "")
        {
            if (this.statOffset > float.Epsilon)
            {
                sb.AppendLine();
                sb.AppendLine(whitespace + "HVMP_StatsReport_FromScannerUpgrade".Translate() + ": " + this.statOffset);
            }
        }
        public override string CompInspectStringExtra()
        {
            if (this.statOffset > float.Epsilon)
            {
                return "HVMP_StatsReport_FromScannerUpgrade".Translate() + ": +" + this.statOffset.ToStringByStyle(ToStringStyle.PercentOne) + " " + HautsDefOf.Hauts_SurveySpeedFactor.label;
            }
            return null;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref this.statOffset, "statOffset", 0f, false);
        }
        public float statOffset;
    }
    public class RoyalTitlePermitWorker_ScannerUpgrade : RoyalTitlePermitWorker_Targeted, ITargetingSource
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
                        CompUpgradeableScanner cus = lti.Thing.TryGetComp<CompUpgradeableScanner>();
                        if (cus != null && cus.statOffset <= float.Epsilon)
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
            if (pme != null && pme.extraNumber != null && target.Thing != null)
            {
                this.UpgradeScanner(target.Thing, this.calledFaction);
            }
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (map.generatorDef.isUnderground)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
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
                    this.BeginUpgradeScanner(pawn, map, faction, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        private void BeginUpgradeScanner(Pawn pawn, Map map, Faction faction, bool free)
        {
            if (faction.HostileTo(Faction.OfPlayer))
            {
                return;
            }
            this.targetingParameters = new TargetingParameters
            {
                canTargetLocations = false,
                canTargetSelf = false,
                canTargetPawns = false,
                canTargetFires = false,
                canTargetBuildings = false,
                canTargetItems = true,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = (TargetInfo target) => this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(this.caller.Position) <= this.def.royalAid.targetingRange
            };
            this.caller = pawn;
            this.map = map;
            this.calledFaction = faction;
            this.free = free;
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void UpgradeScanner(Thing thing, Faction faction)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                CompUpgradeableScanner cus = thing.TryGetComp<CompUpgradeableScanner>();
                if (cus != null && cus.statOffset <= float.Epsilon)
                {
                    cus.statOffset += pme.extraNumber.RandomInRange;
                    PermitGlowVFXUtility.ThrowQualityUpgradeGlow(thing.PositionHeld.ToVector3(), this.map, 1f);
                    Messages.Message(pme.onUseMessage.Translate(faction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
                }
                this.caller.royalty.GetPermit(this.def, this.calledFaction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(this.calledFaction, this.def.royalAid.favorCost);
                }
            }
        }
        private Faction calledFaction;
    }
}
