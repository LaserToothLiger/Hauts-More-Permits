using HautsFramework;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HautsPermits
{
    /*as various existing permit workers, but if the would-be permit-user has an off-cooldown Permit Authorizer of the permit's faction, these permits can be used even if the permit's faction is hostile to the permit-user's faction
     * as CauseCondition from the Framework*/
    public class RoyalTitlePermitWorker_CauseCondition_PTargFriendly : RoyalTitlePermitWorker_CauseCondition
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
    }
    //as DropBook from the Framework
    public class RoyalTitlePermitWorker_DropBook_PTargFriendly : RoyalTitlePermitWorker_DropBook
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
    }
    //as DropPawns from the Framework
    public class RoyalTitlePermitWorker_DropPawns_PTargFriendly : RoyalTitlePermitWorker_DropPawns
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
    }
    //as DropResourcesOfCategory from the Framework
    public class RoyalTitlePermitWorker_DROC_PTargFriendly : RoyalTitlePermitWorker_DropResourcesOfCategory
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
    }
    //as DropResourcesStuff from the Framework
    public class RoyalTitlePermitWorker_DropResourcesStuff_PTargFriendly : RoyalTitlePermitWorker_DropResourcesStuff
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
    }
    //as GenerateQuest from the Framework
    public class RoyalTitlePermitWorker_GenerateQuest_PTargFriendly : RoyalTitlePermitWorker_GenerateQuest
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
    }
    //as... you know what? Guess. Take a guess.
    public class RoyalTitlePermitWorker_GiveHediffs_PTargFriendly : RoyalTitlePermitWorker_GiveHediffs
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
    }
    //as MultiplyItemStack from the Framework
    public class RoyalTitlePermitWorker_Investment : RoyalTitlePermitWorker_MultiplyItemStack
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
    }
    /*the following are all vanilla (well, Royalty, obviously) permit workers
     * CallShuttle*/
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_CallShuttlePTargFriendly : RoyalTitlePermitWorker_CallShuttle
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.CallShuttle(target.Cell);
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            MapGeneratorDef generatorDef = map.generatorDef;
            if (generatorDef != null && generatorDef.isUnderground)
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
                    this.BeginCallShuttle(pawn, pawn.MapHeld, faction, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            string text;
            if (!base.FillCaravanAidOption(pawn, faction, out text, out this.free, out bool flag))
            {
                yield break;
            }
            Command_Action command_Action = new Command_Action
            {
                defaultLabel = this.def.LabelCap + " (" + pawn.LabelShort + ")",
                defaultDesc = text,
                icon = RoyalTitlePermitWorker_CallShuttlePTargFriendly.CommandTex,
                action = delegate
                {
                    this.CallShuttleToCaravan(pawn, faction, this.free);
                }
            };
            if (pawn.MapHeld != null && pawn.MapHeld.generatorDef.isUnderground)
            {
                command_Action.Disable("CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")));
            }
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
        private void BeginCallShuttle(Pawn caller, Map map, Faction faction, bool free)
        {
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetSelf = false;
            this.targetingParameters.canTargetPawns = false;
            this.targetingParameters.canTargetFires = false;
            this.targetingParameters.canTargetBuildings = true;
            this.targetingParameters.canTargetItems = true;
            this.caller = caller;
            this.map = map;
            this.calledFaction = faction;
            this.free = free;
            float rangeActual = base.RangeClamped;
            this.targetingParameters.validator = (TargetInfo target) => rangeActual <= 0f || target.Cell.DistanceTo(caller.Position) <= rangeActual;
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void CallShuttle(IntVec3 landingCell)
        {
            if (this.caller.Spawned)
            {
                Thing thing = ThingMaker.MakeThing(ThingDefOf.Shuttle, null);
                CompShuttle compShuttle = thing.TryGetComp<CompShuttle>();
                compShuttle.permitShuttle = true;
                compShuttle.acceptChildren = true;
                TransportShip transportShip = TransportShipMaker.MakeTransportShip(TransportShipDefOf.Ship_Shuttle, null, thing);
                transportShip.ArriveAt(landingCell, this.map.Parent);
                transportShip.AddJobs(new ShipJobDef[]
                {
                    ShipJobDefOf.WaitForever,
                    ShipJobDefOf.Unload_Destination,
                    ShipJobDefOf.FlyAway
                });
                this.caller.royalty.GetPermit(this.def, this.calledFaction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(this.calledFaction, this.def.royalAid.favorCost);
                }
                PermitAuthorizerUtility.DoPTargeterCooldown(this.calledFaction, this.caller, this);
            }
        }
        private void CallShuttleToCaravan(Pawn caller, Faction faction, bool free)
        {
            MethodInfo CSTS = typeof(RoyalTitlePermitWorker_CallShuttle).GetMethod("CallShuttleToCaravan", BindingFlags.NonPublic | BindingFlags.Instance);
            CSTS.Invoke(this, new object[] { caller, faction, free });
            PermitAuthorizerUtility.DoPTargeterCooldown(faction, caller, this);
        }
        private Faction calledFaction;
        private static readonly Texture2D CommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CallShuttle", true);
    }
    //CallLaborers
    public class RoyalTitlePermitWorker_CallLaborersPTargFriendly : RoyalTitlePermitWorker_CallLaborers
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.CallLaborers(target.Cell);
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            string text;
            if (this.AidDisabled_NewTemp(map, pawn, faction, out text, true))
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + text, null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text2 = this.def.LabelCap + " (" + "CommandCallLaborersNumLaborers".Translate(this.def.royalAid.pawnCount) + "): ";
            bool free;
            if (base.FillAidOption(pawn, faction, ref text2, out free))
            {
                action = delegate
                {
                    this.BeginCallLaborers(pawn, map, faction, free);
                };
            }
            yield return new FloatMenuOption(text2, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        protected override bool AidDisabled_NewTemp(Map map, Pawn pawn, Faction faction, out string reason, bool temperatureMatters = true)
        {
            if (map.generatorDef.isUnderground)
            {
                reason = "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION"));
                return true;
            }
            if (this.def.layerBlacklist.Contains(pawn.MapHeld.Tile.LayerDef))
            {
                reason = "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION"));
                return true;
            }
            if (temperatureMatters && !this.TemperatureIsAcceptable(map, faction))
            {
                reason = "BadTemperature".Translate();
                return true;
            }
            reason = null;
            return false;
        }
        private void BeginCallLaborers(Pawn pawn, Map map, Faction faction, bool free)
        {
            if (faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null)
            {
                return;
            }
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetSelf = false;
            this.targetingParameters.canTargetPawns = false;
            this.targetingParameters.canTargetFires = false;
            this.targetingParameters.canTargetBuildings = true;
            this.targetingParameters.canTargetItems = true;
            this.caller = pawn;
            this.map = map;
            this.calledFaction = faction;
            this.free = free;
            float rangeActual = base.RangeClamped;
            this.targetingParameters.validator = (TargetInfo target) => rangeActual <= 0f || target.Cell.DistanceTo(this.caller.Position) <= rangeActual;
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void CallLaborers(IntVec3 landingCell)
        {
            QuestScriptDef permit_CallLaborers = QuestScriptDefOf.Permit_CallLaborers;
            Slate slate = new Slate();
            slate.Set<Map>("map", this.map, false);
            slate.Set<int>("laborersCount", this.def.royalAid.pawnCount, false);
            slate.Set<Faction>("permitFaction", this.calledFaction, false);
            slate.Set<PawnKindDef>("laborersPawnKind", this.def.royalAid.pawnKindDef, false);
            slate.Set<float>("laborersDurationDays", this.def.royalAid.aidDurationDays, false);
            slate.Set<IntVec3>("landingCell", landingCell, false);
            QuestUtility.GenerateQuestAndMakeAvailable(permit_CallLaborers, slate);
            this.caller.royalty.GetPermit(this.def, this.calledFaction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(this.calledFaction, this.def.royalAid.favorCost);
            }
            PermitAuthorizerUtility.DoPTargeterCooldown(this.calledFaction, caller, this);
        }
        private Faction calledFaction;
    }
    //OrbitalStrike
    public class RoyalTitlePermitWorker_OrbitalStrikePTargFriendly : RoyalTitlePermitWorker_OrbitalStrike
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.CallBombardment(target.Cell);
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            string text;
            if (this.AidDisabled_NewTemp(map, pawn, faction, out text, false))
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + text, null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            string text2 = this.def.LabelCap + ": ";
            Action action = null;
            bool free;
            if (base.FillAidOption(pawn, faction, ref text2, out free))
            {
                action = delegate
                {
                    this.BeginCallBombardment(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text2, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        protected override bool AidDisabled_NewTemp(Map map, Pawn pawn, Faction faction, out string reason, bool temperatureMatters = true)
        {
            if (map.generatorDef.isUnderground)
            {
                reason = "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION"));
                return true;
            }
            if (this.def.layerBlacklist.Contains(pawn.MapHeld.Tile.LayerDef))
            {
                reason = "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION"));
                return true;
            }
            if (temperatureMatters && !this.TemperatureIsAcceptable(map, faction))
            {
                reason = "BadTemperature".Translate();
                return true;
            }
            reason = null;
            return false;
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
            float rangeActual = base.RangeClamped;
            this.targetingParameters.validator = delegate (TargetInfo target)
            {
                if (rangeActual > 0f && target.Cell.DistanceTo(caller.Position) > rangeActual)
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
            Bombardment bombardment = (Bombardment)GenSpawn.Spawn(ThingDefOf.Bombardment, targetCell, this.map, WipeMode.Vanish);
            bombardment.impactAreaRadius = this.def.royalAid.radius;
            bombardment.explosionRadiusRange = this.def.royalAid.explosionRadiusRange;
            bombardment.bombIntervalTicks = this.def.royalAid.intervalTicks;
            bombardment.randomFireRadius = 1;
            bombardment.explosionCount = this.def.royalAid.explosionCount;
            bombardment.warmupTicks = this.def.royalAid.warmupTicks;
            bombardment.instigator = this.caller;
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
}
