using HautsPermits;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace HautsPermits_Biotech
{
    //CompTargetable that can work on pawns with an energy need or buildings with CompPowerBattery
    public class CompProperties_Chargecell : CompProperties_Targetable
    {
        public CompProperties_Chargecell()
        {
            this.compClass = typeof(CompTargetable_Chargecell);
        }
    }
    public class CompTargetable_Chargecell : CompTargetable
    {
        public new CompProperties_Chargecell Props
        {
            get
            {
                return (CompProperties_Chargecell)this.props;
            }
        }
        protected override bool PlayerChoosesTarget
        {
            get
            {
                return true;
            }
        }
        protected override TargetingParameters GetTargetingParameters()
        {
            return new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = true,
                canTargetItems = false,
                canTargetCorpses = false,
                mapObjectTargetsMustBeAutoAttackable = false
            };
        }
        public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
        {
            yield return targetChosenByPlayer;
            yield break;
        }
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            return ((target.Thing is Building b && b.HasComp<CompPowerBattery>()) || (target.Thing is Pawn p && p.needs.energy != null)) && base.ValidateTarget(target.Thing, showMessages);
        }
    }
    //adds batteryWatts to the stored power of the target battery, or [energyForOneBodySizeMech/target's body size] to the energy need of the target pawn
    public class CompProperties_TargetEffectChargecell : CompProperties
    {
        public CompProperties_TargetEffectChargecell()
        {
            this.compClass = typeof(CompTargetEffect_Chargecell);
        }
        public float batteryWatts;
        public float energyForOneBodySizeMech;
        public SoundDef sound;
    }
    public class CompTargetEffect_Chargecell : CompTargetEffect
    {
        public CompProperties_TargetEffectChargecell Props
        {
            get
            {
                return (CompProperties_TargetEffectChargecell)this.props;
            }
        }
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (!user.IsColonistPlayerControlled)
            {
                return;
            }
            if (target is Building)
            {
                Job job = JobMaker.MakeJob(HVMPDefOf.HVMP_InjectChargecellBattery, target, this.parent);
                job.count = 1;
                job.playerForced = true;
                user.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
            }
            if (target is Pawn)
            {
                Job job = JobMaker.MakeJob(HVMPDefOf.HVMP_InjectChargecellMech, target, this.parent);
                job.count = 1;
                job.playerForced = true;
                user.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
            }
        }
    }
    public class JobDriver_ChargecellBattery : JobDriver
    {
        private Building Battery
        {
            get
            {
                return (Building)this.job.GetTarget(TargetIndex.A).Thing;
            }
        }
        private Thing Item
        {
            get
            {
                return this.job.GetTarget(TargetIndex.B).Thing;
            }
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.Battery, this.job, 1, -1, null, errorOnFailed, false) && this.pawn.Reserve(this.Item, this.job, 1, -1, null, errorOnFailed, false);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false, true, false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.A);
            Toil toil = Toils_General.Wait(10, TargetIndex.None);
            toil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            toil.tickIntervalAction = delegate (int delta)
            {
                Pawn actor = toil.actor;
                toil.handlingFacing = true;
                toil.tickAction = delegate
                {
                    actor.rotationTracker.FaceTarget(this.job.GetTarget(TargetIndex.A));
                };
                CompUsable compUsable = this.Item.TryGetComp<CompUsable>();
                if (compUsable != null && this.warmupMote == null && compUsable.Props.warmupMote != null)
                {
                    this.warmupMote = MoteMaker.MakeAttachedOverlay(this.Battery, compUsable.Props.warmupMote, Vector3.zero, 1f, -1f);
                }
                Mote mote = this.warmupMote;
                if (mote == null)
                {
                    return;
                }
                mote.Maintain();
            };
            yield return toil;
            yield return Toils_General.Do(new Action(this.ChargeBattery));
            yield break;
        }
        private void ChargeBattery()
        {
            CompPowerBattery cpb = this.Battery.GetComp<CompPowerBattery>();
            CompTargetEffect_Chargecell ctecc = this.Item.TryGetComp<CompTargetEffect_Chargecell>();
            if (cpb != null && ctecc != null)
            {
                cpb.AddEnergy(ctecc.Props.batteryWatts);
                if (this.Battery.Spawned)
                {
                    ctecc.Props.sound.PlayOneShot(new TargetInfo(this.Battery.Position, this.Battery.Map, false));
                }
            }
            this.Item.SplitOff(1).Destroy(DestroyMode.Vanish);
        }
        private Mote warmupMote;
    }
    public class JobDriver_ChargecellMech : JobDriver
    {
        protected Pawn Mech
        {
            get
            {
                return (Pawn)this.job.GetTarget(TargetIndex.A).Thing;
            }
        }
        private Thing Item
        {
            get
            {
                return this.job.GetTarget(TargetIndex.B).Thing;
            }
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            this.Mech.ClearAllReservations(true);
            return this.pawn.Reserve(this.Mech, this.job, 1, -1, null, errorOnFailed, false) && this.pawn.Reserve(this.Item, this.job, 1, -1, null, errorOnFailed, false);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnAggroMentalStateAndHostile(TargetIndex.A);
            this.FailOn(delegate
            {
                if (this.Mech.needs.energy == null)
                {
                    return true;
                }
                return false;
            });
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false, true, false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_General.Do(new Action(this.ChargeMech));
            yield break;
        }
        private void ChargeMech()
        {
            CompTargetEffect_Chargecell ctecc = this.Item.TryGetComp<CompTargetEffect_Chargecell>();
            if (this.Mech.needs.energy != null && ctecc != null)
            {
                this.Mech.needs.energy.CurLevel += ctecc.Props.energyForOneBodySizeMech / this.Mech.BodySize;
                if (this.Mech.Spawned)
                {
                    ctecc.Props.sound.PlayOneShot(new TargetInfo(this.Mech.Position, this.Mech.Map, false));
                }
            }
            this.Item.SplitOff(1).Destroy(DestroyMode.Vanish);
        }
        private const TargetIndex TakeeIndex = TargetIndex.A;
        private const TargetIndex BedIndex = TargetIndex.B;
    }
}
