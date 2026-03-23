using HautsFramework;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;

namespace HautsPermits_Occult
{
    /*moves around like a tornado, but instead of doing damage it exudes tox gas (or rot stink if you lack Biotech yet somehow decided to pick up Anomaly. What? Why??) and inflicts toxic buildup on nearby pawns
     * Also, it's not supposed to be a persistent hazard until you leave the Natali labyrinth, so as long as there's a Natali labyrinth exit structure on the map it just teleports back to that when it would wander off the map*/
    public class LivingCloudkill : ThingWithComps
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<Vector2>(ref this.realPosition, "realPosition", default(Vector2), false);
            Scribe_Values.Look<float>(ref this.direction, "direction", 0f, false);
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                Vector3 vector = base.Position.ToVector3Shifted();
                this.realPosition = new Vector2(vector.x, vector.z);
                this.direction = Rand.Range(0f, 360f);
            }
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {

        }
        protected override void Tick()
        {
            if (!base.Spawned)
            {
                return;
            }
            if (LivingCloudkill.directionNoise == null)
            {
                LivingCloudkill.directionNoise = new Perlin(0.0020000000949949026, 2.0, 0.5, 4, 1948573612, QualityMode.Medium);
            }
            this.direction += (float)LivingCloudkill.directionNoise.GetValue((double)Find.TickManager.TicksAbs, (double)((float)(this.thingIDNumber % 500) * 1000f), 0.0) * 0.78f;
            this.realPosition = this.realPosition.Moved(this.direction, 0.028333334f);
            IntVec3 intVec = new Vector3(this.realPosition.x, 0f, this.realPosition.y).ToIntVec3();
            if (intVec.InBounds(base.Map))
            {
                base.Position = intVec;
                if (this.IsHashIntervalTick(15))
                {
                    this.DamageCloseThings();
                }
                if (Rand.MTBEventOccurs(15f, 1f, 1f))
                {
                    this.DamageFarThings();
                }
            } else {
                List<Building> obelisksE = base.Map.listerBuildings.AllBuildingsNonColonistOfDef(HVMPDefOf_A.HVMP_WarpedObelisk_Hypercube).ToList();
                if (!obelisksE.NullOrEmpty())
                {
                    base.Position = obelisksE.RandomElement().Position;
                    this.realPosition = base.Position.ToVector2();
                    SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
                } else {
                    this.Destroy();
                }
            }
        }
        private void DamageCloseThings()
        {
            if (ModsConfig.BiotechActive)
            {
                GasUtility.AddGas(base.Position, base.Map, GasType.ToxGas, 0.035f);
            } else {
                GasUtility.AddGas(base.Position, base.Map, GasType.RotStink, 0.035f);
            }
            int num = GenRadial.NumCellsInRadius(4.2f);
            for (int i = 0; i < num; i++)
            {
                IntVec3 intVec = base.Position + GenRadial.RadialPattern[i];
                if (intVec.InBounds(base.Map) && !this.CellImmuneToDamage(intVec))
                {
                    Pawn firstPawn = intVec.GetFirstPawn(base.Map);
                    if (firstPawn != null)
                    {
                        ToxicUtility.DoPawnToxicDamage(firstPawn, 1f);
                    }
                }
            }
        }
        private void DamageFarThings()
        {
            HautsDefOf.Hauts_ToxThornsMist.SpawnMaintained(base.Position, base.Map, 1f);
        }
        private bool CellImmuneToDamage(IntVec3 c)
        {
            if (c.Roofed(base.Map) && c.GetRoof(base.Map).isThickRoof)
            {
                return true;
            }
            Building edifice = c.GetEdifice(base.Map);
            return edifice != null && edifice.def.category == ThingCategory.Building && (edifice.def.building.isNaturalRock || (edifice.def == ThingDefOf.Wall && edifice.Faction == null));
        }
        private Vector2 realPosition;
        private float direction;
        private static ModuleBase directionNoise;
    }
}
