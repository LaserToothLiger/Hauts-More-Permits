using RimWorld;
using Verse;

namespace HautsPermits_Occult
{
    /*for use with buildings that, when triggered in some way, begin 'detonating'. While detonating, it 1) periodically unleashes explosions, and 2) on death, it leaves a Pit Burrow behind...
     * detonationPeriodicity: number of ticks between each explosion.
     * explosiveRadius, explosiveDamageType, damageAmountBase, armorPenetrationBase, chanceToStartFire, damageFalloff: ewisott parameters input into GenExplosion.DoExplosion
     * fleshbeastThreatPointFactor: flesh beasts spawned by the post-detonation pit burrow = storyteller raid points * this value*/
    public class CompProperties_Helldrill : CompProperties
    {
        public CompProperties_Helldrill()
        {
            this.compClass = typeof(CompHelldrill);
        }
        public int detonationPeriodicity;
        public float explosiveRadius = 1.9f;
        public DamageDef explosiveDamageType;
        public int damageAmountBase = -1;
        public float armorPenetrationBase = -1f;
        public float chanceToStartFire;
        public bool damageFalloff;
        public float fleshbeastThreatPointFactor = 0.25f;
    }
    public class CompHelldrill : ThingComp
    {
        public CompProperties_Helldrill Props
        {
            get
            {
                return (CompProperties_Helldrill)this.props;
            }
        }
        public void Detonate(Map map, bool ignoreUnspawned = false)
        {
            if (!ignoreUnspawned && !this.parent.SpawnedOrAnyParentSpawned)
            {
                return;
            }
            IntVec3 positionHeld = this.parent.PositionHeld;
            if (map == null)
            {
                Log.Warning("Tried to detonate CompVoidCharge in a null map.");
                return;
            }
            GenExplosion.DoExplosion(positionHeld, map, this.Props.explosiveRadius, this.Props.explosiveDamageType, this.parent, this.Props.damageAmountBase, this.Props.armorPenetrationBase, chanceToStartFire: this.Props.chanceToStartFire, damageFalloff: this.Props.damageFalloff);
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.detonating && this.parent.IsHashIntervalTick(this.Props.detonationPeriodicity, delta))
            {
                this.Detonate(this.parent.MapHeld, false);
            }
        }
        public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
        {
            if (this.detonating)
            {
                FleshbeastUtility.SpawnFleshbeastsFromPitBurrowEmergence(this.parent.PositionHeld, prevMap, this.Props.fleshbeastThreatPointFactor * StorytellerUtility.DefaultThreatPointsNow(prevMap), new IntRange(300, 1200), new IntRange(180), true);
            }
            base.Notify_Killed(prevMap, dinfo);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.detonating, "detonating", false, false);
        }
        public bool detonating;
    }
    //a trap that starts Helldrill detonating when tripped
    public class Building_TrapHelldrill : Building_Trap
    {
        protected override void SpringSub(Pawn p)
        {
            base.GetComp<CompHelldrill>().detonating = true;
            if (p.stances != null && p.stances.stunner != null)
            {
                p.stances.stunner.StunFor(90,this,false,false);
            }
        }
    }
    //derivative of Helldrill that triggers on reaching max activity (obviously such a Thing requires a CompActivity too)
    public class CompProperties_HelldrillCharge : CompProperties_Helldrill
    {
        public CompProperties_HelldrillCharge()
        {
            this.compClass = typeof(CompHelldrillCharge);
        }
        public string extraInspectStringKey;
    }
    public class CompHelldrillCharge : CompHelldrill, IActivity
    {
        public new CompProperties_HelldrillCharge Props
        {
            get
            {
                return (CompProperties_HelldrillCharge)this.props;
            }
        }
        public override string CompInspectStringExtra()
        {
            string text = "";
            if (this.Props.extraInspectStringKey != null)
            {
                text = this.Props.extraInspectStringKey.Translate();
            }
            return text;
        }
        public void OnActivityActivated()
        {
            this.detonating = true;
        }
        public void OnPassive()
        {
            return;
        }
        public bool ShouldGoPassive()
        {
            return false;
        }
        public bool CanBeSuppressed()
        {
            CompActivity ca = this.parent.GetComp<CompActivity>();
            if (ca != null)
            {
                return !ca.IsActive;
            }
            return false;
        }
        public bool CanActivate()
        {
            return true;
        }
        public string ActivityTooltipExtra()
        {
            return this.Props.extraInspectStringKey;
        }
    }
}
