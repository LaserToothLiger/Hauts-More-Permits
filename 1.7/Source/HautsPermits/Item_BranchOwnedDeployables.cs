using HautsFramework;
using RimWorld;
using Verse;

namespace HautsPermits
{
    //if you damage something with this DME, it doesn't incur goodwill loss with its faction
    public class HVMP_ItsOkToHarmThis : DefModExtension
    {
        public HVMP_ItsOkToHarmThis() { }
    }
    //as FactionColored from the Framework, except it initially makes the thing the player's faction. For some reason, this is required for Mechhive-faction mechanoids to want to attack Pax turrets.
    public class CompProperties_FactionColoredTeamJimmy : CompProperties_FactionColored
    {
        public CompProperties_FactionColoredTeamJimmy()
        {
            this.compClass = typeof(CompFactionColored_TeamJimmy);
        }
    }
    public class CompFactionColored_TeamJimmy : CompFactionColored
    {
        public new CompProperties_FactionColoredTeamJimmy Props
        {
            get
            {
                return (CompProperties_FactionColoredTeamJimmy)this.props;
            }
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.parent.IsHashIntervalTick(5) && this.parent.Spawned)
            {
                if (!this.jimmiedYet)
                {
                    if (this.team == null)
                    {
                        this.team = this.parent.Faction;
                        this.parent.SetFaction(Faction.OfPlayerSilentFail);
                    }
                    else
                    {
                        this.jimmiedYet = true;
                        this.parent.SetFaction(this.team);
                    }
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look<Faction>(ref this.team, "team", false);
            Scribe_Values.Look<bool>(ref this.jimmiedYet, "jimmiedYet", false, false);
        }
        public Faction team;
        public bool jimmiedYet;
    }
}
