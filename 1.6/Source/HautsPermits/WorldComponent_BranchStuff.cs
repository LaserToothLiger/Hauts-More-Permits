using HautsFramework;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace HautsPermits
{
    /*handles a bunch of things
     * every hour, remove all settlements belonging to an e-branch faction
     * keeps track of how many Mastermind quest effect stacks there are
     * [Anomaly] handles the minimum timer between Lovecraft quests firing
     * [Odyssey] handles the timer between wunderchip quests firing*/
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
                if (ModsConfig.OdysseyActive)
                {
                    if (this.wunderQuestTimer > 0)
                    {
                        this.wunderQuestTimer--;
                    } else if (this.wunderQuestTimer < 0) {
                        this.wunderQuestTimer = 24*Rand.RangeInclusive((int)HVMP_Mod.settings.minWunderQuestInterval, (int)HVMP_Mod.settings.maxWunderQuestInterval);
                    } else {
                        if (this.wunderQuests == null)
                        {
                            this.wunderQuests = new List<QuestScriptDef>();
                            foreach (QuestScriptDef qsd in DefDatabase<QuestScriptDef>.AllDefs)
                            {
                                if (qsd.HasModExtension<WunderQuest>())
                                {
                                    this.wunderQuests.Add(qsd);
                                }
                            }
                        }
                        if (this.wunderQuests != null)
                        {
                            IIncidentTarget target = Find.AnyPlayerHomeMap;
                            if (target == null)
                            {
                                target = Find.World;
                            }
                            Slate slate = new Slate();
                            slate.Set<float>("points", StorytellerUtility.DefaultThreatPointsNow(target), false);
                            int tries = 5;
                            while (tries > 0)
                            {
                                QuestScriptDef wunderQuest = this.wunderQuests.RandomElement();
                                if (wunderQuest.CanRun(slate, target))
                                {
                                    Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(wunderQuest, slate);
                                    if (!quest.hidden && quest.root.sendAvailableLetter)
                                    {
                                        QuestUtility.SendLetterQuestAvailable(quest, null);
                                    }
                                    break;
                                }
                                tries--;
                            }
                        }
                        this.wunderQuestTimer = 24*Rand.RangeInclusive((int)HVMP_Mod.settings.minWunderQuestInterval, (int)HVMP_Mod.settings.maxWunderQuestInterval);
                    }
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
            Scribe_Values.Look<int>(ref this.wunderQuestTimer, "wunderQuestTimer", -1, false);
        }
        public int tradeBlockages;
        public int lovecraftEventTimer = 0;
        public int wunderQuestTimer = -1;
        public List<QuestPart_PaxMundi> qppms = new List<QuestPart_PaxMundi>();
        public List<QuestScriptDef> wunderQuests; 
    }
    public class WunderQuest : DefModExtension
    {

    }
}
