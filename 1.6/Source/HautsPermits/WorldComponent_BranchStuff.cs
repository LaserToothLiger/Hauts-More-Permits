using HautsFramework;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace HautsPermits
{
    /*handles a bunch of things
     * every hour, remove all settlements belonging to an e-branch faction
     * keeps track of how many Mastermind quest effect stacks there are
     * [Anomaly] handles the minimum timer between Lovecraft quests firing*/
    public class WorldComponent_BranchStuff : WorldComponent
    {
        public WorldComponent_BranchStuff(World world) : base(world)
        {
            this.world = world;
        }
        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % 2500 == 0)
            {
                List<Settlement> settToRemove = new List<Settlement>();
                foreach (Settlement sett in Find.WorldObjects.Settlements)
                {
                    if (sett.Faction != null && sett.Faction.def.HasModExtension<EBranchQuests>())
                    {
                        settToRemove.Add(sett);
                    }
                }
                foreach (Settlement s in settToRemove)
                {
                    s.Destroy();
                }
            }
            WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
            foreach (Faction f in Find.FactionManager.AllFactionsVisible)
            {
                Hauts_FactionCompHolder fch = WCFC.FindCompsFor(f);
                if (fch != null)
                {
                    HautsFactionComp_PeriodicBranchQuests pbq = fch.TryGetComp<HautsFactionComp_PeriodicBranchQuests>();
                    if (pbq != null && pbq.isBranch)
                    {
                        if (f.defeated)
                        {
                            f.defeated = false;
                        }
                    }
                }
            }
            if (this.lovecraftEventTimer > 0)
            {
                this.lovecraftEventTimer--;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.tradeBlockages, "tradeBlockages", 0, false);
            Scribe_Values.Look<int>(ref this.lovecraftEventTimer, "lovecraftEventTimer", 0, false);
        }
        public int tradeBlockages;
        public int lovecraftEventTimer = 0;
        public List<QuestPart_PaxMundi> qppms = new List<QuestPart_PaxMundi>();
    }
}
