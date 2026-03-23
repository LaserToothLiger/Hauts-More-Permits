using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace HautsPermits
{
    public static class PermitAuthorizerUtility
    {
        //a permit authorizer must have an associated faction. This forces it to be a random e-branch; fallback as a random title-granting faction; fallback as a random faction
        public static Faction AssignFallbackFactionToPermitTargeter()
        {
            List<Faction> factions = new List<Faction>();
            foreach (Faction f in Find.FactionManager.AllFactionsVisible)
            {
                if (f.def.HasModExtension<EBranchQuests>())
                {
                    factions.Add(f);
                }
            }
            if (factions.Count > 0)
            {
                return factions.RandomElement();
            }
            factions.Clear();
            foreach (Faction f in Find.FactionManager.AllFactionsVisible)
            {
                if (f.def.HasRoyalTitles)
                {
                    factions.Add(f);
                }
            }
            if (factions.Count > 0)
            {
                return factions.RandomElement();
            }
            return null;
        }
        //return the permit authorizer implant of a pawn that is 1) of the specified faction and 2) ready to sue
        public static Hediff_PTargeter GetPawnPTargeter(Pawn pawn, Faction faction)
        {
            foreach (Hediff h in pawn.health.hediffSet.hediffs)
            {
                if (h is Hediff_PTargeter hpt && hpt.faction == faction && hpt.Severity >= 1f)
                {
                    return hpt;
                }
            }
            return null;
        }
        //puts the pawn's permit authorizer implant of the appropriate faction on cooldown, which scales with the favor cost of the permit utilized
        public static void DoPTargeterCooldown(Faction faction, Pawn caller, RoyalTitlePermitWorker rptw)
        {
            if (faction.HostileTo(Faction.OfPlayer))
            {
                Hediff_PTargeter hpt = PermitAuthorizerUtility.GetPawnPTargeter(caller, faction);
                if (hpt != null)
                {
                    hpt.Severity = 0.001f;
                    hpt.cooldownTicks = (int)(HVMP_Mod.settings.authorizerCooldownDays * 60000 * rptw.def.royalAid.favorCost);
                }
            }
        }
        /*if you want a permit to utilize Permit Authorizer technology (even when the permit's issuing faction is hostile to you, you can use the permit if the permit-user has an off-cooldown permit authorizer implant of its faction)
         * you sub this in where its FillAidOption would normally be. Examples are amply provided via all of the permit workers in this mod*/
        public static bool ProprietaryFillAidOption(RoyalTitlePermitWorker rptw, Pawn pawn, Faction faction, ref string description, out bool free)
        {
            if (faction.HostileTo(Faction.OfPlayer))
            {
                Hediff_PTargeter hpt = PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction);
                if (hpt != null)
                {
                    description += "CommandCallRoyalAidFreeOption".Translate();
                    free = true;
                    return true;
                }
            }
            int lastUsedTick = pawn.royalty.GetPermit(rptw.def, faction).LastUsedTick;
            int num = Math.Max(GenTicks.TicksGame - lastUsedTick, 0);
            if (lastUsedTick < 0 || num >= rptw.def.CooldownTicks)
            {
                description += "CommandCallRoyalAidFreeOption".Translate();
                free = true;
                return true;
            }
            int num2 = (lastUsedTick > 0) ? Math.Max(rptw.def.CooldownTicks - num, 0) : 0;
            description += "CommandCallRoyalAidFavorOption".Translate(num2.TicksToDays().ToString("0.0"), rptw.def.royalAid.favorCost, faction.Named("FACTION"));
            if (pawn.royalty.GetFavor(faction) >= rptw.def.royalAid.favorCost)
            {
                free = false;
                return true;
            }
            free = false;
            return false;
        }
    }
}
