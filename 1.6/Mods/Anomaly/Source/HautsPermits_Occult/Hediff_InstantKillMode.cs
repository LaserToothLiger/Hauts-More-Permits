using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace HautsPermits_Occult
{
    //intended for use with a nociosphere. Makes it activate as soon as it isn't in a map with the HVMP_HypercubeMapComponent (i.e. a Natali labyrinth) and also despawns it once that activity period is over
    public class Hediff_InstantKillMode : Hediff
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            this.activated = false;
        }
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.Spawned)
            {
                if (this.pawn.Map.GetComponent<HVMP_HypercubeMapComponent>() == null)
                {
                    CompActivity ca = this.pawn.GetComp<CompActivity>();
                    if (ca != null)
                    {
                        if (!ca.IsActive)
                        {
                            if (this.activated)
                            {
                                NociosphereUtility.SkipTo(this.pawn, this.pawn.Position);
                                if (!this.pawn.DestroyedOrNull())
                                {
                                    this.pawn.DeSpawn(DestroyMode.Vanish);
                                    Lord lord = this.pawn.GetLord();
                                    if (lord != null)
                                    {
                                        lord.Notify_PawnLost(this.pawn, PawnLostCondition.ExitedMap, null);
                                    }
                                    Find.WorldPawns.PassToWorld(this.pawn, PawnDiscardDecideMode.Discard);
                                    return;
                                }
                            } else {
                                ca.SetActivity(ca.ActivityLevel + ((float)delta / this.ticksToKillMode));
                            }
                        } else {
                            this.activated = true;
                        }
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.activated, "activated", false, false);
        }
        public bool activated = false;
        public float ticksToKillMode = 900f;
    }
}
