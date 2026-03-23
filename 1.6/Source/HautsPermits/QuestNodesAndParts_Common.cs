using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HautsPermits
{
    //place right before the Success node for a branch quest, as it makes such quests reward ONLY goodwill or standing (favor)
    public class QuestNode_GiveRewardsBranch : QuestNode
    {
        protected override void RunInt()
        {
            this.parms.giverFaction = this.faction.GetValue(QuestGen.slate);
            this.parms.allowGoodwill = true;
            this.parms.allowRoyalFavor = true;
            this.parms.thingRewardDisallowed = true;
            QuestGen.quest.GiveRewards(this.parms, this.inSignal.GetValue(QuestGen.slate), this.customLetterLabel.GetValue(QuestGen.slate), this.LetterText(), null, null, false, delegate
            {
                QuestNode questNode = this.nodeIfChosenPawnSignalUsed;
                if (questNode == null)
                {
                    return;
                }
                questNode.Run();
            }, this.variants.GetValue(QuestGen.slate), false, this.parms.giverFaction.leader);
        }
        public virtual string LetterText()
        {
            return this.customLetterText.GetValue(QuestGen.slate);
        }
        protected override bool TestRunInt(Slate slate)
        {
            return this.nodeIfChosenPawnSignalUsed == null || this.nodeIfChosenPawnSignalUsed.TestRun(slate);
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        public QuestNode nodeIfChosenPawnSignalUsed;
        public RewardsGeneratorParams parms;
        public SlateRef<string> customLetterLabel;
        public SlateRef<string> customLetterText;
        public SlateRef<Faction> faction;
        public SlateRef<int?> variants;
    }
    //apply Hostile Environment Film to all pawns in the provided slate ref. Used for various hospitality quests, to ensure that they don't immediately die just because you live somewhere weird.
    public class QuestNode_GiveHostileEnvironmentFilm : QuestNode
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
            QuestPart_GiveHostileEnvironmentFilm qpghef = new QuestPart_GiveHostileEnvironmentFilm();
            qpghef.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
            qpghef.pawns.AddRange(this.pawns.GetValue(slate));
            QuestGen.quest.AddPart(qpghef);
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        public SlateRef<IEnumerable<Pawn>> pawns;
    }
    public class QuestPart_GiveHostileEnvironmentFilm : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                for (int i = 0; i < this.pawns.Count; i++)
                {
                    Hediff hef = HediffMaker.MakeHediff(HVMPDefOf.HVMP_HostileEnvironmentFilm, this.pawns[i]);
                    this.pawns[i].health.AddHediff(hef, null);
                    hef.Severity = 1f;
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_Collections.Look<Pawn>(ref this.pawns, "pawns", LookMode.Reference, Array.Empty<object>());
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
    }
    //various quest nodes invoke this to impose the mod setting-determined goodwill penalties for failing their quests
    public class QuestPart_BranchGoodwillFailureHandler : QuestPart
    {
        public override void Notify_PreCleanup()
        {
            base.Notify_PreCleanup();
            int num = BranchQuestSetupUtility.ExpectationBasedGoodwillLoss(null, true, true, this.faction);
            if (this.quest.State == QuestState.EndedOfferExpired)
            {
                Faction.OfPlayer.TryAffectGoodwillWith(this.faction, num, true, true, HVMPDefOf.HVMP_IgnoredQuest, null);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Faction>(ref this.faction, "faction", false);
        }
        public Faction faction;
    }
    /*applies a factor to the magnitude of quest rewards. This utilizes the questRewardFactor mod setting, and also can be multiplied by a rewardFactor value.
     * E.g. I think Mastermind is too easy, so I apply a smaller rewardFactor; lesser reward given the lesser risk.*/
    public class QuestNode_SetRewardValue_BranchSettingScaling : QuestNode_Sequence
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            BranchQuestSetupUtility.SetSettingScalingRewardValue(slate, rewardFactor);
            base.RunInt();
        }
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        public float rewardFactor = 1f;
    }
    //basically QuestNode_End, except it imposes QuestPart_BranchGoodwillChange with the specified faction. Put this on failure outcomes to make the failure mod settings apply to that quest.
    public class QuestNode_EndBranch : QuestNode_End
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Map map = slate.Get<Map>("map");
            if (this.faction != null)
            {
                QuestPart_BranchGoodwillChange qpbgc = new QuestPart_BranchGoodwillChange();
                qpbgc.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
                qpbgc.faction = Find.FactionManager.FirstFactionOfDef(this.faction);
                qpbgc.historyEvent = this.goodwillChangeReason.GetValue(slate);
                slate.Set<string>("goodwillPenalty", "HVMP_GoodwillLoss".Translate(), false);
                QuestGen.quest.AddPart(qpbgc);
            }
            QuestPart_QuestEnd questPart_QuestEnd = new QuestPart_QuestEnd();
            questPart_QuestEnd.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
            questPart_QuestEnd.outcome = new QuestEndOutcome?(this.outcome.GetValue(slate));
            questPart_QuestEnd.signalListenMode = this.signalListenMode.GetValue(slate) ?? QuestPart.SignalListenMode.OngoingOnly;
            questPart_QuestEnd.sendLetter = this.sendStandardLetter.GetValue(slate) ?? false;
            QuestGen.quest.AddPart(questPart_QuestEnd);
        }
        public FactionDef faction;
    }
    //also as QuestNode_EndBranch, also, we clean up all of the world objects found in siteToDestroy.
    public class QuestNode_EndAndDestroySite : QuestNode_End
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            QuestPart_DestroySite qp = new QuestPart_DestroySite();
            qp.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
            qp.worldObjects = this.siteToDestroy.GetValue(slate);
            Map map = slate.Get<Map>("map");
            if (this.faction != null && this.outcome.GetValue(slate) == QuestEndOutcome.Fail)
            {
                QuestPart_BranchGoodwillChange qpbgc = new QuestPart_BranchGoodwillChange();
                qpbgc.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
                qpbgc.faction = Find.FactionManager.FirstFactionOfDef(this.faction);
                qpbgc.historyEvent = this.goodwillChangeReason.GetValue(slate);
                slate.Set<string>("goodwillPenalty", "HVMP_GoodwillLoss".Translate(), false);
                QuestGen.quest.AddPart(qpbgc);
            }
            qp.outcome = new QuestEndOutcome?(this.outcome.GetValue(slate));
            qp.signalListenMode = this.signalListenMode.GetValue(slate) ?? QuestPart.SignalListenMode.OngoingOnly;
            qp.sendLetter = this.sendStandardLetter.GetValue(slate) ?? false;
            QuestGen.quest.AddPart(qp);
        }
        public SlateRef<List<WorldObject>> siteToDestroy;
        public FactionDef faction;
    }
    //mama needs you to be brave and figure out what this one does on your own. A hint from mama: the name contains some important words. Another hint: you can glean further context by looking at the XML.
    [StaticConstructorOnStartup]
    public class QuestPart_BranchGoodwillChange : QuestPart
    {
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo globalTargetInfo in base.QuestLookTargets)
                {
                    yield return globalTargetInfo;
                }
                yield return this.lookTarget;
                yield break;
            }
        }
        public override IEnumerable<Faction> InvolvedFactions
        {
            get
            {
                foreach (Faction faction in base.InvolvedFactions)
                {
                    yield return faction;
                }
                if (this.faction != null)
                {
                    yield return this.faction;
                }
                yield break;
            }
        }
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal && this.faction != null && this.faction != Faction.OfPlayer)
            {
                if (this.lookTarget.IsValid)
                {
                    GlobalTargetInfo globalTargetInfo = this.lookTarget;
                } else if (this.getLookTargetFromSignal) {
                    if (SignalArgsUtility.TryGetLookTargets(signal.args, "SUBJECT", out LookTargets lookTargets))
                    {
                        lookTargets.TryGetPrimaryTarget();
                    } else {
                        GlobalTargetInfo invalid = GlobalTargetInfo.Invalid;
                    }
                } else {
                    GlobalTargetInfo invalid2 = GlobalTargetInfo.Invalid;
                }
                FactionRelationKind playerRelationKind = this.faction.PlayerRelationKind;
                int num = BranchQuestSetupUtility.ExpectationBasedGoodwillLoss(null, true, false, this.faction);
                if (this.ensureMakesHostile)
                {
                    num = Mathf.Min(num, Faction.OfPlayer.GoodwillToMakeHostile(this.faction));
                }
                Faction.OfPlayer.TryAffectGoodwillWith(this.faction, num, this.canSendMessage, this.canSendHostilityLetter, (num >= 0) ? (this.historyEvent ?? HistoryEventDefOf.QuestGoodwillReward) : this.historyEvent, null);
                TaggedString taggedString = "";
                this.faction.TryAppendRelationKindChangedInfo(ref taggedString, playerRelationKind, this.faction.PlayerRelationKind, null);
                if (!taggedString.NullOrEmpty())
                {
                    taggedString = "\n\n" + taggedString;
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<HistoryEventDef>(ref this.historyEvent, "historyEvent");
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_References.Look<Faction>(ref this.faction, "faction", false);
            Scribe_Values.Look<bool>(ref this.canSendMessage, "canSendMessage", true, false);
            Scribe_Values.Look<bool>(ref this.canSendHostilityLetter, "canSendHostilityLetter", true, false);
            Scribe_Values.Look<bool>(ref this.getLookTargetFromSignal, "getLookTargetFromSignal", true, false);
            Scribe_TargetInfo.Look(ref this.lookTarget, "lookTarget");
            Scribe_Values.Look<bool>(ref this.ensureMakesHostile, "ensureMakesHostile", false, false);
        }
        public override void AssignDebugData()
        {
            base.AssignDebugData();
            this.inSignal = "DebugSignal" + Rand.Int.ToString();
            this.faction = Find.FactionManager.RandomNonHostileFaction(false, false, false, TechLevel.Undefined);
        }
        public HistoryEventDef historyEvent;
        public string inSignal;
        public Faction faction;
        public bool canSendMessage = true;
        public bool canSendHostilityLetter = true;
        public bool getLookTargetFromSignal = true;
        public GlobalTargetInfo lookTarget;
        public bool ensureMakesHostile;
    }
    //ewisott
    [StaticConstructorOnStartup]
    public class QuestPart_DestroySite : QuestPart_QuestEnd
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag == this.inSignal && this.worldObjects != null)
            {
                QuestEndOutcome questEndOutcome;
                if (this.outcome != null)
                {
                    questEndOutcome = this.outcome.Value;
                }
                else if (!signal.args.TryGetArg<QuestEndOutcome>("OUTCOME", out questEndOutcome))
                {
                    questEndOutcome = QuestEndOutcome.Unknown;
                }
                this.quest.End(questEndOutcome, this.sendLetter, this.playSound);
                foreach (WorldObject wo in this.worldObjects)
                {
                    if (!wo.Destroyed)
                    {
                        wo.Destroy();
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<WorldObject>(ref this.worldObjects, "worldObjects", LookMode.Reference, Array.Empty<object>());
        }
        public List<WorldObject> worldObjects;
    }
    //just makes the specified Thing be a quest look target (looking at the quest in the Quests tab will provide a hyperlink to go to the item)
    public class QuestPart_LookAtThis : QuestPart
    {
        public QuestPart_LookAtThis() { }
        public QuestPart_LookAtThis(Thing thing)
        {
            this.thing = thing;
        }
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo globalTargetInfo in base.QuestLookTargets)
                {
                    yield return globalTargetInfo;
                }
                if (this.thing != null)
                {
                    yield return this.thing;
                }
                yield break;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Thing>(ref this.thing, "thing", false);
        }
        public override void Cleanup()
        {
            base.Cleanup();
            this.thing = null;
        }
        private Thing thing;
    }
    //as LookAtThis, but for world objects
    public class QuestNode_LookOverHere : QuestNode
    {
        protected override void RunInt()
        {
            if (this.worldObject != null)
            {
                QuestGen.quest.AddPart(new QuestPart_LookOverHere(worldObject));
            }
            if (this.worldObjects.GetValue(QuestGen.slate) != null)
            {
                foreach (WorldObject wobj in this.worldObjects.GetValue(QuestGen.slate))
                {
                    QuestGen.quest.AddPart(new QuestPart_LookOverHere(wobj));
                }
            }
        }
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        private WorldObject worldObject;
        public SlateRef<IEnumerable<WorldObject>> worldObjects;
    }
    public class QuestPart_LookOverHere : QuestPart
    {
        public QuestPart_LookOverHere() { }
        public QuestPart_LookOverHere(WorldObject wo)
        {
            this.wo = wo;
        }
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo globalTargetInfo in base.QuestLookTargets)
                {
                    yield return globalTargetInfo;
                }
                if (this.wo != null)
                {
                    yield return this.wo;
                }
                yield break;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<WorldObject>(ref this.wo, "wo", false);
        }
        public override void Cleanup()
        {
            base.Cleanup();
            this.wo = null;
        }
        private WorldObject wo;
    }
}
