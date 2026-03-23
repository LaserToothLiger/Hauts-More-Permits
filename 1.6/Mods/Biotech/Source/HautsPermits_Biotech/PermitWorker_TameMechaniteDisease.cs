using HautsFramework;
using HautsPermits;
using RimWorld;
using Verse;

namespace HautsPermits_Biotech
{
    /*permit authorizer friendly - if the permit-holder has an off-cooldown permit authorizer implant of the permit's issuing faction, they can use this permit even while hostile to its issuing faction
     * Tame Mechanite Disease uses PermitMoreEffects, sepcifically its hediffs field. Load it up only with hediffs that have the TMD comp
     * can only target pawns who have a hediff that is the convertedFrom of one of their TMD comp hediffs, replacing that hediff with the corresponding hediff and giving it half its remaining duration for HediffComp_Disappears
     * XMLable in case another mod adds more mechanite diseases*/
    public class RoyalTitlePermitWorker_TMD : RoyalTitlePermitWorker_TargetPawn
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                foreach (HediffDef h in pme.hediffs)
                {
                    HediffCompProperties_TMD compTMD = null;
                    if (h.comps != null)
                    {
                        foreach (HediffCompProperties hcp in h.comps)
                        {
                            if (hcp is HediffCompProperties_TMD hcptmd)
                            {
                                compTMD = hcptmd;
                            }
                        }
                    }
                    if (compTMD != null)
                    {
                        foreach (Hediff ph in pawn.health.hediffSet.hediffs)
                        {
                            if (ph.def == compTMD.convertedFrom)
                            {
                                this.toReplace = ph;
                                this.toGive = h;
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
            base.AffectPawnInner(pme, pawn, faction);
            if (this.toReplace != null && this.toGive != null)
            {
                HediffComp_Disappears hcd = this.toReplace.TryGetComp<HediffComp_Disappears>();
                int ticksRemaining = hcd != null ? hcd.ticksToDisappear : 900000;
                Hediff hediff = HediffMaker.MakeHediff(this.toGive, pawn);
                pawn.health.AddHediff(hediff);
                hcd = hediff.TryGetComp<HediffComp_Disappears>();
                if (hcd != null)
                {
                    hcd.ticksToDisappear = ticksRemaining / 2;
                }
                pawn.health.RemoveHediff(this.toReplace);

            }
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            PermitAuthorizerUtility.DoPTargeterCooldown(faction, caller, this);
        }
        private Hediff toReplace;
        private HediffDef toGive;
    }
    public class HediffCompProperties_TMD : HediffCompProperties
    {
        public HediffCompProperties_TMD()
        {
            this.compClass = typeof(HediffComp_TMD);
        }
        public HediffDef convertedFrom;
    }
    public class HediffComp_TMD : HediffComp
    {
        public HediffCompProperties_TMD Props
        {
            get
            {
                return (HediffCompProperties_TMD)this.props;
            }
        }
    }
}
