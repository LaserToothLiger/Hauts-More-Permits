using HautsFramework;
using HautsPermits;
using HautsTraitsRoyalty;
using RimWorld;
using Verse;

namespace HVMP_HAT
{
    //when the Ascension Suffusion's natural duration (severity decay) runs out, the pawn gains the Latent Psychic trait of a random type
    public class Hediff_Ascender : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.Severity == this.def.minSeverity)
            {
                if (this.pawn.story != null)
                {
                    if (!this.pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
                    {
                        this.pawn.story.traits.GainTrait(new Trait(HVTRoyaltyDefOf.HVT_LatentPsychic, HVTRoyaltyDefOf.HVT_LatentPsychic.degreeDatas.RandomElement().degree));
                    }
                }
                this.pawn.health.RemoveHediff(this);
            }
        }
    }
    //Ascension Suffusion cannot target latent or woke psychics, because that would be useless.
    public class RoyalTitlePermitWorker_Ascender : RoyalTitlePermitWorker_GiveHediffs
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return !pawn.HostileTo(this.caller) && pawn.story != null && !PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn,false) && !pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic) && base.IsGoodPawn(pawn);
        }
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return HVMP_Utility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            HVMP_Utility.DoPTargeterCooldown(faction, caller, this);
        }
    }
}
