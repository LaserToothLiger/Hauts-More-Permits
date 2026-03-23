using HautsPermits;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Grammar;

namespace HautsPermits_Biotech
{
    //this is the root. random pawn kind from pawnKinds is the patient
    public class QuestNode_CaseStudy : QuestNode_EcosphereIntermediary
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            PawnKindDef pawnKindDef;
            if (pawnKinds.TryRandomElement(out pawnKindDef))
            {
                slate.Set<PawnKindDef>(this.storePawnKindAs.GetValue(slate), pawnKindDef, false);
            }
            base.RunInt();
        }
        [NoTranslate]
        public SlateRef<string> storePawnKindAs;
        public List<PawnKindDef> pawnKinds;
    }
    /*give the patient hediffDef (which is the neopathy in this case) and instate all three mutators
     * cs1: Darwin's Disfavor adds DD_bonusComplications to the max complications the HediffComp_NeopathyComplications can have, and flicks on its DD_on flag, allowing it to use a bonus pool of complications when getting new ones
     * cs2: Don't Expect Miracles gives DD_bonusComplications DEM_failureChance
     * cs3: Late Detection adds LD_bonusComplications to the max complications the HediffComp_NeopathyComplications can have, as well as to the number of complications it starts with
     * Your hediff comp is in another .cs file though*/
    public class QuestNode_AddNeopathy : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            if (this.pawns.GetValue(slate) == null || this.hediffDef.GetValue(slate) == null)
            {
                return;
            }
            QuestPart_AddNeopathy questPart_AddHediff = new QuestPart_AddNeopathy();
            questPart_AddHediff.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
            questPart_AddHediff.hediffDef = this.hediffDef.GetValue(slate);
            questPart_AddHediff.pawns.AddRange(this.pawns.GetValue(slate));
            questPart_AddHediff.addToHyperlinks = this.addToHyperlinks.GetValue(slate);
            bool mayhemMode = HVMP_Mod.settings.csX;
            bool DD_on = BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.cs1, mayhemMode);
            bool DEM_on = BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.cs2, mayhemMode);
            bool LD_on = BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.cs3, mayhemMode);
            if (DD_on)
            {
                questPart_AddHediff.DD_bonusComplications += this.DD_bonusComplications.RandomInRange;
                questPart_AddHediff.DD_on = true;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_DD_info", this.DD_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_DD_info", " ") });
            }
            if (DEM_on)
            {
                questPart_AddHediff.DEM_failureChance = this.DEM_failureChance;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_DEM_info", this.DEM_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_DEM_info", " ") });
            }
            if (LD_on)
            {
                questPart_AddHediff.LD_bonusComplications += this.LD_bonusComplications.RandomInRange;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_LD_info", this.LD_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_LD_info", " ") });
            }
            QuestGen.quest.AddPart(questPart_AddHediff);
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        public SlateRef<IEnumerable<Pawn>> pawns;
        public SlateRef<HediffDef> hediffDef;
        public SlateRef<IEnumerable<BodyPartDef>> partsToAffect;
        public SlateRef<bool> checkDiseaseContractChance;
        public SlateRef<bool> addToHyperlinks;
        public IntRange DD_bonusComplications;
        [MustTranslate]
        public string DD_description;
        public float DEM_failureChance;
        [MustTranslate]
        public string DEM_description;
        public IntRange LD_bonusComplications;
        [MustTranslate]
        public string LD_description;
    }
    public class QuestPart_AddNeopathy : QuestPart
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
                for (int i = 0; i < this.pawns.Count; i = num + 1)
                {
                    yield return this.pawns[i];
                    num = i;
                }
                yield break;
            }
        }
        public override IEnumerable<Dialog_InfoCard.Hyperlink> Hyperlinks
        {
            get
            {
                foreach (Dialog_InfoCard.Hyperlink hyperlink in base.Hyperlinks)
                {
                    yield return hyperlink;
                }
                if (this.addToHyperlinks)
                {
                    yield return new Dialog_InfoCard.Hyperlink(this.hediffDef, -1);
                }
                yield break;
            }
        }
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                for (int i = 0; i < this.pawns.Count; i++)
                {
                    if (!this.pawns[i].DestroyedOrNull())
                    {
                        Hediff h = HediffMaker.MakeHediff(this.hediffDef, this.pawns[i]);
                        HediffComp_NeopathyComplications hcnp = h.TryGetComp<HediffComp_NeopathyComplications>();
                        if (hcnp != null)
                        {
                            if (this.DD_on)
                            {
                                hcnp.DD_on = true;
                            }
                            hcnp.DEM_failureChance = this.DEM_failureChance;
                            hcnp.maxComplications += this.DD_bonusComplications + this.LD_bonusComplications;
                            for (int j = this.LD_bonusComplications; j > 0; j--)
                            {
                                hcnp.GainComplication();
                            }
                        }
                        this.pawns[i].health.AddHediff(h);
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<Pawn>(ref this.pawns, "pawns", LookMode.Reference, Array.Empty<object>());
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_Defs.Look<HediffDef>(ref this.hediffDef, "hediffDef");
            Scribe_Values.Look<bool>(ref this.addToHyperlinks, "addToHyperlinks", false, false);
            Scribe_Values.Look<bool>(ref this.DD_on, "DD_on", false, false);
            Scribe_Values.Look<float>(ref this.DEM_failureChance, "DEM_failureChance", 0f, false);
            Scribe_Values.Look<int>(ref this.LD_bonusComplications, "LD_bonusComplications", 0, false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.pawns.RemoveAll((Pawn x) => x == null);
            }
        }
        public override void AssignDebugData()
        {
            base.AssignDebugData();
            this.inSignal = "DebugSignal" + Rand.Int.ToString();
            this.hediffDef = HediffDefOf.Anesthetic;
            this.pawns.Add(PawnsFinder.AllMaps_FreeColonists.FirstOrDefault<Pawn>());
        }
        public override void ReplacePawnReferences(Pawn replace, Pawn with)
        {
            this.pawns.Replace(replace, with);
        }
        public List<Pawn> pawns = new List<Pawn>();
        public string inSignal;
        public HediffDef hediffDef;
        public bool addToHyperlinks;
        public bool DD_on;
        public float DEM_failureChance;
        public int DD_bonusComplications;
        public int LD_bonusComplications;
    }
    //sends a shuttle, but not on a timer, just when the hediff is removed! (doesn't care how you do it, just get rid of it!)
    public class QuestNode_ShuttleWhenCured : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return this.node == null || this.node.TestRun(slate);
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            QuestPart_ShuttleWhenCured qpswc = new QuestPart_ShuttleWhenCured();
            qpswc.inSignalComplete = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignalComplete.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
            if (this.lodgers.GetValue(slate) != null)
            {
                qpswc.lodgers.AddRange(this.lodgers.GetValue(slate));
            }
            if (this.node != null)
            {
                QuestGenUtility.RunInnerNode(this.node, qpswc);
            }
            if (!this.outSignalComplete.GetValue(slate).NullOrEmpty())
            {
                qpswc.outSignalsCompleted.Add(QuestGenUtility.HardcodedSignalWithQuestID(this.outSignalComplete.GetValue(slate)));
            }
            QuestGen.quest.AddPart(qpswc);
        }
        [NoTranslate]
        public SlateRef<string> inSignalComplete;
        [NoTranslate]
        public SlateRef<string> outSignalComplete;
        public SlateRef<IEnumerable<Pawn>> lodgers;
        public QuestNode node;
    }
    public class QuestPart_ShuttleWhenCured : QuestPartActivable
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
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignalComplete)
            {
                this.Enable(signal.args);
                this.Complete();
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
                return "HVMP_QuestPartShuttleArriveOnCure".Translate();
            }
        }
        public override string AlertExplanation
        {
            get
            {
                if (this.quest.hidden)
                {
                    return "HVMP_QuestPartShuttleArriveOnCure".Translate();
                }
                return "HVMP_QuestPartShuttleArriveOnCureDesc".Translate(this.quest.name, this.lodgers.Select((Pawn p) => p.LabelShort).ToLineList("- ", false));
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignalComplete, "inSignalComplete", null, false);
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
        public string inSignalComplete;
        public List<Pawn> lodgers = new List<Pawn>();
        public bool alert;
    }
}
