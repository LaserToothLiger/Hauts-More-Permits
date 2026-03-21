using HautsFramework;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace HautsPermits
{
    //aura that only turns on when in a caravan. Used by some Rover permits
    public class HediffCompProperties_CaravanHediffAura : HediffCompProperties_AuraHediff
    {
        public HediffCompProperties_CaravanHediffAura()
        {
            this.compClass = typeof(HediffComp_CaravanHediffAura);
        }
    }
    public class HediffComp_CaravanHediffAura : HediffComp_AuraHediff
    {
        public new HediffCompProperties_CaravanHediffAura Props
        {
            get
            {
                return (HediffCompProperties_CaravanHediffAura)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            Pawn pawn = this.parent.pawn;
            Caravan caravan = pawn.GetCaravan();
            if (caravan != null)
            {
                this.AffectSelf();
                this.AffectPawns(pawn, caravan.pawns.InnerListForReading, true);
            }
        }
    }
    /*added to ghouls spawned by various Occult permits, this is supposed to periodically tell them to GTFO and leave the map already once their time is up.
     * (As otherwise, a downed ghoul will lose its connection to its AI Lord that would normally tell it when it's time to go, and so once it regenerates and gets back up it just stays on your map forever, eating your meat).
     * It rarely works, but rarely is better than never. A better solution to Forever Ghouls is on the southern end of my priority list.*/
    public class HediffCompProperties_GetOffMyMap : HediffCompProperties
    {
        public HediffCompProperties_GetOffMyMap()
        {
            this.compClass = typeof(HediffComp_GetOffMyMap);
        }
        public int minDuration;
        public int periodicity;
    }
    public class HediffComp_GetOffMyMap : HediffComp
    {
        public HediffCompProperties_GetOffMyMap Props
        {
            get
            {
                return (HediffCompProperties_GetOffMyMap)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            this.faction = this.Pawn.Faction;
            this.timer = this.Props.minDuration;
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            this.timer -= delta;
            if (this.timer <= 0)
            {
                this.timer = this.Props.periodicity;
                this.JustLeaveAlready();
            }
        }
        public void JustLeaveAlready()
        {
            if (!this.Pawn.Spawned)
            {
                this.parent.Severity = -1f;
                return;
            }
            if (this.Pawn.Faction != null && this.faction != null)
            {
                if (this.Pawn.Faction == Faction.OfPlayerSilentFail)
                {
                    this.parent.Severity = -1f;
                    return;
                }
                if (this.Pawn.Faction == this.faction && this.Pawn.Map.CanEverExit && (this.Pawn.Map.ParentFaction == null || this.Pawn.Map.ParentFaction == this.faction))
                {
                    if (this.Pawn.jobs.curJob != null)
                    {
                        this.Pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
                    }
                    Lord lord2 = this.Pawn.GetLord();
                    bool mustMakeNewLord = true;
                    if (lord2 != null)
                    {
                        if (lord2.LordJob is LordJob_ExitMapBest)
                        {
                            mustMakeNewLord = false;
                        }
                        else
                        {
                            lord2.Notify_PawnLost(this.Pawn, PawnLostCondition.Undefined);
                        }
                    }
                    if (mustMakeNewLord)
                    {
                        List<Pawn> pawn = new List<Pawn>
                        {
                            this.Pawn
                        };
                        Lord lord = LordMaker.MakeNewLord(this.faction, new LordJob_ExitMapBest(LocomotionUrgency.Jog, false, true), this.Pawn.Map, pawn);
                    }
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.timer, "timer", 2500, false);
            Scribe_References.Look<Faction>(ref this.faction, "faction", false);
        }
        public int timer;
        public Faction faction;
    }
}
