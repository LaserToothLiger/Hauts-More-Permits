using HautsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace HautsPermits
{
    /*drops stuff and assigns it to the faction that granted the permit in the first place.
     * Like all permits in this mod, it can be used even when the permit-user's hostile to that faction IF the user has an off-cooldown, correct-faction permit authorizer, putting it on cooldown*/
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_DropFactionThing : RoyalTitlePermitWorker_Targeted
    {
        public override void DrawHighlight(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(this.caller.Position, this.def.royalAid.targetingRange, Color.white, null);
            if (this.def.royalAid.radius > 0f)
            {
                GenDraw.DrawRadiusRing(target.Cell, this.def.royalAid.radius, Color.white, null);
            }
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.CallResources(target.Cell);
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
            bool free;
            if (PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free))
            {
                action = delegate
                {
                    this.BeginCallResources(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            yield break;
        }
        private void BeginCallResources(Pawn caller, Faction faction, Map map, bool free)
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
            this.targetingParameters.validator = (TargetInfo target) => (this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(caller.Position) <= this.def.royalAid.targetingRange) && !target.Cell.Fogged(map) && DropCellFinder.CanPhysicallyDropInto(target.Cell, map, true, true) && target.Cell.GetTerrain(map).affordances.Contains(DefDatabase<TerrainAffordanceDef>.GetNamed("Light"));
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void CallResources(IntVec3 cell)
        {
            foreach (ThingDefCountClass tdcc in this.def.royalAid.itemsToDrop)
            {
                for (int i = 0; i < tdcc.count; i++)
                {
                    IntVec3 intVec;
                    if (i == 0 || !DropCellFinder.TryFindDropSpotNear(cell, this.map, out intVec, false, false, false, new IntVec2?(new IntVec2(1, 1)), false))
                    {
                        intVec = cell;
                    }
                    DropPodIncomingOfFaction dp = (DropPodIncomingOfFaction)SkyfallerMaker.MakeSkyfaller(HVMPDefOf.HVMP_DropPodOfFaction);
                    List<Thing> dummyThingForCompat = new List<Thing>();
                    Thing thing = ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel, null);
                    thing.stackCount = 1;
                    dummyThingForCompat.Add(thing);
                    if (dummyThingForCompat.Any())
                    {
                        ActiveTransporterInfo activeDropPodInfo = new ActiveTransporterInfo();
                        activeDropPodInfo.innerContainer.TryAddRangeOrTransfer(dummyThingForCompat, true, false);
                        ActiveTransporter activeDropPod = (ActiveTransporter)ThingMaker.MakeThing(((faction != null) ? faction.def.dropPodActive : null) ?? ThingDefOf.ActiveDropPod, null);
                        activeDropPod.Contents = activeDropPodInfo;
                        dp.innerContainer.TryAdd(activeDropPod);
                        dp.thing = tdcc.thingDef;
                        dp.faction = this.faction;
                        GenSpawn.Spawn(dp, intVec, this.map, WipeMode.Vanish);
                    }
                }
            }
            Messages.Message("MessagePermitTransportDrop".Translate(this.faction.Named("FACTION")), new LookTargets(cell, this.map), MessageTypeDefOf.NeutralEvent, true);
            this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
            }
            PermitAuthorizerUtility.DoPTargeterCooldown(this.faction, caller, this);
        }
        private Faction faction;
    }
    public class DropPodIncomingOfFaction : DropPodIncoming
    {
        protected override void SpawnThings()
        {
            if (placeDirect)
            {
                GenSpawn.Spawn(thing, base.Position, base.Map).SetFactionDirect(this.faction);
            }
            else
            {
                ThingDef thingDef = GenStuff.RandomStuffFor(thing);
                Thing t = ThingMaker.MakeThing(thing, thingDef);
                GenPlace.TryPlaceThing(t, base.Position, base.Map, ThingPlaceMode.Near, null, null, null, 1);
                t.SetFactionDirect(this.faction);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<ThingDef>(ref this.thing, "thing");
            Scribe_References.Look<Faction>(ref this.faction, "faction", false);
            Scribe_Values.Look<bool>(ref this.placeDirect, "placeDirect", false, false);
        }
        public ThingDef thing;
        public Faction faction;
        public bool placeDirect;
    }
}
