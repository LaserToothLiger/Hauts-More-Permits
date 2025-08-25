using HautsFramework;
using HautsPermits;
using HautsTraits;
using HautsTraitsRoyalty;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HVMP_HAT
{
    public class HVMP_HAT
    {
    }
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
    public class RoyalTitlePermitWorker_Ascender : RoyalTitlePermitWorker_GiveHediffs
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return !pawn.HostileTo(this.caller) && pawn.story != null && !PsychicAwakeningUtility.IsAwakenedPsychic(pawn,false) && !pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic) && base.IsGoodPawn(pawn);
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
