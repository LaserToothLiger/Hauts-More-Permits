using HautsFramework;
using RimWorld;
using System;
using Verse;

namespace HautsPermits
{
    //give this DME to a permit def. When the game is restarted, its description will be substituted out for extraString if the permitsScaleBySeniority mod setting is disabled
    public class ScalingDisabledDescription : DefModExtension
    {
        public ScalingDisabledDescription() { }
        public string extraString;
    }
    /*the following permit workers are all derivatives of other permit authorizer-respecting permit workers in this mod.
     * If permitsScaleBySeniority mod setting is enabled, the magnitude of their created effects is multiplied by [the permit-user's seniority with that faction / 100].
     * Each successive title has 100 more seniority than the last, so essentially the multi is the user's rank.*/
    public class RoyalTitlePermitWorker_DropBookSeniorityScaling_PTargFriendly : RoyalTitlePermitWorker_DropBook_PTargFriendly
    {
        public override int ItemStackCount(PermitMoreEffects pme, Pawn caller)
        {
            int result = base.ItemStackCount(pme, caller);
            if (this.faction != null)
            {
                float curSeniority = HVMP_Mod.settings.permitsScaleBySeniority ? caller.royalty.GetCurrentTitleInFaction(this.faction).def.seniority : this.def.minTitle.seniority;
                float divisor = (pme != null && pme.minPetness > 0) ? pme.minPetness : 100f;
                int seniority = Math.Max((int)(curSeniority / divisor), 1);
                result *= seniority;
            }
            return result;
        }
    }
    public class RoyalTitlePermitWorker_DROCSeniorityScaling_PTargFriendly : RoyalTitlePermitWorker_DROC_PTargFriendly
    {
        public override int ItemStackCount(PermitMoreEffects pme, Pawn caller)
        {
            int result = base.ItemStackCount(pme, caller);
            if (this.faction != null)
            {
                float curSeniority = HVMP_Mod.settings.permitsScaleBySeniority ? caller.royalty.GetCurrentTitleInFaction(this.faction).def.seniority : this.def.minTitle.seniority;
                float divisor = pme.phenomenonCount > 0 ? pme.phenomenonCount : 100f;
                int seniority = Math.Max((int)(curSeniority / divisor), 1);
                result *= seniority;
            }
            return result;
        }
    }
    public class RoyalTitlePermitWorker_DropResourcesStuffSeniorityScaling_PTargFriendly : RoyalTitlePermitWorker_DropResourcesStuff_PTargFriendly
    {
        public override int ItemStackCount(ThingDefCountClass tdcc, PermitMoreEffects pme, Pawn caller)
        {
            int result = base.ItemStackCount(tdcc, pme, caller);
            if (this.faction != null)
            {
                float curSeniority = HVMP_Mod.settings.permitsScaleBySeniority ? caller.royalty.GetCurrentTitleInFaction(this.faction).def.seniority : this.def.minTitle.seniority;
                float divisor = (pme != null && pme.phenomenonCount > 0) ? pme.phenomenonCount : 100f;
                int seniority = Math.Max((int)(curSeniority / divisor), 1);
                result *= seniority;
            }
            return result;
        }
    }
    public class RoyalTitlePermitWorker_GenerateQuestSeniorityScaling_PTargFriendly : RoyalTitlePermitWorker_GenerateQuest_PTargFriendly
    {
        public override int NumQuestsToGenerate(PermitMoreEffects pme, Pawn caller, Faction faction)
        {
            int result = base.NumQuestsToGenerate(pme, caller, faction);
            if (faction != null)
            {
                float curSeniority = HVMP_Mod.settings.permitsScaleBySeniority ? caller.royalty.GetCurrentTitleInFaction(faction).def.seniority : this.def.minTitle.seniority;
                int seniority = Math.Max((int)(curSeniority / 100f), 1);
                result += seniority;
            }
            return result;
        }
    }
}
