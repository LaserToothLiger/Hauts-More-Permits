using HautsFramework;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace HautsPermits_Biotech
{
    /*applies a random nano- or animal- vaccination from Medical Expansion - Vaccines to the target (or if they have a disease that can be vaccinated, grants the corresponding nano-vaccine). Goes w/o saying, should only be used on permits that require MEV
     * why does it search for vaccines by string prefixes? That's how MEV does it, so.*/
    public class RoyalTitlePermitWorker_Vaccinate : RoyalTitlePermitWorker_TargetPawn
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return pawn.RaceProps.IsFlesh;
        }
        public override void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
            base.AffectPawnInner(pme, pawn, faction);
            if (this.GetRandomDiseaseToVax(pawn, out Hediff disease, out HediffDef vaccine))
            {
                Hediff vax = pawn.health.hediffSet.GetFirstHediffOfDef(vaccine);
                if (vax != null)
                {
                    vax.Severity += 1.5f;
                } else {
                    vax = HediffMaker.MakeHediff(vaccine, pawn);
                    vax.Severity = 1.5f;
                    pawn.health.AddHediff(vax);
                }
            } else {
                List<HediffDef> allPossibleVaxes = DefDatabase<HediffDef>.AllDefsListForReading.Where((HediffDef hd) => (hd.defName.StartsWith("AnimalVaccinated_") || hd.defName.StartsWith("NanoVaccinated_")) && !pawn.health.hediffSet.HasHediff(hd)).ToList();
                if (!allPossibleVaxes.NullOrEmpty())
                {
                    Hediff vax = HediffMaker.MakeHediff(allPossibleVaxes.RandomElement(),pawn);
                    vax.Severity = 1.5f;
                    pawn.health.AddHediff(vax);
                }
            }
            Messages.Message("HVMP_MedicalAssistance".Translate(faction.Named("FACTION")), new LookTargets(pawn.Position, pawn.Map), MessageTypeDefOf.NeutralEvent, true);
            this.caller.royalty.GetPermit(this.def, faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
            }
        }
        public bool GetRandomDiseaseToVax(Pawn p, out Hediff disease, out HediffDef vaccine)
        {
            disease = null;
            vaccine = null;
            string[] prefixes = new string[] { "AnimalVaccinated_", "NanoVaccinated_" };
            foreach (Hediff h in p.health.hediffSet.hediffs.InRandomOrder())
            {
                foreach (string prefix in prefixes)
                {
                    HediffDef vax = DefDatabase<HediffDef>.GetNamedSilentFail(prefix + h.def.defName);
                    if (vax != null)
                    {
                        disease = h;
                        vaccine = vax;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
