using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Grammar;

namespace HautsPermits
{
    /*very veeeery similar to QuestNode_Root_ShuttleCrash_Rescue. We don't specify a pawnkind here, and they all get Hostile Environment Film because you shouldn't lose the quest just for living somewhere weird.
     * Soldiers' defs are not hardcoded, but rather XMLable in soldierDefs. You can XML maxCiviliansByPointsCurve and maxSoldiersByPointsCurve as well.
     * Basically, just generally less hardcoded, and doesn't assume the Empire is the asking faction.
     * Also handles all three mutators
     * icarus1: Every Expense Spared disables the generation of soldiers
     * icarus2: Hell On Their Heels multiplies the raid's threat points by HOTH_raidScalar_on and makes them arrive within HOTH_delay_on ticks (instead of HOTH_raidScalar_off and HOTH_delay_off, respectively)
     * icarus3: Revenge of the Space Junk causes QuestPart_ROTSJ, which will fire a random incident that has the ROTSJ_allowedIncidentTag tag (enforcing min 500 threat points). If you have a mod that adds another ship part incident, add it to the list!*/
    public class QuestNode_Icarus : QuestNode
    {
        private static QuestGen_Pawns.GetPawnParms CivilianPawnParams
        {
            get
            {
                return new QuestGen_Pawns.GetPawnParms
                {
                    mustBeOfFaction = QuestGen.slate.Get<Faction>("faction", null, false),
                    canGeneratePawn = true,
                    mustBeWorldPawn = true
                };
            }
        }
        protected override void RunInt()
        {
            if (!ModLister.CheckRoyalty("Shuttle crash rescue"))
            {
                return;
            }
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            Map map = QuestGen_Get.GetMap(false, null, false);
            Faction faction = QuestGen.slate.Get<Faction>("faction", null, false);
            this.TryFindEnemyFaction(out Faction enemyFaction, faction);
            float questPoints = Math.Min(slate.Get<float>("points", 500f, false), 500f);
            slate.Set<Map>("map", map, false);
            slate.Set<int>("rescueDelay", 20000, false);
            slate.Set<int>("leaveDelay", 30000, false);
            slate.Set<int>("rescueShuttleAfterRaidDelay", 10000, false);
            string text = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("civilian");
            int num = Mathf.FloorToInt(this.maxCiviliansByPointsCurve.Evaluate(questPoints));
            Thing crashedShuttle = ThingMaker.MakeThing(this.shuttleDef, null);
            this.TryFindShuttleCrashPosition(map, faction, crashedShuttle.def.size, new IntVec3?(crashedShuttle.def.interactionCellOffset), out IntVec3 shuttleCrashPosition);
            List<Pawn> civilians = new List<Pawn>();
            List<Pawn> list = new List<Pawn>();
            for (int i = 0; i < Rand.Range(1, num) - 1; i++)
            {
                Pawn pawn = quest.GetPawn(QuestNode_Icarus.CivilianPawnParams);
                QuestUtility.AddQuestTag(ref pawn.questTags, text);
                pawn.health.AddHediff(HVMPDefOf.HVMP_HostileEnvironmentFilm, null);
                civilians.Add(pawn);
                list.Add(pawn);
            }
            Pawn asker = quest.GetPawn(new QuestGen_Pawns.GetPawnParms
            {
                mustBeOfFaction = faction,
                canGeneratePawn = true,
                mustBeWorldPawn = true,
                seniorityRange = new FloatRange(0f),
                mustHaveRoyalTitleInCurrentFaction = false
            });
            asker.health.AddHediff(HVMPDefOf.HVMP_HostileEnvironmentFilm, null);
            civilians.Add(asker);
            List<Pawn> soldiers = new List<Pawn>();
            bool mayhemMode = HVMP_Mod.settings.icarusX;
            int soldierCount = BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.icarus1, mayhemMode) ? 0 : Rand.Range(1, Mathf.FloorToInt(this.maxSoldiersByPointsCurve.Evaluate(questPoints)));
            for (int j = 0; j < soldierCount; j++)
            {
                Pawn pawn2 = quest.GetPawn(new QuestGen_Pawns.GetPawnParms
                {
                    mustBeOfFaction = faction,
                    canGeneratePawn = true,
                    mustBeOfKind = this.soldierDefs.RandomElement(),
                    mustBeWorldPawn = true,
                    mustBeCapableOfViolence = true
                });
                pawn2.health.AddHediff(HVMPDefOf.HVMP_HostileEnvironmentFilm, null);
                soldiers.Add(pawn2);
            }
            List<Pawn> allPassengers = new List<Pawn>();
            allPassengers.AddRange(soldiers);
            allPassengers.AddRange(civilians);
            quest.BiocodeWeapons(allPassengers, null);
            Thing rescueShuttle = QuestGen_Shuttle.GenerateShuttle(Faction.OfEmpire, allPassengers, null, false, false, false, -1, false, false, false, false, null, null, -1, null, false, true, false, false, false);
            string text2 = QuestGenUtility.HardcodedSignalWithQuestID("soldiers.Rescued");
            quest.RemoveFromRequiredPawnsOnRescue(rescueShuttle, soldiers, text2);
            quest.Delay(120, delegate
            {
                quest.Letter(LetterDefOf.NegativeEvent, null, null, null, null, false, QuestPart.SignalListenMode.OngoingOnly, Gen.YieldSingle<Thing>(crashedShuttle), false, "LetterTextShuttleCrashed".Translate(), null, "LetterLabelShuttleCrashed".Translate(), null, null);
                quest.SpawnSkyfaller(map, ThingDefOf.ShuttleCrashing, Gen.YieldSingle<Thing>(crashedShuttle), Faction.OfPlayer, new IntVec3?(shuttleCrashPosition), null, false, false, null, null);
                IEnumerable<Thing> allPassengers2 = allPassengers;
                IntVec3? intVec = new IntVec3?(shuttleCrashPosition);
                quest.DropPods(map.Parent, allPassengers2, null, null, null, null, new bool?(false), false, false, false, null, null, QuestPart.SignalListenMode.OngoingOnly, intVec, true, false, false, false, null);
                quest.DefendPoint(map.Parent, asker, shuttleCrashPosition, soldiers, faction, null, null, new float?((float)12), false, false);
                IntVec3 intVec2 = shuttleCrashPosition + IntVec3.South;
                quest.WaitForEscort(map.Parent, civilians, faction, intVec2, null, false);
                string text17 = QuestGenUtility.HardcodedSignalWithQuestID("rescueShuttle.Spawned");
                quest.ExitOnShuttle(map.Parent, allPassengers, faction, rescueShuttle, text17, false);
                Quest quest3 = quest;
                int num5 = 20000;
                IEnumerable<Pawn> civilians2 = civilians;
                Action action = (delegate
                {
                    quest.Letter(LetterDefOf.NeutralEvent, null, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, Gen.YieldSingle<Thing>(rescueShuttle), false, "[rescueShuttleArrivedLetterText]", null, "[rescueShuttleArrivedLetterLabel]", null, null);
                    TransportShip transportShip = quest.GenerateTransportShip(TransportShipDefOf.Ship_Shuttle, null, rescueShuttle, null).transportShip;
                    quest.SendTransportShipAwayOnCleanup(transportShip, false, TransportShipDropMode.NonRequired);
                    IntVec3 intVec3;
                    DropCellFinder.TryFindDropSpotNear(shuttleCrashPosition, map, out intVec3, false, false, false, new IntVec2?(ThingDefOf.Shuttle.Size + new IntVec2(2, 2)), false);
                    quest.AddShipJob_Arrive(transportShip, map.Parent, null, new IntVec3?(intVec3), ShipJobStartMode.Instant, faction, null);
                    quest.AddShipJob_WaitTime(transportShip, 30000, true, allPassengers.Cast<Thing>().ToList<Thing>(), null);
                    quest.ShuttleLeaveDelay(rescueShuttle, 30000, null, null, null, null);
                    quest.AddShipJob_FlyAway(transportShip, null, null, TransportShipDropMode.None, null);
                });
                quest3.ShuttleDelay(num5, civilians2, action, null, null, true);
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.icarus3, mayhemMode) && this.ROTSJ_allowedIncidentTag != null)
                {
                    IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
                    incidentParms.forced = true;
                    if (incidentParms.points < 500)
                    {
                        incidentParms.points = 500;
                    }
                    List<IncidentDef> ids = DefDatabase<IncidentDef>.AllDefsListForReading.Where((IncidentDef incident) => incident.tags != null && incident.tags.Contains(this.ROTSJ_allowedIncidentTag) && incident.Worker.CanFireNow(incidentParms)).ToList();
                    if (ids.Count > 0)
                    {
                        QuestPart_Icarus_ROTSJ qpROTSJ = new QuestPart_Icarus_ROTSJ();
                        qpROTSJ.ROTSJ_incident = ids.RandomElement();
                        qpROTSJ.map = QuestGen.slate.Get<Map>("map", null, false);
                        qpROTSJ.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(quest.InitiateSignal);
                        quest.AddPart(qpROTSJ);
                        QuestGen.AddQuestDescriptionRules(new List<Rule>
                        {
                            new Rule_String("mutator_ROTSJ_info", this.ROTSJ_description.Formatted())
                        });
                    }
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_ROTSJ_info", " ") });
                }
                this.TryFindRaidWalkInPosition(map, shuttleCrashPosition, out IntVec3 walkIntSpot);
                float soldiersTotalCombatPower = 0f;
                for (int k = 0; k < soldiers.Count; k++)
                {
                    soldiersTotalCombatPower += soldiers[k].kindDef.combatPower;
                }
                int raidDelay = this.HOTH_delay_off.RandomInRange;
                float raidScalar = this.HOTH_raidScalar_off;
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.icarus2, mayhemMode))
                {
                    raidScalar = this.HOTH_raidScalar_on;
                    raidDelay = this.HOTH_delay_on.RandomInRange;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_HOTH_info", this.HOTH_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_HOTH_info", " ") });
                }
                quest.Delay(raidDelay, delegate
                {
                    List<Pawn> list2 = PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
                    {
                        faction = enemyFaction,
                        groupKind = PawnGroupKindDefOf.Combat,
                        points = Math.Max(questPoints * raidScalar, enemyFaction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat, null) * 1.05f),
                        generateFightersOnly = true,
                        tile = map.Tile
                    }, true).ToList<Pawn>();
                    for (int l = 0; l < list2.Count; l++)
                    {
                        Find.WorldPawns.PassToWorld(list2[l], PawnDiscardDecideMode.Decide);
                        QuestGen.AddToGeneratedPawns(list2[l]);
                    }
                    QuestPart_PawnsArrive questPart_PawnsArrive = new QuestPart_PawnsArrive();
                    questPart_PawnsArrive.pawns.AddRange(list2);
                    questPart_PawnsArrive.arrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                    questPart_PawnsArrive.joinPlayer = false;
                    questPart_PawnsArrive.mapParent = map.Parent;
                    questPart_PawnsArrive.spawnNear = walkIntSpot;
                    questPart_PawnsArrive.inSignal = QuestGen.slate.Get<string>("inSignal", null, false);
                    questPart_PawnsArrive.sendStandardLetter = false;
                    quest.AddPart(questPart_PawnsArrive);
                    quest.AssaultThings(map.Parent, list2, enemyFaction, allPassengers, null, null, true);
                    quest.Letter(LetterDefOf.ThreatBig, null, null, enemyFaction, null, false, QuestPart.SignalListenMode.OngoingOnly, list2, false, "[raidArrivedLetterText]", null, "[raidArrivedLetterLabel]", null, null);
                }, null, null, null, false, null, null, false, null, null, "RaidDelay", false, QuestPart.SignalListenMode.OngoingOnly, false);
            }, null, null, null, false, null, null, false, null, null, null, false, QuestPart.SignalListenMode.OngoingOnly, false);
            string text3 = QuestGenUtility.HardcodedSignalWithQuestID("rescueShuttle.SentSatisfied");
            string text4 = QuestGenUtility.HardcodedSignalWithQuestID("rescueShuttle.SentUnsatisfied");
            string[] array = new string[] { text3, text4 };
            string text5 = QuestGenUtility.HardcodedSignalWithQuestID("rescueShuttle.Destroyed");
            string text6 = QuestGenUtility.HardcodedSignalWithQuestID("rescueShuttle.LeftBehind");
            string text7 = QuestGenUtility.HardcodedSignalWithQuestID("asker.Destroyed");
            string text8 = QuestGenUtility.HardcodedSignalWithQuestID("civilian.Destroyed");
            string text9 = QuestGenUtility.HardcodedSignalWithQuestID("map.MapRemoved");
            quest.GoodwillChangeShuttleSentThings(faction, list, -5, null, array, text5, HistoryEventDefOf.ShuttleGuardsMissedShuttle, true, false, QuestPart.SignalListenMode.Always);
            quest.GoodwillChangeShuttleSentThings(faction, Gen.YieldSingle<Pawn>(asker), -10, null, array, text5, HistoryEventDefOf.ShuttleCommanderMissedShuttle, true, false, QuestPart.SignalListenMode.Always);
            quest.Leave(allPassengers, "", false, true, null, false);
            quest.Letter(LetterDefOf.PositiveEvent, text3, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, null, false, "[questCompletedSuccessLetterText]", null, "[questCompletedSuccessLetterLabel]", null, null);
            string questSuccess = QuestGen.GenerateNewSignal("QuestSuccess", true);
            quest.SignalPass(null, text3, questSuccess);
            quest.AnyOnTransporter(allPassengers, rescueShuttle, delegate
            {
                Quest quest5 = quest;
                IEnumerable<Pawn> enumerable = Gen.YieldSingle<Pawn>(asker);
                Thing rescueShuttle2 = rescueShuttle;
                Action action2 = (delegate
                {
                    quest.Letter(LetterDefOf.PositiveEvent, null, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, null, false, "[questCompletedCiviliansLostSuccessLetterText]", null, "[questCompletedCiviliansLostSuccessLetterLabel]", null, null);
                    quest.SignalPass(null, null, questSuccess);
                });
                Action action3 = (delegate
                {
                    quest.Letter(LetterDefOf.NegativeEvent, null, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, null, false, "[askerLostLetterText]", null, "[askerLostLetterLabel]", null, null);
                    quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, false, false);
                });
                quest5.AnyOnTransporter(enumerable, rescueShuttle2, action2, action3, null, null, null, QuestPart.SignalListenMode.OngoingOnly);
            }, delegate {
                quest.Letter(LetterDefOf.NegativeEvent, null, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, null, false, "[allLostLetterText]", null, "[allLostLetterLabel]", null, null);
                quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, false, false);
            }, text4, null, null, QuestPart.SignalListenMode.OngoingOnly);
            quest.Letter(LetterDefOf.NegativeEvent, text7, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, Gen.YieldSingle<Pawn>(asker), false, "[askerDiedLetterText]", null, "[askerDiedLetterLabel]", null, null);
            quest.End(QuestEndOutcome.Fail, 0, null, text7, QuestPart.SignalListenMode.OngoingOnly, false, false);
            quest.Letter(LetterDefOf.NegativeEvent, text8, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, civilians, false, "[civilianDiedLetterText]", null, "[civilianDiedLetterLabel]", null, null);
            quest.End(QuestEndOutcome.Fail, 0, null, text8, QuestPart.SignalListenMode.OngoingOnly, false, false);
            quest.Letter(LetterDefOf.NegativeEvent, text5, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, Gen.YieldSingle<Thing>(rescueShuttle), false, "[shuttleDestroyedLetterText]", null, "[shuttleDestroyedLetterLabel]", null, null);
            quest.End(QuestEndOutcome.Fail, 0, null, text5, QuestPart.SignalListenMode.OngoingOnly, false, false);
            quest.Letter(LetterDefOf.NegativeEvent, text6, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, Gen.YieldSingle<Thing>(rescueShuttle), false, "[shuttleLeftBehindLetterText]", null, "[shuttleLeftBehindLetterLabel]", null, null);
            quest.End(QuestEndOutcome.Fail, 0, null, text6, QuestPart.SignalListenMode.OngoingOnly, false, false);
            quest.End(QuestEndOutcome.Fail, 0, null, QuestGenUtility.HardcodedSignalWithQuestID("asker.LeftMap"), QuestPart.SignalListenMode.OngoingOnly, true, false);
            string text10 = QuestGenUtility.HardcodedSignalWithQuestID("askerFaction.BecameHostileToPlayer");
            quest.End(QuestEndOutcome.Fail, 0, null, text10, QuestPart.SignalListenMode.OngoingOnly, true, false);
            quest.End(QuestEndOutcome.InvalidPreAcceptance, 0, null, text10, QuestPart.SignalListenMode.NotYetAcceptedOnly, false, false);
            quest.Letter(LetterDefOf.NegativeEvent, text9, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, Gen.YieldSingle<Pawn>(asker), false, "[mapRemovedLetterText]", null, "[mapRemovedLetterLabel]", null, null);
            quest.End(QuestEndOutcome.Fail, 0, null, text9, QuestPart.SignalListenMode.OngoingOnly, false, false);
            slate.Set<Pawn>("asker", asker, false);
            slate.Set<Faction>("askerFaction", faction, false);
            slate.Set<Faction>("enemyFaction", enemyFaction, false);
            slate.Set<List<Pawn>>("soldiers", soldiers, false);
            slate.Set<List<Pawn>>("civilians", civilians, false);
            slate.Set<int>("civilianCountMinusOne", civilians.Count - 1, false);
            slate.Set<Thing>("rescueShuttle", rescueShuttle, false);
        }
        private bool TryFindEnemyFaction(out Faction enemyFaction, Faction faction)
        {
            return Find.FactionManager.AllFactionsVisible.Where((Faction f) => f.HostileTo(faction) && f.HostileTo(Faction.OfPlayer)).TryRandomElement(out enemyFaction);
        }
        private bool TryFindShuttleCrashPosition(Map map, Faction faction, IntVec2 size, IntVec3? interactionCell, out IntVec3 spot)
        {
            return DropCellFinder.FindSafeLandingSpot(out spot, faction, map, 35, 15, 25, new IntVec2?(size), interactionCell);
        }
        private bool TryFindRaidWalkInPosition(Map map, IntVec3 shuttleCrashSpot, out IntVec3 spawnSpot)
        {
            Predicate<IntVec3> predicate = (IntVec3 p) => (map.TileInfo.AllowRoofedEdgeWalkIn || !map.roofGrid.Roofed(p)) && p.Walkable(map) && map.reachability.CanReach(p, shuttleCrashSpot, PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Some);
            if (RCellFinder.TryFindEdgeCellFromPositionAvoidingColony(shuttleCrashSpot, map, predicate, out spawnSpot))
            {
                return true;
            }
            if (CellFinder.TryFindRandomEdgeCellWith(predicate, map, CellFinder.EdgeRoadChance_Hostile, out spawnSpot))
            {
                return true;
            }
            spawnSpot = IntVec3.Invalid;
            return false;
        }
        protected override bool TestRunInt(Slate slate)
        {
            if (!Find.Storyteller.difficulty.allowViolentQuests)
            {
                return false;
            }
            Faction faction = QuestGen.slate.Get<Faction>("faction", null, false);
            if (faction == null)
            {
                BranchQuestSetupUtility.TryFindPaxFaction(out faction);
            }
            if (faction == null)
            {
                return false;
            }
            Pawn pawn;
            if (!QuestGen_Pawns.GetPawnTest(QuestNode_Icarus.CivilianPawnParams, out pawn))
            {
                return false;
            }
            Faction enemyFaction;
            if (!this.TryFindEnemyFaction(out enemyFaction, faction))
            {
                return false;
            }
            Map map = QuestGen_Get.GetMap(false, null, true);
            return map != null && this.TryFindShuttleCrashPosition(map, faction, ThingDefOf.ShuttleCrashed.size, new IntVec3?(ThingDefOf.ShuttleCrashed.interactionCellOffset), out IntVec3 intVec) && this.TryFindRaidWalkInPosition(map, intVec, out IntVec3 intVec2);
        }
        public FactionDef faction;
        public SimpleCurve maxCiviliansByPointsCurve;
        public SimpleCurve maxSoldiersByPointsCurve;
        public List<PawnKindDef> soldierDefs;
        public ThingDef shuttleDef;
        public float HOTH_raidScalar_on;
        public float HOTH_raidScalar_off;
        public IntRange HOTH_delay_on;
        public IntRange HOTH_delay_off;
        [MustTranslate]
        public string HOTH_description;
        public string ROTSJ_allowedIncidentTag;
        [MustTranslate]
        public string ROTSJ_description;
    }
    public class QuestPart_Icarus_ROTSJ : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                if (this.ROTSJ_incident != null)
                {
                    IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
                    incidentParms.forced = true;
                    if (incidentParms.points < 500)
                    {
                        incidentParms.points = 500;
                    }
                    Find.Storyteller.incidentQueue.Add(this.ROTSJ_incident, Find.TickManager.TicksGame + 60, incidentParms, 0);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<IncidentDef>(ref this.ROTSJ_incident, "ROTSJ_incident");
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_References.Look<Map>(ref this.map, "map", false);
        }
        public IncidentDef ROTSJ_incident;
        public string inSignal;
        public Map map;
    }
}
