using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace HautsPermits
{
    /*get raided. as the name indicates, also handles all three Bellum mutators
     * bellum1: Advanced Weaponry either inflicts AW_condition when the raid arrives for a random number of hours within AW_conditionHours (if AW_conditionChance chance check passes)
     *   or else creates bonus raiders consisting of up to AW_bonusPoints of pawns from the AW_pawnRosterOtherwise pawn group maker's options (otherwise)
     * bellum2: Dead Raiders Tell No Tales gives each raider a DRTNT_spyChance chance to have DRTNT_hediff
     * bellum3: Traitor Guard creates bonus raiders consisting of up to TG_bonusPoints of pawns from the TG_pawnRoster's options. They each gain a random hediff from TG_hediffChances (values are weightings of corresponding keys).*/
    public class QuestNode_AllThreeBellumMutators : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return Find.Storyteller.difficulty.allowViolentQuests && slate.Exists("map", false) && slate.Exists("enemyFaction", false);
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Map map = QuestGen.slate.Get<Map>("map", null, false);
            float num = QuestGen.slate.Get<float>("points", 0f, false);
            Faction faction = QuestGen.slate.Get<Faction>("enemyFaction", null, false);
            QuestPart_Incident questPart_Incident = new QuestPart_Incident();
            questPart_Incident.debugLabel = "raid";
            questPart_Incident.incident = HVMPDefOf.HVMP_RaidBellum;
            int num2 = 0;
            IncidentParms incidentParms;
            PawnGroupMakerParms defaultPawnGroupMakerParms;
            IEnumerable<PawnKindDef> enumerable;
            do
            {
                incidentParms = this.GenerateIncidentParms(map, num, faction, slate, questPart_Incident);
                defaultPawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, incidentParms, true);
                defaultPawnGroupMakerParms.points = IncidentWorker_Raid.AdjustedRaidPoints(defaultPawnGroupMakerParms.points, incidentParms.raidArrivalMode, incidentParms.raidStrategy, defaultPawnGroupMakerParms.faction, PawnGroupKindDefOf.Combat, incidentParms.target, null);
                enumerable = PawnGroupMakerUtility.GeneratePawnKindsExample(defaultPawnGroupMakerParms);
                num2++;
            }
            while (!enumerable.Any<PawnKindDef>() && num2 < 50);
            if (!enumerable.Any<PawnKindDef>())
            {
                string[] array = new string[6];
                array[0] = "No pawnkinds example for ";
                array[1] = QuestGen.quest.root.defName;
                array[2] = " parms=";
                int num3 = 3;
                PawnGroupMakerParms pawnGroupMakerParms = defaultPawnGroupMakerParms;
                array[num3] = ((pawnGroupMakerParms != null) ? pawnGroupMakerParms.ToString() : null);
                array[4] = " iterations=";
                array[5] = num2.ToString();
                Log.Error(string.Concat(array));
            }
            IncidentWorker_RaidBellum iwrf = (IncidentWorker_RaidBellum)questPart_Incident.incident.Worker;
            this.ImplementQuestMutators(slate, faction);
            questPart_Incident.SetIncidentParmsAndRemoveTarget(incidentParms);
            questPart_Incident.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
            QuestGen.quest.AddPart(questPart_Incident);
            QuestGen.AddQuestDescriptionRules(new List<Rule>
            {
                new Rule_String("raidPawnKinds", PawnUtility.PawnKindsToLineList(enumerable, "  - ", ColoredText.ThreatColor)),
                new Rule_String("raidArrivalModeInfo", incidentParms.raidArrivalMode.textWillArrive.Formatted(faction))
            });
        }
        private IncidentParms GenerateIncidentParms(Map map, float points, Faction faction, Slate slate, QuestPart_Incident questPart)
        {
            IncidentParms incidentParms = new IncidentParms
            {
                forced = true,
                target = map,
                points = Mathf.Max(points, faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat, null)),
                faction = faction,
                pawnGroupMakerSeed = new int?(Rand.Int),
                inSignalEnd = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignalLeave.GetValue(slate)),
                questTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(this.tag.GetValue(slate)),
                canTimeoutOrFlee = (map.CanEverExit && (this.canTimeoutOrFlee.GetValue(slate) ?? true))
            };
            if (this.raidPawnKind.GetValue(slate) != null)
            {
                incidentParms.pawnKind = this.raidPawnKind.GetValue(slate);
                incidentParms.pawnCount = Mathf.Max(1, Mathf.RoundToInt(incidentParms.points / incidentParms.pawnKind.combatPower));
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
            IncidentWorker_RaidBellum iwrf = (IncidentWorker_RaidBellum)questPart.incident.Worker;
            iwrf.ResolveRaidStrategy(incidentParms, PawnGroupKindDefOf.Combat);
            iwrf.ResolveRaidArriveMode(incidentParms);
            iwrf.ResolveRaidAgeRestriction(incidentParms);
            if (incidentParms.raidArrivalMode.walkIn)
            {
                incidentParms.spawnCenter = this.walkInSpot.GetValue(slate) ?? QuestGen.slate.Get<IntVec3?>("walkInSpot", null, false) ?? IntVec3.Invalid;
            } else {
                incidentParms.spawnCenter = this.dropSpot.GetValue(slate) ?? QuestGen.slate.Get<IntVec3?>("dropSpot", null, false) ?? IntVec3.Invalid;
            }
            return incidentParms;
        }
        public void ImplementQuestMutators(Slate slate, Faction faction)
        {
            QuestPart_AllThreeBellumMutators qpa3 = new QuestPart_AllThreeBellumMutators();
            bool mayhemMode = HVMP_Mod.settings.bellumX;
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.bellum1, mayhemMode))
            {
                if (Rand.Chance(this.AW_conditionChance))
                {
                    qpa3.AW_condition = this.AW_conditions.RandomElement();
                    qpa3.AW_conditionTicks = (int)(this.AW_conditionHours.RandomInRange * 2500);
                } else {
                    qpa3.AW_bonusPoints = (float)this.AW_bonusPoints.GetValue(slate);
                    qpa3.AW_pawnRosterOtherwise = this.AW_pawnRosterOtherwise.RandomElement();
                }
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_AW_info", this.AW_description.Formatted(faction))
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_AW_info", " ") });
            }
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.bellum2, mayhemMode))
            {
                qpa3.DRTNT_hediff = this.DRTNT_hediff;
                qpa3.DRTNT_spyChance = this.DRTNT_spyChance;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_DRTNT_info", this.DRTNT_description.Formatted(faction))
                    });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_DRTNT_info", " ") });
            }
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.bellum3, mayhemMode))
            {
                qpa3.TG_hediffChances = this.TG_hediffChances;
                qpa3.TG_bonusPoints = (float)this.TG_bonusPoints.GetValue(slate);
                qpa3.TG_pawnRoster = this.TG_pawnRoster.RandomElement();
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_TG_info", this.TG_description.Formatted(faction))
                    });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_TG_info", " ") });
            }
            QuestGen.quest.AddPart(qpa3);
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
        public float AW_conditionChance;
        public List<GameConditionDef> AW_conditions;
        public FloatRange AW_conditionHours;
        public List<PawnGroupMaker> AW_pawnRosterOtherwise;
        public SlateRef<float?> AW_bonusPoints;
        [MustTranslate]
        public string AW_description;
        public float DRTNT_spyChance;
        public HediffDef DRTNT_hediff;
        [MustTranslate]
        public string DRTNT_description;
        public Dictionary<HediffDef, float> TG_hediffChances;
        public List<PawnGroupMaker> TG_pawnRoster;
        public SlateRef<float?> TG_bonusPoints;
        [MustTranslate]
        public string TG_description;
    }
    public class QuestPart_AllThreeBellumMutators : QuestPart
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<GameConditionDef>(ref this.AW_condition, "AW_condition");
            Scribe_Values.Look<int>(ref this.AW_conditionTicks, "AW_conditionTicks", 0, false);
            Scribe_Values.Look<PawnGroupMaker>(ref this.AW_pawnRosterOtherwise, "AW_pawnRosterOtherwise", null, false);
            Scribe_Values.Look<float>(ref this.AW_bonusPoints, "AW_bonusPoints", 0f, false);
            Scribe_Values.Look<float>(ref this.DRTNT_spyChance, "DRTNT_spyChance", 0f, false);
            Scribe_Defs.Look<HediffDef>(ref this.DRTNT_hediff, "DRTNT_hediff");
            Scribe_Collections.Look<HediffDef, float>(ref this.TG_hediffChances, "TG_hediffChances", LookMode.Def, LookMode.Value, ref this.tmpHediffs, ref this.tmpChances);
            Scribe_Values.Look<PawnGroupMaker>(ref this.TG_pawnRoster, "TG_pawnRoster", null, false);
            Scribe_Values.Look<float>(ref this.TG_bonusPoints, "TG_bonusPoints", 0f, false);
        }
        public GameConditionDef AW_condition;
        public int AW_conditionTicks;
        public PawnGroupMaker AW_pawnRosterOtherwise;
        public float AW_bonusPoints;
        public float DRTNT_spyChance;
        public HediffDef DRTNT_hediff;
        public Dictionary<HediffDef, float> TG_hediffChances;
        public PawnGroupMaker TG_pawnRoster;
        public float TG_bonusPoints;
        private List<HediffDef> tmpHediffs;
        private List<float> tmpChances;
    }
    //derivative of RaidEnemy, which actually implements all the stuff I just described in the prior comment about the mutators, by referencing values stored in QuestPart_AllThreeBellumMutators
    public class IncidentWorker_RaidBellum : IncidentWorker_RaidEnemy
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            List<Pawn> list;
            if (!this.TryGenerateRaidInfo(parms, out list, false))
            {
                return false;
            }
            List<Pawn> listMut = new List<Pawn>();
            PawnGroupMakerParms pgmp = new PawnGroupMakerParms();
            pgmp.tile = parms.target.Tile;
            pgmp.faction = parms.faction;
            pgmp.traderKind = parms.traderKind;
            pgmp.generateFightersOnly = parms.generateFightersOnly;
            pgmp.raidStrategy = parms.raidStrategy;
            pgmp.forceOneDowned = parms.raidForceOneDowned;
            pgmp.seed = parms.pawnGroupMakerSeed;
            pgmp.ideo = parms.pawnIdeo;
            pgmp.raidAgeRestriction = parms.raidAgeRestriction;
            QuestPart_AllThreeBellumMutators qpa3 = parms.quest.GetFirstPartOfType<QuestPart_AllThreeBellumMutators>();
            if (qpa3 != null)
            {
                if (qpa3.AW_pawnRosterOtherwise != null)
                {
                    pgmp.groupKind = qpa3.AW_pawnRosterOtherwise.kindDef;
                    if (!pgmp.faction.def.pawnGroupMakers.ContainsAny((PawnGroupMaker pgm) => pgm.kindDef == pgmp.groupKind))
                    {
                        pgmp.faction.def.pawnGroupMakers.Add(qpa3.AW_pawnRosterOtherwise);
                    }
                    float minPoints = 200f;
                    foreach (PawnGenOption pgo in qpa3.AW_pawnRosterOtherwise.options)
                    {
                        float combatPower = pgo.kind.combatPower;
                        if (combatPower > minPoints)
                        {
                            minPoints = combatPower;
                        }
                    }
                    pgmp.points = Math.Max(qpa3.AW_bonusPoints, minPoints * 4f);
                    foreach (Pawn pawn in qpa3.AW_pawnRosterOtherwise.GeneratePawns(pgmp, true))
                    {
                        listMut.Add(pawn);
                    }
                }
                if (qpa3.TG_pawnRoster != null)
                {
                    pgmp.groupKind = qpa3.TG_pawnRoster.kindDef;
                    if (!pgmp.faction.def.pawnGroupMakers.ContainsAny((PawnGroupMaker pgm) => pgm.kindDef == pgmp.groupKind))
                    {
                        pgmp.faction.def.pawnGroupMakers.Add(qpa3.AW_pawnRosterOtherwise);
                    }
                    float minPoints = 200f;
                    foreach (PawnGenOption pgo in qpa3.TG_pawnRoster.options)
                    {
                        float combatPower = pgo.kind.combatPower;
                        if (combatPower > minPoints)
                        {
                            minPoints = combatPower;
                        }
                    }
                    pgmp.points = Math.Max(qpa3.TG_bonusPoints, minPoints * 4f);
                    foreach (Pawn pawn in qpa3.TG_pawnRoster.GeneratePawns(pgmp, true))
                    {
                        pawn.health.AddHediff(HVMPDefOf.HVMP_HostileEnvironmentFilm);
                        listMut.Add(pawn);
                    }
                }
                if (!listMut.NullOrEmpty())
                {
                    parms.raidArrivalMode.Worker.Arrive(listMut, parms);
                }
                if (qpa3.AW_condition != null)
                {
                    GameCondition gameCondition = GameConditionMaker.MakeCondition(qpa3.AW_condition, qpa3.AW_conditionTicks);
                    gameCondition.forceDisplayAsDuration = true;
                    gameCondition.Permanent = false;
                    gameCondition.quest = parms.quest;
                    Map map = (Map)parms.target;
                    if (map == null)
                    {
                        MapParent mapParent = gameCondition.quest.TryFindNewSuitableMapParentForRetarget();
                        map = ((mapParent != null) ? mapParent.Map : null) ?? Find.AnyPlayerHomeMap;
                    }
                    if (map != null)
                    {
                        List<Rule> listRule = new List<Rule>();
                        Dictionary<string, string> dictionary = new Dictionary<string, string>();
                        gameCondition.RandomizeSettings(parms.points, map, listRule, dictionary);
                        map.gameConditionManager.RegisterCondition(gameCondition);
                    }
                    Find.LetterStack.ReceiveLetter(gameCondition.LabelCap, gameCondition.LetterText, gameCondition.def.letterDef, LookTargets.Invalid, null, gameCondition.quest, null, null, 0, true);
                }
            }
            TaggedString taggedString = this.GetLetterLabel(parms);
            TaggedString taggedString2 = this.GetLetterText(parms, list);
            PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(list, ref taggedString, ref taggedString2, this.GetRelatedPawnsInfoLetterText(parms), true, true);
            List<TargetInfo> list2 = new List<TargetInfo>();
            if (parms.pawnGroups != null)
            {
                List<List<Pawn>> list3 = IncidentParmsUtility.SplitIntoGroups(list, parms.pawnGroups);
                if (!listMut.NullOrEmpty())
                {
                    list3.Add(listMut);
                }
                List<Pawn> list4 = list3.MaxBy((List<Pawn> x) => x.Count);
                if (list4.Any<Pawn>())
                {
                    list2.Add(list4[0]);
                }
                for (int i = 0; i < list3.Count; i++)
                {
                    if (list3[i] != list4 && list3[i].Any<Pawn>())
                    {
                        list2.Add(list3[i][0]);
                    }
                }
            } else if (list.Any<Pawn>()) {
                foreach (Pawn pawn in list)
                {
                    list2.Add(pawn);
                }
            }
            base.SendStandardLetter(taggedString, taggedString2, this.GetLetterDef(), parms, list2, Array.Empty<NamedArgument>());
            if (parms.controllerPawn == null || parms.controllerPawn.Faction != Faction.OfPlayer)
            {
                if (!listMut.NullOrEmpty())
                {
                    list.AddRange(listMut);
                }
                parms.raidStrategy.Worker.MakeLords(parms, list);
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
        protected override void PostProcessSpawnedPawns(IncidentParms parms, List<Pawn> pawns)
        {
            base.PostProcessSpawnedPawns(parms, pawns);
            QuestPart_AllThreeBellumMutators qpa3 = parms.quest.GetFirstPartOfType<QuestPart_AllThreeBellumMutators>();
            if (qpa3 != null && qpa3.DRTNT_hediff != null)
            {
                int minSpyCount = 1;
                foreach (Pawn p in pawns.InRandomOrder())
                {
                    if (p.RaceProps.intelligence == Intelligence.Humanlike && (minSpyCount > 0 || Rand.Chance(qpa3.DRTNT_spyChance)))
                    {
                        p.health.AddHediff(qpa3.DRTNT_hediff);
                        minSpyCount--;
                    }
                    if (!qpa3.TG_hediffChances.NullOrEmpty())
                    {
                        foreach (KeyValuePair<HediffDef, float> kvp in qpa3.TG_hediffChances)
                        {
                            if (Rand.Chance(kvp.Value))
                            {
                                p.health.AddHediff(kvp.Key);
                            }
                        }
                    }
                }
            }
        }
    }
    /*Psychic Suppression would make part of the raiders suck too, so we use something that closely resembles (but is not actually) psy suppression.
     * The chief distinction is that it only hits player pawns and pawns from factions not hostile to the player*/
    public class HVMP_GameCondition_TargetedPsychicSuppression : GameCondition
    {
        public override string LetterText
        {
            get
            {
                return base.LetterText.Formatted(this.gender.GetLabel(false).ToLower());
            }
        }
        public override string Description
        {
            get
            {
                return base.Description.Formatted(this.gender.GetLabel(false).ToLower());
            }
        }
        public override void Init()
        {
            base.Init();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<Gender>(ref this.gender, "gender", Gender.None, false);
        }
        public static void CheckPawn(Pawn pawn, Gender targetGender)
        {
            if (pawn.RaceProps.Humanlike && pawn.gender == targetGender && (pawn.Faction == null || pawn.Faction == Faction.OfPlayer || !pawn.Faction.HostileTo(Faction.OfPlayer)) && !pawn.health.hediffSet.HasHediff(HVMPDefOf.HVMP_TargetedPsychicSuppression, false))
            {
                pawn.health.AddHediff(HVMPDefOf.HVMP_TargetedPsychicSuppression, null, null, null);
            }
        }
        public override void GameConditionTick()
        {
            foreach (Map map in base.AffectedMaps)
            {
                List<Pawn> allPawns = map.mapPawns.AllPawns;
                for (int i = 0; i < allPawns.Count; i++)
                {
                    HVMP_GameCondition_TargetedPsychicSuppression.CheckPawn(allPawns[i], this.gender);
                }
            }
        }
        public override void RandomizeSettings(float points, Map map, List<Rule> outExtraDescriptionRules, Dictionary<string, string> outExtraDescriptionConstants)
        {
            base.RandomizeSettings(points, map, outExtraDescriptionRules, outExtraDescriptionConstants);
            if (map.mapPawns.FreeColonistsCount > 0)
            {
                this.gender = map.mapPawns.FreeColonists.RandomElement<Pawn>().gender;
            }
            else
            {
                this.gender = Rand.Element<Gender>(Gender.Male, Gender.Female);
            }
            outExtraDescriptionRules.Add(new Rule_String("psychicSuppressorGender", this.gender.GetLabel(false)));
        }
        public Gender gender;
    }
    //works like HediffComp_PsychicSuppression, but, y'know, for TargetedPsychicSuppression
    public class HediffComp_TargetedPsychicSuppression : HediffComp
    {
        public override bool CompShouldRemove
        {
            get
            {
                if (base.Pawn.SpawnedOrAnyParentSpawned)
                {
                    HVMP_GameCondition_TargetedPsychicSuppression activeCondition = base.Pawn.MapHeld.gameConditionManager.GetActiveCondition<HVMP_GameCondition_TargetedPsychicSuppression>();
                    if (activeCondition != null && base.Pawn.gender == activeCondition.gender)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
