using RimWorld;
using Verse;

namespace HautsPermits_Occult
{
    //the more severe the hediff caused by Absence of Energy and Happiness' game condition, the worse the mood
    public class Thought_AOEAH : Thought_Situational
    {
        protected override float BaseMoodOffset
        {
            get
            {
                float num = 0f;
                Hediff firstHediffOfDef = this.pawn.health.hediffSet.GetFirstHediffOfDef(this.def.hediff, false);
                if (firstHediffOfDef != null)
                {
                    num = firstHediffOfDef.Severity;
                }
                return -num;
            }
        }
    }
}
