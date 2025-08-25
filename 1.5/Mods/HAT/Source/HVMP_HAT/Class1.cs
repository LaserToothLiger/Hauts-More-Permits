using HautsFramework;
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
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (this.pawn.story == null)
            {
                this.Severity = 0f;
            } else if (!this.pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic)) {
                this.Severity = this.def.initialSeverity * 5;
            } else {
                this.Severity = this.def.initialSeverity * 25;
            }
        }
        public override void PostTick()
        {
            base.PostTick();
            if (this.Severity == this.def.minSeverity)
            {
                if (this.pawn.story != null)
                {
                    if (this.pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
                    {
                        PsychicAwakeningUtility.AwakenPsychicTalent(this.pawn, true, "HVMP_WokeOccult".Translate().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), "HVMP_WokeOccultF".Translate().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve());
                    }
                    else
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
            return !pawn.HostileTo(this.caller) && pawn.story != null && !PsychicAwakeningUtility.IsAwakenedPsychic(pawn,false) && base.IsGoodPawn(pawn);
        }
    }
}
