using HautsFramework;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;
using Verse.Grammar;

namespace HautsPermits
{
    /*one node and one part handles all the custom stuff about this quest. It's got very few moving parts, basically just adding a tradeBlockage to WorldComponent_BranchStuff and doing the mutators. Speaking of:
     * mm1: Communications Blackout registers CB_conditionDef to the world game condition manager for a random amount of ticks within CB_duration
     * mm2: Isolation Invites Predation adds default threat points (at time the quest is accepted) times IIP_pointFactor to the spyPoints of all hostile factions.
     *   When such a faction issues a raid, points are transferred out of the faction's spyPoints reserve to bolster the threat points of that raid. Any given raid can take up to as many spyPoints as its original threat points.
     * mm3: Untrustworthy Unreliability inflicts a goodwill penalty = a random value within UU_goodwillPenaltyAlly|Neutral|Hostile with each allied|neutral|hostile faction*/
    public class QuestNode_GiveRewardsMastermind : QuestNode_GiveRewardsBranch
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;
            WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
            if (wcbs != null)
            {
                slate.Set<int>(this.tradeBlocksRemaining.GetValue(slate), wcbs.tradeBlockages, false);
                QuestPart_TradeBlocker qptb = new QuestPart_TradeBlocker();
                qptb.map = QuestGen.slate.Get<Map>("map", null, false);
                qptb.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(quest.InitiateSignal);
                quest.AddPart(qptb);
                bool mayhemMode = HVMP_Mod.settings.mmX;
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.mm1, mayhemMode))
                {
                    qptb.CB_gcd = this.CB_conditionDef;
                    qptb.CB_duration = this.CB_duration.RandomInRange;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_CB_info", this.CB_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_CB_info", " ") });
                }
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.mm2, mayhemMode))
                {
                    qptb.IIP_pointFactor = this.IIP_pointFactor;
                    qptb.doIIP = true;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_IIP_info", this.IIP_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_IIP_info", " ") });
                }
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.mm3, mayhemMode))
                {
                    qptb.doUU = true;
                    qptb.UU_allyMax = this.UU_goodwillPenaltyAlly.max;
                    qptb.UU_allyMin = this.UU_goodwillPenaltyAlly.min;
                    qptb.UU_neutMax = this.UU_goodwillPenaltyNeutral.max;
                    qptb.UU_neutMin = this.UU_goodwillPenaltyNeutral.min;
                    qptb.UU_hostMax = this.UU_goodwillPenaltyHostile.max;
                    qptb.UU_hostMin = this.UU_goodwillPenaltyHostile.min;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_UU_info", this.UU_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_UU_info", " ") });
                }
            }
            base.RunInt();
        }
        public override string LetterText()
        {
            WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
            if (wcbs != null)
            {
                return base.LetterText().Translate(wcbs.tradeBlockages);
            }
            return base.LetterText();
        }
        [NoTranslate]
        public SlateRef<string> tradeBlocksRemaining;
        public GameConditionDef CB_conditionDef;
        public IntRange CB_duration;
        [MustTranslate]
        public string CB_description;
        public float IIP_pointFactor;
        [MustTranslate]
        public string IIP_description;
        public IntRange UU_goodwillPenaltyAlly;
        public IntRange UU_goodwillPenaltyNeutral;
        public IntRange UU_goodwillPenaltyHostile;
        [MustTranslate]
        public string UU_description;
    }
    public class QuestPart_TradeBlocker : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
                if (wcbs != null)
                {
                    wcbs.tradeBlockages++;
                }
                Faction p = Faction.OfPlayerSilentFail;
                if (p != null)
                {
                    if (this.CB_gcd != null)
                    {
                        GameCondition gameCondition = GameConditionMaker.MakeCondition(this.CB_gcd, -1);
                        gameCondition.Duration = this.CB_duration;
                        Find.World.GameConditionManager.RegisterCondition(gameCondition);
                    }
                    if (this.doIIP)
                    {
                        WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
                        if (WCFC != null)
                        {
                            float spyPoints = 0f;
                            Map m = this.map ?? Find.AnyPlayerHomeMap;
                            if (m != null)
                            {
                                spyPoints = StorytellerUtility.DefaultThreatPointsNow(m);
                            }
                            spyPoints *= this.IIP_pointFactor;
                            foreach (Faction f in Find.FactionManager.AllFactionsListForReading)
                            {
                                if (f != p && f.RelationKindWith(p) == FactionRelationKind.Hostile)
                                {
                                    Hauts_FactionCompHolder fch = WCFC.FindCompsFor(f);
                                    if (fch != null)
                                    {
                                        HautsFactionComp_SpyPoints spc = fch.TryGetComp<HautsFactionComp_SpyPoints>();
                                        if (spc != null)
                                        {
                                            spc.spyPoints += (int)spyPoints;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (this.doUU)
                    {
                        IntRange allyRange = new IntRange(this.UU_allyMin, this.UU_allyMax);
                        IntRange neutRange = new IntRange(this.UU_neutMin, this.UU_neutMax);
                        IntRange hostRange = new IntRange(this.UU_hostMin, this.UU_hostMax);
                        foreach (Faction f in Find.FactionManager.AllFactionsListForReading)
                        {
                            if (f != p)
                            {
                                EBranchQuests gq = f.def.GetModExtension<EBranchQuests>();
                                if (gq == null)
                                {
                                    switch (f.RelationKindWith(p))
                                    {
                                        case FactionRelationKind.Ally:
                                            f.TryAffectGoodwillWith(p, allyRange.RandomInRange, false, true);
                                            break;
                                        case FactionRelationKind.Hostile:
                                            f.TryAffectGoodwillWith(p, hostRange.RandomInRange, false, true);
                                            break;
                                        default:
                                            f.TryAffectGoodwillWith(p, neutRange.RandomInRange, false, true);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_Defs.Look<GameConditionDef>(ref this.CB_gcd, "CB_gcd");
            Scribe_Values.Look<int>(ref this.CB_duration, "CB_duration", 0, false);
            Scribe_Values.Look<bool>(ref this.doIIP, "doCB", false, false);
            Scribe_Values.Look<bool>(ref this.doUU, "doCB", false, false);
            Scribe_Values.Look<float>(ref this.IIP_pointFactor, "IIP_pointFactor", 0.5f, false);
            Scribe_Values.Look<int>(ref this.UU_allyMax, "UU_allyMax", 0, false);
            Scribe_Values.Look<int>(ref this.UU_allyMin, "UU_allyMin", 0, false);
            Scribe_Values.Look<int>(ref this.UU_neutMax, "UU_neutMax", 0, false);
            Scribe_Values.Look<int>(ref this.UU_neutMin, "UU_neutMin", 0, false);
            Scribe_Values.Look<int>(ref this.UU_hostMax, "UU_hostMax", 0, false);
            Scribe_Values.Look<int>(ref this.UU_hostMin, "UU_hostMin", 0, false);
            Scribe_References.Look<Map>(ref this.map, "map", false);
        }
        public string inSignal;
        public GameConditionDef CB_gcd;
        public int CB_duration;
        public float IIP_pointFactor;
        public bool doIIP;
        public bool doUU;
        public int UU_allyMax;
        public int UU_allyMin;
        public int UU_neutMax;
        public int UU_neutMin;
        public int UU_hostMax;
        public int UU_hostMin;
        public Map map;
    }
}
