using HautsFramework;
using RimWorld;
using Verse;

namespace HautsPermits_Occult
{
    public class Hediff_Ptuning : HediffWithComps
    {
        public override string LabelBase
        {
            get
            {
                HediffComp_PTuner hcpt = this.TryGetComp<HediffComp_PTuner>();
                if (hcpt != null && !hcpt.isActive)
                {
                    return hcpt.Props.permanentLabel;
                }
                return base.LabelBase;
            }
        }
    }
    /*deactivates on reaching psychic sensitivity
     * sensPerHour: while active, gains this much severity per hour (which, given its XML configuration, results in this much psychic sensitivity being lost each hour)
     * MTBhoursToGoodEvent: while the pawn has non-zero psychic sensitivity, there is an MTB to create a good event (see the Framework for what constitutes a "good" event) and then deactivate this hediff
     * permanentLabel: while inactive, this is the label shown in the pawn's health tab (handled by Hediff_Ptuning)
     * postEventRecoveryPerHour: while inactive, lose this much severity per hour*/
    public class HediffCompProperties_PTuner : HediffCompProperties
    {
        public HediffCompProperties_PTuner()
        {
            this.compClass = typeof(HediffComp_PTuner);
        }
        public float sensPerHour;
        public float MTBhoursToGoodEvent;
        public string permanentLabel;
        public float postEventRecoveryPerHour;
    }
    public class HediffComp_PTuner : HediffComp
    {
        public HediffCompProperties_PTuner Props
        {
            get
            {
                return (HediffCompProperties_PTuner)this.props;
            }
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
            this.isActive = true;
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(60, delta))
            {
                if (this.isActive)
                {
                    if (this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon)
                    {
                        this.parent.Severity += this.Props.sensPerHour * 0.024f;
                        if (Rand.MTBEventOccurs(this.Props.MTBhoursToGoodEvent, 2500f, 60))
                        {
                            GoodAndBadIncidentsUtility.MakeGoodEvent(this.Pawn,0,null);
                            Messages.Message("HVMP_ProbabilityTunerActivated".Translate().CapitalizeFirst().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve(), this.Pawn, MessageTypeDefOf.PositiveEvent, true);
                            this.isActive = false;
                        }
                    } else {
                        this.isActive = false;
                    }
                } else {
                    this.parent.Severity -= this.Props.postEventRecoveryPerHour * 0.024f;
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<bool>(ref this.isActive, "isActive", false, false);
        }
        public bool isActive = true;
    }
}
