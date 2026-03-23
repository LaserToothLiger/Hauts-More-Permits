using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace HautsPermits
{
    /*derivative of CompTargetEffect, rather similar to UseEffectInstallImplant. freshFromVault is toggled on when this is created by a CompCreateRewardsForHack (i.e. drops from a hacked Enterprise Security Crate).
     * standingGainFactorIfSecondhand: if freshFromVault, injection grants the full standing gain the mod setting says Permit Authorizers should grant; otherwise, it grants that much times this field's value*/
    public class CompProperties_TargetEffectInstallPTargeter : CompProperties
    {
        public CompProperties_TargetEffectInstallPTargeter()
        {
            this.compClass = typeof(CompTargetEffect_InstallPTargeter);
        }
        public HediffDef hediffDef;
        public BodyPartDef bodyPart;
        public bool canUpgrade;
        public bool requiresExistingHediff;
        public SoundDef soundOnUsed;
        public float standingGainFactorIfSecondhand;
    }
    public class CompTargetEffect_InstallPTargeter : CompTargetEffect
    {
        public CompProperties_TargetEffectInstallPTargeter Props
        {
            get
            {
                return (CompProperties_TargetEffectInstallPTargeter)this.props;
            }
        }
        public override string CompInspectStringExtra()
        {
            if (this.faction != null)
            {
                return "HVMP_GiveStandingFromFaction".Translate(this.StandingToGive, this.faction.NameColored);
            }
            return base.CompInspectStringExtra();
        }
        public int StandingToGive
        {
            get
            {
                float result = Math.Max(1, (int)Math.Ceiling(HVMP_Mod.settings.authorizerStandingGain));
                if (!this.freshFromVault)
                {
                    result *= this.Props.standingGainFactorIfSecondhand;
                }
                return (int)result;
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            if (this.faction == null)
            {
                if (this.parent.Map != null && this.parent.Map.ParentFaction != null && this.parent.Map.ParentFaction.def.HasModExtension<EBranchQuests>())
                {
                    this.faction = this.parent.Map.ParentFaction;
                    return;
                }
                this.faction = PermitAuthorizerUtility.AssignFallbackFactionToPermitTargeter();
            }
            this.freshFromVault = false;
        }
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (!user.IsColonistPlayerControlled)
            {
                return;
            }
            Job job = JobMaker.MakeJob(HVMPDefOf.HVMP_InstallPTargeter, target, this.parent);
            job.count = 1;
            job.playerForced = true;
            user.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look<Faction>(ref this.faction, "faction", false);
            Scribe_Values.Look<bool>(ref this.freshFromVault, "freshFromVault", false, false);
        }
        public Faction faction;
        public bool freshFromVault;
    }
    public class JobDriver_InstallPTargeter : JobDriver
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
            Toil toil = Toils_General.WaitWith(TargetIndex.A, 240, false, true, false, TargetIndex.A, PathEndMode.Touch);
            toil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return toil;
            yield return Toils_General.Do(new Action(this.Install));
            yield break;
        }
        private void Install()
        {
            CompTargetEffect_InstallPTargeter cteipt = this.Item.TryGetComp<CompTargetEffect_InstallPTargeter>();
            if (cteipt == null || cteipt.faction == null || this.TargetPawn.royalty == null)
            {
                return;
            }
            BodyPartRecord bodyPartRecord = this.TargetPawn.RaceProps.body.GetPartsWithDef(cteipt.Props.bodyPart).FirstOrFallback(null);
            if (bodyPartRecord == null)
            {
                return;
            }
            Faction f = cteipt.faction;
            if (f == null)
            {
                f = PermitAuthorizerUtility.AssignFallbackFactionToPermitTargeter();
            }
            Hediff firstHediffOfDef = this.TargetPawn.health.hediffSet.GetFirstHediffOfDef(cteipt.Props.hediffDef, false);
            if (firstHediffOfDef == null && !cteipt.Props.requiresExistingHediff)
            {
                Hediff newHediff = HediffMaker.MakeHediff(cteipt.Props.hediffDef, this.TargetPawn, bodyPartRecord);
                this.TargetPawn.health.AddHediff(newHediff);
                if (newHediff is Hediff_PTargeter ptarg)
                {
                    ptarg.faction = f;
                }
                this.TargetPawn.royalty.GainFavor(cteipt.faction, cteipt.StandingToGive);
            }
            else if (cteipt.Props.canUpgrade && firstHediffOfDef is Hediff_PTargeter ptargf && f == ptargf.faction)
            {
                this.TargetPawn.royalty.GainFavor(f, cteipt.StandingToGive);
            }
            else
            {
                return;
            }
            if (this.TargetPawn.Map == Find.CurrentMap && cteipt.Props.soundOnUsed != null)
            {
                cteipt.Props.soundOnUsed.PlayOneShot(SoundInfo.InMap(this.TargetPawn, MaintenanceType.None));
            }
            this.Item.SplitOff(1).Destroy(DestroyMode.Vanish);
        }
    }
    //Permit Authorizers go on cooldown when used to use a permit. They can only allow the use of permits when severity is high, but it's low when on cooldown. Also, the tooltip tells you its cooldown and what faction it's keyed to.
    public class Hediff_PTargeter : Hediff
    {
        public override string Label
        {
            get
            {
                string label = this.faction != null ? this.faction.Name + " " : "";
                label += base.Label;
                if (this.Severity < 1f)
                {
                    if (this.cooldownTicks < 2500)
                    {
                        label += "\n(" + this.cooldownTicks.ToStringSecondsFromTicks("F0") + ")";
                    } else {
                        label += "\n(" + this.cooldownTicks.ToStringTicksToPeriod(true, true, true, true, false) + ")";
                    }
                }
                return label;
            }
        }
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.cooldownTicks > 0)
            {
                this.Severity = 0.001f;
                this.cooldownTicks -= delta;
            } else {
                this.Severity = 1.001f;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Faction>(ref this.faction, "faction", false);
            Scribe_Values.Look<int>(ref this.cooldownTicks, "cooldownTicks", 0, false);
        }
        public Faction faction;
        public int cooldownTicks;
    }
}
