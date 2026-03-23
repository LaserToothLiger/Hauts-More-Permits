using HautsPermits;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using Verse;
using Verse.Grammar;

namespace HautsPermits_Occult
{
    /*generates the incident(s) for QuestPart_PsychicDischarge from the list of possibleBadIncidents and possibleOkIncidents, as well as sets up the three mutators:
     * lc1: Escape From Tartarus there's an EFT_chance to also cause a random incident from EFT_incidents.
     *   the randomly selected one gets saved to prevent save-scumming, but the others are passed on to QuestPart_EFT as otherIncidents, which get randomly drawn in case the chosen incident cannot fire when called on.
     * lc2: No Kindness Nor Succor prevents rolling any possibleOkIncidents
     * lc3: Touch of the Unfathomable also inflicts a random game condition from TOTU_conditions on the target map for a random amount of ticks in TOTU_durationTicks*/
    public class QuestNode_GiveRewardsLovecraft : QuestNode_GiveRewardsBranch
    {
        protected override bool TestRunInt(Slate slate)
        {
            return base.TestRunInt(slate);
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;
            QuestPart_PsychicDischarge qppd = new QuestPart_PsychicDischarge
            {
                inSignal = QuestGenUtility.HardcodedSignalWithQuestID(quest.InitiateSignal),
                cooldownBetweenLovecrafts = this.cooldownBetweenLovecrafts
            };
            IncidentParms ip = new IncidentParms
            {
                target = slate.Get<Map>("map", Find.AnyPlayerHomeMap, false),
                faction = slate.Get<Faction>("faction", null, false),
                points = slate.Get<float>("points", 350f, false),
                forced = true,
                quest = quest,
                bypassStorytellerSettings = true
            };
            bool mayhemMode = HVMP_Mod.settings.lcX;
            bool EFT_on = BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.lc1, mayhemMode);
            bool NKNS_on = BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.lc2, mayhemMode);
            bool TOTU_on = BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.lc3, mayhemMode);
            List<IncidentDef> usableIncidents = new List<IncidentDef>();
            usableIncidents.AddRange(this.possibleBadIncidents);
            if (EFT_on)
            {
                if (Rand.Chance(this.EFT_chance))
                {
                    QuestPart_EFT qeft = new QuestPart_EFT
                    {
                        inSignal = QuestGenUtility.HardcodedSignalWithQuestID(quest.InitiateSignal)
                    };
                    IncidentDef ieft = this.EFT_incidents.RandomElement();
                    if (ieft != null)
                    {
                        qeft.incidentParms = ip;
                        qeft.incident = ieft;
                        List<IncidentDef> iefts = new List<IncidentDef>();
                        iefts.AddRange(this.EFT_incidents);
                        iefts.Remove(ieft);
                        qeft.otherIncidents = iefts;
                    }
                    qeft.SetIncidentParmsAndRemoveTarget(ip);
                    quest.AddPart(qeft);
                }
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_EFT_info", this.EFT_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_EFT_info", " ") });
            }
            if (NKNS_on)
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_NKNS_info", this.NKNS_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_NKNS_info", this.nonNKNS_description.Formatted())
                });
                usableIncidents.AddRange(this.possibleOkIncidents);
            }
            if (TOTU_on)
            {
                float num = slate.Get<float>("points", 0f, false);
                GameCondition gameCondition2 = GameConditionMaker.MakeCondition(this.TOTU_conditions.RandomElement(), this.TOTU_durationTicks.RandomInRange);
                QuestPart_GameCondition questPart_GameCondition2 = new QuestPart_GameCondition();
                questPart_GameCondition2.gameCondition = gameCondition2;
                List<Rule> list2 = new List<Rule>();
                Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
                Map map = BranchQuestSetupUtility.GetMap_QuestNodeGameCondition(slate);
                questPart_GameCondition2.mapParent = map.Parent;
                gameCondition2.RandomizeSettings(num, map, list2, dictionary2);
                questPart_GameCondition2.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
                questPart_GameCondition2.sendStandardLetter = false;
                quest.AddPart(questPart_GameCondition2);
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_TOTU_info", this.TOTU_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_TOTU_info", " ") });
            }
            IncidentDef incident = usableIncidents.RandomElement();
            if (incident == null)
            {
                incident = HVMPDefOf_A.HVMP_LovecraftAlignment;
            }
            if (incident != null)
            {
                qppd.incidentParms = ip;
                qppd.incident = incident;
                List<IncidentDef> incidents = usableIncidents;
                incidents.Remove(incident);
                qppd.otherIncidents = incidents;
            }
            qppd.SetIncidentParmsAndRemoveTarget(ip);
            quest.AddPart(qppd);
            base.RunInt();
        }
        public List<IncidentDef> possibleOkIncidents;
        public List<IncidentDef> possibleBadIncidents;
        public int cooldownBetweenLovecrafts;
        public float EFT_chance;
        public List<IncidentDef> EFT_incidents;
        [MustTranslate]
        public string EFT_description;
        [MustTranslate]
        public string nonNKNS_description;
        [MustTranslate]
        public string NKNS_description;
        public IntRange TOTU_durationTicks;
        public List<GameConditionDef> TOTU_conditions;
        [MustTranslate]
        public string TOTU_description;
    }
    /*to minimize the possibility that Lovecraft incidents override or block each other, a Lovecraft quest can't be accepted if it's been no more than the QuestNode's cooldownBetweenLovecrafts ticks since the last one you accepted
     * anyways, this QuestPart also fires the selected incident, or if it can't fire for some reason, a random one from otherIncidents that could fire*/
    public class QuestPart_PsychicDischarge : QuestPart_RequirementsToAccept
    {
        public override AcceptanceReport CanAccept()
        {
            WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
            if (wcbs != null)
            {
                if (wcbs.lovecraftEventTimer <= 0)
                {
                    return true;
                }
                return new AcceptanceReport("HVMP_LovecraftTooSoon".Translate(wcbs.lovecraftEventTimer.ToStringTicksToPeriodVerbose(true, true).Colorize(ColoredText.DateTimeColor)));
            }
            return new AcceptanceReport("HVMP_LovecraftTooSoon".Translate(0));
        }
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                if (this.incident != null && this.incident.Worker.CanFireNow(this.incidentParms))
                {
                    this.incident.Worker.TryExecute(this.incidentParms);
                } else if (!this.otherIncidents.NullOrEmpty()) {
                    while (this.otherIncidents.Count > 0)
                    {
                        IncidentDef id = this.otherIncidents.RandomElement();
                        if (id.Worker.CanFireNow(this.incidentParms))
                        {
                            id.Worker.TryExecute(this.incidentParms);
                            break;
                        } else {
                            this.otherIncidents.Remove(id);
                        }
                    }
                }
                WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
                if (wcbs != null)
                {
                    wcbs.lovecraftEventTimer = this.cooldownBetweenLovecrafts;
                }
            }
        }
        public void SetIncidentParmsAndRemoveTarget(IncidentParms value)
        {
            this.incidentParms = value;
            Map map = this.incidentParms.target as Map;
            if (map != null)
            {
                this.mapParent = map.Parent;
                //this.incidentParms.target = null;
                return;
            }
            this.mapParent = null;
        }
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo globalTargetInfo in base.QuestLookTargets)
                {
                    yield return globalTargetInfo;
                }
                if (this.mapParent != null)
                {
                    yield return this.mapParent;
                }
                yield break;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_Defs.Look<IncidentDef>(ref this.incident, "incident");
            Scribe_Deep.Look<IncidentParms>(ref this.incidentParms, "incidentParms", Array.Empty<object>());
            Scribe_References.Look<MapParent>(ref this.mapParent, "mapParent", false);
            Scribe_Collections.Look<IncidentDef>(ref this.otherIncidents, "otherIncidents", LookMode.Def, Array.Empty<object>());
            Scribe_Values.Look<int>(ref this.cooldownBetweenLovecrafts, "cooldownBetweenLovecrafts", 60000, false);
        }
        public string inSignal;
        public IncidentDef incident;
        public List<IncidentDef> otherIncidents;
        public IncidentParms incidentParms;
        private MapParent mapParent;
        public int cooldownBetweenLovecrafts;
    }
    //fires the stored incident
    public class QuestPart_EFT : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                if (this.incident != null && this.incident.Worker.CanFireNow(this.incidentParms))
                {
                    this.incident.Worker.TryExecute(this.incidentParms);
                }
                else if (!this.otherIncidents.NullOrEmpty())
                {
                    while (this.otherIncidents.Count > 0)
                    {
                        IncidentDef id = this.otherIncidents.RandomElement();
                        if (id.Worker.CanFireNow(this.incidentParms))
                        {
                            id.Worker.TryExecute(this.incidentParms);
                            break;
                        } else {
                            this.otherIncidents.Remove(id);
                        }
                    }
                }
            }
        }
        public void SetIncidentParmsAndRemoveTarget(IncidentParms value)
        {
            this.incidentParms = value;
            Map map = this.incidentParms.target as Map;
            if (map != null)
            {
                this.mapParent = map.Parent;
                return;
            }
            this.mapParent = null;
        }
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo globalTargetInfo in base.QuestLookTargets)
                {
                    yield return globalTargetInfo;
                }
                if (this.mapParent != null)
                {
                    yield return this.mapParent;
                }
                yield break;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_Defs.Look<IncidentDef>(ref this.incident, "incident");
            Scribe_Deep.Look<IncidentParms>(ref this.incidentParms, "incidentParms", Array.Empty<object>());
            Scribe_References.Look<MapParent>(ref this.mapParent, "mapParent", false);
            Scribe_Collections.Look<IncidentDef>(ref this.otherIncidents, "otherIncidents", LookMode.Def, Array.Empty<object>());
        }
        public string inSignal;
        public IncidentDef incident;
        public List<IncidentDef> otherIncidents;
        public IncidentParms incidentParms;
        private MapParent mapParent;
    }
}
