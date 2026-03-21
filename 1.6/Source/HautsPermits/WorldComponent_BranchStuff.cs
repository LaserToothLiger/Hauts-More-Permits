using HautsFramework;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace HautsPermits
{
    /*handles a bunch of things
     * every hour, remove all settlements belonging to an e-branch faction, except for branch platforms
     * keeps track of how many Mastermind quest effect stacks there are
     * [Anomaly] handles the minimum timer between Lovecraft quests firing
     * [Odyssey] handles the timer to create new branch platforms for each e-branch faction. Both the timer's duration and the maximum number of platforms any faction can have are set in the mod settings*/
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
                    if (!(sett is BranchPlatform) && sett.Faction != null && sett.Faction.def.HasModExtension<EBranchQuests>())
                    {
                        settToRemove.Add(sett);
                    }
                }
                foreach (Settlement s in settToRemove)
                {
                    s.Destroy();
                }
            }
            if (this.lovecraftEventTimer > 0)
            {
                this.lovecraftEventTimer--;
            }
            if (this.newSettlementTick > 0)
            {
                this.newSettlementTick--;
            } else if (ModsConfig.OdysseyActive && Find.WorldGrid.Orbit != null) {
                WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
                List<Faction> factionsToMakePlatformsFor = new List<Faction>();
                foreach (Faction f in Find.FactionManager.AllFactionsVisible)
                {
                    Hauts_FactionCompHolder fch = WCFC.FindCompsFor(f);
                    if (fch != null)
                    {
                        HautsFactionComp_PeriodicBranchQuests pbq = fch.TryGetComp<HautsFactionComp_PeriodicBranchQuests>();
                        if (pbq != null && pbq.isBranch)
                        {
                            int platforms = 0;
                            foreach (Settlement bpsh in Find.World.worldObjects.Settlements)
                            {
                                if (bpsh is BranchPlatform && bpsh.Faction != null && bpsh.Faction == f)
                                {
                                    platforms++;
                                }
                            }
                            if (platforms < HVMP_Mod.settings.maxPlatformsPerBranch)
                            {
                                factionsToMakePlatformsFor.Add(f);
                            }
                            if (f.defeated)
                            {
                                f.defeated = false;
                            }
                        }
                    }
                }
                foreach (Faction f2 in factionsToMakePlatformsFor)
                {
                    WorldObject worldObject = WorldObjectMaker.MakeWorldObject(HVMPDefOf.HVMP_BranchPlatform);
                    worldObject.SetFaction(f2);
                    worldObject.Tile = TileFinder.RandomSettlementTileFor(Find.WorldGrid.Orbit, f2, false, null);
                    INameableWorldObject nameableWorldObject = worldObject as INameableWorldObject;
                    if (nameableWorldObject != null)
                    {
                        nameableWorldObject.Name = SettlementNameGenerator.GenerateSettlementName(worldObject, null);
                    }
                    Find.WorldObjects.Add(worldObject);
                }
                this.newSettlementTick = (int)(HVMP_Mod.settings.makeNewBranchPlatformInterval * 60000);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.tradeBlockages, "tradeBlockages", 0, false);
            Scribe_Values.Look<int>(ref this.lovecraftEventTimer, "lovecraftEventTimer", 0, false);
            Scribe_Values.Look<int>(ref this.newSettlementTick, "newSettlementTick", 100, false);
        }
        public int tradeBlockages;
        public int lovecraftEventTimer = 0;
        public int newSettlementTick = 100;
        public List<QuestPart_PaxMundi> qppms = new List<QuestPart_PaxMundi>();
    }
}
