using HautsFramework;
using HautsPermits;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace HautsPermits_Biotech
{
    /*all permit targeter friendly. Go do some reading (in the core directory, or the manual) if you don't know what that means
     * HealStim: activating on a caravan applies it to the pawn who would take the longest to recover from their healable injuries. Does not account for regeneration effects. Defaults to permit-user*/
    public class RoyalTitlePermitWorker_HealStim : RoyalTitlePermitWorker_GiveHediffs_PTargFriendly
    {
        public override void GiveHediffInCaravanInner(Pawn caller, Faction faction, bool free, Caravan caravan)
        {
            Pawn bestPawn = caller;
            float bestScore = 0f;
            foreach (Pawn p in caravan.pawns)
            {
                float score = 0f;
                foreach (Hediff h in p.health.hediffSet.hediffs)
                {
                    if (h is Hediff_Injury hi && !hi.IsPermanent())
                    {
                        score += h.Severity;
                    }
                }
                score /= Math.Max(0.5f, p.GetStatValue(StatDefOf.InjuryHealingFactor));
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPawn = p;
                }
            }
            this.AffectPawn(bestPawn, faction);
        }
    }
    //activating ImmunityStim on a caravan applies it to the pawn who would take the longest to attain immunity to their diseases. Defaults to permit-user
    public class RoyalTitlePermitWorker_ImmunityStim : RoyalTitlePermitWorker_GiveHediffs_PTargFriendly
    {
        public override void GiveHediffInCaravanInner(Pawn caller, Faction faction, bool free, Caravan caravan)
        {
            Pawn bestPawn = caller;
            float bestScore = 0f;
            foreach (Pawn p in caravan.pawns)
            {
                float score = 0f;
                foreach (Hediff h in p.health.hediffSet.hediffs)
                {
                    HediffComp_Immunizable hci = h.TryGetComp<HediffComp_Immunizable>();
                    if (hci != null && !hci.FullyImmune)
                    {
                        float ls = h.def.lethalSeverity;
                        if (ls == 0)
                        {
                            ls = 0.01f;
                        }
                        if (ls < 0)
                        {
                            ls = 1f;
                        }
                        score += Math.Max(0f, (h.Severity / h.def.lethalSeverity) - hci.Immunity);
                    }
                }
                score /= Math.Max(0.5f, p.GetStatValue(StatDefOf.ImmunityGainSpeed));
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPawn = p;
                }
            }
            this.AffectPawn(bestPawn, faction);
        }
    }
    //activating TempregStim on a caravan applies it to whoever is suffering the most from the temperature. Defaults to permit-user
    public class RoyalTitlePermitWorker_TempregStim : RoyalTitlePermitWorker_GiveHediffs_PTargFriendly
    {
        public override void GiveHediffInCaravanInner(Pawn caller, Faction faction, bool free, Caravan caravan)
        {
            Pawn bestPawn = caller;
            float bestScore = 0f;
            foreach (Pawn p in caravan.pawns)
            {
                float burnup = Math.Max(0f, p.AmbientTemperature - p.GetStatValue(StatDefOf.ComfyTemperatureMax));
                Hediff heatstroke = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Heatstroke);
                if (heatstroke != null)
                {
                    burnup *= (1f + heatstroke.Severity);
                }
                float cooldown = Math.Max(0f, p.GetStatValue(StatDefOf.ComfyTemperatureMin) - p.AmbientTemperature);
                Hediff hypothermia = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Hypothermia);
                if (hypothermia != null)
                {
                    burnup *= (1f + hypothermia.Severity);
                }
                float score = burnup + cooldown;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPawn = p;
                }
            }
            this.AffectPawn(bestPawn, faction);
        }
    }
    //activating ToxtolStim on a caravan applies it to whoever would take longest to clear their toxic buildup. Defaults to permit-user
    public class RoyalTitlePermitWorker_ToxtolStim : RoyalTitlePermitWorker_GiveHediffs_PTargFriendly
    {
        public override void GiveHediffInCaravanInner(Pawn caller, Faction faction, bool free, Caravan caravan)
        {
            Pawn bestPawn = caller;
            float bestScore = 0f;
            foreach (Pawn p in caravan.pawns)
            {
                float score = p.GetStatValue(StatDefOf.ToxicEnvironmentResistance);
                Hediff toxBuildup = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup);
                if (toxBuildup != null)
                {
                    score *= (1f + toxBuildup.Severity);
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPawn = p;
                }
            }
            this.AffectPawn(bestPawn, faction);
        }
    }
    /*activating TendStim on a caravan targets whichever pawn has the highest net severity of tendable hediffs, though pawns with higher injury healing factor are deprioritized. Defaults to permit-user
     * cannot be deliberately cast on a pawn who lacks tendable hediffs*/
    public class RoyalTitlePermitWorker_TendStim : RoyalTitlePermitWorker_GiveHediffs_PTargFriendly
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return base.IsGoodPawn(pawn) && pawn.health.hediffSet.HasTendableHediff();
        }
        public override void GiveHediffInCaravanInner(Pawn caller, Faction faction, bool free, Caravan caravan)
        {
            Pawn bestPawn = caller;
            float bestScore = 0f;
            foreach (Pawn p in caravan.pawns)
            {
                float score = 0f;
                List<Hediff> tendables = p.health.hediffSet.GetHediffsTendable().ToList();
                foreach (Hediff h in tendables)
                {
                    score += h.Severity;
                }
                score /= Math.Max(0.5f, p.GetStatValue(StatDefOf.InjuryHealingFactor));
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPawn = p;
                }
            }
            this.AffectPawn(bestPawn, faction);
        }
        public override void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
            List<Hediff> tendables = pawn.health.hediffSet.GetHediffsTendable().ToList();
            float tendQuality = Math.Max(0.01f, pme.extraNumber.RandomInRange);
            foreach (Hediff h in tendables)
            {
                h.Tended(tendQuality, tendQuality);
            }
            base.AffectPawnInner(pme, pawn, faction);
        }
    }
}
