using HautsF_Ideology;
using HautsFramework;
using HautsPermits;
using System;
using Verse;

namespace HautsPermits_Ideology
{
    //as DropIdeoBookForCaller, but seniority scaling
    public class RoyalTitlePermitWorker_DropIdeoBookSeniorityScaling : RoyalTitlePermitWorker_DropIdeoBookForCaller
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
}