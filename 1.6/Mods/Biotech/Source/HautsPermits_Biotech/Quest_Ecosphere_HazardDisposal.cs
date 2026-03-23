using HautsPermits;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using Verse;
using Verse.Grammar;

namespace HautsPermits_Biotech
{
    /*like QuestNode_ManhunterPack, except using a different incident, assigning a random hediff from the MutationsPool mod extension of aformeentioned incident def, saving a lot of text for questdesc, and handling some mutators
     * hd1: Double Trouble assigns the quest part's DT_mutation field, using a different random hediff from the MutationsPool. The quest part will be referenced by IncidentWorker_HazardDisposal
     * hd2: multiplies the incident's points by MM_factor
     * hd3: Unpredictable Pleiotropy assigns the quest part's UP_chance*/
    public class QuestNode_MutantManhunterPack : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return Find.Storyteller.difficulty.allowViolentQuests && slate.Exists("map", false) && AggressiveAnimalIncidentUtility.TryFindAggressiveAnimalKind(slate.Get<float>("points", 0f, false), slate.Get<Map>("map", null, false), out PawnKindDef pawnKindDef);
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Map map = QuestGen.slate.Get<Map>("map", null, false);
            float num = QuestGen.slate.Get<float>("points", 0f, false);
            MutationsPool mp = HVMPDefOf.HVMP_MutantManhunterPack.GetModExtension<MutationsPool>();
            HediffDef hd = mp.mutations.RandomElement();
            slate.Set<string>("mutationLabel", hd.label, false);
            slate.Set<string>("mutationDesc", hd.description, false);
            bool mayhemMode = HVMP_Mod.settings.hdX;
            QuestPart_IncidentMutantMPs questPart_Incident = new QuestPart_IncidentMutantMPs
            {
                incident = HVMPDefOf.HVMP_MutantManhunterPack,
                mutation = hd
            };
            HediffDef hd2 = null;
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.hd1, mayhemMode))
            {
                List<HediffDef> possibleSecondMutations = mp.mutations;
                possibleSecondMutations.Remove(hd);
                hd2 = possibleSecondMutations.RandomElement();
                questPart_Incident.DT_mutation = hd2;
                slate.Set<string>("DT_mutationLabel", hd2.label, false);
                slate.Set<string>("DT_mutationDesc", hd2.description, false);
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_DT_info", this.DT_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_DT_info", " ") });
            }
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.hd3, mayhemMode))
            {
                questPart_Incident.UP_chance = this.UP_chance;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_UP_info", this.UP_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_UP_info", " ") });
            }
            IncidentParms incidentParms = new IncidentParms
            {
                forced = true,
                target = map,
                points = num,
                quest = QuestGen.quest,
                questTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(this.tag.GetValue(slate)),
                spawnCenter = this.walkInSpot.GetValue(slate) ?? QuestGen.slate.Get<IntVec3?>("walkInSpot", null, false) ?? IntVec3.Invalid,
                pawnCount = this.animalCount.GetValue(slate)
            };
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.hd2, mayhemMode))
            {
                incidentParms.points *= this.MM_factor;
            }
            if (AggressiveAnimalIncidentUtility.TryFindAggressiveAnimalKind(num, map, out PawnKindDef pawnKindDef))
            {
                incidentParms.pawnKind = pawnKindDef;
            }
            slate.Set<PawnKindDef>("animalKindDef", pawnKindDef, false);
            int num2 = ((incidentParms.pawnCount > 0) ? incidentParms.pawnCount : AggressiveAnimalIncidentUtility.GetAnimalsCount(pawnKindDef, num));
            QuestGen.slate.Set<int>("animalCount", num2, false);
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
            questPart_Incident.SetIncidentParmsAndRemoveTarget(incidentParms);
            questPart_Incident.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
            QuestGen.quest.AddPart(questPart_Incident);
            List<Rule> list = new List<Rule>
            {
                new Rule_String("animalKind_label", pawnKindDef.label),
                new Rule_String("animalKind_labelPlural", pawnKindDef.GetLabelPlural(num2))
            };
            QuestGen.AddQuestDescriptionRules(list);
            QuestGen.AddQuestNameRules(list);
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        public SlateRef<string> customLetterLabel;
        public SlateRef<string> customLetterText;
        public SlateRef<RulePack> customLetterLabelRules;
        public SlateRef<RulePack> customLetterTextRules;
        public SlateRef<IntVec3?> walkInSpot;
        public SlateRef<int> animalCount;
        [NoTranslate]
        public SlateRef<string> tag;
        [MustTranslate]
        public string DT_description;
        public float MM_factor;
        public float UP_chance;
        [MustTranslate]
        public string UP_description;
    }
    public class QuestPart_IncidentMutantMPs : QuestPart_Incident
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<HediffDef>(ref this.mutation, "mutation");
            Scribe_Defs.Look<HediffDef>(ref this.DT_mutation, "DT_mutation");
            Scribe_Values.Look<float>(ref this.UP_chance, "UP_chance", 0f, false);
        }
        public HediffDef mutation;
        public HediffDef DT_mutation;
        public float UP_chance;
    }
    //any and all possible mutations you want to give to the manhunters of this quest, put in this DME for the incident def
    public class MutationsPool : DefModExtension
    {
        public MutationsPool()
        {

        }
        public List<HediffDef> mutations;
    }
    //it's like a manhunter pack. But it also adds the mutation(s) from the quest and mutators alongside scaria
    public class IncidentWorker_HazardDisposal : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
            {
                return false;
            }
            Map map = (Map)parms.target;
            return this.def.HasModExtension<MutationsPool>() && AggressiveAnimalIncidentUtility.TryFindAggressiveAnimalKind(parms.points, map, out PawnKindDef pawnKindDef) && RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 intVec, map, CellFinder.EdgeRoadChance_Animal, false, null);
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (parms.quest == null)
            {
                return false;
            }
            Map map = (Map)parms.target;
            PawnKindDef pawnKind = parms.pawnKind;
            if ((pawnKind == null && !AggressiveAnimalIncidentUtility.TryFindAggressiveAnimalKind(parms.points, map, out pawnKind)) || AggressiveAnimalIncidentUtility.GetAnimalsCount(pawnKind, parms.points) == 0)
            {
                return false;
            }
            IntVec3 spawnCenter = parms.spawnCenter;
            if (!spawnCenter.IsValid && !RCellFinder.TryFindRandomPawnEntryCell(out spawnCenter, map, CellFinder.EdgeRoadChance_Animal, false, null))
            {
                return false;
            }
            List<Pawn> list = AggressiveAnimalIncidentUtility.GenerateAnimals(pawnKind, map.Tile, parms.points * 1f, parms.pawnCount);
            Rot4 rot = Rot4.FromAngleFlat((map.Center - spawnCenter).AngleFlat);
            QuestPart_IncidentMutantMPs qpimp = parms.quest.GetFirstPartOfType<QuestPart_IncidentMutantMPs>();
            if (qpimp != null)
            {
                MutationsPool mp = HVMPDefOf.HVMP_MutantManhunterPack.GetModExtension<MutationsPool>();
                List<HediffDef> possibleMutations = mp.mutations;
                possibleMutations.Remove(qpimp.mutation);
                if (qpimp.DT_mutation != null && possibleMutations.Contains(qpimp.DT_mutation))
                {
                    possibleMutations.Remove(qpimp.DT_mutation);
                }
                for (int i = 0; i < list.Count; i++)
                {
                    Pawn pawn = list[i];
                    IntVec3 intVec = CellFinder.RandomClosewalkCellNear(spawnCenter, map, 10, null);
                    QuestUtility.AddQuestTag(GenSpawn.Spawn(pawn, intVec, map, rot, WipeMode.Vanish, false, false), parms.questTag);
                    pawn.health.AddHediff(HediffDefOf.Scaria, null, null, null);
                    pawn.health.AddHediff(qpimp.mutation, null, null, null);
                    if (qpimp.DT_mutation != null)
                    {
                        pawn.health.AddHediff(qpimp.DT_mutation, null, null, null);
                    }
                    if (Rand.Chance(qpimp.UP_chance))
                    {
                        pawn.health.AddHediff(possibleMutations.RandomElement());
                    }
                    pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent, null, false, false, false, null, false, false, false);
                    pawn.mindState.exitMapAfterTick = Find.TickManager.TicksGame + Rand.Range(60000, 120000);
                }
            }
            base.SendStandardLetter("LetterLabelManhunterPackArrived".Translate(), "ManhunterPackArrived".Translate(pawnKind.GetLabelPlural(-1)), LetterDefOf.ThreatBig, parms, list[0], Array.Empty<NamedArgument>());
            Find.TickManager.slower.SignalForceNormalSpeedShort();
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.ForbiddingDoors, OpportunityType.Critical);
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.AllowedAreas, OpportunityType.Important);
            return true;
        }
    }
}
