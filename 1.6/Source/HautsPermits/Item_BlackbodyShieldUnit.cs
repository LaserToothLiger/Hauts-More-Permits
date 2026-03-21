using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace HautsPermits
{
    /*as InstallImplantInOtherPawn, except...
     * -without canUpgrade or requiresExistingHediff since BSUs don't stack like that
     * -it instigates a JobDef that utilizes JobDriver_InstallBSU, which takes only 60 ticks instead of 600 so that it's easy to use in-combat*/
    public class CompProperties_TargetEffectInstallBSU : CompProperties
    {
        public CompProperties_TargetEffectInstallBSU()
        {
            this.compClass = typeof(CompTargetEffect_InstallBSU);
        }
        public HediffDef hediffDef;
        public BodyPartDef bodyPart;
        public SoundDef soundOnUsed;
    }
    public class CompTargetEffect_InstallBSU : CompTargetEffect
    {
        public CompProperties_TargetEffectInstallBSU Props
        {
            get
            {
                return (CompProperties_TargetEffectInstallBSU)this.props;
            }
        }
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (!user.IsColonistPlayerControlled)
            {
                return;
            }
            Job job = JobMaker.MakeJob(HVMPDefOf.HVMP_InstallBSU, target, this.parent);
            job.count = 1;
            job.playerForced = true;
            user.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
        }
    }
    public class JobDriver_InstallBSU : JobDriver
    {
        private Pawn TargetPawn
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
            return this.pawn.Reserve(this.TargetPawn, this.job, 1, -1, null, errorOnFailed, false) && this.pawn.Reserve(this.Item, this.job, 1, -1, null, errorOnFailed, false);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false, true, false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.A);
            Toil toil = Toils_General.WaitWith(TargetIndex.A, 60, false, true, false, TargetIndex.A, PathEndMode.Touch);
            toil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return toil;
            yield return Toils_General.Do(new Action(this.Install));
            yield break;
        }
        private void Install()
        {
            CompTargetEffect_InstallBSU ibsu = this.Item.TryGetComp<CompTargetEffect_InstallBSU>();
            if (ibsu == null)
            {
                return;
            }
            BodyPartRecord bodyPartRecord = this.TargetPawn.RaceProps.body.GetPartsWithDef(ibsu.Props.bodyPart).FirstOrFallback(null);
            if (bodyPartRecord == null)
            {
                return;
            }
            Hediff firstHediffOfDef = this.TargetPawn.health.hediffSet.GetFirstHediffOfDef(ibsu.Props.hediffDef, false);
            if (firstHediffOfDef == null)
            {
                this.TargetPawn.health.AddHediff(ibsu.Props.hediffDef, bodyPartRecord, null, null);
                if (this.TargetPawn.Map == Find.CurrentMap && ibsu.Props.soundOnUsed != null)
                {
                    ibsu.Props.soundOnUsed.PlayOneShot(SoundInfo.InMap(this.TargetPawn, MaintenanceType.None));
                }
                this.Item.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
        }
    }
}
