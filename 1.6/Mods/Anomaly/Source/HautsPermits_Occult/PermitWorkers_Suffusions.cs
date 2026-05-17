using HautsFramework;
using HautsPermits;
using RimWorld;
using Verse;

namespace HautsPermits_Occult
{
    /*most suffusions do not need any bespoke code. A few of them are quirky and cute with it though.
     * When Hediff_Rehumanizer reaches minimum severity, it... rehumanizes... the pawn, then is removed. (the "wait til the end for the effect" suffusions all work like this to prevent cheese with at least some hediff removal effects)
     * Appropriately, its permit worker will only let you target inhumanized pawns.*/
    public class Hediff_Rehumanizer : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.Severity == this.def.minSeverity)
            {
                if (this.pawn.Inhumanized())
                {
                    this.pawn.Rehumanize();
                }
                this.pawn.health.RemoveHediff(this);
            }
        }
    }
    public class RoyalTitlePermitWorker_Rehumanizer : RoyalTitlePermitWorker_GiveHediffs
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return pawn.Inhumanized() && base.IsGoodPawn(pawn);
        }
    }
    //on reaching minimum severity, revert the pawn's ghoul mutation and then remove from pawn. Appropriately, permit worker only lets you target ghouls.
    public class Hediff_Deghoulizer : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.Severity == this.def.minSeverity)
            {
                if (this.pawn.IsGhoul)
                {
                    this.pawn.mutant.Revert();
                }
                this.pawn.health.RemoveHediff(this);
            }
        }
    }
    public class RoyalTitlePermitWorker_Deghoulizer : RoyalTitlePermitWorker_GiveHediffs
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return pawn.IsGhoul && base.IsGoodPawn(pawn);
        }
    }
    /*Probability flux can only be given to a psy-sensitive humanlike who doesn't already have the hediff (including its permanent version). It then
     * gains sensPerHour severity per hour (which, given its XML, results in this much psysens being lost each hour)
     * creates a good event (see the Framework for the full roster) with an MTB of MTBhoursToGoodEvent*/
    public class Hediff_ProbabilityFlux : HediffWithComps
    {
        public override string LabelBase
        {
            get
            {
                HediffComp_ProbabilityFlux hcpf = this.TryGetComp<HediffComp_ProbabilityFlux>();
                if (hcpf != null && !hcpf.isActive)
                {
                    return hcpf.Props.permanentLabel;
                }
                return base.LabelBase;
            }
        }
    }
    public class HediffCompProperties_ProbabilityFlux : HediffCompProperties
    {
        public HediffCompProperties_ProbabilityFlux()
        {
            this.compClass = typeof(HediffComp_ProbabilityFlux);
        }
        public float sensPerHour;
        public float MTBhoursToGoodEvent;
        public string permanentLabel;
        public float postEventRecoveryPerHour;
    }
    public class HediffComp_ProbabilityFlux : HediffComp
    {
        public HediffCompProperties_ProbabilityFlux Props
        {
            get
            {
                return (HediffCompProperties_ProbabilityFlux)this.props;
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
            if (this.Pawn.IsHashIntervalTick(60, delta) && this.Pawn.Spawned)
            {
                if (this.isActive)
                {
                    if (this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon)
                    {
                        this.parent.Severity += this.Props.sensPerHour * 0.024f;
                        if (Rand.MTBEventOccurs(this.Props.MTBhoursToGoodEvent, 2500f, 60))
                        {
                            GoodAndBadIncidentsUtility.MakeGoodEvent(this.Pawn, 0, null);
                            Messages.Message("HVMP_ProbabilityFluxProc".Translate().CapitalizeFirst().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve(), this.Pawn, MessageTypeDefOf.PositiveEvent, true);
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
    public class RoyalTitlePermitWorker_ProbFlux : RoyalTitlePermitWorker_GiveHediffs
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            if (pawn.RaceProps.Humanlike && pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon && base.IsGoodPawn(pawn))
            {
                PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
                if (pme != null && pme.hediffs != null)
                {
                    foreach (HediffDef h in pme.hediffs)
                    {
                        if (pawn.health.hediffSet.HasHediff(h))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }
    }
    /*on reaching min severity, gain +1 psylink and then remove from pawn. Initial severity (and thus timer, given SeverityPerDay) is scaled by however high the pawn's psylink level already is
     * Can't target psydeaf pawns, because 1) why are you tryna give them psylinks? and 2) it would super suck if you misclicked and wasted this permit on a psydeaf individual.*/
    public class Hediff_Psylinker : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.Severity == this.def.minSeverity)
            {
                this.pawn.ChangePsylinkLevel(1, PawnUtility.ShouldSendNotificationAbout(this.pawn));
                this.pawn.health.RemoveHediff(this);
            }
        }
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            this.Severity += this.def.initialSeverity * this.pawn.GetPsylinkLevel();
        }
    }
    public class RoyalTitlePermitWorker_Psylinker : RoyalTitlePermitWorker_GiveHediffs
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return !pawn.HostileTo(this.caller) && pawn.RaceProps.Humanlike && pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon && base.IsGoodPawn(pawn);
        }
    }
}
