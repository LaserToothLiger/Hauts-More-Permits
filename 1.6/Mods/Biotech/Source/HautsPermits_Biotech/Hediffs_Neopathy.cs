using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HautsPermits_Biotech
{
    //only usable if the hediff's HediffComp_NeopathyComplications' cureDiscovered. Fairly obvious what this is for. Sends you a notif when finished.
    public class Recipe_RemoveNeopathy : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (!base.AvailableOnNow(thing, part))
            {
                return false;
            }
            if (!(thing is Pawn pawn))
            {
                return false;
            }
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i].def == this.recipe.removesHediff)
                {
                    HediffComp_NeopathyComplications hcnc = hediffs[i].TryGetComp<HediffComp_NeopathyComplications>();
                    if (hcnc != null && hcnc.cureDiscovered)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            List<Hediff> allHediffs = pawn.health.hediffSet.hediffs;
            int num;
            for (int i = 0; i < allHediffs.Count; i = num + 1)
            {
                if (allHediffs[i].Part != null && allHediffs[i].def == recipe.removesHediff && allHediffs[i].Visible)
                {
                    yield return allHediffs[i].Part;
                }
                num = i;
            }
            yield break;
        }
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            Hediff hediff = pawn.health.hediffSet.hediffs.Find((Hediff x) => x.def == this.recipe.removesHediff && x.Part == part && x.Visible);
            bool shouldGetRemoved = true;
            if (hediff != null)
            {
                HediffComp_NeopathyComplications hcnp = hediff.TryGetComp<HediffComp_NeopathyComplications>();
                if (hcnp != null && Rand.Chance(hcnp.DEM_failureChance))
                {
                    shouldGetRemoved = false;
                }
            }
            if (billDoer != null)
            {
                if (base.CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                {
                    return;
                }
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, new object[] { billDoer, pawn });
                if (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(billDoer))
                {
                    if (shouldGetRemoved)
                    {
                        string text;
                        if (!this.recipe.successfullyRemovedHediffMessage.NullOrEmpty())
                        {
                            text = this.recipe.successfullyRemovedHediffMessage.Formatted(billDoer.LabelShort, pawn.LabelShort);
                        } else {
                            text = "MessageSuccessfullyRemovedHediff".Translate(billDoer.LabelShort, pawn.LabelShort, this.recipe.removesHediff.label.Named("HEDIFF"), billDoer.Named("SURGEON"), pawn.Named("PATIENT"));
                        }
                        Messages.Message(text, pawn, MessageTypeDefOf.PositiveEvent, true);
                    } else {
                        string text = "HVMP_NeopathyCureFailed".Translate(billDoer.LabelShort, pawn.LabelShort);
                        Messages.Message(text, pawn, MessageTypeDefOf.PositiveEvent, true);
                    }
                }
            }
            if (hediff != null && shouldGetRemoved)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }
    }
    /*the comp that governs all the wacky bullshit neopathies can do.
     * Adds "complication" hediffs randomly drawn from the possibleComplications list (as well as the DD_complications list, if DD_on due to the quest node) over time,
     *   with the interval between each complication's addition being a random number of ticks within nextComplicationTimer.
     * It can add up to maxComplications (which is a random value within complicationCount, plus some mutator-dependent values added in the quest node).
     * Whenever it is tended, there's a chanceTendRevealsCureMethod chance for cureDiscovered to be flipped on, enabling the above recipe to work on it.
     *   This can't happen until it's been tended at least a number of times equal to the min of tendDiscoveryRange, and it's guaranteed to happen on reaching a number of tends equal to the max.*/
    public class HediffCompProperties_NeopathyComplications : HediffCompProperties_TendDuration
    {
        public HediffCompProperties_NeopathyComplications()
        {
            this.compClass = typeof(HediffComp_NeopathyComplications);
        }
        public IntRange complicationCount;
        public IntRange nextComplicationTimer;
        public List<HediffDef> possibleComplications;
        public List<HediffDef> DD_complications;
        public float chanceTendRevealsCureMethod;
        public FloatRange tendDiscoveryRange;
    }
    public class HediffComp_NeopathyComplications : HediffComp_TendDuration
    {
        public HediffCompProperties_NeopathyComplications Props
        {
            get
            {
                return (HediffCompProperties_NeopathyComplications)this.props;
            }
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
            this.maxComplications = this.Props.complicationCount.RandomInRange;
            this.ticksToNextComplication = this.Props.nextComplicationTimer.RandomInRange;
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.complicationsCreated < this.maxComplications)
            {
                this.ticksToNextComplication -= delta;
                if (this.ticksToNextComplication <= 0)
                {
                    this.ticksToNextComplication = this.Props.nextComplicationTimer.RandomInRange;
                    this.GainComplication();
                }
            }
        }
        public void GainComplication()
        {
            if (!this.Props.possibleComplications.NullOrEmpty())
            {
                List<HediffDef> compPool = this.Props.possibleComplications;
                if (this.DD_on)
                {
                    compPool.AddRange(this.Props.DD_complications);
                }
                Hediff complication = HediffMaker.MakeHediff(compPool.RandomElement(), this.Pawn);
                this.Pawn.health.AddHediff(complication);
                this.complicationsCreated++;
            }
        }
        public override void CompTended(float quality, float maxQuality, int batchPosition = 0)
        {
            base.CompTended(quality, maxQuality, batchPosition);
            this.numTends++;
            if (this.numTends >= this.Props.tendDiscoveryRange.min && !this.cureDiscovered && (this.numTends >= this.Props.tendDiscoveryRange.max || Rand.Chance(this.Props.chanceTendRevealsCureMethod)))
            {
                this.cureDiscovered = true;
                Find.LetterStack.ReceiveLetter("HVMP_NeopathyCureDiscoveredLabel".Translate(), "HVMP_NeopathyCureDiscoveredText".Translate().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve(), LetterDefOf.PositiveEvent, this.Pawn);
            }
        }
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (!this.Pawn.questTags.NullOrEmpty())
            {
                QuestUtility.SendQuestTargetSignals(this.Pawn.questTags, "NeopathyCured", this.Named("SUBJECT"));
            }
            List<Hediff> toRemove = new List<Hediff>();
            foreach (Hediff h in this.Pawn.health.hediffSet.hediffs)
            {
                if (this.Props.possibleComplications.Contains(h.def) || this.Props.DD_complications.Contains(h.def))
                {
                    toRemove.Add(h);
                }
            }
            foreach (Hediff h in toRemove)
            {
                this.Pawn.health.RemoveHediff(h);
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.maxComplications, "maxComplications", 5, false);
            Scribe_Values.Look<int>(ref this.ticksToNextComplication, "ticksToNextComplication", 60000, false);
            Scribe_Values.Look<int>(ref this.complicationsCreated, "complicationsCreated", 0, false);
            Scribe_Values.Look<int>(ref this.numTends, "numTends", 0, false);
            Scribe_Values.Look<bool>(ref this.cureDiscovered, "cureDiscovered", false, false);
            Scribe_Values.Look<bool>(ref this.DD_on, "DD_on", false, false);
            Scribe_Values.Look<float>(ref this.DEM_failureChance, "DEM_failureChance", 0f, false);
        }
        public int maxComplications;
        public int ticksToNextComplication;
        public int complicationsCreated;
        public int numTends;
        public bool cureDiscovered;
        public bool DD_on;
        public float DEM_failureChance;
    }
    //for Gerogenesis
    public class HediffCompProperties_AcceleratedAging : HediffCompProperties
    {
        public HediffCompProperties_AcceleratedAging()
        {
            this.compClass = typeof(HediffComp_AcceleratedAging);
        }
        public int daysPerDay;
    }
    public class HediffComp_AcceleratedAging : HediffComp
    {
        public HediffCompProperties_AcceleratedAging Props
        {
            get
            {
                return (HediffCompProperties_AcceleratedAging)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(60000, delta))
            {
                this.Pawn.ageTracker.AgeBiologicalTicks += (this.Props.daysPerDay * 60000);
            }
        }
    }
    //for... hang on, actually what did I call this one? One sec. For "Insect Bait".
    public class HediffCompProperties_Infestation : HediffCompProperties
    {
        public HediffCompProperties_Infestation()
        {
            this.compClass = typeof(HediffComp_Infestation);
        }
        public float MTBdays;
    }
    public class HediffComp_Infestation : HediffComp
    {
        public HediffCompProperties_Infestation Props
        {
            get
            {
                return (HediffCompProperties_Infestation)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(2500, delta) && this.Pawn.SpawnedOrAnyParentSpawned && Rand.MTBEventOccurs(this.Props.MTBdays, 60000f, 2500))
            {
                IncidentParms incidentParms = new IncidentParms();
                incidentParms.target = this.Pawn.MapHeld;
                incidentParms.points = StorytellerUtility.DefaultThreatPointsNow(this.Pawn.MapHeld);
                incidentParms.infestationLocOverride = new IntVec3?(this.Pawn.PositionHeld);
                incidentParms.forced = true;
                IncidentDefOf.Infestation.Worker.TryExecute(incidentParms);
            }
        }
    }
    //for Miasmatic Rot. Terrible fucking complication, all my homies hate this complication
    public class Hediff_MiasmaticRot : Hediff
    {
        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (this.pawn.IsHashIntervalTick(2000, delta) && this.pawn.SpawnedOrAnyParentSpawned)
            {
                GasUtility.AddGas(this.pawn.PositionHeld, this.pawn.MapHeld, GasType.RotStink, 4444);
            }
        }
    }
}
