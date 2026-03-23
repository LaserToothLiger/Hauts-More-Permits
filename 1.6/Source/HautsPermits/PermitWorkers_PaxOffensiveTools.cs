using HautsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HautsPermits
{
    /*Like all permits in this mod, these are all permit authorizer-friendly (see PermitWorkers_PermitAuthorizerFriendlyVariants.cs).
     * |||||ORBITAL SCALPEL|||||
     * apply to building or pawn not under a mortar-blocking roof or shield. After a brief delay, a dark miracle occurs (unless the target is now under a mortar-blocking roof or shield).*/
    public class RoyalTitlePermitWorker_OrbitalScalpel : RoyalTitlePermitWorker_Targeted, ITargetingSource
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
                }
                else
                {
                    foreach (Thing t in lti.Cell.GetThingList(this.caller.Map))
                    {
                        if ((t is Pawn p && !p.IsPsychologicallyInvisible()) || (t is Building && t.def.useHitPoints && t.def.building.canBeDamagedByAttacks))
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
            if (target.IsValid && !HautsMiscUtility.CanBeHitByAirToSurface(target.Cell, this.caller.Map, false))
            {
                if (showMessages)
                {
                    Messages.Message(this.def.LabelCap + ": " + "AbilityCannotHitTarget".Translate(), MessageTypeDefOf.RejectInput, true);
                }
                return false;
            }
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
                if (target.Thing != null)
                {
                    if (target.Thing is Building b)
                    {
                        if (b.def.useHitPoints && b.def.building.canBeDamagedByAttacks)
                        {
                            this.InstantiateEffect(b, pme);
                        }
                        return;
                    }
                    if (target.Thing is Pawn p)
                    {
                        this.InstantiateEffect(p, pme);
                    }
                }
            }
        }
        public void InstantiateEffect(Thing thing, PermitMoreEffects pme)
        {
            if (!pme.hediffs.NullOrEmpty())
            {
                Hediff h = HediffMaker.MakeHediff(pme.hediffs.First(), this.caller, null);
                HediffComp_BoomHeadshot hcbh = h.TryGetComp<HediffComp_BoomHeadshot>();
                if (hcbh != null)
                {
                    hcbh.victim = thing;
                }
                this.caller.health.AddHediff(h);
            }
            PermitGlowVFXUtility.ThrowScalpelScope(thing, thing.Map, 1f);
            this.caller.royalty.GetPermit(this.def, this.calledFaction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(this.calledFaction, this.def.royalAid.favorCost);
            }
            PermitAuthorizerUtility.DoPTargeterCooldown(this.calledFaction, this.caller, this);
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
            if (PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free))
            {
                action = delegate
                {
                    this.BeginAffectPawn(pawn, map, faction, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null;
        }
        private void BeginAffectPawn(Pawn pawn, Map map, Faction faction, bool free)
        {
            if (this.IsFactionHostileToPlayer(faction, pawn))
            {
                return;
            }
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = false;
            this.targetingParameters.canTargetSelf = true;
            this.targetingParameters.canTargetPawns = true;
            this.targetingParameters.canTargetFires = false;
            this.targetingParameters.canTargetBuildings = true;
            this.targetingParameters.canTargetItems = false;
            this.targetingParameters.validator = (TargetInfo target) => this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(this.caller.Position) <= this.def.royalAid.targetingRange;
            this.caller = pawn;
            this.map = map;
            this.calledFaction = faction;
            this.free = free;
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        public Faction CalledFaction
        {
            get
            {
                return this.CalledFaction;
            }
        }
        private Faction calledFaction;
    }
    /*Orbital Scalpel's delay is handled by a hidden hediff given to the permit-user. This does mean that if the permit-user is suspended in time, the delay will be paused too, but this is a weird and rare exception.
     * If a pawn invokes multiple Scalpels at once, they should have separate timers; therefore, the hidden hediffs never merge into each other.*/
    public class ScalpelTracker : HediffWithComps
    {
        public override bool TryMergeWith(Hediff other)
        {
            return false;
        }
    }
    /*this is the comp for aforementioned hidden hediff, which lets you specify how much damage (and what kind, with how much armor pen) it deals, as well as what sound should play on hit.
     * Effects occur when hediff is removed for any reason, which prevents some situations where Things(TM) happening to the permit-user could remove the hediff before the shot should fire.*/
    public class HediffCompProperties_BoomHeadshot : HediffCompProperties
    {
        public HediffCompProperties_BoomHeadshot()
        {
            this.compClass = typeof(HediffComp_BoomHeadshot);
        }
        public DamageDef damageType;
        public float damageAmount;
        public float armorPen;
        public SoundDef soundDef;
    }
    public class HediffComp_BoomHeadshot : HediffComp
    {
        public HediffCompProperties_BoomHeadshot Props
        {
            get
            {
                return (HediffCompProperties_BoomHeadshot)this.props;
            }
        }
        public override void CompPostPostRemoved()
        {
            Thing victim = this.victim;
            if (!victim.DestroyedOrNull() && victim.Spawned)
            {
                this.Props.soundDef.PlayOneShot(new TargetInfo(victim.Position, victim.Map, false));
                if (HautsMiscUtility.CanBeHitByAirToSurface(victim.Position, victim.Map, false))
                {
                    RoofDef roof = victim.Map.roofGrid.RoofAt(victim.Position);
                    if (roof != null && !roof.isThickRoof)
                    {
                        victim.Map.roofGrid.SetRoof(victim.Position, null);
                    }
                    this.victim.TakeDamage(new DamageInfo(DamageDefOf.Bomb, this.Props.damageAmount, this.Props.armorPen, instigator: this.Pawn));
                }
                //FleckMaker.ThrowSmoke(victim.DrawPos, victim.Map, 10f);
            }
            base.CompPostPostRemoved();
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look<Thing>(ref this.victim, "victim", false);
        }
        public Thing victim;
    }
    /*|||||ALL THE OTHER PAX OFFENSIVE PERMITS WITH UNIQUE WORKERS|||||
     * EMI doesn't just cause an electrical grid-disabling effect, it also stuns all hostile buildings on the map*/
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_EMI : RoyalTitlePermitWorker_CauseCondition
    {
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null;
        }
        protected override void MakeCondition(Pawn caller, Faction faction, IncidentParms parms, bool free)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            base.MakeCondition(caller, faction, parms, free);
            if (caller.MapHeld != null && pme != null && pme.extraNumber != null)
            {
                foreach (Building b in caller.MapHeld.listerBuildings.allBuildingsNonColonist)
                {
                    if (b.Faction == null || b.Faction.RelationKindWith(Faction.OfPlayerSilentFail) == FactionRelationKind.Hostile)
                    {
                        CompStunnable stunComp = b.GetComp<CompStunnable>();
                        if (stunComp != null)
                        {
                            stunComp.StunHandler.StunFor((int)pme.extraNumber.RandomInRange, null, false);
                        }
                    }
                }
            }
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            PermitAuthorizerUtility.DoPTargeterCooldown(faction, caller, this);
        }
    }
    //target a location. Infestation spawn there.
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_Infestation : RoyalTitlePermitWorker_Targeted
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.BaitInfestation(target.Cell);
        }
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
                    this.BeginInfestation(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            yield break;
        }
        private void BeginInfestation(Pawn caller, Faction faction, Map map, bool free)
        {
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetPawns = false;
            this.caller = caller;
            this.map = map;
            this.faction = faction;
            this.free = free;
            this.targetingParameters.validator = (TargetInfo target) => (this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(caller.Position) <= this.def.royalAid.targetingRange) && !target.Cell.Fogged(map) && target.Cell.GetRegion(map, RegionType.Set_Passable) != null && target.Cell.GetTemperature(map) >= -17f;
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void BaitInfestation(IntVec3 cell)
        {
            IncidentParms incidentParms = new IncidentParms();
            incidentParms.target = this.map;
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                incidentParms.points = pme.incidentPoints.RandomInRange;
            }
            else
            {
                incidentParms.points = 1750;
            }
            incidentParms.infestationLocOverride = cell;
            incidentParms.forced = true;
            IncidentDefOf.Infestation.Worker.TryExecute(incidentParms);
            this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
            }
            PermitAuthorizerUtility.DoPTargeterCooldown(this.faction, this.caller, this);
        }
        private Faction faction;
    }
    //generates a Psychic Animal Pulser effect
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_ManhunterPulse : RoyalTitlePermitWorker_Targeted
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
                    this.MakeCondition(pawn, faction, new IncidentParms(), this.free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            yield break;
        }
        protected virtual void MakeCondition(Pawn caller, Faction faction, IncidentParms parms, bool free)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null && caller.MapHeld != null)
            {
                foreach (Pawn p in caller.MapHeld.mapPawns.AllPawnsSpawned)
                {
                    if (p.IsAnimal && p.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon && (p.Faction == null || p.Faction != Faction.OfPlayerSilentFail) && !p.IsQuestLodger() && !p.Dead)
                    {
                        if (!p.Awake())
                        {
                            RestUtility.WakeUp(p, true);
                        }
                        p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, null, false, false, false, null, false, false, false);
                    }
                }
                Messages.Message(pme.onUseMessage.Translate(faction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
                if (pme.screenShake && caller.MapHeld == Find.CurrentMap)
                {
                    Find.CameraDriver.shaker.DoShake(1f);
                }
                if (pme.soundDef != null)
                {
                    pme.soundDef.PlayOneShot(new TargetInfo(caller.PositionHeld, caller.MapHeld, false));
                }
                caller.royalty.GetPermit(this.def, faction).Notify_Used();
                if (!free)
                {
                    caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
                }
                PermitAuthorizerUtility.DoPTargeterCooldown(faction, caller, this);
            }
        }
    }
    //target a location. Create a DelayedPowerBeam there, which after a brief delay creates a real Power Beam at its location.
    public class RoyalTitlePermitWorker_OrbitalBeam : RoyalTitlePermitWorker_Targeted
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
            GenDraw.DrawRadiusRing(target.Cell, this.def.royalAid.radius + this.def.royalAid.explosionRadiusRange.max, Color.white, null);
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.CallBombardment(target.Cell);
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
                    this.BeginCallBombardment(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        private void BeginCallBombardment(Pawn caller, Faction faction, Map map, bool free)
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
        private void CallBombardment(IntVec3 targetCell)
        {
            DelayedPowerBeam dpb = (DelayedPowerBeam)GenSpawn.Spawn(HVMPDefOf.HVMP_DelayedPowerBeam, targetCell, this.map, WipeMode.Vanish);
            dpb.duration = this.def.royalAid.explosionCount;
            dpb.instigator = this.caller;
            SoundDefOf.OrbitalStrike_Ordered.PlayOneShotOnCamera(null);
            this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
            }
            PermitAuthorizerUtility.DoPTargeterCooldown(this.faction, this.caller, this);
        }
        private Faction faction;
    }
    public class DelayedPowerBeam : ThingWithComps
    {
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.Comps_PostDraw();
        }
        protected override void Tick()
        {
            base.Tick();
            if (this.warmupTicks > 0)
            {
                this.warmupTicks--;
                if (this.warmupTicks == 60)
                {
                    this.angle = DelayedPowerBeam.AngleRange.RandomInRange;
                    base.GetComp<CompOrbitalBeam>().StartAnimation(this.duration, 10, this.angle);
                }
                if (this.warmupTicks == 0)
                {
                    PowerBeam powerBeam = (PowerBeam)GenSpawn.Spawn(ThingDefOf.PowerBeam, this.Position, this.Map, WipeMode.Vanish);
                    powerBeam.duration = this.duration;
                    powerBeam.instigator = this.instigator;
                    powerBeam.weaponDef = null;
                    if (!powerBeam.Spawned)
                    {
                        Log.Error("Called StartStrike() on unspawned thing.");
                        return;
                    }
                    powerBeam.StartStrike();
                    SoundDefOf.OrbitalStrike_Ordered.PlayOneShotOnCamera(null);
                    this.Destroy();
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.warmupTicks, "warmupTicks", 0, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 600, false);
            Scribe_References.Look<Thing>(ref this.instigator, "instigator", false);
            Scribe_Values.Look<float>(ref this.angle, "angle", 0f, false);
        }
        public int warmupTicks = 120;
        public int duration;
        public Thing instigator;
        private float angle;
        private static readonly FloatRange AngleRange = new FloatRange(-12f, 12f);
    }
    //target a location. Dora the Explorer wants to know if you can tell what this one does
    public class RoyalTitlePermitWorker_MechCluster : RoyalTitlePermitWorker_Targeted
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
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.CallCluster(target.Cell);
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
                    this.BeginCallCluster(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        private void BeginCallCluster(Pawn caller, Faction faction, Map map, bool free)
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
        private void CallCluster(IntVec3 targetCell)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            float points = (pme != null && pme.incidentPoints.max > 0) ? pme.incidentPoints.RandomInRange : StorytellerUtility.DefaultThreatPointsNow(this.map);
            MechClusterSketch mechClusterSketch = MechClusterGenerator.GenerateClusterSketch(points, this.map, true, false);
            MechClusterUtility.SpawnCluster(targetCell, this.map, mechClusterSketch, true, false, null);
            this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
            }
            PermitAuthorizerUtility.DoPTargeterCooldown(this.faction, this.caller, this);
        }
        private Faction faction;
    }
}
