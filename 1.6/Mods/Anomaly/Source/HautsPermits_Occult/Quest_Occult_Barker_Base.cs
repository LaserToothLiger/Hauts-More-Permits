using HautsFramework;
using HautsPermits;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace HautsPermits_Occult
{
    /*Barker cannot be run if you don't meet the conventional conditions to be able to study and suppress anomalous objects. Otherwise you have no solution to the unnatural corpse or the cube, they just fuck you over.
     * Generates at least one QuestPart_MakeAndAcceptNewQuests, with the quest type that is made and auto-accepted being randomly drawn from possibleQuests.
     * This also handles all three mutators:
     * barker1: Late Grows the Hour sets LGTH_storeAs to true, which is referenced by the various quest nodes that set up each possible item.
     * barker2: Repo Reapers adds QuestPart_Barker_RR with a min and max value set to RR_provocationDelayHoursRange's min and max
     * barker3: Twin Terrors adds TT_bonusItems to the number of QuestPart_MakeAndAcceptNewQuests that will be instantiated*/
    public class QuestNode_GiveRewardsBarker : QuestNode_GiveRewardsBranch
    {
        protected override bool TestRunInt(Slate slate)
        {
            return base.TestRunInt(slate) && (Find.Anomaly.HighestLevelReached >= 1 || !Find.Anomaly.GenerateMonolith);
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;
            bool mayhemMode = HVMP_Mod.settings.barkerX;
            bool LGTH_on = BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.barker1, mayhemMode);
            bool RR_on = BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.barker2, mayhemMode);
            int TT_on = 1 + (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.barker3, mayhemMode) ? this.TT_bonusItems : 0);
            slate.Set<bool>(this.LGTH_storeAs.GetValue(slate), LGTH_on, false);
            if (LGTH_on)
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_LGTH_info", this.LGTH_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_LGTH_info", " ") });
            }
            if (RR_on)
            {
                QuestPart_Barker_RR qppd = new QuestPart_Barker_RR
                {
                    inSignal = QuestGenUtility.HardcodedSignalWithQuestID(quest.InitiateSignal),
                    provocationDelayHoursMin = this.RR_provocationDelayHoursRange.min,
                    provocationDelayHoursMax = this.RR_provocationDelayHoursRange.max
                };
                quest.AddPart(qppd);
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_RR_info", this.RR_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_RR_info", " ") });
            }
            if (TT_on > 1)
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_TT_info", this.TT_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_TT_info", this.nonTT_description.Formatted())
                });
            }
            while (TT_on > 0)
            {
                TT_on--;
                this.AddQuestlet(quest, LGTH_on);
            }
            base.RunInt();
        }
        public void AddQuestlet(Quest quest, bool LGTH_on)
        {
            QuestPart_MakeAndAcceptNewQuest qppd = new QuestPart_MakeAndAcceptNewQuest
            {
                inSignal = QuestGenUtility.HardcodedSignalWithQuestID(quest.InitiateSignal),
                questToMake = this.possibleQuests.RandomElement(),
                LGTH_on = LGTH_on
            };
            quest.AddPart(qppd);
        }
        public List<QuestScriptDef> possibleQuests;
        [MustTranslate]
        public string LGTH_description;
        [NoTranslate]
        public SlateRef<string> LGTH_storeAs;
        public IntRange RR_provocationDelayHoursRange;
        [MustTranslate]
        public string RR_description;
        public int TT_bonusItems;
        [MustTranslate]
        public string TT_description;
        [MustTranslate]
        public string nonTT_description;
    }
    //upon accepting the quest, creates a quest of the questToMake type and automatically accepts it on your behalf. Also passes on the LGTH_on ref (which is what LGTH_storeAs should be set to) to that quest
    public class QuestPart_MakeAndAcceptNewQuest : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                IIncidentTarget randomPlayerHomeMap = Find.RandomPlayerHomeMap;
                if (this.questToMake != null && this.questToMake.CanRun(this.QuestPoints, randomPlayerHomeMap ?? Find.World))
                {
                    Slate newSlate = new Slate();
                    newSlate.Set<float>("points", this.QuestPoints, false);
                    newSlate.Set<bool>("LGTH_on", QuestGen.slate.Get<bool>("LGTH_on", LGTH_on, false), false);
                    Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(this.questToMake, newSlate);
                    quest.SetInitiallyAccepted();
                    quest.Initiate();
                }
            }
        }
        public float QuestPoints
        {
            get
            {
                return Math.Max(QuestGen.slate.Get<float>("points"), 1f);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_Defs.Look<QuestScriptDef>(ref this.questToMake, "questToMake");
            Scribe_Values.Look<bool>(ref this.LGTH_on, "LGTH_on", false, false);
        }
        public string inSignal;
        public QuestScriptDef questToMake;
        public bool LGTH_on;
    }
    //provokes the void, occuring after a number of ticks between provocationDelayHoursMin and provocationDelayHoursMax
    public class QuestPart_Barker_RR : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                Map map = Find.RandomPlayerHomeMap;
                List<IncidentDef> list = new List<IncidentDef>();
                bool flag = false;
                foreach (EntityCategoryDef entityCategoryDef in DefDatabase<EntityCategoryDef>.AllDefs.OrderBy((EntityCategoryDef x) => x.listOrder))
                {
                    foreach (EntityCodexEntryDef entityCodexEntryDef in DefDatabase<EntityCodexEntryDef>.AllDefs)
                    {
                        if (entityCodexEntryDef.category == entityCategoryDef && !entityCodexEntryDef.provocationIncidents.NullOrEmpty<IncidentDef>() && !entityCodexEntryDef.Discovered)
                        {
                            foreach (IncidentDef incidentDef in entityCodexEntryDef.provocationIncidents)
                            {
                                IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(incidentDef.category, map);
                                incidentParms.bypassStorytellerSettings = true;
                                if (incidentDef.Worker.CanFireNow(incidentParms))
                                {
                                    list.Add(incidentDef);
                                    flag = true;
                                }
                            }
                        }
                    }
                    if (flag)
                    {
                        break;
                    }
                }
                if (!list.Any<IncidentDef>())
                {
                    foreach (EntityCodexEntryDef entityCodexEntryDef2 in DefDatabase<EntityCodexEntryDef>.AllDefs)
                    {
                        if (!entityCodexEntryDef2.provocationIncidents.NullOrEmpty<IncidentDef>())
                        {
                            foreach (IncidentDef incidentDef2 in entityCodexEntryDef2.provocationIncidents)
                            {
                                IncidentParms incidentParms2 = StorytellerUtility.DefaultParmsNow(incidentDef2.category, map);
                                incidentParms2.bypassStorytellerSettings = true;
                                if (incidentDef2.Worker.CanFireNow(incidentParms2))
                                {
                                    list.Add(incidentDef2);
                                }
                            }
                        }
                    }
                }
                if (!list.NullOrEmpty() && list.TryRandomElement(out IncidentDef incidentDef3))
                {
                    IncidentParms incidentParms3 = StorytellerUtility.DefaultParmsNow(incidentDef3.category, map);
                    incidentParms3.bypassStorytellerSettings = true;
                    Find.Storyteller.incidentQueue.Add(incidentDef3, Find.TickManager.TicksGame + Mathf.RoundToInt(Rand.RangeInclusive(this.provocationDelayHoursMin, this.provocationDelayHoursMax) * 2500f), incidentParms3, 0);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_Values.Look<int>(ref this.provocationDelayHoursMin, "provocationDelayHoursMin", 1, false);
            Scribe_Values.Look<int>(ref this.provocationDelayHoursMax, "provocationDelayHoursMax", 3, false);
        }
        public string inSignal;
        public int provocationDelayHoursMin;
        public int provocationDelayHoursMax;
    }
    //the parent of all the roots for the specific quests that set up each possible item. Sets up important values (map, points, pawn to initially victimize, etc.), and generates the object and drop pods it to you.
    public abstract class QuestNode_Root_BarkerBox : QuestNode
    {
        protected virtual bool RequiresPawn { get; } = true;
        protected override bool TestRunInt(Slate slate)
        {
            Map map = QuestSetupUtility.Quest_TryGetMap();
            return map != null && (!this.RequiresPawn || QuestUtility.TryGetIdealColonist(out Pawn pawn, map, new Func<Pawn, bool>(this.ValidatePawn)));
        }
        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            quest.hiddenInUI = true;
            Map map = QuestSetupUtility.Quest_TryGetMap();
            float points = slate.Get<float>("points", 0f, false);
            slate.Set<Map>("map", map, false);
            Pawn asker = this.FindAsker();
            Pawn pawn = null;
            if (this.RequiresPawn && !QuestUtility.TryGetIdealColonist(out pawn, map, new Func<Pawn, bool>(this.ValidatePawn)))
            {
                Log.ErrorOnce("Attempted to create a mysterious cargo quest but no valid pawns or world pawns could be found", 94657346);
                quest.End(QuestEndOutcome.InvalidPreAcceptance, true, true);
            }
            Thing thing = this.GenerateThing(pawn);
            quest.Delay(120, delegate
            {
                quest.DropPods(map.Parent, new List<Thing> { thing }, "[deliveredLetterLabel]", null, "[deliveredLetterText]", null, new bool?(true), false, false, false, null, null, QuestPart.SignalListenMode.OngoingOnly, null, true, true, false, true, null);
                this.AddPostDroppedQuestParts(pawn, thing, quest);
                quest.End(QuestEndOutcome.Success, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, false, false);
            }, null, null, null, false, null, null, false, null, null, null, false, QuestPart.SignalListenMode.OngoingOnly, false);
            slate.Set<Pawn>("asker", asker, false);
            slate.Set<bool>("askerIsNull", asker == null, false);
            slate.Set<Pawn>("pawn", pawn, false);
            Slate slate2 = slate;
            string text = "pawnOnMap";
            Pawn pawn2 = pawn;
            slate2.Set<bool>(text, ((pawn2 != null) ? pawn2.MapHeld : null) == map, false);
        }
        protected abstract Thing GenerateThing(Pawn pawn);
        protected virtual void AddPostDroppedQuestParts(Pawn pawn, Thing thing, Quest quest) { }
        protected virtual bool ValidatePawn(Pawn pawn)
        {
            return pawn.IsColonist || pawn.IsSlaveOfColony;
        }
        private Pawn FindAsker()
        {
            return null;
        }
    }
    //some pawns generated by Barker gain a hediff with this comp when Late Grows the Hour is applied. This makes the pawn gain activity faster, and also gain activity when harmed
    public class HediffCompProperties_LGTH : HediffCompProperties
    {
        public HediffCompProperties_LGTH()
        {
            this.compClass = typeof(HediffComp_LGTH);
        }
        public float activityPerHour;
        public float activityPerDamage;
    }
    public class HediffComp_LGTH : HediffComp
    {
        public HediffCompProperties_LGTH Props
        {
            get
            {
                return (HediffCompProperties_LGTH)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            CompActivity ca = this.Pawn.GetComp<CompActivity>();
            if (ca != null)
            {
                if (ca.IsActive)
                {
                    this.Pawn.health.RemoveHediff(this.parent);
                    return;
                }
                ca.SetActivity(ca.ActivityLevel + (this.Props.activityPerHour * delta / 2500f));
            }
        }
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            if (totalDamageDealt > 0f && this.Props.activityPerDamage > 0f)
            {
                CompActivity ca = this.Pawn.GetComp<CompActivity>();
                if (ca != null && ca.IsDormant)
                {
                    ca.SetActivity(ca.ActivityLevel + this.Props.activityPerDamage, false);
                }
            }
        }
    }
}
