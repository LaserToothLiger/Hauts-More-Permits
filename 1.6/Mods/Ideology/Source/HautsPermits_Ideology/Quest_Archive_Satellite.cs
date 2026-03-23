using HautsPermits;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Grammar;
using Verse.Sound;

namespace HautsPermits_Ideology
{
    /*I give you a spacedrone and you hack it. It's surrounded by dormant mechs which awake after a specified time. This takes care of allat. It also handles all three mutators:
     * sat1: Descend tells the quest part to make the mechs' Lord be LordJob_MechanoidsDefend, not LordJob_SleepThenMechanoidsDefendDrone; they'll just be awake from the start
     * sat2: Homing Beacon flips on the drone's CompHB, which causes it to periodically call down mechanoids
     * sat3: Shipboard Weaponry replaces the spacedrone's usual thing def with SW_droneDef (as opposed to nonSW_droneDef), and gives it to the mech faction (which is good because SW_droneDef has a gun for its neutral special).*/
    public class QuestNode_SpawnDronePlusGuards : QuestNode
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;
            QuestPart_DronePlusGuards qpdpg = new QuestPart_DronePlusGuards();
            qpdpg.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
            qpdpg.tag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(this.tag.GetValue(slate));
            qpdpg.mapParent = slate.Get<Map>("map", null, false).Parent;
            float num = Mathf.Max(slate.Get<float>("points", 0f, false) * 0.9f, 300f);
            string spacedroneDestroyedSignal = QuestGenUtility.HardcodedSignalWithQuestID("spacedrone.Destroyed");
            string spacedroneHackedSignal = QuestGenUtility.HardcodedSignalWithQuestID("spacedrone.Hacked");
            List<Pawn> list2 = PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
            {
                groupKind = PawnGroupKindDefOf.Combat,
                tile = slate.Get<Map>("map", null, false).Tile,
                faction = Faction.OfMechanoids,
                points = num
            }, true).ToList<Pawn>();
            qpdpg.pawnsToSpawn = new List<PawnKindDef>();
            foreach (Pawn p in list2)
            {
                qpdpg.pawnsToSpawn.Add(p.kindDef);
            }
            bool mayhemMode = HVMP_Mod.settings.satX;
            Thing thing;
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.sat3, mayhemMode))
            {
                thing = ThingMaker.MakeThing(this.SW_droneDef, null);
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_SW_info", this.SW_description.Formatted())
                });
            } else {
                thing = ThingMaker.MakeThing(this.nonSW_droneDef, null);
                thing.SetFaction(Faction.OfMechanoids);
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_SW_info", " ") });
            }
            slate.Set<Thing>("spacedrone", thing, false);
            QuestPart_Filter_AllThingsHacked questPart_Filter_AllThingsHacked = new QuestPart_Filter_AllThingsHacked();
            questPart_Filter_AllThingsHacked.things.Add(thing);
            questPart_Filter_AllThingsHacked.inSignal = spacedroneDestroyedSignal;
            questPart_Filter_AllThingsHacked.outSignal = QuestGen.GenerateNewSignal("QuestEndSuccess", true);
            questPart_Filter_AllThingsHacked.outSignalElse = QuestGen.GenerateNewSignal("QuestEndFailure", true);
            quest.AddPart(questPart_Filter_AllThingsHacked);
            qpdpg.drone = thing;
            qpdpg.sleepyTime = this.sleepyTime.RandomInRange;
            slate.Set<int>("timeToWake", qpdpg.sleepyTime, false);
            List<PawnKindDef> pkdList = new List<PawnKindDef>();
            foreach (Pawn p in list2)
            {
                pkdList.Add(p.kindDef);
            }
            qpdpg.dropSpot = this.dropSpot.GetValue(slate) ?? IntVec3.Invalid;
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.sat1, mayhemMode))
            {
                qpdpg.DESC_on = true;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_DESC_info", this.DESC_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_DESC_info", this.nonDESC_description.Formatted())
                });
            }
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.sat2, mayhemMode))
            {
                CompHB chb = thing.TryGetComp<CompHB>();
                if (chb != null)
                {
                    chb.HB_on = true;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_HB_info", this.HB_description.Formatted())
                    });
                }
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_HB_info", " ") });
            }
            quest.AddPart(qpdpg);
            string text = PawnUtility.PawnKindsToLineList(pkdList, "  - ", ColoredText.ThreatColor);
            if (text != "")
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("allThreats", text)
                });
            }
            QuestGen.quest.AddPart(new QuestPart_LookAtThis(thing));
        }
        protected override bool TestRunInt(Slate slate)
        {
            return Find.Storyteller.difficulty.allowViolentQuests && Faction.OfMechanoids != null && slate.Get<Map>("map", null, false) != null;
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        [NoTranslate]
        public SlateRef<string> tag;
        public SlateRef<float?> points;
        public SlateRef<IntVec3?> dropSpot;
        public IntRange sleepyTime;
        [MustTranslate]
        public string DESC_description;
        [MustTranslate]
        public string nonDESC_description;
        [MustTranslate]
        public string HB_description;
        public ThingDef nonSW_droneDef;
        public ThingDef SW_droneDef;
        [MustTranslate]
        public string SW_description;
    }
    //drop the drone, drop the mech guards, and give them their AI Lord (as set by Descend in the quest node)
    public class QuestPart_DronePlusGuards : QuestPart
    {
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo globalTargetInfo in base.QuestLookTargets)
                {
                    yield return globalTargetInfo;
                }
                if (this.spawnedClusterPos.IsValid && this.mapParent != null && this.mapParent.HasMap)
                {
                    yield return new GlobalTargetInfo(this.spawnedClusterPos, this.mapParent.Map, false);
                }
                yield break;
            }
        }
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal && this.mapParent != null && this.mapParent.HasMap)
            {
                List<TargetInfo> list = new List<TargetInfo>();
                this.spawnedClusterPos = this.dropSpot;
                if (this.spawnedClusterPos == IntVec3.Invalid)
                {
                    this.TryFindSpacedronePosition(this.mapParent.Map, out this.spawnedClusterPos);
                }
                if (this.spawnedClusterPos == IntVec3.Invalid)
                {
                    return;
                }
                List<Pawn> spawnedPawns = new List<Pawn>();
                foreach (PawnKindDef pkd in this.pawnsToSpawn)
                {
                    spawnedPawns.Add(PawnGenerator.GeneratePawn(new PawnGenerationRequest(pkd, Faction.OfMechanoids, PawnGenerationContext.NonPlayer, this.mapParent.Tile, false, false, false, true, true, 1f, false, true, true, false, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false)));
                }
                List<Thing> toWakeUpOn = new List<Thing> {
                    this.drone
                };
                toWakeUpOn.AddRange(spawnedPawns);
                Lord lj;
                if (!this.DESC_on)
                {
                    lj = LordMaker.MakeNewLord(Faction.OfMechanoids, new LordJob_SleepThenMechanoidsDefendDrone(toWakeUpOn, Faction.OfMechanoids, 28f, this.spawnedClusterPos, false, false, this.sleepyTime * 2500), this.mapParent.Map, spawnedPawns);
                } else {
                    lj = LordMaker.MakeNewLord(Faction.OfMechanoids, new LordJob_MechanoidsDefend(toWakeUpOn, Faction.OfMechanoids, 14f, this.spawnedClusterPos, false, false), this.mapParent.Map, spawnedPawns);
                }
                if (this.drone is Building bdrone)
                {
                    lj.AddBuilding(bdrone);
                    bdrone.SetFaction(Faction.OfMechanoids);
                }
                DropPodUtility.DropThingsNear(this.spawnedClusterPos, this.mapParent.Map, spawnedPawns.Cast<Thing>(), 110, false, false, true, true, true, null);
                list.AddRange(spawnedPawns.Select((Pawn p) => new TargetInfo(p)));
                GenSpawn.Spawn(SkyfallerMaker.MakeSkyfaller(ThingDefOf.CrashedShipPartIncoming, this.drone), this.spawnedClusterPos, this.mapParent.Map, WipeMode.Vanish);
                list.Add(new TargetInfo(this.spawnedClusterPos, this.mapParent.Map, false));
                this.spawned = true;
                Find.LetterStack.ReceiveLetter("HVMP_DroneArrivedLabel".Translate(), "HVMP_DroneArrivedText".Translate(), LetterDefOf.ThreatBig, new TargetInfo(this.spawnedClusterPos, this.mapParent.Map, false), null, this.quest, null, null, 0, true);
            }
        }
        public bool TryFindSpacedronePosition(Map map, out IntVec3 spot)
        {
            IntVec2 size = this.drone.def.size;
            CellRect cellRect = GenAdj.OccupiedRect(IntVec3.Zero, this.drone.def.defaultPlacingRot, this.drone.def.size);
            IntVec3 intVec = cellRect.CenterCell + this.drone.def.interactionCellOffset;
            cellRect = cellRect.ExpandToFit(intVec);
            return DropCellFinder.FindSafeLandingSpot(out spot, null, map, 35, 15, 25, new IntVec2?(new IntVec2(cellRect.Width, cellRect.Height)), new IntVec3?(this.drone.def.interactionCellOffset));
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<PawnKindDef>(ref this.pawnsToSpawn, "pawnsToSpawn", LookMode.Def, Array.Empty<object>());
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_Values.Look<string>(ref this.hackingCompletedSignal, "hackingCompletedSignal", null, false);
            Scribe_Values.Look<string>(ref this.tag, "tag", null, false);
            Scribe_References.Look<MapParent>(ref this.mapParent, "mapParent", false);
            Scribe_Values.Look<IntVec3>(ref this.dropSpot, "dropSpot", default(IntVec3), false);
            Scribe_Values.Look<bool>(ref this.spawned, "spawned", false, false);
            Scribe_Values.Look<IntVec3>(ref this.spawnedClusterPos, "spawnedClusterPos", default(IntVec3), false);
            if (!this.spawned && (this.drone == null || !(this.drone is Pawn)))
            {
                Scribe_Deep.Look<Thing>(ref this.drone, "drone", Array.Empty<object>());
            }
            else
            {
                Scribe_References.Look<Thing>(ref this.drone, "drone", false);
            }
            Scribe_Values.Look<int>(ref this.sleepyTime, "sleepyTime", 2500, false);
            Scribe_Values.Look<bool>(ref this.DESC_on, "DESC_on", false, false);
        }
        public string hackingCompletedSignal;
        public Thing drone;
        public bool spawned;
        public List<PawnKindDef> pawnsToSpawn = new List<PawnKindDef>();
        public string inSignal;
        public string tag;
        public MapParent mapParent;
        public IntVec3 dropSpot = IntVec3.Invalid;
        private IntVec3 spawnedClusterPos = IntVec3.Invalid;
        public int sleepyTime;
        public bool DESC_on;
    }
    //as hackable, but once hacked the thing loses whatever faction it had (which is important for the Shipboard Weaponry drone variant, so it stops using its gun). Also, it provides a gizmo to look at the related quest.
    public class CompProperties_HackableQuestLink : CompProperties_Hackable
    {
        public CompProperties_HackableQuestLink()
        {
            this.compClass = typeof(CompHackableQuestLink);
        }
    }
    public class CompHackableQuestLink : CompHackable
    {
        public new CompProperties_HackableQuestLink Props
        {
            get
            {
                return (CompProperties_HackableQuestLink)this.props;
            }
        }
        protected override void OnHacked(Pawn hacker = null, bool suppressMessages = false)
        {
            base.OnHacked(hacker, suppressMessages);
            if (this.parent.Faction != null)
            {
                this.parent.SetFaction(null);
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            foreach (Gizmo gizmo3 in QuestUtility.GetQuestRelatedGizmos(this.parent))
            {
                yield return gizmo3;
            }
            yield break;
        }
    }
    //as MechanoidDefendBase, but the mechs only wake up after sleepyTime ticks elapses, or when the player harms any of the mechs
    public class LordJob_SleepThenMechanoidsDefendDrone : LordJob_MechanoidDefendBase
    {
        public override bool GuiltyOnDowned
        {
            get
            {
                return true;
            }
        }
        public LordJob_SleepThenMechanoidsDefendDrone()
        {
        }
        public LordJob_SleepThenMechanoidsDefendDrone(List<Thing> things, Faction faction, float defendRadius, IntVec3 defSpot, bool canAssaultColony, bool isMechCluster, int sleepyTime)
        {
            if (things != null)
            {
                this.things.AddRange(things);
            }
            this.faction = faction;
            this.defendRadius = defendRadius;
            this.defSpot = defSpot;
            this.canAssaultColony = canAssaultColony;
            this.isMechCluster = isMechCluster;
            this.sleepyTime = sleepyTime;
        }
        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            LordToil_Sleep lordToil_Sleep = new LordToil_Sleep();
            stateGraph.StartingToil = lordToil_Sleep;
            LordToil startingToil = stateGraph.AttachSubgraph(new LordJob_MechanoidsDefend(this.things, this.faction, this.defendRadius, this.defSpot, this.canAssaultColony, this.isMechCluster).CreateGraph()).StartingToil;
            Transition transition = new Transition(lordToil_Sleep, startingToil, false, true);
            transition.AddTrigger(new Trigger_DormancyWakeup());
            transition.AddTrigger(new Trigger_OnHumanlikeHarmAnyThing(this.things));
            transition.AddTrigger(new Trigger_OnPlayerMechHarmAnything(this.things));
            transition.AddTrigger(new Trigger_TicksPassed(this.sleepyTime));
            transition.AddPreAction(new TransitionAction_Message("MessageSleepingPawnsWokenUp".Translate(this.faction.def.pawnsPlural).CapitalizeFirst(), MessageTypeDefOf.ThreatBig, null, 1f, null));
            transition.AddPostAction(new TransitionAction_WakeAll());
            transition.AddPostAction(new TransitionAction_Custom(action: delegate
            {
                Find.SignalManager.SendSignal(new Signal("CompCanBeDormant.WakeUp", this.things.First<Thing>().Named("SUBJECT"), Faction.OfMechanoids.Named("FACTION")));
                SoundDefOf.MechanoidsWakeUp.PlayOneShot(new TargetInfo(this.defSpot, base.Map, false));
            }));
            stateGraph.AddTransition(transition, false);
            return stateGraph;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.sleepyTime, "sleepyTime", 2500, false);
        }
        public int sleepyTime;
    }
    //if turned HB_on (in the quest node) and not yet hacked, every periodicity ticks causes a mechanoid raid whose threat points are multiplied by pointFactor
    public class CompProperties_HB : CompProperties
    {
        public CompProperties_HB()
        {
            this.compClass = typeof(CompHB);
        }
        public int periodicity;
        public float pointFactor;
    }
    public class CompHB : ThingComp
    {
        public CompProperties_HB Props
        {
            get
            {
                return (CompProperties_HB)this.props;
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            this.HB_cooldown = this.Props.periodicity;
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.HB_on && this.parent.Spawned)
            {
                CompHackable ch = this.parent.GetComp<CompHackable>();
                if (ch != null && ch.IsHacked)
                {
                    this.HB_on = false;
                    return;
                }
                this.HB_cooldown -= delta;
                if (this.HB_cooldown <= 0)
                {
                    this.HB_cooldown = this.Props.periodicity;
                    IncidentParms incidentParms = new IncidentParms();
                    incidentParms.forced = true;
                    incidentParms.target = this.parent.MapHeld;
                    incidentParms.points = StorytellerUtility.DefaultThreatPointsNow(this.parent.MapHeld) * this.Props.pointFactor;
                    incidentParms.faction = Faction.OfMechanoids;
                    incidentParms.generateFightersOnly = true;
                    incidentParms.sendLetter = true;
                    IncidentDef incidentDef = IncidentDefOf.RaidEnemy;
                    incidentParms.points = Mathf.Max(incidentParms.points, Faction.OfMechanoids.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat, null));
                    if (incidentDef.Worker.CanFireNow(incidentParms))
                    {
                        incidentDef.Worker.TryExecute(incidentParms);
                    }
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.HB_on, "HB_on", false, false);
            Scribe_Values.Look<int>(ref this.HB_cooldown, "HB_cooldown", -1, false);
        }
        public bool HB_on;
        public int HB_cooldown;
    }
}
