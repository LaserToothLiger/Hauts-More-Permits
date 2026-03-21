using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace HautsPermits
{
    /*opening an Enterprise Security Crate (or, obviously, any CompCreateRewardsForHack) drops some group of items, randomly drawn from the pool of all HackRewardDefs.
     * itemsToDrop: if this HRD is selected, all such items are created.
     * baseChance: this HRD's weighting when an HRD is being randomly selected
     * guaranteedForFaction: if the crate belongs to a specific faction, the first random HRD it uses to create rewards will be a random one whose guaranteedForFaction is its faction's def (if any).
     *   Any subsequent random HRDs it uses have a 35% chance to be drawn from the same subset of HRDs as well*/
    public class HackRewardDef : Def
    {
        public HackRewardDef()
        {

        }
        public List<ThingDefCountClass> itemsToDrop;
        public float baseChance = 0.1f;
        public FactionDef guaranteedForFaction;
    }
    /*guaranteedReward: always creates this HRD, if any. (This is set so they always drop a permit authorizer)
     * bonusRewardCount: creates a number of additional reward sets equal to a random value within this range. This is where HRDs' baseChance and guaranteedForFaction come into play, see comments for HackRewardDef
     * soundOnUnlock: plays when the items are created*/
    public class CompProperties_CreateRewardsForHack : CompProperties
    {
        public CompProperties_CreateRewardsForHack()
        {
            this.compClass = typeof(CompCreateRewardsForHack);
        }
        public HackRewardDef guaranteedReward;
        public IntRange bonusRewardCount;
        public SoundDef soundOnUnlock;
    }
    public class CompCreateRewardsForHack : ThingComp
    {
        public CompProperties_CreateRewardsForHack Props
        {
            get
            {
                return (CompProperties_CreateRewardsForHack)this.props;
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            this.beenOpened = false;
            this.SetFactionToMapFaction();
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                this.SetFactionToMapFaction();
            }
        }
        public void SetFactionToMapFaction()
        {
            if (this.parent.Faction == null && this.parent.Map != null && this.parent.Map.ParentFaction != null)
            {
                this.parent.SetFaction(this.parent.Map.ParentFaction);
            }
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (!this.beenOpened)
            {
                CompHackable ch = this.parent.TryGetComp<CompHackable>();
                if (ch != null && ch.IsHacked)
                {
                    this.beenOpened = true;
                    if (this.parent.Spawned)
                    {
                        this.Props.soundOnUnlock.PlayOneShot(new TargetInfo(this.parent.Position, this.parent.Map, false));
                        if (this.Props.guaranteedReward != null)
                        {
                            this.SpawnReward(this.Props.guaranteedReward);
                        }
                        if (this.Props.bonusRewardCount != null)
                        {
                            int bonusRewards = this.Props.bonusRewardCount.RandomInRange;
                            if (bonusRewards > 0 && this.parent.Faction != null)
                            {
                                HackRewardDef factionHrd = this.GetFactionSpecificReward();
                                if (factionHrd != null)
                                {
                                    this.SpawnReward(factionHrd);
                                    bonusRewards--;
                                }
                            }
                            if (bonusRewards > 0)
                            {
                                for (int i = 0; i < bonusRewards; i++)
                                {
                                    HackRewardDef hrdextra;
                                    if (Rand.Chance(0.35f))
                                    {
                                        hrdextra = this.GetFactionSpecificReward();
                                    } else {
                                        hrdextra = DefDatabase<HackRewardDef>.AllDefsListForReading.RandomElementByWeight((HackRewardDef hrdish) => hrdish.baseChance);
                                    }
                                    SpawnReward(hrdextra);
                                }
                            }
                        }
                    }
                }
            }
        }
        public HackRewardDef GetFactionSpecificReward()
        {
            List<HackRewardDef> factionHrds = new List<HackRewardDef>();
            foreach (HackRewardDef hrdi in DefDatabase<HackRewardDef>.AllDefsListForReading)
            {
                if (hrdi.guaranteedForFaction != null && hrdi.guaranteedForFaction == this.parent.Faction.def)
                {
                    factionHrds.Add(hrdi);
                }
            }
            if (factionHrds.Count > 0)
            {
                return factionHrds.RandomElement();
            }
            return DefDatabase<HackRewardDef>.AllDefsListForReading.RandomElement();
        }
        public void SpawnReward(HackRewardDef hrd)
        {
            if (hrd != null && hrd.itemsToDrop != null)
            {
                foreach (ThingDefCountClass tdcc in hrd.itemsToDrop)
                {
                    Thing thing = ThingMaker.MakeThing(tdcc.thingDef, tdcc.stuff);
                    thing.stackCount = tdcc.count;
                    if (thing.TryGetComp(out CompQuality compQuality))
                    {
                        compQuality.SetQuality(tdcc.quality, new ArtGenerationContext?(ArtGenerationContext.Outsider));
                    }
                    if (thing.TryGetComp(out CompPowerBattery compBattery))
                    {
                        compBattery.SetStoredEnergyPct(1f);
                    }
                    if (this.parent.Faction != null && thing.TryGetComp(out CompTargetEffect_InstallPTargeter cteipt))
                    {
                        cteipt.faction = this.parent.Faction;
                        cteipt.freshFromVault = true;
                    }
                    if (thing.def.Minifiable)
                    {
                        MinifiedThing minifiedThing = thing.MakeMinified();
                        GenSpawn.Spawn(minifiedThing, this.parent.Position, this.parent.Map);
                        minifiedThing.Notify_DebugSpawned();
                    } else {
                        GenSpawn.Spawn(thing, this.parent.Position, this.parent.Map);
                        thing.Notify_DebugSpawned();
                    }
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.beenOpened, "beenOpened", false, false);
        }
        public bool beenOpened;
    }
}
