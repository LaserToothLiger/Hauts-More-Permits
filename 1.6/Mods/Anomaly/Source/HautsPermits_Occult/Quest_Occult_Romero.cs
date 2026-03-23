using HautsPermits;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Grammar;

namespace HautsPermits_Occult
{
    //responsible for just one mutator, romero1: Black Skies puts on a death pall when you accept the quest, lasting for the [delay until first wave * durationFactor]*/
    public class QuestNode_Romero_BS : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.romero1, HVMP_Mod.settings.romeroX))
            {
                float num = slate.Get<float>("points", 0f, false);
                GameCondition gameCondition = GameConditionMaker.MakeCondition(GameConditionDefOf.DeathPall, (int)(this.durationFactor.RandomInRange * this.duration.GetValue(slate)));
                QuestPart_GameCondition questPart_GameCondition = new QuestPart_GameCondition();
                questPart_GameCondition.gameCondition = gameCondition;
                List<Rule> list = new List<Rule>();
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                Map map = QuestNode_Romero_BS.GetMap(slate);
                questPart_GameCondition.mapParent = map.Parent;
                questPart_GameCondition.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
                QuestGen.quest.AddPart(questPart_GameCondition);
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_BS_info", this.BS_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_BS_info", " ") });
            }
        }
        private static Map GetMap(Slate slate)
        {
            Map randomPlayerHomeMap;
            if (!slate.TryGet<Map>("map", out randomPlayerHomeMap, false))
            {
                randomPlayerHomeMap = Find.RandomPlayerHomeMap;
            }
            return randomPlayerHomeMap;
        }
        public SlateRef<int> duration;
        [NoTranslate]
        public SlateRef<string> inSignal;
        [MustTranslate]
        public string BS_description;
        public FloatRange durationFactor;
    }
    //gets the Dark Entities faction and loads it into QuestPart_InvolvedFactions. If, SOMEHOW, your game is borked enough to not have the Dark Entities, loads a random hostile faction instead
    public class QuestNode_GetShamblerFaction : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return Find.FactionManager.FirstFactionOfDef(FactionDefOf.Entities) != null;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Entities);
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
    }
    /*generate a custom variant of the typical ShamblerAssault which can implement the other two mutator effects
     * romero2: Combined Appendages Doctrine adds bonus pawns = [threat points * CAD_pointFactor] of a random pawn kind drawn from CAD_pawnType
     * romero3: Night of the Everliving Dead grants the attacking shamblers NOTED_hediff and multiplies their shambler lifespan duration by NOTED_lifespanFactor*/
    public class QuestNode_Romero : QuestNode
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
            Faction faction = Faction.OfEntities ?? slate.Get<Faction>("enemyFaction", null, false);
            QuestPart_Incident questPart_Incident = new QuestPart_Incident
            {
                debugLabel = "raid",
                incident = HVMPDefOf.HVMP_ShamblerAssault
            };
            IncidentParms incidentParms = this.GenerateIncidentParms(map, num, faction, slate, questPart_Incident);
            PawnGroupMakerParms defaultPawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Shamblers, incidentParms, true);
            defaultPawnGroupMakerParms.points = IncidentWorker_Raid.AdjustedRaidPoints(defaultPawnGroupMakerParms.points, incidentParms.raidArrivalMode, incidentParms.raidStrategy, defaultPawnGroupMakerParms.faction, PawnGroupKindDefOf.Shamblers, map);
            questPart_Incident.SetIncidentParmsAndRemoveTarget(incidentParms);
            questPart_Incident.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
            quest.AddPart(questPart_Incident);
            QuestPart_RomeroMutators qprm = new QuestPart_RomeroMutators();
            bool mayhemMode = HVMP_Mod.settings.romeroX;
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.romero2, mayhemMode))
            {
                qprm.CAD_pawnType = this.CAD_pawnList.RandomElement();
                qprm.CAD_pointFactor = this.CAD_pointFactor;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_CAD_info", this.CAD_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_CAD_info", " ") });
            }
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.romero3, mayhemMode))
            {
                qprm.NOTED_hediff = this.NOTED_bonusHediff;
                qprm.NOTED_lifespanFactor = this.NOTED_lifespanFactor;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_NOTED_info", this.NOTED_description.Formatted())
                });
            } else {
                qprm.NOTED_lifespanFactor = 1f;
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_NOTED_info", " ") });
            }
            quest.AddPart(qprm);
        }
        private IncidentParms GenerateIncidentParms(Map map, float points, Faction faction, Slate slate, QuestPart_Incident questPart)
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
            IncidentWorker_Raid incidentWorker_Raid = (IncidentWorker_Raid)questPart.incident.Worker;
            incidentWorker_Raid.ResolveRaidStrategy(incidentParms, PawnGroupKindDefOf.Combat);
            incidentWorker_Raid.ResolveRaidArriveMode(incidentParms);
            incidentWorker_Raid.ResolveRaidAgeRestriction(incidentParms);
            if (incidentParms.raidArrivalMode.walkIn)
            {
                incidentParms.spawnCenter = this.walkInSpot.GetValue(slate) ?? QuestGen.slate.Get<IntVec3?>("walkInSpot", null, false) ?? IntVec3.Invalid;
            } else {
                incidentParms.spawnCenter = this.dropSpot.GetValue(slate) ?? QuestGen.slate.Get<IntVec3?>("dropSpot", null, false) ?? IntVec3.Invalid;
            }
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
        private const string RootSymbol = "root";
        public List<PawnKindDef> CAD_pawnList;
        public float CAD_pointFactor;
        [MustTranslate]
        public string CAD_description;
        public float NOTED_lifespanFactor;
        public HediffDef NOTED_bonusHediff;
        [MustTranslate]
        public string NOTED_description;
    }
    public class QuestPart_RomeroMutators : QuestPart
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<PawnKindDef>(ref this.CAD_pawnType, "CAD_pawnType");
            Scribe_Values.Look<float>(ref this.CAD_pointFactor, "CAD_pointFactor", 1f, false);
            Scribe_Defs.Look<HediffDef>(ref this.NOTED_hediff, "NOTED_hediff");
            Scribe_Values.Look<float>(ref this.NOTED_lifespanFactor, "NOTED_lifespanFactor", 1f, false);
        }
        public PawnKindDef CAD_pawnType;
        public float CAD_pointFactor;
        public HediffDef NOTED_hediff;
        public float NOTED_lifespanFactor;
    }
    //aforementioned incident which implements the effects of romero2 and romero3 mutators
    public class IncidentWorker_RomeroAssault : IncidentWorker_ShamblerAssault
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (base.TryExecuteWorker(parms))
            {
                if (parms.quest != null)
                {
                    QuestPart_RomeroMutators qprm = parms.quest.GetFirstPartOfType<QuestPart_RomeroMutators>();
                    if (qprm != null && qprm.CAD_pawnType != null)
                    {
                        Map map = (Map)parms.target;
                        if (map != null)
                        {
                            IntVec3 iv3 = IntVec3.Invalid;
                            Pawn p = null;
                            if (!parms.pawnGroups.NullOrEmpty())
                            {
                                p = parms.pawnGroups.RandomElement().Key;
                                if (p != null)
                                {
                                    iv3 = p.Position;
                                }
                            }
                            if (!iv3.IsValid || !iv3.InBounds(map))
                            {
                                RCellFinder.TryFindRandomPawnEntryCell(out iv3, map, CellFinder.EdgeRoadChance_Hostile, false, null);
                            }
                            if (iv3.IsValid && iv3.InBounds(map))
                            {
                                float combatPower = qprm.CAD_pawnType.combatPower;
                                float points = Math.Max(parms.points * qprm.CAD_pointFactor, combatPower);
                                Lord lord = parms.lord;
                                if (lord == null)
                                {
                                    if (p != null)
                                    {
                                        lord = p.lord;
                                    }
                                }
                                Lord backupLord = LordMaker.MakeNewLord(parms.faction, new LordJob_AssaultColony(parms.faction, false, false, false, false, false, false, false), map, null);
                                while (points >= combatPower)
                                {
                                    Pawn CADdie = PawnGenerator.GeneratePawn(qprm.CAD_pawnType, parms.faction, null);
                                    GenSpawn.Spawn(CADdie, CellFinder.RandomClosewalkCellNear(iv3, map, 10), map, WipeMode.Vanish);
                                    if (!CADdie.Downed)
                                    {
                                        Lord lord2 = CADdie.lord;
                                        if (lord2 != null && lord2 != lord)
                                        {
                                            lord2.RemovePawn(CADdie);
                                        }
                                        if (!lord.CanAddPawn(CADdie))
                                        {
                                            backupLord.AddPawn(CADdie);
                                        } else {
                                            lord.AddPawn(CADdie);
                                        }
                                    }
                                    points -= combatPower;
                                }
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }
        protected override void PostProcessSpawnedPawns(IncidentParms parms, List<Pawn> pawns)
        {
            base.PostProcessSpawnedPawns(parms, pawns);
            QuestPart_RomeroMutators qprm = parms.quest.GetFirstPartOfType<QuestPart_RomeroMutators>();
            if (qprm != null && qprm.NOTED_hediff != null)
            {
                foreach (Pawn p in pawns)
                {
                    p.health.AddHediff(qprm.NOTED_hediff);
                    if (qprm.NOTED_lifespanFactor != 1f)
                    {
                        Hediff shambler = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Shambler);
                        if (shambler != null)
                        {
                            HediffComp_DisappearsAndKills hcdak = shambler.TryGetComp<HediffComp_DisappearsAndKills>();
                            if (hcdak != null)
                            {
                                hcdak.ticksToDisappear = (int)(hcdak.ticksToDisappear * qprm.NOTED_lifespanFactor);
                            }
                        }
                    }
                }
            }
        }
    }
}
