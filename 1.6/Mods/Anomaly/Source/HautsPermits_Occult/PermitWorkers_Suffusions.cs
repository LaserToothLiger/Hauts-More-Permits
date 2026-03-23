using HautsFramework;
using HautsPermits;
using RimWorld;
using Verse;

namespace HautsPermits_Occult
{
    /*as with all HEMP permits, these are usable even if the permit's issuing faction is hostile to the permit-user, provided that the user has an off-cooldown permit authorizer hediff of the issuing faction
     * with that obligate preamble out of the way, most suffusions do not need any bespoke code. A few of them are quirky and cute with it though.
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
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            PermitAuthorizerUtility.DoPTargeterCooldown(faction, caller, this);
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
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            PermitAuthorizerUtility.DoPTargeterCooldown(faction, caller, this);
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
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            PermitAuthorizerUtility.DoPTargeterCooldown(faction, caller, this);
        }
    }
}
