using HautsFramework;
using RimWorld;
using Verse;

namespace HautsPermits_Occult
{
    //MTB 3 hours, a random organic, non-anomalous pawn releases a cute little viscera explosion and gain blood rot with a third its usual duration
    public class GameCondition_BloodBlight : GameCondition
    {
        public override void GameConditionTick()
        {
            base.GameConditionTick();
            if (Find.TickManager.TicksGame % 120 == 0 && Rand.MTBEventOccurs(3f, 2500f, 120))
            {
                foreach (Map map in base.AffectedMaps)
                {
                    foreach (Pawn p in map.mapPawns.AllPawnsSpawned.InRandomOrder())
                    {
                        if (p.RaceProps.IsFlesh && !p.IsMutant && !p.IsEntity)
                        {
                            //Log.Error("p: " + p.Label);
                            Hediff hediff = HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed("BloodRot"), p);
                            p.health.DropBloodFilth();
                            p.health.AddHediff(hediff);
                            HediffComp_Disappears hcd = hediff.TryGetComp<HediffComp_Disappears>();
                            if (hcd != null)
                            {
                                hcd.disappearsAfterTicks /= 3;
                                hcd.ticksToDisappear = hcd.disappearsAfterTicks;
                            }
                            FleshbeastUtility.MeatSplatter(2, p.PositionHeld, p.MapHeld, FleshbeastUtility.MeatExplosionSize.Normal);
                            break;
                        }
                    }
                }
            }
        }
    }
    //the other three possible conditions inflicted by Lovecraft's Touch of the Unfathomable mutator are derivatives of GameCondition_InflictHediff. They can't inflict hediffs on non-organic or anomalous pawns
    public class GameCondition_InflictHediff_Lovecraft : GameCondition_InflictHediff
    {
        public override bool CheckPawnInner(Pawn pawn, InflictedHediff ih)
        {
            return pawn.RaceProps.IsFlesh && !pawn.IsMutant && !pawn.IsEntity && base.CheckPawnInner(pawn, ih);
        }
    }
}
