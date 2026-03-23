using HautsPermits;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Grammar;

namespace HautsPermits_Occult
{
    //does exactly what it says on the tin, then stuffs it into a QuestPart_InvolvedFactions. Defaults to any hostile faction if the Cult of Horax is not present in your playthru for some reason
    public class QuestNode_GetHoraxFaction : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return Find.FactionManager.FirstFactionOfDef(FactionDefOf.HoraxCult) != null;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.HoraxCult);
            if (faction == null)
            {
                this.TryFindFaction(out faction, slate);
            }
            if (faction != null)
            {
                QuestGen.slate.Set<Faction>(this.storeAs.GetValue(slate), faction, false);
                if (!faction.Hidden)
                {
                    QuestPart_InvolvedFactions questPart_InvolvedFactions = new QuestPart_InvolvedFactions();
                    questPart_InvolvedFactions.factions.Add(faction);
                    QuestGen.quest.AddPart(questPart_InvolvedFactions);
                }
            }
            slate.Set<PawnsArrivalModeDef>(this.storeArrivalModeAs.GetValue(slate), HVMPDefOf_A.HVMP_UsherArrival, false);
        }
        private bool TryFindFaction(out Faction faction, Slate slate)
        {
            return (from x in Find.FactionManager.GetFactions(true, false, true, TechLevel.Undefined, false)
                    where this.IsGoodFaction(x, slate)
                    select x).TryRandomElement(out faction);
        }
        private bool IsGoodFaction(Faction faction, Slate slate)
        {
            return faction.HostileTo(Faction.OfPlayer);
        }
        [NoTranslate]
        public SlateRef<string> storeAs;
        [NoTranslate]
        public SlateRef<string> storeArrivalModeAs;
    }
    /*generate a HVMP_UsherStrike incident (usherKind is the pawn kind spawned). Also weaves the mutators into it
     * wells1: First Strike gives the spawned usherKind pawns FS_hediff
     * wells2: Never Say Die gives the spawned usherKind pawns NSD_hediff; this should be a Hediff_RapidRegeneration, so that its HP capacity can be set to NSD_hpThreshold
     * wells3: Tandemonium has a TANDY_bonusUsherChance to spawn +1 usherKind pawn, oooooor if you turned on TANDY_bonusUsherChanceCanExplode in the XML, up to +9 via additional rolls on each success.
     *   It also spawns a random number within TANDY_bonusPawnCount of a random pawn kind within TANDY_bonusPawnTypes*/
    public class QuestNode_Wells : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return Find.Storyteller.difficulty.allowViolentQuests && slate.Exists("map", false);
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;
            Map map = QuestGen.slate.Get<Map>("map", null, false);
            float num = QuestGen.slate.Get<float>("points", 0f, false);
            Faction faction = Faction.OfHoraxCult ?? slate.Get<Faction>("enemyFaction", null, false);
            QuestPart_Incident questPart_Incident = new QuestPart_Incident
            {
                debugLabel = "raid",
                incident = HVMPDefOf.HVMP_UsherStrike
            };
            bool mayhemMode = HVMP_Mod.settings.wellsX;
            bool tandemonium = BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.wells3, mayhemMode);
            int usherCount = 1;
            if (tandemonium)
            {
                if (this.TANDY_bonusUsherChanceCanExplode)
                {
                    while (Rand.Chance(this.TANDY_bonusUsherChance) && usherCount < 10)
                    {
                        usherCount++;
                    }
                } else if (Rand.Chance(this.TANDY_bonusUsherChance)) {
                    usherCount++;
                }
            }
            IncidentParms incidentParms = this.GenerateIncidentParms(map, num, usherCount, faction, slate, questPart_Incident);
            questPart_Incident.SetIncidentParmsAndRemoveTarget(incidentParms);
            questPart_Incident.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
            quest.AddPart(questPart_Incident);
            QuestPart_WellsMutators qpwm = new QuestPart_WellsMutators();
            qpwm.usherKind = this.usherKind;
            qpwm.usherCount = usherCount;
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.wells1, mayhemMode))
            {
                qpwm.FS_hediff = this.FS_hediff;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_FS_info", this.FS_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_FS_info", " ") });
            }
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.wells2, mayhemMode))
            {
                qpwm.NSD_hediff = this.NSD_hediff;
                qpwm.NSD_hpThreshold = this.NSD_hpThreshold;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_NSD_info", this.NSD_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_NSD_info", " ") });
            }
            if (tandemonium)
            {
                qpwm.TANDY_pawnCount = this.TANDY_bonusPawnCount.RandomInRange;
                qpwm.TANDY_pawnType = this.TANDY_bonusPawnTypes.RandomElement();
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_TANDY_info", this.TANDY_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_TANDY_info", " ") });
            }
            quest.AddPart(qpwm);
        }
        private IncidentParms GenerateIncidentParms(Map map, float points, int usherCount, Faction faction, Slate slate, QuestPart_Incident questPart)
        {
            IncidentParms incidentParms = new IncidentParms();
            incidentParms.forced = true;
            incidentParms.target = map;
            incidentParms.points = Mathf.Max(points, faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat, null));
            incidentParms.faction = faction;
            incidentParms.pawnGroupMakerSeed = new int?(Rand.Int);
            incidentParms.inSignalEnd = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignalLeave.GetValue(slate));
            incidentParms.questTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(this.tag.GetValue(slate));
            incidentParms.quest = QuestGen.quest;
            incidentParms.canTimeoutOrFlee = this.canTimeoutOrFlee.GetValue(slate) ?? true;
            if (this.raidPawnKind.GetValue(slate) != null)
            {
                incidentParms.pawnKind = this.usherKind;
                incidentParms.pawnCount = usherCount;
            }
            if (this.arrivalMode.GetValue(slate) != null)
            {
                incidentParms.raidArrivalMode = this.arrivalMode.GetValue(slate);
            }
            if (!this.customLetterLabel.GetValue(slate).NullOrEmpty() || this.customLetterLabelRules.GetValue(slate) != null)
            {
                QuestGen.AddTextRequest("root", delegate (string x)
                {
                    incidentParms.customLetterLabel = x;
                }, QuestGenUtility.MergeRules(this.customLetterLabelRules.GetValue(slate), this.customLetterLabel.GetValue(slate), "root"));
            }
            if (!this.customLetterText.GetValue(slate).NullOrEmpty() || this.customLetterTextRules.GetValue(slate) != null)
            {
                QuestGen.AddTextRequest("root", delegate (string x)
                {
                    incidentParms.customLetterText = x;
                }, QuestGenUtility.MergeRules(this.customLetterTextRules.GetValue(slate), this.customLetterText.GetValue(slate), "root"));
            }
            IncidentWorker_Raid incidentWorker_Raid = (IncidentWorker_Raid)questPart.incident.Worker;
            incidentWorker_Raid.ResolveRaidStrategy(incidentParms, PawnGroupKindDefOf.Combat);
            incidentWorker_Raid.ResolveRaidArriveMode(incidentParms);
            incidentWorker_Raid.ResolveRaidAgeRestriction(incidentParms);
            return incidentParms;
        }

        [NoTranslate]
        public SlateRef<string> inSignal;
        public SlateRef<IntVec3?> walkInSpot;
        public SlateRef<IntVec3?> dropSpot;
        public SlateRef<string> customLetterLabel;
        public SlateRef<string> customLetterText;
        public SlateRef<RulePack> customLetterLabelRules;
        public SlateRef<RulePack> customLetterTextRules;
        public SlateRef<PawnsArrivalModeDef> arrivalMode;
        public SlateRef<PawnKindDef> raidPawnKind;
        public SlateRef<bool?> canTimeoutOrFlee;
        [NoTranslate]
        public SlateRef<string> inSignalLeave;
        [NoTranslate]
        public SlateRef<string> tag;
        public PawnKindDef usherKind;
        public HediffDef FS_hediff;
        public HediffDef NSD_hediff;
        public float NSD_hpThreshold;
        public float TANDY_bonusUsherChance;
        public bool TANDY_bonusUsherChanceCanExplode;
        public List<PawnKindDef> TANDY_bonusPawnTypes;
        public IntRange TANDY_bonusPawnCount;
        [MustTranslate]
        public string FS_description;
        [MustTranslate]
        public string NSD_description;
        [MustTranslate]
        public string TANDY_description;
    }
    //stores a bunch of the information needed to actually implement the mutators, for reference with the incident worker
    public class QuestPart_WellsMutators : QuestPart
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<PawnKindDef>(ref this.usherKind, "usherKind");
            Scribe_Defs.Look<HediffDef>(ref this.FS_hediff, "FS_hediff");
            Scribe_Defs.Look<HediffDef>(ref this.NSD_hediff, "NSD_hediff");
            Scribe_Values.Look<float>(ref this.NSD_hpThreshold, "NSD_hpThreshold", 2000f, false);
            Scribe_Values.Look<int>(ref this.usherCount, "usherCount", 1, false);
            Scribe_Defs.Look<PawnKindDef>(ref this.TANDY_pawnType, "TANDY_pawnType");
            Scribe_Values.Look<int>(ref this.TANDY_pawnCount, "TANDY_pawnCount", 0, false);
        }
        public PawnKindDef usherKind;
        public HediffDef FS_hediff;
        public HediffDef NSD_hediff;
        public float NSD_hpThreshold;
        public int usherCount;
        public PawnKindDef TANDY_pawnType;
        public int TANDY_pawnCount;
    }
    /*a raid that generates its pawns in a particular way, using references from QuestPart_WellsMutators to determine what pawns are made and what bonus hediffs they should get.
     * to minimize AI stupidity, it tells generated pawns to attack a nearby target IMMEDIATELY upon spawn*/
    public class IncidentWorker_WellsStrike : IncidentWorker_RaidEnemy
    {
        public override bool TryResolveRaidArriveMode(IncidentParms parms)
        {
            parms.raidArrivalMode = HVMPDefOf_A.HVMP_UsherArrival;
            return true;
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            PawnKindDef pawnKind = parms.pawnKind;
            QuestPart_WellsMutators qpwm = parms.quest.GetFirstPartOfType<QuestPart_WellsMutators>();
            if (map != null && qpwm != null && qpwm.usherCount > 0)
            {
                if (qpwm.usherKind != null)
                {
                    pawnKind = qpwm.usherKind;
                }
                List<Pawn> list = new List<Pawn>();
                for (int i = 0; i < qpwm.usherCount; i++)
                {
                    Faction faction = parms.faction;
                    PawnGenerationContext pawnGenerationContext = PawnGenerationContext.NonPlayer;
                    Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(pawnKind, faction, pawnGenerationContext, null, false, false, false, true, true, 1f, false, true, false, false, true, false, false, false, false, parms.biocodeWeaponsChance, parms.biocodeApparelChance, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false)
                    {
                        BiocodeApparelChance = 0f
                    });
                    if (pawn != null)
                    {
                        list.Add(pawn);
                    }
                }
                int stealerCount = qpwm.TANDY_pawnCount;
                while (stealerCount > 0)
                {
                    Pawn stealer = PawnGenerator.GeneratePawn(qpwm.TANDY_pawnType, parms.faction, null);
                    list.Add(stealer);
                    stealerCount--;
                }
                if (list.Any<Pawn>())
                {
                    parms.raidArrivalMode.Worker.Arrive(list, parms);
                }
                this.PostProcessSpawnedPawns(parms, list);
                TaggedString taggedString = this.GetLetterLabel(parms);
                TaggedString taggedString2 = this.GetLetterText(parms, list);
                List<TargetInfo> list2 = new List<TargetInfo>();
                foreach (Pawn p in list)
                {
                    list2.Add(p);
                }
                base.SendStandardLetter(taggedString, taggedString2, this.GetLetterDef(), parms, list2, Array.Empty<NamedArgument>());
                if (parms.controllerPawn == null || parms.controllerPawn.Faction != Faction.OfPlayer)
                {
                    this.MakeLords(parms, list);
                }
                foreach (Pawn p in list)
                {
                    if (p.Spawned && !p.Downed && !p.IsAttacking())
                    {
                        Job job = this.TryGetAttackNearbyEnemyJob(p);
                        if (job != null)
                        {
                            p.jobs.StartJob(job, JobCondition.InterruptForced);
                        }
                    }
                }
                LessonAutoActivator.TeachOpportunity(ConceptDefOf.EquippingWeapons, OpportunityType.Critical);
                if (!PlayerKnowledgeDatabase.IsComplete(ConceptDefOf.ShieldBelts))
                {
                    for (int j = 0; j < list.Count; j++)
                    {
                        Pawn pawn2 = list[j];
                        if (pawn2.apparel != null)
                        {
                            if (pawn2.apparel.WornApparel.Any((Apparel ap) => ap.def == ThingDefOf.Apparel_ShieldBelt))
                            {
                                LessonAutoActivator.TeachOpportunity(ConceptDefOf.ShieldBelts, OpportunityType.Critical);
                                break;
                            }
                        }
                    }
                }
                if (DebugSettings.logRaidInfo)
                {
                    Log.Message(string.Format("Raid: {0} ({1}) {2} {3} c={4} p={5}", new object[]
                    {
                    parms.faction.Name,
                    parms.faction.def.defName,
                    parms.raidArrivalMode.defName,
                    parms.raidStrategy.defName,
                    parms.spawnCenter,
                    parms.points
                    }));
                }
                return true;
            }
            return false;
        }
        public Job TryGetAttackNearbyEnemyJob(Pawn pawn)
        {
            if (pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                return null;
            }
            bool isMeleeAttack = pawn.CurrentEffectiveVerb.IsMeleeAttack;
            float num = 8f;
            if (!isMeleeAttack)
            {
                num = Mathf.Clamp(pawn.CurrentEffectiveVerb.EffectiveRange * 0.66f, 2f, 20f);
            }
            Thing thing = (Thing)AttackTargetFinder.BestAttackTarget(pawn, TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedLOSToNonPawns | TargetScanFlags.NeedReachableIfCantHitFromMyPos | TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable, null, 0f, num, default(IntVec3), float.MaxValue, false, true, false, false);
            if (thing == null)
            {
                return null;
            }
            if (isMeleeAttack || pawn.CanReachImmediate(thing, PathEndMode.Touch))
            {
                return JobMaker.MakeJob(JobDefOf.AttackMelee, thing);
            }
            Verb verb = pawn.TryGetAttackVerb(thing, !pawn.IsColonist, false);
            if (verb == null || verb.ApparelPreventsShooting())
            {
                return null;
            }
            Job job = JobMaker.MakeJob(JobDefOf.AttackStatic, thing);
            job.maxNumStaticAttacks = 2;
            job.expiryInterval = 2000;
            job.endIfCantShootTargetFromCurPos = true;
            return job;
        }
        public void MakeLords(IncidentParms parms, List<Pawn> list)
        {
            Map map = (Map)parms.target;
            Lord lord = LordMaker.MakeNewLord(parms.faction, this.MakeLordJob(parms, map, list, Rand.Int), map, list);
            lord.inSignalLeave = parms.inSignalEnd;
            QuestUtility.AddQuestTag(lord, parms.questTag);
        }
        public LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed)
        {
            IntVec3 intVec = (parms.spawnCenter.IsValid ? parms.spawnCenter : pawns[0].PositionHeld);
            if (parms.faction.HostileTo(Faction.OfPlayer))
            {
                Faction faction = parms.faction;
                bool canTimeoutOrFlee = parms.canTimeoutOrFlee;
                return new LordJob_AssaultColony(faction, parms.canKidnap, canTimeoutOrFlee, false, false, parms.canSteal, false, false);
            }
            RCellFinder.TryFindRandomSpotJustOutsideColony(intVec, map, out IntVec3 intVec2);
            return new LordJob_AssistColony(parms.faction, intVec2);
        }
        protected override void PostProcessSpawnedPawns(IncidentParms parms, List<Pawn> pawns)
        {
            base.PostProcessSpawnedPawns(parms, pawns);
            QuestPart_WellsMutators qpwm = parms.quest.GetFirstPartOfType<QuestPart_WellsMutators>();
            if (qpwm != null)
            {
                foreach (Pawn p in pawns)
                {
                    if (p.kindDef == qpwm.usherKind)
                    {
                        if (qpwm.FS_hediff != null)
                        {
                            p.health.AddHediff(qpwm.FS_hediff);
                        }
                        if (qpwm.NSD_hediff != null)
                        {
                            Hediff h = HediffMaker.MakeHediff(qpwm.NSD_hediff, p);
                            p.health.AddHediff(h);
                            Hediff_RapidRegeneration hrr = h as Hediff_RapidRegeneration;
                            if (hrr != null)
                            {
                                hrr.SetHpCapacity(qpwm.NSD_hpThreshold);
                            }
                        }
                    }
                }
            }
        }
    }
    //only usable by incidents that utilize Ushers as their pawn kind. The pawns all arrive around a random player pawn (which can include tame animals, so having a ton of animals is a safety net).
    public class PawnsArrivalModeWorker_Wells : PawnsArrivalModeWorker
    {
        public override bool CanUseWith(IncidentParms parms)
        {
            return parms.quest != null && parms.pawnKind == HVMPDefOf_A.HVMP_HoraxianUsher;
        }
        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            Map map = (Map)parms.target;
            List<Pawn> validTargets = map.mapPawns.PawnsInFaction(Faction.OfPlayer);
            if (!validTargets.NullOrEmpty())
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    IntVec3 loc = CellFinder.RandomClosewalkCellNear(validTargets.RandomElement().PositionHeld, map, 6, null);
                    int tries = 50;
                    while (tries > 0 && !loc.IsValid)
                    {
                        loc = CellFinder.RandomClosewalkCellNear(validTargets.RandomElement().PositionHeld, map, 6, null);
                    }
                    if (loc.IsValid)
                    {
                        GenSpawn.Spawn(pawns[i], loc, map, parms.spawnRotation, WipeMode.Vanish, false);
                        if (pawns[i].stances != null && pawns[i].stances.stunner != null)
                        {
                            pawns[i].stances.stunner.StunFor(30, null, showMote: false);
                        }
                    }
                }
            }
        }
        public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            List<Pawn> validTargets = map.mapPawns.PawnsInFaction(Faction.OfPlayer);
            if (!validTargets.NullOrEmpty())
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(validTargets.RandomElement().PositionHeld, map, 6, null);
                int tries = 50;
                while (tries > 0 && !loc.IsValid)
                {
                    loc = CellFinder.RandomClosewalkCellNear(validTargets.RandomElement().PositionHeld, map, 6, null);
                }
                if (loc.IsValid)
                {
                    return true;
                }
            }
            CellFinder.TryFindRandomEdgeCellWith((IntVec3 p) => p.Standable(map) && !p.GetTerrain(map).dangerous, map, CellFinder.EdgeRoadChance_Hostile, out parms.spawnCenter);
            return true;
        }
    }
    /*Ushers have a starting hediff that grants a bunch of stats.
     * It also turns them into a hostile ghoul (and destroys their clothes) on recruitment or imprisonment, because I don't want this quest to be a way to farm Body Mastery super soldiers.*/
    public class Hediff_SpontaneousGhoulization : Hediff
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (!this.pawn.Downed && (this.pawn.IsPrisoner || (ModsConfig.IdeologyActive && this.pawn.IsSlave) || (this.pawn.Faction != null && this.pawn.Faction != Faction.OfPlayer && this.pawn.Faction.RelationKindWith(Faction.OfPlayer) != FactionRelationKind.Hostile)))
            {
                if (this.pawn.apparel != null && !this.pawn.apparel.WornApparel.NullOrEmpty())
                {
                    for (int i = this.pawn.apparel.WornApparel.Count - 1; i >= 0; i--)
                    {
                        this.pawn.apparel.WornApparel[i].Destroy();
                    }
                }
                this.pawn.mutant = new Pawn_MutantTracker(this.pawn, MutantDefOf.Ghoul, RotStage.Fresh);
                this.pawn.mutant.Turn(true);
                Faction hostileFaction = Faction.OfHoraxCult;
                if (hostileFaction == null)
                {
                    hostileFaction = Faction.OfEntities;
                }
                if (hostileFaction != null)
                {
                    this.pawn.SetFaction(hostileFaction);
                }
                this.pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, null, false, true, false, null, false, false, false);
                MutantUtility.RegenerateHealth(this.pawn);
            }
        }
    }
}
