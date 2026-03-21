using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace HautsPermits
{
    /*the base chassis for the ROVER Atlas, ARCHIVE Remnant, and ECOSPHERE Retroviral Agent quest items.
     * totalRequiredProgressHours: amount of work needed to finish a given instance of this item is a random value in this range
     * possibleExtraSkillDefs: a random skill from this list improves the speed at which a researcher studies this item.
     * requiredSkillDefs: all skills in this list improve the speed at which a researcher studies this item.
     * extraStat: a random stat from possibleExtraStatDefsLikely|Probable|Unlikely (with weighting = 6 for a likely one, 3 for probable, 1 for unlikely) also improves the researcher's speed. Set when the item is created
     * requiredStatDefs: all stats in this list improve the researcher's speed.
     * canStudyInPlace: you don't need to bring this back to a research bench. I don't think I turned this on for any of them, because you're supposed to sacrifice not just a researcher but ALSO research apparatus to do the quest.
     * progressInspectStringSkills|Stats: the little bottom left box that shows up when you click on this object shows a string describing which skills|stats improve study speed. These are those strings.
     *   See HVMP_BookProgress1 and HVMP_BookProgress2 in the Languages folder for examples.
     * mustBeOnList: a pawn that hasn't somehow been added to the StudiableQuestItem's "pawns" list can't study this item. Relevant to ROVER Atlas, as the caravan arrival action adds the pawns in the caravan to that list.
     * notOnListstring: when you have a pawn selected and right-click this item, but the option to study it is disabled because of mustBeOnList, this is the string that tells you why the option is disabled.*/
    public class CompProperties_StudiableQuestItem : CompProperties_Interactable
    {
        public CompProperties_StudiableQuestItem()
        {
            this.compClass = typeof(CompStudiableQuestItem);
        }
        public FloatRange totalRequiredProgressHours;
        public List<SkillDef> possibleExtraSkillDefs;
        public List<SkillDef> requiredSkillDefs;
        public bool extraStat;
        public List<StatDef> possibleExtraStatDefsLikely;
        public List<StatDef> possibleExtraStatDefsProbable;
        public List<StatDef> possibleExtraStatDefsUnlikely;
        public List<StatDef> requiredStatDefs;
        public bool canStudyInPlace;
        public string progressInspectStringSkills;
        public string progressInspectStringStats;
        public bool mustBeOnList;
        public string notOnListstring;
    }
    public class CompStudiableQuestItem : CompInteractable
    {
        public new CompProperties_StudiableQuestItem Props
        {
            get
            {
                return (CompProperties_StudiableQuestItem)this.props;
            }
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            Building_ResearchBench building_ResearchBench = null;
            if (base.ValidateTarget(target, false) && (this.Props.canStudyInPlace || StudyUtility.TryFindResearchBench(target.Pawn, out building_ResearchBench)))
            {
                target.Pawn.jobs.TryTakeOrderedJob(this.DoStudyJob(building_ResearchBench), new JobTag?(JobTag.Misc), false);
            }
        }
        public Job DoStudyJob(Thing brb)
        {
            ThingWithComps parent = this.parent;
            CompForbiddable compForbiddable = ((parent != null) ? parent.TryGetComp<CompForbiddable>() : null);
            if (compForbiddable != null)
            {
                compForbiddable.Forbidden = false;
            }
            return JobMaker.MakeJob(HVMPDefOf.HVMP_StudyQuestItem, this.parent, brb, (brb != null) ? brb.Position : IntVec3.Invalid);
        }
        public void Study(int delta, Pawn researcher, Thing brb, Thing researchBench)
        {
            if (researcher.skills != null)
            {
                float toAdd = 0f;
                foreach (SkillDef sd in this.relevantSkills)
                {
                    toAdd += researcher.skills.GetSkill(sd).Level / 4f;
                }
                foreach (StatDef sd in this.relevantStats)
                {
                    toAdd += researcher.GetStatValue(sd) + 1f - sd.defaultBaseValue;
                }
                if (researchBench != null)
                {
                    toAdd *= researchBench.GetStatValue(StatDefOf.ResearchSpeedFactor);
                }
                this.curProgress += (toAdd * (float)delta / 2500f);
                if (this.curProgress >= this.RequiredProgress)
                {
                    this.curProgress = this.RequiredProgress;
                }
                this.ExtraStudyEffects(delta, researcher, brb, researchBench);
            }
        }
        public virtual void ExtraStudyEffects(int delta, Pawn researcher, Thing brb, Thing researchBench)
        {

        }
        public override AcceptanceReport CanInteract(Pawn activateBy = null, bool checkOptionalItems = true)
        {
            AcceptanceReport acceptanceReport = base.CanInteract(activateBy, checkOptionalItems);
            if (!acceptanceReport.Accepted)
            {
                return acceptanceReport;
            }
            if (activateBy != null && StatDefOf.ResearchSpeed.Worker.IsDisabledFor(activateBy))
            {
                return "Incapable".Translate();
            }
            Building_ResearchBench building_ResearchBench;
            if (activateBy != null)
            {
                if (!this.Props.canStudyInPlace && !StudyUtility.TryFindResearchBench(activateBy, out building_ResearchBench))
                {
                    return "NoResearchBench".Translate();
                }
                if (!this.Props.canStudyInPlace && !this.parent.MapHeld.listerBuildings.ColonistsHaveResearchBench())
                {
                    return "NoResearchBench".Translate();
                }
                if (!this.PawnOnList(activateBy))
                {
                    return this.Props.notOnListstring.Translate();
                }
                if (this.PawnHasAnyUsableSkill(activateBy))
                {
                    return true;
                }
                return "HVMP_WrongSkillsToStudy".Translate();
            }
            return true;
        }
        public bool PawnOnList(Pawn pawn)
        {
            return !this.Props.mustBeOnList || this.pawns.Contains(pawn);
        }
        public bool PawnHasAnyUsableSkill(Pawn pawn)
        {
            if (pawn.skills != null)
            {
                foreach (SkillDef sd in this.relevantSkills)
                {
                    if (!pawn.skills.GetSkill(sd).TotallyDisabled)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public void OnAnalyzed(Pawn pawn)
        {
            if (!this.parent.questTags.NullOrEmpty())
            {
                QuestUtility.SendQuestTargetSignals(this.parent.questTags, "StudiableItemFinished", this.Named("SUBJECT"));
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            this.relevantSkills = new List<SkillDef>
            {
                SkillDefOf.Intellectual
            };
            this.reqProgress = this.Props.totalRequiredProgressHours.RandomInRange;
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (!this.parent.questTags.NullOrEmpty())
            {
                QuestUtility.SendQuestTargetSignals(this.parent.questTags, "StudiableItemDestroyed", this.Named("SUBJECT"), previousMap.Named("MAP"));
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Finish analysis",
                    action = delegate
                    {
                        this.OnAnalyzed(Find.CurrentMap.mapPawns.FreeColonistsSpawned.First<Pawn>());
                    }
                };
            }
            foreach (Gizmo gizmo3 in QuestUtility.GetQuestRelatedGizmos(this.parent))
            {
                yield return gizmo3;
            }
            yield break;
        }
        public override string CompInspectStringExtra()
        {
            string text = "HVMP_CCProgress".Translate(this.curProgress.ToStringByStyle(ToStringStyle.FloatOne), this.RequiredProgress.ToStringByStyle(ToStringStyle.FloatOne));
            if (!this.relevantSkills.NullOrEmpty())
            {
                text += "\n" + this.Props.progressInspectStringSkills.Translate();
                for (int i = 0; i < this.relevantSkills.Count; i++)
                {
                    text += this.relevantSkills[i].LabelCap;
                    if (i < this.relevantSkills.Count - 1)
                    {
                        text += ", ";
                    }
                }
            }
            if (!this.relevantStats.NullOrEmpty())
            {
                text += "\n" + this.Props.progressInspectStringStats.Translate();
                for (int i = 0; i < this.relevantStats.Count; i++)
                {
                    text += this.relevantStats[i].LabelCap;
                    if (i < this.relevantStats.Count - 1)
                    {
                        text += ", ";
                    }
                }
            }
            return text;
        }
        public virtual float RequiredProgress
        {
            get
            {
                return this.reqProgress * this.challengeRating;
            }
        }
        public bool Completed
        {
            get
            {
                return this.curProgress >= this.RequiredProgress;
            }
        }
        public float ProgressPercent
        {
            get
            {
                return this.curProgress / this.RequiredProgress;
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref this.curProgress, "curProgress", 0f, false);
            Scribe_Values.Look<float>(ref this.reqProgress, "reqProgress", 1f, false);
            Scribe_Values.Look<int>(ref this.challengeRating, "challengeRating", 1, false);
            Scribe_Collections.Look<SkillDef>(ref this.relevantSkills, "relevantSkills", LookMode.Undefined, LookMode.Undefined);
            Scribe_Collections.Look<StatDef>(ref this.relevantStats, "relevantStats", LookMode.Undefined, LookMode.Undefined);
            Scribe_Collections.Look<Pawn>(ref this.pawns, "pawns", LookMode.Reference, Array.Empty<object>());
        }
        public float curProgress;
        public float reqProgress;
        public int challengeRating = 1;
        public List<SkillDef> relevantSkills = new List<SkillDef>();
        public List<StatDef> relevantStats = new List<StatDef>();
        public List<Pawn> pawns = new List<Pawn>();
    }
    /*it's a research job, taking priority just before research, and happening at a research bench unless canStudyInPlace.
     * Since all the StudiableQuestItems I've made are forbiddable, you can reprioritize research by just forbidding those items.*/
    public class JobDriver_StudyQuestItem : JobDriver_StudyItem
    {
        public CompStudiableQuestItem StudiableComp
        {
            get
            {
                return base.ThingToStudy.TryGetComp<CompStudiableQuestItem>();
            }
        }
        protected override IEnumerable<Toil> GetStudyToils()
        {
            Toil study = ToilMaker.MakeToil("GetStudyToils");
            study.tickIntervalAction = delegate (int delta)
            {
                Pawn actor = study.actor;
                study.handlingFacing = true;
                study.tickIntervalAction = delegate
                {
                    actor.rotationTracker.FaceTarget(this.job.GetTarget(TargetIndex.A));
                    this.StudiableComp.Study(delta, actor, this.TargetThingB, this.job.GetTarget(TargetIndex.A).Thing ?? null);
                    if (!this.StudiableComp.relevantSkills.NullOrEmpty())
                    {
                        foreach (SkillDef sd in this.StudiableComp.relevantSkills)
                        {
                            actor.skills.Learn(sd, 0.1f * (float)delta, false, false);
                        }
                    }
                    actor.GainComfortFromCellIfPossible(delta, true);
                    if (this.StudiableComp.Completed)
                    {
                        this.StudiableComp.OnAnalyzed(actor);
                        actor.jobs.curDriver.ReadyForNextToil();
                    }
                };
            };
            study.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            study.WithProgressBar(TargetIndex.A, () => this.StudiableComp.ProgressPercent, false, -0.5f, false);
            study.defaultCompleteMode = ToilCompleteMode.Delay;
            study.defaultDuration = 2500;
            study.activeSkill = () => SkillDefOf.Intellectual;
            yield return study;
            yield break;
        }
    }
    public class WorkGiver_StudyQuestItem : WorkGiver_Scanner
    {
        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Some;
        }
        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.ResearchBench);
            }
        }
        public override bool Prioritized
        {
            get
            {
                return true;
            }
        }
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (!pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.ResearchBench).NullOrEmpty())
            {
                foreach (Thing t in pawn.Map.listerThings.AllThings)
                {
                    if (this.def.fixedBillGiverDefs.Contains(t.def))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            if (pawn.CanReserve(thing, 1, -1, null, forced) && (!thing.def.hasInteractionCell || pawn.CanReserveSittableOrSpot(thing.InteractionCell, forced)))
            {
                foreach (Thing t in pawn.Map.listerThings.AllThings)
                {
                    if (this.def.fixedBillGiverDefs.Contains(t.def) && pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Deadly) && !t.IsForbidden(pawn) && !t.Fogged())
                    {
                        CompStudiableQuestItem cda = t.TryGetComp<CompStudiableQuestItem>();
                        if (cda != null && cda.PawnOnList(pawn) && cda.PawnHasAnyUsableSkill(pawn))
                        {
                            return cda.DoStudyJob(thing);
                        }
                    }
                }
            }
            return null;
        }
    }
}
