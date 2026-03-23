using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;
using Verse.Grammar;
using Verse.Sound;

namespace HautsPermits
{
    /*Forced Weather quest. Differs only in that it has mutators
     * caelum1: Mood Killeralso inflicts MK_gameCondition
     * caelum2: Sensory Overload stuns all organic, non-anomalous pawns on the map when the quest is accepted for SO_stunDuration ticks, and also gives them SO_hediff
     * caelum3: Tempest Engine also inflicts TE_gameCondition (a slate reference, which in this case is also drawn from QuestNode_GetRandomNegativeGameCondition) for [TE_durationPct * duration of primary condition]*/
    public class QuestNode_AllThreeCaelumMutators : QuestNode_GameCondition
    {
        protected override void RunInt()
        {
            base.RunInt();
            bool mayhemMode = HVMP_Mod.settings.caelumX;
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.caelum1, mayhemMode))
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_MK_info", this.MK_description.Formatted())
                });
                float num = slate.Get<float>("points", 0f, false);
                GameCondition gameCondition2 = GameConditionMaker.MakeCondition(this.MK_condition, this.duration.GetValue(slate));
                QuestPart_GameCondition questPart_GameCondition2 = new QuestPart_GameCondition();
                questPart_GameCondition2.gameCondition = gameCondition2;
                List<Rule> list2 = new List<Rule>();
                Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
                if (this.targetWorld.GetValue(slate))
                {
                    questPart_GameCondition2.targetWorld = true;
                    gameCondition2.RandomizeSettings(num, null, list2, dictionary2);
                } else {
                    Map map = BranchQuestSetupUtility.GetMap_QuestNodeGameCondition(slate);
                    questPart_GameCondition2.mapParent = map.Parent;
                    gameCondition2.RandomizeSettings(num, map, list2, dictionary2);
                }
                questPart_GameCondition2.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
                questPart_GameCondition2.sendStandardLetter = false;
                quest.AddPart(questPart_GameCondition2);
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_MK_info", " ") });
            }
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.caelum2, mayhemMode))
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_SO_info", this.SO_description.Formatted())
                });
                QuestPart_SensoryOverload qpSO = new QuestPart_SensoryOverload();
                qpSO.SO_hediff = this.SO_hediff;
                qpSO.SO_stunDuration = this.SO_stunDuration;
                qpSO.map = slate.Get<Map>("map", null, false);
                qpSO.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(quest.InitiateSignal);
                quest.AddPart(qpSO);
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_SO_info", " ") });
            }
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.caelum3, mayhemMode))
            {
                float num = QuestGen.slate.Get<float>("points", 0f, false);
                GameCondition gameCondition = GameConditionMaker.MakeCondition(this.TE_gameCondition.GetValue(slate), (int)(this.TE_duration.GetValue(slate) * this.TE_durationPct.RandomInRange));
                QuestPart_GameCondition questPart_GameCondition = new QuestPart_GameCondition
                {
                    gameCondition = gameCondition,
                    sendStandardLetter = false
                };
                List<Rule> list = new List<Rule>();
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                if (this.targetWorld.GetValue(slate))
                {
                    questPart_GameCondition.targetWorld = true;
                    gameCondition.RandomizeSettings(num, null, list, dictionary);
                } else {
                    Map map = BranchQuestSetupUtility.GetMap_QuestNodeGameCondition(QuestGen.slate);
                    questPart_GameCondition.mapParent = map.Parent;
                    gameCondition.RandomizeSettings(num, map, list, dictionary);
                }
                questPart_GameCondition.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
                QuestGen.quest.AddPart(questPart_GameCondition);
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_TE_info", this.TE_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_TE_info", " ") });
            }
        }
        [MustTranslate]
        public string TE_description;
        public FloatRange TE_durationPct;
        public SlateRef<GameConditionDef> TE_gameCondition;
        public SlateRef<int> TE_duration;
        public GameConditionDef MK_condition;
        [MustTranslate]
        public string MK_description;
        public HediffDef SO_hediff;
        public int SO_stunDuration;
        [MustTranslate]
        public string SO_description;
    }
    //wouldjve figured the Mood Killer game condition affects mood? These tins...
    public class ThoughtWorker_MoodKiller : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            Map mapHeld = p.MapHeld;
            if (mapHeld != null)
            {
                GameCondition mk = mapHeld.gameConditionManager.GetActiveCondition(HVMPDefOf.HVMP_MoodKiller);
                if (mk != null)
                {
                    return true;
                }
            }
            return false;
        }
    }
    //causes the stun and hediff assignation for Sensory Overload
    public class QuestPart_SensoryOverload : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag == this.inSignal)
            {
                if (this.SO_hediff != null)
                {
                    SoundDefOf.ShipTakeoff.PlayOneShotOnCamera(null);
                    foreach (Pawn p in this.map.mapPawns.AllPawnsSpawned)
                    {
                        if (p.RaceProps.IsFlesh && (!ModsConfig.AnomalyActive || !(p.RaceProps.IsAnomalyEntity && !p.IsMutant)))
                        {
                            p.health.AddHediff(this.SO_hediff);
                            p.stances.stunner.StunFor(this.SO_stunDuration, null);
                        }
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<HediffDef>(ref this.SO_hediff, "SO_hediff");
            Scribe_Values.Look<int>(ref this.SO_stunDuration, "SO_stunDuration", 180, false);
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_References.Look<Map>(ref this.map, "map", false);
        }
        public HediffDef SO_hediff;
        public int SO_stunDuration;
        public string inSignal;
        public Map map;
    }
}
