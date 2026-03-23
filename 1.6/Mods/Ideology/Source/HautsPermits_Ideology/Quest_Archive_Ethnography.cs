using HautsPermits;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace HautsPermits_Ideology
{
    /*hospitality-like quest, except instead of a timer the lodgers must initiate social interactions with your pawns a specified num of times. No more hiding them away from your colonists, you gotta let them mingle
     * HospitalityPawnType makes the lodger pawn kind a random kind from pawnKinds*/
    public class QuestNode_HospitalityPawnType : QuestNode
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            PawnKindDef pawnKindDef;
            if (pawnKinds.TryRandomElement(out pawnKindDef))
            {
                slate.Set<PawnKindDef>(this.storePawnKindAs.GetValue(slate), pawnKindDef, false);
            }
        }
        protected override bool TestRunInt(Slate slate)
        {
            return !this.pawnKinds.NullOrEmpty();
        }
        [NoTranslate]
        public SlateRef<string> storePawnKindAs;
        public List<PawnKindDef> pawnKinds;
    }
    //handles the first mutator. ethnog1: Quiverful of Questions multplies value1 (the number of social interactions the lodgers must perform with your pawns) by QQ_factor
    public class QuestNode_Multiply_QQ : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return !this.storeAs.GetValue(slate).NullOrEmpty();
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            slate.Set<int>(this.storeAs.GetValue(slate), (int)(this.value1.GetValue(slate) * (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.ethnog1, HVMP_Mod.settings.ethnogX) ? this.QQ_factor : 1f)), false);
        }
        public SlateRef<int> value1;
        public float QQ_factor;
        [NoTranslate]
        public SlateRef<string> storeAs;
    }
    //handles the second mutator. ethnog2: Sensitive Souls gives all lodgers SS_hediff
    public class QuestNode_Give_SS : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            if (this.pawns.GetValue(slate) == null)
            {
                return;
            }
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.ethnog2, HVMP_Mod.settings.ethnogX))
            {
                QuestPart_Give_SS qpss = new QuestPart_Give_SS();
                qpss.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
                qpss.pawns.AddRange(this.pawns.GetValue(slate));
                qpss.hediff = this.SS_hediff;
                QuestGen.quest.AddPart(qpss);
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_SS_info_singular", this.SS_description_singular.Formatted()),
                    new Rule_String("mutator_SS_info_plural", this.SS_description_plural.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_SS_info_singular", " "), new Rule_String("mutator_SS_info_plural", " ") });
            }
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        public SlateRef<IEnumerable<Pawn>> pawns;
        public HediffDef SS_hediff;
        [MustTranslate]
        public string SS_description_singular;
        [MustTranslate]
        public string SS_description_plural;
    }
    public class QuestPart_Give_SS : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                for (int i = 0; i < this.pawns.Count; i++)
                {
                    Hediff hef = HediffMaker.MakeHediff(this.hediff, this.pawns[i]);
                    this.pawns[i].health.AddHediff(hef, null);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_Collections.Look<Pawn>(ref this.pawns, "pawns", LookMode.Reference, Array.Empty<object>());
            Scribe_Defs.Look<HediffDef>(ref this.hediff, "hediff");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.pawns.RemoveAll((Pawn x) => x == null);
            }
        }
        public override void ReplacePawnReferences(Pawn replace, Pawn with)
        {
            this.pawns.Replace(replace, with);
        }
        public string inSignal;
        public List<Pawn> pawns = new List<Pawn>();
        public HediffDef hediff;
    }
    /*handles the third mutator. ethnog3: Worse Than Useless assigns all lodgers a random-per-pawn trait from WTU_traitList.
     * If you have a mod that adds traits that make a pawn more irritating/dangerous to host, add it here*/
    public class QuestNode_WTU : QuestNode
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            if (this.pawns.GetValue(slate) == null || this.WTU_traitList.NullOrEmpty())
            {
                return;
            }
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.ethnog3, HVMP_Mod.settings.ethnogX))
            {
                List<Pawn> pawns = this.pawns.GetValue(slate).ToList();
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn_StoryTracker story = pawns[i].story;
                    if (story != null)
                    {
                        BackstoryTrait bt = this.WTU_traitList.Where((BackstoryTrait bst) => !story.traits.HasTrait(bst.def) && !story.traits.allTraits.Any((Trait tr) => bst.def.ConflictsWith(tr))).RandomElement();
                        if (bt != null)
                        {
                            story.traits.GainTrait(new Trait(bt.def, bt.degree, true), false);
                        }
                    }
                }
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_WTU_info_singular", this.WTU_description_singular.Formatted()),
                    new Rule_String("mutator_WTU_info_plural", this.WTU_description_plural.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_WTU_info_singular", " "), new Rule_String("mutator_WTU_info_plural", " ") });
            }
        }
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        public SlateRef<IEnumerable<Pawn>> pawns;
        public List<BackstoryTrait> WTU_traitList = new List<BackstoryTrait>();
        [MustTranslate]
        public string WTU_description_singular;
        [MustTranslate]
        public string WTU_description_plural;
    }
    //handles the shuttle arriving after enough social interactions are performed (num = questionsToAsk) by the lodgers, instead of being time-based
    public class QuestNode_ShuttleAnthro : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return this.node == null || this.node.TestRun(slate);
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            QuestPart_ShuttleAnthro qpsa = new QuestPart_ShuttleAnthro();
            qpsa.inSignalEnable = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignalEnable.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
            qpsa.questionsLeft = this.questionsToAsk.GetValue(slate);
            qpsa.questionsInit = this.questionsToAsk.GetValue(slate);
            if (this.lodgers.GetValue(slate) != null)
            {
                qpsa.lodgers.AddRange(this.lodgers.GetValue(slate));
            }
            qpsa.expiryInfoPart = "HVMP_ShuttleAnthroArrivesIn".Translate();
            qpsa.expiryInfoPartTip = "HVMP_ShuttleAnthroArrivesOn".Translate();
            if (this.node != null)
            {
                QuestGenUtility.RunInnerNode(this.node, qpsa);
            }
            if (!this.outSignalComplete.GetValue(slate).NullOrEmpty())
            {
                qpsa.outSignalsCompleted.Add(QuestGenUtility.HardcodedSignalWithQuestID(this.outSignalComplete.GetValue(slate)));
            }
            QuestGen.quest.AddPart(qpsa);
        }
        [NoTranslate]
        public SlateRef<string> inSignalEnable;
        [NoTranslate]
        public SlateRef<string> outSignalComplete;
        public SlateRef<int> questionsToAsk;
        public SlateRef<IEnumerable<Pawn>> lodgers;
        public QuestNode node;
    }
    public class QuestPart_ShuttleAnthro : QuestPartActivable
    {
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo globalTargetInfo in base.QuestLookTargets)
                {
                    yield return globalTargetInfo;
                }
                int num;
                for (int i = 0; i < this.lodgers.Count; i = num + 1)
                {
                    yield return this.lodgers[i];
                    num = i;
                }
                yield break;
            }
        }
        public override string ExpiryInfoPart
        {
            get
            {
                if (this.quest.Historical)
                {
                    return null;
                }
                return this.expiryInfoPart.Formatted(this.questionsLeft);
            }
        }
        public override string ExpiryInfoPartTip
        {
            get
            {
                return this.expiryInfoPartTip.Formatted(this.questionsInit);
            }
        }
        public override AlertReport AlertReport
        {
            get
            {
                if (!this.alert || base.State != QuestPartState.Enabled)
                {
                    return false;
                }
                return AlertReport.CulpritsAre(this.lodgers);
            }
        }
        public override string AlertLabel
        {
            get
            {
                return "HVMP_ShuttleArriveAnthro".Translate(this.questionsLeft);
            }
        }
        public override string AlertExplanation
        {
            get
            {
                if (this.quest.hidden)
                {
                    return "HVMP_ShuttleArriveAnthroDescHidden".Translate(this.questionsLeft.ToString().Colorize(this.ColorString));
                }
                return "HVMP_ShuttleArriveAnthroDesc".Translate(this.quest.name, this.questionsLeft.ToString().Colorize(this.ColorString), this.lodgers.Select((Pawn p) => p.LabelShort).ToLineList("- ", false));
            }
        }
        public void CompleteOnLastQuestion()
        {
            base.Complete();
        }
        public Color ColorString
        {
            get
            {
                return GenColor.FromHex("87f6f6");
            }
        }
        public override string ExtraInspectString(ISelectable target)
        {
            Pawn pawn = target as Pawn;
            if (pawn != null && this.lodgers.Contains(pawn))
            {
                return "HVMP_ShuttleAnthroInspectString".Translate(this.questionsLeft);
            }
            return null;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.questionsLeft, "questionsLeft", 0, false);
            Scribe_Values.Look<int>(ref this.questionsInit, "questionsInit", 0, false);
            Scribe_Values.Look<string>(ref this.expiryInfoPart, "expiryInfoPart", null, false);
            Scribe_Values.Look<string>(ref this.expiryInfoPartTip, "expiryInfoPartTip", null, false);
            Scribe_Collections.Look<Pawn>(ref this.lodgers, "lodgers", LookMode.Reference, Array.Empty<object>());
            Scribe_Values.Look<bool>(ref this.alert, "alert", false, false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.lodgers.RemoveAll((Pawn x) => x == null);
            }
        }
        public override void AssignDebugData()
        {
            base.AssignDebugData();
            if (Find.AnyPlayerHomeMap != null)
            {
                this.lodgers.AddRange(Find.RandomPlayerHomeMap.mapPawns.FreeColonists);
            }
        }
        public override void ReplacePawnReferences(Pawn replace, Pawn with)
        {
            this.lodgers.Replace(replace, with);
        }
        public int questionsLeft;
        public int questionsInit;
        public List<Pawn> lodgers = new List<Pawn>();
        public bool alert;
        public string expiryInfoPart;
        public string expiryInfoPartTip;
    }
}
