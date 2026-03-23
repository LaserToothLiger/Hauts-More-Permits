using HautsFramework;
using HautsPermits;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Grammar;
using Verse.Sound;

namespace HautsPermits_Biotech
{
    /*make the studiable quest item (thingDef), assign relevantSkills and relevantStats, also? Mutators
     * ra1: BSL-4,      * ra2: Cannibal Code,      * ra3: Playing God these all flick on their own bools in the created item's comps (see said comps below)*/
    public class QuestNode_GenerateRetroviralPackage : QuestNode
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
            slate.Set<Thing>(this.storeAs.GetValue(slate), thing, false);
            CompStudiableQuestItem cda = thing.TryGetComp<CompStudiableQuestItem>();
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
                }
                bool mayhemMode = HVMP_Mod.settings.raX;
                CompRetroviralInjection cri = thing.TryGetComp<CompRetroviralInjection>();
                if (cri != null)
                {
                    if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.ra1, mayhemMode))
                    {
                        cri.BSL4_on = true;
                        QuestGen.AddQuestDescriptionRules(new List<Rule>
                        {
                            new Rule_String("mutator_BSL4_info", this.BSL4_description.Formatted())
                        });
                    } else {
                        QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_BSL4_info", " ") });
                    }
                    if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.ra2, mayhemMode))
                    {
                        CompHackableRA chra = thing.TryGetComp<CompHackableRA>();
                        if (chra != null)
                        {
                            chra.CC_on = true;
                        }
                        QuestGen.AddQuestDescriptionRules(new List<Rule>
                        {
                            new Rule_String("mutator_CC_info", this.CC_description.Formatted())
                        });
                    } else {
                        QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_CC_info", " ") });
                    }
                    if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.ra3, mayhemMode))
                    {
                        cri.PG_on = true;
                        cri.effectOnInjection = cri.Props.possibleInjectionEffects.Where((RetroviralEffectDef red) => red.isBad).RandomElement();
                        QuestGen.AddQuestDescriptionRules(new List<Rule>
                        {
                            new Rule_String("mutator_PG_info", this.PG_description.Formatted())
                        });
                    } else {
                        QuestGen.AddQuestDescriptionRules(new List<Rule>
                        {
                            new Rule_String("mutator_PG_info", this.nonPG_description.Formatted())
                        });
                    }
                }
                QuestGen.quest.AddPart(new QuestPart_LookAtThis(thing));
            }
        }
        [NoTranslate]
        public SlateRef<string> storeAs;
        public ThingDef thingDef;
        [MustTranslate]
        public string BSL4_description;
        [MustTranslate]
        public string CC_description;
        [MustTranslate]
        public string nonPG_description;
        [MustTranslate]
        public string PG_description;
    }
    //derivative of CompHackable that necessarily handles Cannibal Code, as you only need to hack the item to be able to inject it IF that mutator is on. Otherwise, no hacking in this quest!
    public class CompProperties_HackableRA : CompProperties_Hackable
    {
        public CompProperties_HackableRA()
        {
            this.compClass = typeof(CompHackableRA);
        }
    }
    public class CompHackableRA : CompHackable
    {
        public new CompProperties_HackableRA Props
        {
            get
            {
                return (CompProperties_HackableRA)this.props;
            }
        }
        public override string CompInspectStringExtra()
        {
            if (!this.CC_on)
            {
                return null;
            }
            return base.CompInspectStringExtra();
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.parent.Spawned && !this.IsHacked && !this.CC_on)
            {
                this.Hack(this.defence, null, true);
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!this.CC_on)
            {
                yield break;
            }
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            yield break;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.CC_on, "CC_on", false, false);
        }
        public bool CC_on;
    }
    /*implements BSL-4 and Playing God, and constrains the possible outcomes of injection. The actual outcome is set when the item is created
     * chanceForInjectionEffect: if this isn't passed and Playing God doesn't apply, no RetroviralEffectDef (RED) is applied to the pawn
     * possibleInjectionEffects: a random RED from here is selected as the outcome effect
     * BSL4_diseaseMTBdays: if BSL-4 applies and the CompStudiableQuestItem has positive progress, this is the MTB to cause a random disease (kind of like what Sickly does, but it doesn't just hit one pawn)
     * PG_extraGoodwillPenalty: if Playing God applies, goodwill penalty on injection is increased by this amount*/
    public class CompProperties_RetroviralInjection : CompProperties_UseEffect
    {
        public CompProperties_RetroviralInjection()
        {
            this.compClass = typeof(CompRetroviralInjection);
        }
        public float chanceForInjectionEffect;
        public List<RetroviralEffectDef> possibleInjectionEffects;
        public float BSL4_diseaseMTBdays;
        public int PG_extraGoodwillPenalty;
    }
    public class CompRetroviralInjection : CompTargetEffect
    {
        public CompProperties_RetroviralInjection Props
        {
            get
            {
                return (CompProperties_RetroviralInjection)this.props;
            }
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.parent.IsHashIntervalTick(2500,delta) && Rand.MTBEventOccurs(this.Props.BSL4_diseaseMTBdays, 60000f, 2500) && this.parent.SpawnedOrAnyParentSpawned && this.BSL4_on)
            {
                CompStudiableQuestItem csqi = this.parent.GetComp<CompStudiableQuestItem>();
                if (csqi != null && csqi.curProgress > 0f)
                {
                    HautsMiscUtility.DoRandomDiseaseOutbreak(this.parent);
                }
            }
        }
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (!user.IsColonistPlayerControlled)
            {
                return;
            }
            Job job = JobMaker.MakeJob(HVMP_BDefOf.HVMP_InjectRetroviralPackage, target, this.parent);
            job.count = 1;
            job.playerForced = true;
            user.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            this.effectOnInjection = Rand.Chance(this.Props.chanceForInjectionEffect) ? this.Props.possibleInjectionEffects.RandomElement() : null;
            if (this.PG_on)
            {
                this.effectOnInjection = this.Props.possibleInjectionEffects.Where((RetroviralEffectDef red) => red.isBad).RandomElement();
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look<RetroviralEffectDef>(ref this.effectOnInjection, "effectOnInjection");
            Scribe_Values.Look<bool>(ref this.BSL4_on, "BSL4_on", false, false);
            Scribe_Values.Look<bool>(ref this.PG_on, "PG_on", false, false);
        }
        public RetroviralEffectDef effectOnInjection;
        public bool BSL4_on;
        public bool PG_on;
    }
    /*when you inject the item into a pawn, it picks a random RetroviralEffectDef in its possibleInjectionEffects and implements its effects
     * inflictedHediffPool: give you a random one of these hediffs
     * addXenogene: adds a random xenogene, unless that would put you over removeXenogeneIfOverComplexity (provided it's a non-negative value). If you're already at that much complexity, lose a random non-archite gene instead
     * droppedItemPool: creates a random thing from this list and spawn it at the pawn's location
     * healWorstInjury: ewisott
     * inflictedChronicHediffPool: apply random HediffGiver_Birthday or HediffGiver_RandomAgeCurved element from this def.
     * inflictedDisease: runs this disease incident, only targeting this one pawn
     * isBad: if the item is under the effect of Playing God, it can only pick a RED that has this as true*/
    public class RetroviralEffectDef : Def
    {
        public List<HediffDef> inflictedHediffPool;
        public bool addXenogene;
        public int removeXenogeneIfOverComplexity = -1;
        public List<ThingDef> droppedItemPool;
        public bool healWorstInjury;
        public HediffGiverSetDef inflictedChronicHediffPool;
        public IncidentDef inflictedDisease;
        public bool isBad;
    }
    public class JobDriver_InjectRpackage : JobDriver
    {
        private Pawn Pawn
        {
            get
            {
                return (Pawn)this.job.GetTarget(TargetIndex.A).Thing;
            }
        }
        private Thing Item
        {
            get
            {
                return this.job.GetTarget(TargetIndex.B).Thing;
            }
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.Pawn, this.job, 1, -1, null, errorOnFailed, false) && this.pawn.Reserve(this.Item, this.job, 1, -1, null, errorOnFailed, false);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            if (this.Item != null)
            {
                CompHackableRA chra = this.Item.TryGetComp<CompHackableRA>();
                if (chra != null && chra.CC_on && !chra.IsHacked)
                {
                    Messages.Message("HVMP_MustHackToInject".Translate(), this.Pawn, MessageTypeDefOf.NeutralEvent, true);
                    yield break;
                }
            }
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false, true, false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.A);
            Toil toil = Toils_General.Wait(600, TargetIndex.None);
            toil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            toil.tickIntervalAction = delegate(int delta)
            {
                CompUsable compUsable = this.Item.TryGetComp<CompUsable>();
                if (compUsable != null && this.warmupMote == null && compUsable.Props.warmupMote != null)
                {
                    this.warmupMote = MoteMaker.MakeAttachedOverlay(this.Pawn, compUsable.Props.warmupMote, Vector3.zero, 1f, -1f);
                }
                Mote mote = this.warmupMote;
                if (mote == null)
                {
                    return;
                }
                mote.Maintain();
            };
            yield return toil;
            yield return Toils_General.Do(new Action(this.DoInjectionEffects));
            yield break;
        }
        private void DoInjectionEffects()
        {
            CompRetroviralInjection rinject = this.Item.TryGetComp<CompRetroviralInjection>();
            if (rinject != null && this.Pawn != null)
            {
                SoundDefOf.MechSerumUsed.PlayOneShot(SoundInfo.InMap(this.Pawn, MaintenanceType.None));
                TaggedString taggedString = "";
                RetroviralEffectDef red = rinject.effectOnInjection;
                if (rinject.BSL4_on)
                {
                    HautsMiscUtility.DoRandomDiseaseOutbreak(this.Pawn);
                }
                if (red != null)
                {
                    if (red.healWorstInjury)
                    {
                        taggedString = HealthUtility.FixWorstHealthCondition(this.Pawn, Array.Empty<HediffDef>());
                    }
                    if (!red.droppedItemPool.NullOrEmpty() && this.Pawn.Spawned)
                    {
                        Thing thing = ThingMaker.MakeThing(red.droppedItemPool.RandomElement());
                        GenSpawn.Spawn(thing, this.Pawn.Position, this.Pawn.Map, WipeMode.Vanish);
                        taggedString = "HVMP_RetroviralItem".Translate(this.Pawn, thing.Label);
                    }
                    if (red.addXenogene && this.Pawn.genes != null)
                    {
                        int xenogeneComplexity = 0;
                        if (!this.Pawn.genes.Xenogenes.NullOrEmpty())
                        {
                            foreach (Gene g in this.Pawn.genes.Xenogenes)
                            {
                                xenogeneComplexity += g.def.biostatCpx;
                            }
                        }
                        if (red.removeXenogeneIfOverComplexity >= 0 && xenogeneComplexity > red.removeXenogeneIfOverComplexity)
                        {
                            if (!this.Pawn.genes.Xenogenes.NullOrEmpty())
                            {
                                List<Gene> nonArcGenes = this.Pawn.genes.Xenogenes.Where((Gene g) => g.def.biostatArc <= 0).ToList();
                                if (nonArcGenes.Count > 0)
                                {
                                    Gene toRemove = nonArcGenes.RandomElement();
                                    taggedString = "HVMP_RetroviralRemovedGene".Translate(this.Pawn, toRemove.Label);
                                    this.Pawn.genes.Xenogenes.Remove(toRemove);
                                }
                            }
                        } else {
                            List<GeneDef> possibleGenes = DefDatabase<GeneDef>.AllDefsListForReading.Where((GeneDef g) => g.biostatArc <= 0 && (red.removeXenogeneIfOverComplexity < 0 || g.biostatCpx + xenogeneComplexity <= red.removeXenogeneIfOverComplexity) && !this.Pawn.genes.HasActiveGene(g)).ToList();
                            if (!possibleGenes.NullOrEmpty())
                            {
                                GeneDef toAdd = possibleGenes.RandomElement();
                                taggedString = "HVMP_RetroviralGainedGene".Translate(this.Pawn, toAdd.label);
                                this.Pawn.genes.AddGene(toAdd, true);
                            }
                        }
                    }
                    if (!red.inflictedHediffPool.NullOrEmpty())
                    {
                        Hediff hediff = HediffMaker.MakeHediff(red.inflictedHediffPool.RandomElement(), this.Pawn);
                        this.Pawn.health.AddHediff(hediff);
                        taggedString = "HVMP_RetroviralHediff".Translate(this.Pawn, hediff.Label);
                    }
                    if (red.inflictedChronicHediffPool != null)
                    {
                        List<HediffGiver> hgivers = new List<HediffGiver>();
                        foreach (HediffGiver hg in red.inflictedChronicHediffPool.hediffGivers)
                        {
                            if (hg is HediffGiver_Birthday || hg is HediffGiver_RandomAgeCurved)
                            {
                                hgivers.Add(hg);
                            }
                        }
                        if (hgivers.Count > 0)
                        {
                            List<Hediff> oah = new List<Hediff>();
                            HediffGiver toGive = hgivers.RandomElement();
                            if (toGive.TryApply(this.Pawn, oah))
                            {
                                taggedString = "HVMP_RetroviralHediff".Translate(this.Pawn, oah[0].Label);
                            }
                        }
                    }
                    if (red.inflictedDisease != null)
                    {
                        IncidentDef incidentDef = red.inflictedDisease;
                        string text;
                        List<Pawn> list = ((IncidentWorker_Disease)incidentDef.Worker).ApplyToPawns(Gen.YieldSingle<Pawn>(this.pawn), out text);
                        if (PawnUtility.ShouldSendNotificationAbout(this.pawn))
                        {
                            if (list.Contains(this.pawn))
                            {
                                Find.LetterStack.ReceiveLetter("LetterLabelTraitDisease".Translate(incidentDef.diseaseIncident.label), "LetterTraitDisease".Translate(this.pawn.LabelCap, incidentDef.diseaseIncident.label, this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true), LetterDefOf.NegativeEvent, this.pawn, null, null, null, null, 0, true);
                            } else if (!text.NullOrEmpty()) {
                                Messages.Message(text, this.pawn, MessageTypeDefOf.NeutralEvent, true);
                            }
                        }
                    }
                }
                if ((this.Pawn.Faction != null && this.pawn.Faction != null && this.Pawn.Faction != this.pawn.Faction) || this.Pawn.IsQuestLodger())
                {
                    Faction.OfPlayer.TryAffectGoodwillWith(this.Pawn.HomeFaction, -(70+(rinject.PG_on?rinject.Props.PG_extraGoodwillPenalty :0)), true, !this.Pawn.HomeFaction.temporary, HVMP_BDefOf.HVMP_PerformedHarmfulInjection, null);
                    QuestUtility.SendQuestTargetSignals(pawn.questTags, "SurgeryViolation", pawn.Named("SUBJECT"));
                }
                if (PawnUtility.ShouldSendNotificationAbout(this.Pawn))
                {
                    Messages.Message(taggedString, this.Pawn, MessageTypeDefOf.PositiveEvent, true);
                }
                if (!this.Item.questTags.NullOrEmpty())
                {
                    QuestUtility.SendQuestTargetSignals(this.Item.questTags, "StudiableItemFinished", this.Named("SUBJECT"));
                }
            }
        }
        private Mote warmupMote;
    }
    //no animals allowed as targets! that would be way too cheap.
    public class CompTargetable_AnyHumanlikeWillDo : CompTargetable
    {
        protected override bool PlayerChoosesTarget
        {
            get
            {
                return true;
            }
        }
        protected override TargetingParameters GetTargetingParameters()
        {
            TargetingParameters targetingParameters = new TargetingParameters();
            targetingParameters.canTargetPawns = true;
            targetingParameters.canTargetSelf = false;
            targetingParameters.canTargetItems = false;
            targetingParameters.canTargetBuildings = false;
            targetingParameters.mapObjectTargetsMustBeAutoAttackable = false;
            targetingParameters.validator = (TargetInfo x) => x.Thing is Pawn p && p.RaceProps.Humanlike;
            return targetingParameters;
        }
        public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
        {
            yield return targetChosenByPlayer;
            yield break;
        }
    }
}
