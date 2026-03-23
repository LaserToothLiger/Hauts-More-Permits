using HautsFramework;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Grammar;

namespace HautsPermits
{
    /*if you haven't got max goodwill with all non-branch factions you could have goodwill with, this runs elseNode. Otherwise, it runs noGoodwillableFactionsNode.
     * goodwillThree|Two|OneStarRange are used to determine the magnitude of goodwillAmount (relevant to QuestNode_PaxMundiTracker) as based on the quest's challenge rating.
     *   Since this is "how much net goodwill you must earn to succeed the quest", obviously you want higher challenge ratings to have higher goodwillAmounts.*/
    public class QuestNode_PaxMundi : QuestNode
    {
        protected override void RunInt()
        {
            if (BranchQuestSetupUtility.TryFindPaxFaction(out Faction paxFaction))
            {
                Slate slate = QuestGen.slate;
                float points = slate.Get<float>("points", 0f, false);
                IntRange goodwillAmount;
                switch (QuestGen.quest.challengeRating)
                {
                    case 3:
                        goodwillAmount = this.goodwillThreeStarRange;
                        break;
                    case 2:
                        goodwillAmount = this.goodwillTwoStarRange;
                        break;
                    case 1:
                        goodwillAmount = this.goodwillOneStarRange;
                        break;
                    default:
                        goodwillAmount = this.goodwillTwoStarRange;
                        break;
                }
                slate.Set<bool>("noGoodwillableFactions", !this.TryFindFaction(out Faction faction), false);
                slate.Set<int>("goodwillAmount", Rand.Range(goodwillAmount.min, goodwillAmount.max), false);
                this.DoWork(QuestGen.slate, delegate (QuestNode n)
                {
                    n.Run();
                    return true;
                });
            }
        }
        private bool DoWork(Slate slate, Func<QuestNode, bool> func)
        {
            if (slate.Get<bool>("noGoodwillableFactions", false, false))
            {
                if (this.noGoodwillableFactionsNode != null)
                {
                    return func(this.noGoodwillableFactionsNode);
                }
            } else if (this.elseNode != null) {
                return func(this.elseNode);
            }
            return true;
        }
        private bool TryFindFaction(out Faction faction)
        {
            return (from x in Find.FactionManager.GetFactions(false, false, false, TechLevel.Undefined, false)
                    where this.IsGoodFaction(x)
                    select x).TryRandomElement(out faction);
        }
        private bool IsGoodFaction(Faction faction)
        {
            if (faction.def.HasModExtension<EBranchQuests>() || faction.IsPlayer || !faction.HasGoodwill || faction.def.PermanentlyHostileTo(Faction.OfPlayer.def) || faction.GoodwillWith(Faction.OfPlayer) >= 100)
            {
                return false;
            }
            return true;
        }
        protected override bool TestRunInt(Slate slate)
        {
            return BranchQuestSetupUtility.TryFindPaxFaction(out Faction paxFaction);
        }
        public IntRange goodwillThreeStarRange;
        public IntRange goodwillTwoStarRange;
        public IntRange goodwillOneStarRange;
        public QuestNode noGoodwillableFactionsNode;
        public QuestNode elseNode;
    }
    /*This is what is in the elseNode. You need to get goodwillAmount to win (or max out all possible goodwill).
     * When the quest is accepted, QuestPart_PaxMundi adds itself to the WorldComponent_BranchStuff's "qppms", which ensures that goodwill gains always go to the oldest one, and that goodwill gain in excess of that needed
     *   to succeed on the oldest one is then passed onto the next oldest one.
     * Handles two of the mutators:
     * mundi1: Marshal the Enemies of Paradise adds default threat points (at time the quest is accepted) times MTEOP_pointFactor to the spyPoints of all permanently-hostile factions.
     *   See Quest_Commerce_Mastermind.cs for an explanatory comment on spyPoints, or read the Framework documentation.
     * mundi3: The Wages of Sin flips on TWOS_on, which causes any goodwill losses to count twice as much against the net goodwill gain that the quest is tracking.*/
    public class QuestNode_PaxMundiTracker : QuestNode
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;
            QuestPart_PaxMundi qppm;
            if (!quest.TryGetFirstPartOfType<QuestPart_PaxMundi>(out qppm))
            {
                qppm = quest.AddPart<QuestPart_PaxMundi>();
                qppm.goodwillChangesInt = 0;
                qppm.denominator = slate.Get<int>("goodwillAmount", 0, false);
                qppm.inSignalEnable = slate.Get<string>("inSignal", null, false);
            }
            bool mayhemMode = HVMP_Mod.settings.mundiX;
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.mundi1, mayhemMode))
            {
                qppm.MTEOP_pointFactor = this.MTEOP_pointFactor;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_MTEOP_info", this.MTEOP_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_MTEOP_info", " ") });
            }
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.mundi3, mayhemMode))
            {
                qppm.TWOS_on = true;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_TWOS_info", this.TWOS_description.Formatted())
                    });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_TWOS_info", " ") });
            }
        }
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        public float MTEOP_pointFactor;
        [MustTranslate]
        public string MTEOP_description;
        [MustTranslate]
        public string TWOS_description;
    }
    public class QuestPart_PaxMundi : QuestPartActivable
    {
        public override string ExpiryInfoPart
        {
            get
            {
                return "HVMP_GoodwillCurried".Translate(this.goodwillChangesInt, this.denominator);
            }
        }
        public override void PostQuestAdded()
        {
            WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
            if (wcbs != null)
            {
                wcbs.qppms.Add(this);
            }
        }
        public override void QuestPartTick()
        {
            base.QuestPartTick();
            if (this.MTEOP_pointFactor > 0f)
            {
                Faction p = Faction.OfPlayerSilentFail;
                WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
                if (WCFC != null && p != null)
                {
                    float spyPoints = 0f;
                    Map m = Find.AnyPlayerHomeMap;
                    spyPoints = m != null ? StorytellerUtility.DefaultThreatPointsNow(m) : StorytellerUtility.DefaultThreatPointsNow(Find.World);
                    spyPoints *= this.MTEOP_pointFactor;
                    foreach (Faction f in Find.FactionManager.AllFactionsListForReading)
                    {
                        if (f != p && f.def.PermanentlyHostileTo(p.def))
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
                this.MTEOP_pointFactor = 0f;
            }
            if (this.goodwillChangesInt >= this.denominator)
            {
                base.Complete();
            }
            if (this.quest.State == QuestState.Ongoing)
            {
                WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
                if (wcbs != null && wcbs.qppms != null && !wcbs.qppms.Contains(this))
                {
                    wcbs.qppms.Add(this);
                }
            }
        }
        public string OutSignalCompletedPO
        {
            get
            {
                return string.Concat(new object[]
                {
                    "Quest",
                    this.quest.id,
                    ".desiredGoodwillReached"
                });
            }
        }
        protected override void Complete(SignalArgs signalArgs)
        {
            base.Complete(signalArgs);
            Find.SignalManager.SendSignal(new Signal(this.OutSignalCompletedPO, signalArgs, false));
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.goodwillChangesInt, "goodwillChangesInt", 0, false);
            Scribe_Values.Look<int>(ref this.denominator, "denominator", 0, false);
            Scribe_Values.Look<float>(ref this.MTEOP_pointFactor, "MTEOP_pointFactor", 0f, false);
            Scribe_Values.Look<bool>(ref this.TWOS_on, "TWOS_on", false, false);
        }
        public int goodwillChangesInt;
        public int denominator;
        public float MTEOP_pointFactor;
        public bool TWOS_on;
    }
    //handles one mutator. mundi2: Peace In Our Lifetime runs node.
    public class QuestNode_MundiPIOL : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.mundi2, HVMP_Mod.settings.mundiX))
            {
                if (this.node != null)
                {
                    this.node.Run();
                }
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_PIOL_info", this.PIOL_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_PIOL_info", " ") });
            }
        }
        public QuestNode node;
        [MustTranslate]
        public string PIOL_description;
    }
}
