using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HautsPermits_Biotech
{
    //animals usually can't have abilities. Acid Breath grants an ability. Therefore, it must initialize the pawn's ability tracker
    public class Hediff_AnimalAbility : Hediff
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (this.pawn.abilities == null)
            {
                this.pawn.abilities = new Pawn_AbilityTracker(this.pawn);
            }
        }
    }
    /*On death, Brightdeath mutation inflicts hediff on all pawns that do not also have Brightdeath themselves within flashRadius cells
     * it also plays the specified mote and sound, and there is a flameChance for a fire to start on the corpse's position*/
    public class HediffCompProperties_Brightdeath : HediffCompProperties
    {
        public HediffCompProperties_Brightdeath()
        {
            this.compClass = typeof(HediffComp_Brightdeath);
        }
        public float flashRadius;
        public HediffDef hediff;
        public ThingDef mote;
        public SoundDef sound;
        public float flameChance = 0.1f;
    }
    public class HediffComp_Brightdeath : HediffComp
    {
        public HediffCompProperties_Brightdeath Props
        {
            get
            {
                return (HediffCompProperties_Brightdeath)this.props;
            }
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (this.Pawn.SpawnedOrAnyParentSpawned)
            {
                Map m = this.Pawn.MapHeld;
                if (this.Props.sound != null)
                {
                    this.Props.sound.PlayOneShot(new TargetInfo(this.Pawn.PositionHeld, m, false));
                }
                Vector3 v3 = this.Pawn.PositionHeld.ToVector3();
                FleckMaker.ThrowMicroSparks(v3, m);
                FleckMaker.Static(v3 + new Vector3(0.5f, 0f, 0.5f), m, FleckDefOf.ExplosionFlash, this.Props.flashRadius);
                if (this.Pawn.Spawned)
                {
                    MoteMaker.MakeAttachedOverlay(this.Pawn, this.Props.mote, Vector3.zero, 1f, -1f);
                    this.StartFire(this.Pawn);
                } else if (this.Pawn.Corpse != null && this.Pawn.Corpse.Spawned) {
                    MoteMaker.MakeAttachedOverlay(this.Pawn.Corpse, this.Props.mote, Vector3.zero, 1f, -1f);
                    this.StartFire(this.Pawn.Corpse);
                }
                foreach (Pawn p in GenRadial.RadialDistinctThingsAround(this.Pawn.PositionHeld, this.Pawn.MapHeld, this.Props.flashRadius, true).OfType<Pawn>().Distinct<Pawn>())
                {
                    if (p != this.Pawn && !p.health.hediffSet.HasHediff(this.parent.def))
                    {
                        Hediff toGive = HediffMaker.MakeHediff(this.Props.hediff, p);
                        p.health.AddHediff(toGive);
                    }
                }
            }
        }
        public void StartFire(Thing thing)
        {
            if (thing.Spawned && Rand.Chance(this.Props.flameChance))
            {
                if (thing.CanEverAttachFire())
                {
                    thing.TryAttachFire(1.2f, null);
                } else {
                    FireUtility.TryStartFireIn(thing.Position, thing.Map, 1.75f, null, null);
                }
            }
        }
    }
    /*Frenzy mutation oscillates between two severity states, 1 and min. There is a minimum time until severity can go up to 1 (random ticks in frenzyCooldown) and down to min (frenzyDuration)
     * While in min, every 500 ticks has frenzyChancePerHourFifth chance to oscillate up. Doing so plays the specified moteToPlayOnFrenzy*/
    public class HediffCompProperties_FrenzyTimer : HediffCompProperties
    {
        public HediffCompProperties_FrenzyTimer()
        {
            this.compClass = typeof(HediffComp_FrenzyTimer);
        }
        public IntRange frenzyCooldown;
        public float frenzyChancePerHourFifth;
        public IntRange frenzyDuration;
        public ThingDef moteToPlayOnFrenzy;
    }
    public class HediffComp_FrenzyTimer : HediffComp
    {
        public HediffCompProperties_FrenzyTimer Props
        {
            get
            {
                return (HediffCompProperties_FrenzyTimer)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            this.curPhaseDuration = this.Props.frenzyCooldown.RandomInRange;
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            this.curPhaseDuration -= delta;
            if (this.curPhaseDuration <= 0)
            {
                if (this.parent.CurStageIndex == 0)
                {
                    if (this.Pawn.IsHashIntervalTick(500, delta) && Rand.Chance(this.Props.frenzyChancePerHourFifth))
                    {
                        this.parent.Severity = 1f;
                        this.curPhaseDuration = this.Props.frenzyDuration.min;
                        MoteMaker.MakeAttachedOverlay(this.Pawn, this.Props.moteToPlayOnFrenzy, Vector3.zero, 1f, -1f);
                    }
                } else {
                    this.parent.Severity = 0f;
                    this.curPhaseDuration = this.Props.frenzyCooldown.RandomInRange;
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.curPhaseDuration, "curPhaseDuration", 0, false);
        }
        public int curPhaseDuration;
    }
    //On death, Vengeant mutation inflicts the specified hediff on all pawns in vengeanceRadius who have the Vengeant mutation as well
    public class HediffCompProperties_Vengeant : HediffCompProperties
    {
        public HediffCompProperties_Vengeant()
        {
            this.compClass = typeof(HediffComp_Vengeant);
        }
        public float vengeanceRadius;
        public HediffDef hediff;
    }
    public class HediffComp_Vengeant : HediffComp
    {
        public HediffCompProperties_Vengeant Props
        {
            get
            {
                return (HediffCompProperties_Vengeant)this.props;
            }
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (this.Pawn.SpawnedOrAnyParentSpawned)
            {
                foreach (Pawn p in GenRadial.RadialDistinctThingsAround(this.Pawn.PositionHeld, this.Pawn.MapHeld, this.Props.vengeanceRadius, true).OfType<Pawn>().Distinct<Pawn>())
                {
                    if (p != this.Pawn && p.health.hediffSet.HasHediff(this.parent.def))
                    {
                        Hediff toGive = HediffMaker.MakeHediff(this.Props.hediff, p);
                        p.health.AddHediff(toGive);
                    }
                }
            }
        }
    }
    //On death, Hatchery mutation has a chance (50%, proportionally scaled by body size) to pop out a megascarab that ALSO has scaria
    public class Hediff_Broodlings : Hediff
    {
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (this.pawn.SpawnedOrAnyParentSpawned && Rand.Chance(this.pawn.BodySize / 2f))
            {
                Pawn prawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Megascarab, this.pawn.Faction, PawnGenerationContext.NonPlayer, -1, true, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, new float?(0f), null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
                Hediff scaria = HediffMaker.MakeHediff(HediffDefOf.Scaria, prawn);
                prawn.health.AddHediff(scaria);
                GenSpawn.Spawn(prawn, this.pawn.PositionHeld, this.pawn.MapHeld, WipeMode.VanishOrMoveAside);
                if (prawn.mindState != null)
                {
                    prawn.ClearMind_NewTemp();
                    prawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent, HediffDefOf.Scaria.LabelCap, false, false, false, null, true, false, false);
                }
                FilthMaker.TryMakeFilth(this.pawn.PositionHeld, this.pawn.MapHeld, ThingDefOf.Filth_AmnioticFluid, 1, FilthSourceFlags.None, true);
            }
        }
    }
}
