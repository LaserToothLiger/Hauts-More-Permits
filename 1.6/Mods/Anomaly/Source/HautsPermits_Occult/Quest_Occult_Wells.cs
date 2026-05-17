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
using Verse.Sound;

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
     * wells1: Blow This All To Hell spawns BTATH_count of BTATH_buildings
     * wells2: To Be Nightmare gives the spawned usherKind pawn TBN_hediff
     * wells3: Wetwork Frankenstein gives the spawned usherKind pawn a number of different hediffs from WWF_hediffRoster equal to a random value within WWF_hediffCount*/
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
            int usherCount = 1;
            IncidentParms incidentParms = this.GenerateIncidentParms(map, num, usherCount, faction, slate, questPart_Incident);
            questPart_Incident.SetIncidentParmsAndRemoveTarget(incidentParms);
            questPart_Incident.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
            quest.AddPart(questPart_Incident);
            QuestPart_WellsMutators qpwm = new QuestPart_WellsMutators();
            qpwm.usherKind = this.usherKind;
            qpwm.usherCount = usherCount;
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.wells1, mayhemMode))
            {
                qpwm.BTATH_building = this.BTATH_building;
                qpwm.BTATH_count = this.BTATH_count.RandomInRange;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_BTATH_info", this.BTATH_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_BTATH_info", " ") });
            }
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.wells2, mayhemMode))
            {
                qpwm.TBN_hediff = this.TBN_hediff;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_TBN_info", this.TBN_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_TBN_info", " ") });
            }
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.wells3, mayhemMode))
            {
                int hediffCount = this.WWF_hediffCount.RandomInRange;
                qpwm.WWF_hediffs = new List<HediffDef>();
                while (hediffCount > 0 && this.WWF_hediffRoster.Count > 0)
                {
                    IEnumerable<HediffDef> roster = this.WWF_hediffRoster.Where((HediffDef def) => !qpwm.WWF_hediffs.Contains(def));
                    if (roster.Count() > 0)
                    {
                        HediffDef hd = roster.RandomElement();
                        if (!qpwm.WWF_hediffs.Contains(hd))
                        {
                            qpwm.WWF_hediffs.Add(hd);
                        }
                    }
                    hediffCount--;
                }
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_WWF_info", this.WWF_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_WWF_info", " ") });
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
        public ThingDef BTATH_building;
        public IntRange BTATH_count;
        public HediffDef TBN_hediff;
        public IntRange WWF_hediffCount;
        public List<HediffDef> WWF_hediffRoster;
        [MustTranslate]
        public string BTATH_description;
        [MustTranslate]
        public string TBN_description;
        [MustTranslate]
        public string WWF_description;
    }
    //stores a bunch of the information needed to actually implement the mutators, for reference with the incident worker
    public class QuestPart_WellsMutators : QuestPart
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<PawnKindDef>(ref this.usherKind, "usherKind");
            Scribe_Defs.Look<ThingDef>(ref this.BTATH_building, "BTATH_building");
            Scribe_Values.Look<int>(ref this.BTATH_count, "BTATH_count", 4, false);
            Scribe_Defs.Look<HediffDef>(ref this.TBN_hediff, "TBN_hediff");
            Scribe_Values.Look<int>(ref this.usherCount, "usherCount", 1, false);
            Scribe_Collections.Look<HediffDef>(ref this.WWF_hediffs, "WWF_hediffs", LookMode.Def, Array.Empty<object>());
        }
        public PawnKindDef usherKind;
        public ThingDef BTATH_building;
        public int BTATH_count;
        public HediffDef TBN_hediff;
        public int usherCount;
        public List<HediffDef> WWF_hediffs;
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
                if (qpwm.BTATH_building != null && !list.NullOrEmpty())
                {
                    Pawn planter = list.Where((Pawn candidate)=>candidate.Spawned).RandomElement();
                    while (qpwm.BTATH_count > 0)
                    {
                        if (CellFinder.TryFindRandomCell(map, (IntVec3 iv) => iv.IsValid && iv.Walkable(map) && map.reachability.CanReachMapEdge(iv, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, true, false, true, false, false)), out IntVec3 iv3))
                        {
                            Thing voidCharge = ThingMaker.MakeThing(qpwm.BTATH_building, null);
                            voidCharge.SetFactionDirect(planter.Faction);
                            GenSpawn.Spawn(voidCharge, iv3, map, WipeMode.Vanish);
                            list2.Add(voidCharge);
                        }
                        qpwm.BTATH_count--;
                    }
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
                        if (qpwm.TBN_hediff != null)
                        {
                            Hediff h = HediffMaker.MakeHediff(qpwm.TBN_hediff, p);
                            p.health.AddHediff(h);
                        }
                        if (!qpwm.WWF_hediffs.NullOrEmpty())
                        {
                            foreach (HediffDef hd in qpwm.WWF_hediffs)
                            {
                                Hediff h = HediffMaker.MakeHediff(hd, p);
                                p.health.AddHediff(h);
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
     * It also turns them into a hostile ghoul (and destroys their clothes) right after they die or when the buff is removed*/
    public class Hediff_SpontaneousGhoulization : HediffWithComps
    {
        public override void PostRemoved()
        {
            base.PostRemoved();
            if (!this.pawn.IsGhoul)
            {
                this.Ghoulize();
            }
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            ResurrectionUtility.TryResurrect(this.pawn,new ResurrectionParams());
            this.Ghoulize();
        }
        public void Ghoulize()
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
            FleshbeastUtility.MeatSplatter(Rand.RangeInclusive(1, 3), this.pawn.PositionHeld, this.pawn.MapHeld, FleshbeastUtility.ExplosionSizeFor(this.pawn));
            this.pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, null, false, true, false, null, false, false, false);
            MutantUtility.RegenerateHealth(this.pawn);
            this.pawn.health.RemoveHediff(this);
        }
    }
    /*Mutator hediffs
     * wells2: To Be Nightmare periodically causes invulnerability, stuns the pawn, and has it teleport around doing nociosphere stuff. It can't die until the responsible hediff is removed
     * ticksToNextTrigger: when the pawn spawns, and whenever it drops out of invulnerability, it takes a random number of ticks in this range to enter its next invuln period (which is just the hediff being set to max severity)
     * invulnDurationTicks: each invulnerability period lasts exactly this long
     * abilities: the pawn gains these abilities while on its period, and casts them on itself every 2s
     * teleportRadius: while on its period, teleports to a random point in this range. Causes clamorType clamor in clamorRadius
     * severityPerTrigger: after dropping out of invulnerability, gain this much severity. (should be negative so that it eventually drops to a nonviable severity)*/
    public class HediffCompProperties_NightmareMode : HediffCompProperties
    {
        public HediffCompProperties_NightmareMode()
        {
            this.compClass = typeof(HediffComp_NightmareMode);
        }
        public float severityPerTrigger;
        public IntRange ticksToNextTrigger;
        public int invulnDurationTicks;
        public List<AbilityDef> abilities;
        public float clamorRadius;
        public ClamorDef clamorType;
        public int teleportRadius;
    }
    public class HediffComp_NightmareMode : HediffComp
    {
        public HediffCompProperties_NightmareMode Props
        {
            get
            {
                return (HediffCompProperties_NightmareMode)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            this.timer = this.Props.ticksToNextTrigger.RandomInRange;
            this.storedSeverity = this.parent.Severity;
        }
        public bool IsActive
        {
            get
            {
                return this.parent.Severity == this.parent.def.maxSeverity;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.timer > 0)
            {
                this.timer -= delta;
            }
            if (this.timer <= 0)
            {
                if (!this.IsActive)
                {
                    this.storedSeverity = this.parent.Severity;
                    this.parent.Severity = this.parent.def.maxSeverity;
                    this.timer = this.Props.invulnDurationTicks;
                    if (this.Pawn.abilities != null && this.Props.abilities != null)
                    {
                        foreach (AbilityDef ad in this.Props.abilities)
                        {
                            this.Pawn.abilities.GainAbility(ad);
                        }
                    }
                } else {
                    this.parent.Severity = this.storedSeverity + this.Props.severityPerTrigger;
                    this.timer = this.Props.ticksToNextTrigger.RandomInRange;
                    if (this.Pawn.abilities != null && this.Props.abilities != null)
                    {
                        foreach (AbilityDef ad in this.Props.abilities)
                        {
                            this.Pawn.abilities.RemoveAbility(ad);
                        }
                    }
                }
            }
            if (this.IsActive && this.Pawn.IsHashIntervalTick(120))
            {
                Hediff h = this.Pawn.health.hediffSet.hediffs.FirstOrDefault((Hediff h2) => (h2.def.isBad && h2.def.everCurableByItem) || h2 is Hediff_MissingPart);
                if (h != null)
                {
                    HealthUtility.Cure(h);
                }
                if (this.Pawn.Spawned)
                {
                    Pawn pawn = this.Pawn;
                    Map map = pawn.Map;
                    if (CellFinder.TryFindRandomCellNear(pawn.Position, map, this.Props.teleportRadius, (IntVec3 c) => c.Walkable(map) && !c.Fogged(map) && c.GetFirstPawn(map) == null && c.GetRoom(map) == pawn.Position.GetRoom(map), out IntVec3 intVec, -1))
                    {
                        if (intVec.IsValid)
                        {
                            SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(pawn.Position, map, false));
                            pawn.Position = intVec;
                            if ((pawn.Faction == Faction.OfPlayer || pawn.IsPlayerControlled) && pawn.Position.Fogged(map))
                            {
                                FloodFillerFog.FloodUnfog(pawn.Position, map);
                            }
                            pawn.Notify_Teleported(true, true);
                            CompAbilityEffect_Teleport.SendSkipUsedSignal(pawn.Position, pawn);
                            GenClamor.DoClamor(pawn, intVec, (float)this.Props.clamorRadius, this.Props.clamorType);
                            FleckCreationData dataStatic = FleckMaker.GetDataStatic(pawn.Position.ToVector3Shifted(), map, FleckDefOf.PsycastSkipInnerExit, 1f);
                            dataStatic.rotationRate = (float)Rand.Range(-30, 30);
                            dataStatic.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                            map.flecks.CreateFleck(dataStatic);
                            FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(pawn.Position.ToVector3Shifted(), map, FleckDefOf.PsycastSkipOuterRingExit, 1f);
                            dataStatic2.rotationRate = (float)Rand.Range(-30, 30);
                            dataStatic2.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                            map.flecks.CreateFleck(dataStatic2);
                            SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(intVec, map, false));
                            if (pawn.stances != null && pawn.stances.stunner != null)
                            {
                                pawn.stances.stunner.StopStun();
                            }
                        }
                        if (!this.Props.abilities.NullOrEmpty() && this.Pawn.abilities != null)
                        {
                            AbilityDef ad = this.Props.abilities.RandomElement();
                            Ability ab = this.Pawn.abilities.GetAbility(ad);
                            if (ab != null)
                            {
                                ab.ResetCooldown();
                                LocalTargetInfo target = this.Pawn;
                                if (!ab.CanApplyOn(target))
                                {
                                    target = this.Pawn.Position;
                                }
                                this.Pawn.jobs.StartJob(ab.GetJob(target, null), JobCondition.InterruptForced);
                            }
                        }
                        MentalState ms = this.Pawn.MentalState;
                        if (ms != null)
                        {
                            ms.RecoverFromState();
                        }
                    }
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.timer, "timer", 600, false);
            Scribe_Values.Look<float>(ref this.storedSeverity, "storedSeverity", 3f, false);
        }
        public int timer;
        public float storedSeverity;
    }
    /*wells3: Wetware Frankenstein grants three random entity-inspired buffs.
     * Chimera: near-identical to CompChimera's rage speed mechanics
     * RapidRegeneration: like a normal RapidRegeneration hediff, but it automatically sets its initial healing reservoir to its max severity
     * DeathPall: every deadlifePeriodicity ticks, play effecter on own position and raise all corpses in deadlifeRadius as shamblers of the pawn's faction
     * Nociosphere: every novaPeriodicity ticks, play effecter on own position and apply durationSecs worth of Psychic Agony to all pawns in radius of oneself*/
    public class HediffCompProperties_WWF_Chimera : HediffCompProperties
    {
        public HediffCompProperties_WWF_Chimera()
        {
            this.compClass = typeof(HediffComp_WWF_Chimera);
        }
        public float rageEndHealthPercentThreshold = 0.98f;
    }
    public class HediffComp_WWF_Chimera : HediffComp
    {
        public HediffCompProperties_WWF_Chimera Props
        {
            get
            {
                return (HediffCompProperties_WWF_Chimera)this.props;
            }
        }
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            this.totalDamageTaken += totalDamageDealt;
            if (!this.Pawn.Dead && this.totalDamageTaken > 0f && !this.Pawn.health.hediffSet.HasHediff(HediffDefOf.RageSpeed, false) && !this.Pawn.health.Downed)
            {
                this.Pawn.health.AddHediff(HediffMaker.MakeHediff(HediffDefOf.RageSpeed, this.Pawn, null), null, null, null);
                if (this.Pawn.Spawned)
                {
                    EffecterDefOf.ChimeraRage.Spawn(this.Pawn.Position, this.Pawn.Map, 1f).Cleanup();
                }
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(240) && this.Pawn.health.summaryHealth.SummaryHealthPercent >= this.Props.rageEndHealthPercentThreshold)
            {
                Hediff firstHediffOfDef = this.Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.RageSpeed, false);
                if (firstHediffOfDef != null)
                {
                    this.Pawn.health.RemoveHediff(firstHediffOfDef);
                    this.totalDamageTaken = 0f;
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<float>(ref this.totalDamageTaken, "totalDamageTaken", 0f, false);
        }
        public float totalDamageTaken;
    }
    public class Hediff_WWF_RapidRegeneration : Hediff_RapidRegeneration
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            this.SetHpCapacity(this.def.maxSeverity);
        }
    }
    public class HediffCompProperties_WWF_DeathPall : HediffCompProperties
    {
        public HediffCompProperties_WWF_DeathPall()
        {
            this.compClass = typeof(HediffComp_WWF_DeathPall);
        }
        public int deadlifePeriodicity;
        public float deadlifeRadius;
        public FleckDef fleck;
        public float fleckScale;
    }
    public class HediffComp_WWF_DeathPall : HediffComp
    {
        public HediffCompProperties_WWF_DeathPall Props
        {
            get
            {
                return (HediffCompProperties_WWF_DeathPall)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(this.Props.deadlifePeriodicity, delta) && this.Pawn.Spawned)
            {
                Pawn p = this.Pawn;
                FleckMaker.AttachedOverlay(p, this.Props.fleck, Vector3.zero, this.Props.fleckScale, -1f);
                foreach (Corpse c in GenRadial.RadialDistinctThingsAround(p.Position, p.Map, this.Props.deadlifeRadius, true).OfType<Corpse>().Distinct<Corpse>())
                {
                    if (MutantUtility.CanResurrectAsShambler(c))
                    {
                        c.InnerPawn.MarkDeadlifeDustForFaction(p.Faction);
                        MutantUtility.ResurrectAsShambler(c.InnerPawn, 15000, c.InnerPawn.DeadlifeDustFaction);
                    }
                }
            }
        }
    }
    public class HediffCompProperties_WWF_Nociosphere : HediffCompProperties
    {
        public HediffCompProperties_WWF_Nociosphere()
        {
            this.compClass = typeof(HediffComp_WWF_Nociosphere);
        }
        public int novaPeriodicity;
        public float radius;
        public IntRange durationSecs;
        public EffecterDef effecter;
    }
    public class HediffComp_WWF_Nociosphere : HediffComp
    {
        public HediffCompProperties_WWF_Nociosphere Props
        {
            get
            {
                return (HediffCompProperties_WWF_Nociosphere)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(this.Props.novaPeriodicity, delta))
            {
                Pawn p = this.Pawn;
                if (p.Spawned)
                {
                    foreach (Pawn pawn in p.MapHeld.mapPawns.AllPawnsSpawned)
                    {
                        if (pawn.Position.InHorDistOf(p.Position, this.Props.radius))
                        {
                            this.ApplyOrRefreshHediff(pawn);
                        }
                    }
                    SoundDefOf.PsychicBanshee.PlayOneShot(p);
                    MoteMaker.MakeAttachedOverlay(p, ThingDefOf.Mote_PsychicBanshee, Vector3.zero, 1f, -1f);
                    this.Props.effecter.SpawnMaintained(p.Position, p.Map, 1f);
                }
            }
        }
        private void ApplyOrRefreshHediff(Pawn pawn)
        {
            if (pawn.health.hediffSet.TryGetHediff(HediffDefOf.AgonyPulse, out Hediff hediff))
            {
                hediff.Severity = 0f;
            } else {
                hediff = pawn.health.AddHediff(HediffDefOf.AgonyPulse, null, null, null);
            }
            HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (hediffComp_Disappears != null)
            {
                hediffComp_Disappears.ticksToDisappear = this.Props.durationSecs.RandomInRange * 60;
            }
        }
    }
}
