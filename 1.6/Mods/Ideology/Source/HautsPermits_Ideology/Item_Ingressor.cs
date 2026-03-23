using HautsPermits;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace HautsPermits_Ideology
{
    //ewisott
    public class CompTargetable_DamageableBuildingOrHackableItem : CompTargetable
    {
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
                canTargetPawns = false,
                canTargetBuildings = true,
                canTargetItems = true,
                mapObjectTargetsMustBeAutoAttackable = false
            };
        }
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (target.Thing != null)
            {
                bool goAhead = false;
                CompHackable ch = target.Thing.TryGetComp<CompHackable>();
                if (ch != null && !ch.IsHacked)
                {
                    goAhead = true;
                }
                if (!goAhead)
                {
                    if (target.Thing is Building && target.Thing.def.useHitPoints)
                    {
                        goAhead = true;
                    }
                }
                if (!goAhead)
                {
                    return false;
                }
            }
            return base.ValidateTarget(target, showMessages);
        }
        public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
        {
            if (this.ValidateTarget(targetChosenByPlayer))
            {
                yield return targetChosenByPlayer;
            }
            yield break;
        }
    }
    /*if targeting a hackable item, the JobDriver from this CompTargetEffect hacks the target for hackingPower
     * Otherwise, if it's a building that can take damage, it takes damageAmount of damageType*/
    public class CompProperties_TargetEffectIngression : CompProperties
    {
        public CompProperties_TargetEffectIngression()
        {
            this.compClass = typeof(CompTargetEffect_Ingression);
        }
        public DamageDef damageType;
        public int damageAmount;
        public float hackingPower;
    }
    public class CompTargetEffect_Ingression : CompTargetEffect
    {
        public CompProperties_TargetEffectIngression Props
        {
            get
            {
                return (CompProperties_TargetEffectIngression)this.props;
            }
        }
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (!user.IsColonistPlayerControlled)
            {
                return;
            }
            Job job = JobMaker.MakeJob(HVMPDefOf.HVMP_AttachIngressor, target, this.parent);
            job.count = 1;
            job.playerForced = true;
            user.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
        }
    }
    public class JobDriver_ApplyIngressor : JobDriver
    {
        private Thing TargetToIngress
        {
            get
            {
                return this.job.GetTarget(TargetIndex.A).Thing;
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
            return this.pawn.Reserve(this.TargetToIngress, this.job, 1, -1, null, errorOnFailed, false) && this.pawn.Reserve(this.Item, this.job, 1, -1, null, errorOnFailed, false);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false, true, false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.A);
            Toil toil = Toils_General.WaitWith(TargetIndex.A, 300, false, true, false, TargetIndex.A, PathEndMode.Touch);
            toil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return toil;
            yield return Toils_General.Do(new Action(this.Ingress));
            yield break;
        }
        private void Ingress()
        {
            CompTargetEffect_Ingression ctei = this.Item.TryGetComp<CompTargetEffect_Ingression>();
            if (ctei == null)
            {
                return;
            }
            CompHackable ch = this.TargetToIngress.TryGetComp<CompHackable>();
            if (ch != null && !ch.IsHacked)
            {
                ch.Hack(ctei.Props.hackingPower, null);
            } else if (this.TargetToIngress is Building && this.TargetToIngress.def.useHitPoints) {
                this.TargetToIngress.TakeDamage(new DamageInfo(ctei.Props.damageType, ctei.Props.damageAmount));
            }
            this.Item.SplitOff(1).Destroy(DestroyMode.Vanish);
        }
    }
}
