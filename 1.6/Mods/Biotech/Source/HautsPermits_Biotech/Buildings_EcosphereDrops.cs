using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;

namespace HautsPermits_Biotech
{
    /*every periodicity ticks, destroy all things of the Blight class in radius. Any such blights' plants take damageToPlant rotting damage
     * effecter and sound play whenever this effect occurs*/
    public class CompProperties_BlightBlast : CompProperties
    {
        public CompProperties_BlightBlast()
        {
            this.compClass = typeof(CompBlightBlast);
        }
        public float radius;
        public int periodicity;
        public float damageToPlant;
        public EffecterDef effecter;
        public SoundDef sound;
    }
    public class CompBlightBlast : ThingComp
    {
        public CompProperties_BlightBlast Props
        {
            get
            {
                return (CompProperties_BlightBlast)this.props;
            }
        }
        public override void PostDrawExtraSelectionOverlays()
        {
            GenDraw.DrawRadiusRing(this.parent.Position, this.Props.radius);
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.parent.Spawned)
            {
                this.ticksToNextBlast -= delta;
                if (this.ticksToNextBlast <= 0)
                {
                    this.Props.effecter?.SpawnMaintained(this.parent.PositionHeld, this.parent.MapHeld, 1f);
                    bool anyBlightKilled = false;
                    foreach (Blight blight in GenRadial.RadialDistinctThingsAround(this.parent.PositionHeld, this.parent.MapHeld, this.Props.radius, true).OfType<Blight>().Distinct<Blight>())
                    {
                        anyBlightKilled = true;
                        Plant plant = blight.Plant;
                        blight.Destroy();
                        plant?.TakeDamage(new DamageInfo(DamageDefOf.Rotting, this.Props.damageToPlant, 0f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true, QualityCategory.Normal, true));
                    }
                    if (anyBlightKilled && this.Props.sound != null)
                    {
                        this.Props.sound.PlayOneShot(new TargetInfo(this.parent.PositionHeld, this.parent.MapHeld, false));
                    }
                    this.ticksToNextBlast = this.Props.periodicity;
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.ticksToNextBlast, "ticksToNextBlast", 0, false);
        }
        public int ticksToNextBlast = 0;
    }
    //every periodicity ticks, adds fishPerTrigger to the fish population of the water body which this thing is currently in
    public class CompProperties_FishGenerator : CompProperties
    {
        public CompProperties_FishGenerator()
        {
            this.compClass = typeof(CompFishGenerator);
        }
        public int periodicity;
        public int fishPerTrigger;
    }
    public class CompFishGenerator : ThingComp
    {
        public CompProperties_FishGenerator Props
        {
            get
            {
                return (CompProperties_FishGenerator)this.props;
            }
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.parent.Spawned)
            {
                this.ticksToNextBlast -= delta;
                if (this.ticksToNextBlast <= 0)
                {
                    if (ModsConfig.OdysseyActive)
                    {
                        WaterBody wb = this.parent.Position.GetWaterBody(this.parent.Map);
                        if (wb != null)
                        {
                            wb.Population += this.Props.fishPerTrigger;
                        }
                    }
                    this.ticksToNextBlast = this.Props.periodicity;
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.ticksToNextBlast, "ticksToNextBlast", 0, false);
        }
        public int ticksToNextBlast = 0;
    }
    //every periodicity ticks, add growthAmount to the percentage growth of all non-blighted plants in radius. This is affected by those plants' growth rate (light level, fertility, etc.)
    public class CompProperties_PlantGrow : CompProperties
    {
        public CompProperties_PlantGrow()
        {
            this.compClass = typeof(CompPlantGrow);
        }
        public float radius;
        public int periodicity;
        public float growthAmount;
    }
    public class CompPlantGrow : ThingComp
    {
        public CompProperties_PlantGrow Props
        {
            get
            {
                return (CompProperties_PlantGrow)this.props;
            }
        }
        public override void PostDrawExtraSelectionOverlays()
        {
            GenDraw.DrawRadiusRing(this.parent.Position, this.Props.radius);
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.parent.Spawned)
            {
                this.ticksToNextBlast -= delta;
                if (this.ticksToNextBlast <= 0)
                {
                    foreach (Plant plant in GenRadial.RadialDistinctThingsAround(this.parent.Position, this.parent.Map, this.Props.radius, true).OfType<Plant>().Distinct<Plant>())
                    {
                        if (!plant.Blighted && plant.LifeStage > PlantLifeStage.Sowing)
                        {
                            plant.Growth += (this.Props.growthAmount * this.Props.periodicity * plant.GrowthRate) / (60000f * plant.def.plant.growDays);
                            plant.DirtyMapMesh(plant.Map);
                        }
                    }
                    this.ticksToNextBlast = this.Props.periodicity;
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.ticksToNextBlast, "ticksToNextBlast", 0, false);
        }
        public int ticksToNextBlast = 0;
    }
    /*after being alive and spawned for ticksToFinish, die and spawn a steam geyser at own location. Ideally, you want this on a 2x2 building.
     * While extant, emits a sustained soundWorking, and intermittently creates thick dust puffs around itself*/
    public class CompProperties_GTDrill : CompProperties
    {
        public CompProperties_GTDrill()
        {
            this.compClass = typeof(CompGTDrill);
        }
        public int ticksToFinish;
        public float dustRange;
        public SoundDef soundWorking;
    }
    public class CompGTDrill : ThingComp
    {
        private CompProperties_GTDrill Props
        {
            get
            {
                return (CompProperties_GTDrill)this.props;
            }
        }
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            if (this.sustainer != null && !this.sustainer.Ended)
            {
                this.sustainer.End();
            }
        }
        public override void CompTickRare()
        {
            base.CompTickRare();
            if (this.parent.Spawned)
            {
                if (!this.Props.soundWorking.NullOrUndefined())
                {
                    if (this.sustainer == null || this.sustainer.Ended)
                    {
                        this.sustainer = this.Props.soundWorking.TrySpawnSustainer(SoundInfo.InMap(this.parent, MaintenanceType.None));
                    }
                    this.sustainer.Maintain();
                } else if (this.sustainer != null && !this.sustainer.Ended) {
                    this.sustainer.End();
                }
                this.progressTicks += 250;
                if (this.progressTicks >= this.Props.ticksToFinish)
                {
                    Thing geyser = ThingMaker.MakeThing(ThingDefOf.SteamGeyser);
                    GenSpawn.Spawn(geyser, this.parent.Position, this.parent.Map, geyser.def.defaultPlacingRot, WipeMode.Vanish, false, false);
                    this.parent.Destroy(DestroyMode.KillFinalize);
                }
                if (this.Props.dustRange > 0f)
                {
                    int maxDust = 10;
                    foreach (IntVec3 intVec in GenRadial.RadialCellsAround(this.parent.Position, this.Props.dustRange, true).InRandomOrder())
                    {
                        if (maxDust > 0 && Rand.Chance(0.6f))
                        {
                            FleckMaker.ThrowDustPuffThick(intVec.ToVector3Shifted(), this.parent.Map, Rand.Range(1f, 3f), CompAbilityEffect_Wallraise.DustColor);
                            maxDust--;
                        }
                    }
                }
            }
        }
        public override string CompInspectStringExtra()
        {
            return "TimePassed".Translate().CapitalizeFirst() + ": " + this.progressTicks.ToStringTicksToPeriod(true, false, true, true, false);
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Progress 1 day",
                    action = delegate
                    {
                        this.progressTicks += 60000;
                    }
                };
            }
            yield break;
        }
        public override void PostExposeData()
        {
            Scribe_Values.Look<int>(ref this.progressTicks, "progressTicks", 0, false);
        }
        private Sustainer sustainer;
        private int progressTicks;
    }
}
