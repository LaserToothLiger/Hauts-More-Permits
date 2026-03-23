using HautsFramework;
using HautsPermits;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Grammar;

namespace HautsPermits_Ideology
{
    /*generate an item of thingDef, and assign the relevantSkills and relevantStats of its CompStudiableRemnant (a derivative of CompStudiableQuestItem). Also gives info about which mutators are enabled to that comp:
     * remnant1: Bad Juju assigns its BJ_activationPoint to somewhere within 10-90% its required progress, and BJ_incident to a random bad incident (see GoodAndBadIncidents in the Framework) that could fire on the quest's targeted map
     *   reaching that much progress causes the incident to fire on the item's map
     * remnant2: Creeping Decay flips on CD_on, which causes periodic damage (the comp's CD_damagePerDay, dealt out in 250-tick chunks)
     * remnant3: Perplexicon flips on PI_on. Studying the item sets its PI_cooldown to comp's PI_maxCooldown,
     *   but it decays over time and if it ever hits 0, progress becomes lost at comp's PI_progressLostPerDay, dealt in 250-tick chunks*/
    public class QuestNode_GenerateStrangeArtifact : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Thing thing = ThingMaker.MakeThing(this.thingDef, null);
            int challengeRating = QuestGen.quest.challengeRating;
            CompStudiableRemnant cda = thing.TryGetComp<CompStudiableRemnant>();
            if (cda != null)
            {
                cda.challengeRating = challengeRating;
                cda.relevantSkills = new List<SkillDef>();
                if (!cda.Props.requiredSkillDefs.NullOrEmpty())
                {
                    cda.relevantSkills.AddRange(cda.Props.requiredSkillDefs);
                }
                if (!cda.Props.possibleExtraSkillDefs.NullOrEmpty())
                {
                    SkillDef extraSkill = cda.Props.possibleExtraSkillDefs.RandomElement();
                    cda.relevantSkills.Add(extraSkill);
                }
                cda.relevantStats = new List<StatDef>();
                if (!cda.Props.requiredStatDefs.NullOrEmpty())
                {
                    cda.relevantStats.AddRange(cda.Props.requiredStatDefs);
                }
                if (cda.Props.extraStat)
                {
                    float secondStatDeterminer = Rand.Value;
                    StatDef secondStat;
                    if (secondStatDeterminer <= 0.6f)
                    {
                        secondStat = cda.Props.possibleExtraStatDefsLikely.RandomElement();
                    } else if (secondStatDeterminer <= 0.9f) {
                        secondStat = cda.Props.possibleExtraStatDefsProbable.NullOrEmpty() ? cda.Props.possibleExtraStatDefsLikely.RandomElement() : cda.Props.possibleExtraStatDefsProbable.RandomElement();
                    } else {
                        secondStat = cda.Props.possibleExtraStatDefsUnlikely.NullOrEmpty() ? cda.Props.possibleExtraStatDefsLikely.RandomElement() : cda.Props.possibleExtraStatDefsUnlikely.RandomElement();
                    }
                    cda.relevantStats.Add(secondStat);
                    slate.Set<string>(this.storeSecondStatAs.GetValue(slate), secondStat.defName, false);
                    slate.Set<string>(this.storeSecondStatLabelAs.GetValue(slate), secondStat.label, false);
                }
                bool mayhemMode = HVMP_Mod.settings.remnantX;
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.remnant1, mayhemMode))
                {
                    Map map = slate.Get<Map>("map", null, false) ?? Find.AnyPlayerHomeMap;
                    IncidentParms incidentParms = new IncidentParms
                    {
                        target = map,
                        forced = true,
                        points = StorytellerUtility.DefaultThreatPointsNow(map),
                    };
                    cda.BJ_activationPoint = cda.reqProgress * Rand.Range(0.1f, 0.9f);
                    cda.BJ_incident = GoodAndBadIncidentsUtility.badEventPool.Where((IncidentDef id) => id.Worker.CanFireNow(incidentParms)).RandomElement();
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_BJ_info", this.BJ_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_BJ_info", " ") });
                }
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.remnant2, mayhemMode))
                {
                    cda.CD_on = true;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_CD_info", this.CD_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_CD_info", " ") });
                }
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.remnant3, mayhemMode))
                {
                    cda.PI_on = true;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_PI_info", this.PI_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_PI_info", " ") });
                }
                QuestGen.quest.AddPart(new QuestPart_LookAtThis(thing));
            }
            slate.Set<Thing>(this.storeAs.GetValue(slate), thing, false);
        }
        [NoTranslate]
        public SlateRef<string> storeAs;
        public ThingDef thingDef;
        [NoTranslate]
        public SlateRef<string> storeSecondStatAs;
        [NoTranslate]
        public SlateRef<string> storeSecondStatLabelAs;
        [MustTranslate]
        public string BJ_description;
        [MustTranslate]
        public string CD_description;
        [MustTranslate]
        public string PI_description;
    }
    public class CompProperties_StudiableRemnant : CompProperties_StudiableQuestItem
    {
        public CompProperties_StudiableRemnant()
        {
            this.compClass = typeof(CompStudiableRemnant);
        }
        public float CD_damagePerDay;
        public int PI_maxCooldown;
        public float PI_progressLossPerDay;
    }
    public class CompStudiableRemnant : CompStudiableQuestItem
    {
        public new CompProperties_StudiableRemnant Props
        {
            get
            {
                return (CompProperties_StudiableRemnant)this.props;
            }
        }
        public override void ExtraStudyEffects(int delta, Pawn researcher, Thing brb, Thing researchBench)
        {
            base.ExtraStudyEffects(delta, researcher, brb, researchBench);
            this.PI_cooldown = this.Props.PI_maxCooldown;
            if (this.BJ_activationPoint > 0f && this.curProgress > this.BJ_activationPoint && this.BJ_incident != null)
            {
                IncidentParms incidentParms = new IncidentParms
                {
                    target = this.parent.MapHeld ?? Find.AnyPlayerHomeMap,
                    forced = true,
                    points = StorytellerUtility.DefaultThreatPointsNow(this.parent.MapHeld ?? Find.AnyPlayerHomeMap),
                };
                if (this.BJ_incident.Worker.CanFireNow(incidentParms))
                {
                    Find.Storyteller.incidentQueue.Add(this.BJ_incident, Find.TickManager.TicksGame + 60, incidentParms, 60000);
                } else {
                    Log.Error("Tried to fire " + this.BJ_incident.label + " for the Remnant quest's Bad Juju mutator, but its worker could not fire. Using a random other bad event instead...");
                    List<IncidentDef> incidents = GoodAndBadIncidentsUtility.badEventPool.Where((IncidentDef id) => id.Worker.CanFireNow(incidentParms)).ToList();
                    Find.Storyteller.incidentQueue.Add(incidents.RandomElement(), Find.TickManager.TicksGame + 60, incidentParms, 60000);
                }
                this.BJ_activationPoint = -1f;
            }
        }
        public override void CompTickRare()
        {
            base.CompTickRare();
            if (this.CD_on)
            {
                if (this.parent.IsHashIntervalTick((int)(60000 / this.Props.CD_damagePerDay), 250))
                {
                    this.parent.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, 1f, 0f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true, QualityCategory.Normal, true, false));
                }
            }
            if (this.PI_on)
            {
                if (this.PI_cooldown < 0)
                {
                    this.curProgress -= Math.Min(this.curProgress, this.Props.PI_progressLossPerDay / 250f);
                } else {
                    this.PI_cooldown -= 250;
                }
            }
        }
        public override string CompInspectStringExtra()
        {
            string desc = base.CompInspectStringExtra();
            if (this.BJ_activationPoint > this.curProgress && this.BJ_incident != null)
            {
                desc += "\n" + "HVMP_Remnant_BJlabel".Translate();
            }
            if (this.CD_on)
            {
                desc += "\n" + "HVMP_Remnant_CDlabel".Translate(this.Props.CD_damagePerDay);
            }
            if (this.PI_on)
            {
                desc += "\n" + "HVMP_Remnant_PIlabel".Translate(this.Props.PI_maxCooldown.ToStringTicksToPeriod(true, true, true, true, true), this.Props.PI_progressLossPerDay);
            }
            return desc;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref this.BJ_activationPoint, "BJ_activationPoint", -1f, false);
            Scribe_Defs.Look<IncidentDef>(ref this.BJ_incident, "BJ_incident");
            Scribe_Values.Look<bool>(ref this.CD_on, "CD_on", false, false);
            Scribe_Values.Look<int>(ref this.PI_cooldown, "PI_cooldown", -1, false);
            Scribe_Values.Look<bool>(ref this.PI_on, "PI_on", false, false);
        }
        public float BJ_activationPoint;
        public IncidentDef BJ_incident;
        public bool CD_on;
        public bool PI_on;
        public int PI_cooldown;
    }
}
