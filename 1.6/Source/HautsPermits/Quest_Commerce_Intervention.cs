using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Grammar;

namespace HautsPermits
{
    /*based on QuestNode_Raid, but you can specify the incident that fires, and the faction is always insects.
     * Also handles two of the mutators, which will be implemented in QuestPart_OtherTwoInterventionMutators
     * interv2: Infestation Infection - gives the bugs II_hediff
     * interv3: Jitterbugs - gives the bugs JB_hediff*/
    public class QuestNode_InterventionInner : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return Find.Storyteller.difficulty.allowViolentQuests && slate.Exists("map", false) && Faction.OfInsects != null;
        }
        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            Map map = slate.Get<Map>("map", null, false);
            float num = slate.Get<float>("points", 0f, false);
            Faction faction = Faction.OfInsects;
            QuestPart_Incident questPart_Incident = new QuestPart_Incident();
            questPart_Incident.debugLabel = "raid";
            questPart_Incident.incident = this.incidentDef;
            this.ImplementQuestMutators(slate, quest);
            IncidentParms incidentParms;
            incidentParms = this.GenerateIncidentParms(map, num, faction, slate, quest, questPart_Incident);
            questPart_Incident.SetIncidentParmsAndRemoveTarget(incidentParms);
            questPart_Incident.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
            quest.AddPart(questPart_Incident);
        }
        private IncidentParms GenerateIncidentParms(Map map, float points, Faction faction, Slate slate, Quest quest, QuestPart_Incident questPart)
        {
            IncidentParms incidentParms = new IncidentParms();
            incidentParms.forced = true;
            incidentParms.target = map;
            incidentParms.points = slate.Get<float>("points", 0f, false);
            incidentParms.faction = faction;
            incidentParms.pawnGroupMakerSeed = new int?(Rand.Int);
            incidentParms.inSignalEnd = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignalLeave.GetValue(slate));
            incidentParms.questTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(this.tag.GetValue(slate));
            incidentParms.quest = quest;
            incidentParms.canTimeoutOrFlee = false;
            return incidentParms;
        }
        public void ImplementQuestMutators(Slate slate, Quest quest)
        {
            QuestPart_OtherTwoInterventionMutators qpa3 = new QuestPart_OtherTwoInterventionMutators();
            bool mayhemMode = HVMP_Mod.settings.intervX;
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.interv2, mayhemMode))
            {
                qpa3.II_hediff = this.II_hediff;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_II_info", this.II_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_II_info", " ") });
            }
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.interv3, mayhemMode))
            {
                qpa3.JB_hediff = this.JB_hediff;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_JB_info", this.JB_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_JB_info", " ") });
            }
            quest.AddPart(qpa3);
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        [NoTranslate]
        public SlateRef<string> inSignalLeave;
        [NoTranslate]
        public SlateRef<string> tag;
        public IncidentDef incidentDef;
        public HediffDef II_hediff;
        [MustTranslate]
        public string II_description;
        public HediffDef JB_hediff;
        [MustTranslate]
        public string JB_description;
    }
    /*Burning, Unadulterated Burning is alphabetically the first mutator, hence why it's listed here first and the other quest part is called the "OtherTwoInterventionMutators".
     * When triggered, the quest part inflicts BUB_hediff on all non-insect pawns on the map, and makes wild animals that aren't already in a mental state flee the map.*/
    public class QuestNode_Intervention_BUB : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return this.BUB_hediff != null && slate.Exists("map", false);
        }
        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.interv1, HVMP_Mod.settings.intervX))
            {
                QuestPart_Intervention_BUB qpBUB = new QuestPart_Intervention_BUB();
                qpBUB.BUB_hediff = this.BUB_hediff;
                qpBUB.map = QuestGen.slate.Get<Map>("map", null, false);
                qpBUB.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(quest.InitiateSignal);
                quest.AddPart(qpBUB);
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_BUB_info", this.BUB_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_BUB_info", " ") });
            }
        }
        public HediffDef BUB_hediff;
        [MustTranslate]
        public string BUB_description;
    }
    public class QuestPart_Intervention_BUB : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                if (this.BUB_hediff != null)
                {
                    foreach (Pawn p in this.map.mapPawns.AllPawnsSpawned)
                    {
                        if (!p.RaceProps.Insect)
                        {
                            p.health.AddHediff(this.BUB_hediff);
                            if (TameUtility.CanTame(p) && !p.InMentalState)
                            {
                                p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, null, false, true, false, null, false, false, false);
                            }
                        }
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<HediffDef>(ref this.BUB_hediff, "BUB_hediff");
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_References.Look<Map>(ref this.map, "map", false);
        }
        public HediffDef BUB_hediff;
        public string inSignal;
        public Map map;
    }
    //as SeverityPerDay, but its rate of decay is improved the higher that the pawn's adjustingStat is
    public class HediffCompProperties_SeverityPerDay_BUB : HediffCompProperties_SeverityPerDay
    {
        public HediffCompProperties_SeverityPerDay_BUB()
        {
            this.compClass = typeof(HediffComp_SeverityPerDay_BUB);
        }
        public StatDef adjustingStat;
    }
    public class HediffComp_SeverityPerDay_BUB : HediffComp_SeverityPerDay
    {
        private HediffCompProperties_SeverityPerDay_BUB Props
        {
            get
            {
                return (HediffCompProperties_SeverityPerDay_BUB)this.props;
            }
        }
        public override float SeverityChangePerDay()
        {
            return base.SeverityChangePerDay() * (1f + this.Pawn.GetStatValue(this.Props.adjustingStat));
        }
    }
    //as explained in the comments for QuestNode_InterventionInner. Stores II_hediff and JB_hediff for TunnelHiveSpawner_Intervention to reference later
    public class QuestPart_OtherTwoInterventionMutators : QuestPart
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<HediffDef>(ref this.II_hediff, "II_hediff");
            Scribe_Defs.Look<HediffDef>(ref this.JB_hediff, "JB_hediff");
        }
        public HediffDef II_hediff;
        public HediffDef JB_hediff;
    }
    //worker for the actual incident. Find a random spot to put down some infestation spawners. They don't need to be under roofs or whatever insects normally like, just somewhere standable away from colony buildings
    public class IncidentWorker_InterventionInfestation : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
            {
                return false;
            }
            if (Faction.OfInsects == null)
            {
                return false;
            }
            return true;
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 intVec;
            Func<IntVec3, bool> validator = delegate (IntVec3 x)
            {
                if (!x.Standable(map) || x.Fogged(map))
                {
                    return false;
                }
                return true;
            };
            IntVec3 loc;
            Faction hostFaction = map.ParentFaction ?? Faction.OfPlayer;
            IEnumerable<Thing> enumerable = map.mapPawns.FreeHumanlikesSpawnedOfFaction(hostFaction).Cast<Thing>();
            if (hostFaction == Faction.OfPlayer)
            {
                enumerable = enumerable.Concat(map.listerBuildings.allBuildingsColonist.Cast<Thing>());
            } else {
                enumerable = enumerable.Concat(from x in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
                                               where x.Faction == hostFaction
                                               select x);
            }
            int num = 0;
            float num2 = 65f;
            for (; ; )
            {
                intVec = CellFinder.RandomCell(map);
                num++;
                if (!intVec.Fogged(map) && intVec.Standable(map))
                {
                    if (num > 300)
                    {
                        break;
                    }
                    num2 -= 0.2f;
                    bool flag = false;
                    foreach (Thing thing in enumerable)
                    {
                        if ((float)(intVec - thing.Position).LengthHorizontalSquared < num2 * num2)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag && map.reachability.CanReachFactionBase(intVec, hostFaction))
                    {
                        loc = intVec;
                    }
                }
            }
            loc = intVec;
            TunnelHiveSpawner_Intervention tunnelHiveSpawner = (TunnelHiveSpawner_Intervention)ThingMaker.MakeThing(HVMPDefOf.HVMP_TunnelHiveSpawner, null);
            tunnelHiveSpawner.spawnHive = false;
            tunnelHiveSpawner.quest = parms.quest;
            tunnelHiveSpawner.insectsPoints = parms.points * Rand.Range(0.3f, 0.6f);
            tunnelHiveSpawner.spawnedByInfestationThingComp = true;
            GenSpawn.Spawn(tunnelHiveSpawner, loc, map, WipeMode.FullRefund);
            base.SendStandardLetter(parms, new TargetInfo(tunnelHiveSpawner.Position, map, false), Array.Empty<NamedArgument>());
            return true;
        }
    }
    //the bugs are spawned via a derivative of TunnelHiveSpawner which, if the relevant mutators are active for this instance of the Intervention quest, inflict the II and JB hediffs on spawned pawns
    public class TunnelHiveSpawner_Intervention : TunnelHiveSpawner
    {
        protected override void Spawn(Map map, IntVec3 loc)
        {
            if (this.spawnHive)
            {
                HiveUtility.SpawnHive(loc, map, WipeMode.FullRefund, false, true, true, false, true, true, false).questTags = this.questTags;
            }
            if (this.insectsPoints > 0f)
            {
                this.insectsPoints = Mathf.Max(this.insectsPoints, Hive.spawnablePawnKinds.Min((PawnKindDef x) => x.combatPower));
                float pointsLeft = this.insectsPoints;
                List<Pawn> list = new List<Pawn>();
                int num = 0;
                HediffDef iiHediff = null, jbHediff = null;
                QuestPart_OtherTwoInterventionMutators qpa3 = this.quest.GetFirstPartOfType<QuestPart_OtherTwoInterventionMutators>();
                if (qpa3 != null)
                {
                    iiHediff = qpa3.II_hediff;
                    jbHediff = qpa3.JB_hediff;
                }
                while (pointsLeft > 0f)
                {
                    num++;
                    if (num > 1000)
                    {
                        Log.Error("Too many iterations.");
                        break;
                    }
                    IEnumerable<PawnKindDef> spawnablePawnKinds = Hive.spawnablePawnKinds;
                    if (!spawnablePawnKinds.Where((PawnKindDef x) => x.combatPower <= pointsLeft).TryRandomElement(out PawnKindDef pawnKindDef))
                    {
                        break;
                    }
                    Pawn pawn = PawnGenerator.GeneratePawn(pawnKindDef, Faction.OfInsects, null);
                    GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(loc, map, 2, null), map, WipeMode.Vanish);
                    pawn.mindState.spawnedByInfestationThingComp = this.spawnedByInfestationThingComp;
                    if (iiHediff != null)
                    {
                        pawn.health.AddHediff(iiHediff);
                    }
                    if (jbHediff != null)
                    {
                        pawn.health.AddHediff(jbHediff);
                    }
                    list.Add(pawn);
                    pointsLeft -= pawnKindDef.combatPower;
                    if (ModsConfig.BiotechActive)
                    {
                        PollutionUtility.Notify_TunnelHiveSpawnedInsect(pawn);
                    }
                }
                if (list.Any<Pawn>())
                {
                    LordMaker.MakeNewLord(Faction.OfInsects, new LordJob_AssaultColony(Faction.OfInsects, true, false, false, false, true, false, false), map, list);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Quest>(ref this.quest, "quest", false);
        }
        public Quest quest;
    }
    /*the Infestation Infection hediff uses this class so that any damage the pawn does can inflict scaria infection.
     * (Ostensibly. This is the same mechanism scaria uses to inflict scaria infection, just not reliant on bites or scratches specifically;
     * however, I've literally never seen scaria infection occur from either this hediff or scaria itself. Maybe it's just ultra unlikely)*/
    public class Hediff_ScariaVector : Hediff
    {
        public override void Notify_PawnDamagedThing(Thing thing, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            if (result.hediffs.NullOrEmpty<Hediff>())
            {
                return;
            }
            foreach (Hediff hediff in result.hediffs)
            {
                HediffComp_Infecter hediffComp_Infecter = hediff.TryGetComp<HediffComp_Infecter>();
                if (hediffComp_Infecter != null)
                {
                    hediffComp_Infecter.fromScaria = true;
                }
            }
        }
    }
}
