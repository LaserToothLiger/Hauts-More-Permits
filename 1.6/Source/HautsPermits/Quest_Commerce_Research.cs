using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace HautsPermits
{
    /*i give you a building and you babysit it. Basically unfailable, but takes a long while to finish up. You can speed it up if you have spare electrical power, but this could cause raids.
     * Sets the various parameters and properties of CompChainComputer, including how much time it needs to finish, and what mutators are active. Speaking of:
     * research1: Brain Drain flips on needsPsyConnection (see CompChainComputer)
     * research2: Optimization Issues enables the CompHeatPusher_OI to work
     * research3: Please Verify You Are Humanlike specifies a captchaThreshold for CompChainComputer, the exact possible value is affected by its PVYAH_captchaAtPctProgress field*/
    public class QuestNode_GenerateChainComputer : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Thing thing = ThingMaker.MakeThing(this.thingDef, null);
            int challengeRating = QuestGen.quest.challengeRating;
            CompChainComputer ccc = thing.TryGetComp<CompChainComputer>();
            if (ccc != null)
            {
                ccc.challengeRating = challengeRating;
                bool mayhemMode = HVMP_Mod.settings.researchX;
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.research1, mayhemMode))
                {
                    ccc.needsPsyConnection = true;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_BD_info", this.BD_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_BD_info", " ") });
                }
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.research2, mayhemMode))
                {
                    CompHeatPusher_OI chpoi = thing.TryGetComp<CompHeatPusher_OI>();
                    if (chpoi != null)
                    {
                        chpoi.enabled = true;
                    }
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_OI_info", this.OI_description.Formatted())
                    });
                } else {
                    CompHeatPusher_OI chpoi = thing.TryGetComp<CompHeatPusher_OI>();
                    if (chpoi != null)
                    {
                        chpoi.enabled = false;
                    }
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_OI_info", " ") });
                }
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.research3, mayhemMode))
                {
                    ccc.captchaThreshold = ccc.Props.PVYAH_captchaAtPctProgress.RandomInRange * ccc.RequiredProgress;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_PVYAH_info", this.PVYAH_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_PVYAH_info", " ") });
                }
            }
            string srpa;
            switch (challengeRating)
            {
                case 1:
                    srpa = "HVMP_InterventionCR1";
                    break;
                case 2:
                    srpa = "HVMP_InterventionCR2";
                    break;
                case 3:
                    srpa = "HVMP_InterventionCR3";
                    break;
                default:
                    srpa = "HVMP_InterventionCR1";
                    break;
            }
            slate.Set<string>(this.storeReqProgressAs.GetValue(slate), srpa.Translate(), false);
            slate.Set<Thing>(this.storeAs.GetValue(slate), thing, false);
            QuestGen.quest.AddPart(new QuestPart_LookAtThis(thing));
        }
        [NoTranslate]
        public SlateRef<string> storeAs;
        public ThingDef thingDef;
        [NoTranslate]
        public SlateRef<string> storeReqProgressAs;
        [MustTranslate]
        public string BD_description;
        [MustTranslate]
        public string OI_description;
        [MustTranslate]
        public string PVYAH_description;
    }
    /*accrues baseProgressPerTick + [bonusProgressPerInterval*however much power is being funneled into the chaincomp] progress. When its progress reaches maxProgress, chaincomp explodes (harmlessly, into smoke) and you win.
     * has two buttons you can use to up or lower the amount of power the chaincomp is using. It uses 0 by default, and can go up to maxExternalPower. Each press of such a button alters power draw by powerInterval watts.
     * if it's been at least minTimeBetweenRaids ticks since the last time this chaincomp caused a raid, and a [current power draw / max possible power draw] chance check is passed, then it's MTB raidMTBdays days to a random raid.
     *   this raid's threat points are multiplied by raidPointFactor.
     * if needsPsyConnection was flipped on by the quest node that made this chaincomp, then it won't accrue progress unless a pawn is psychically connected. Use the gizmo to target a pawn on the same map
     *   (respecting BD_targetingParameters) to inflict BD_hediff on it. So long as the hediff persists, progress can be gained. Loss of the hediff negatively offsets progress.
     * if captchaThreshold was set to a positive value, the chain comp stops accruing progress and becomes Hackable upon reaching that much progress. Finishing the hack causes progress to accrue again.*/
    public class CompProperties_ChainComputer : CompProperties_Hackable
    {
        public CompProperties_ChainComputer()
        {
            this.compClass = typeof(CompChainComputer);
        }
        public float maxProgress = 100f;
        public float baseProgressPerTick;
        public float powerInterval = 1f;
        public float maxExternalPower = 1f;
        public float bonusProgressPerInterval;
        [NoTranslate]
        public string texUp = "UI/Commands/TempRaise";
        [NoTranslate]
        public string labelUp = "HVMP_CCompToggleUpLabel";
        [NoTranslate]
        public string tooltipUp = "HVMP_CCompToggleUpTooltip";
        [NoTranslate]
        public string texDown = "UI/Commands/TempLower";
        [NoTranslate]
        public string labelDown = "HVMP_CCompToggleDownLabel";
        [NoTranslate]
        public string tooltipDown = "HVMP_CCompToggleDownTooltip";
        public float raidMTBdays;
        public int minTimeBetweenRaids;
        public float raidPointFactor;
        public HediffDef BD_hediff;
        public TargetingParameters BD_targetingParameters;
        [NoTranslate]
        public string texBD = "UI/Abilities/Focus";
        [NoTranslate]
        public string labelBD = "HVMP_CComp_BDLabel";
        [NoTranslate]
        public string tooltipBD = "HVMP_CComp_BDTooltip";
        public FloatRange PVYAH_captchaAtPctProgress;
        public string PVYAH_messageLabel;
        public string PVYAH_messageText;
        public ColorInt PVYAH_mustHackColor;
    }
    public class CompChainComputer : CompHackable, ITargetingSource
    {
        public new CompProperties_ChainComputer Props
        {
            get
            {
                return (CompProperties_ChainComputer)this.props;
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (this.captchaThreshold > 0f)
            {
                foreach (Gizmo gizmo in base.CompGetGizmosExtra())
                {
                    yield return gizmo;
                }
            }
            yield return new Command_Action
            {
                icon = this.TexUp,
                defaultLabel = this.Props.labelUp.Translate(),
                defaultDesc = this.Props.tooltipUp.Translate(),
                action = delegate
                {
                    this.curPowerConsumption = Math.Min(this.curPowerConsumption + this.Props.powerInterval, this.Props.maxExternalPower);
                }
            };
            yield return new Command_Action
            {
                icon = this.TexDown,
                defaultLabel = this.Props.labelDown.Translate(),
                defaultDesc = this.Props.tooltipDown.Translate(),
                action = delegate
                {
                    this.curPowerConsumption = Math.Max(this.curPowerConsumption - this.Props.powerInterval, 0f);
                }
            };
            if (this.needsPsyConnection && this.linkedPawn == null)
            {
                yield return new Command_Action
                {
                    icon = this.UIIcon,
                    defaultLabel = this.Props.labelBD.Translate(),
                    defaultDesc = this.Props.tooltipBD.Translate(),
                    action = delegate
                    {
                        Find.Targeter.BeginTargeting(this, null, false, null, null, true);
                    }
                };
            }
            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Finish computation",
                    action = delegate
                    {
                        this.FinishComputation();
                    }
                };
            }
            foreach (Gizmo gizmo3 in QuestUtility.GetQuestRelatedGizmos(this.parent))
            {
                yield return gizmo3;
            }
            yield break;
        }
        public bool CanHitTarget(LocalTargetInfo target)
        {
            return this.ValidateTarget(target, false);
        }
        public bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!target.IsValid || target.Pawn == null)
            {
                return false;
            }
            Pawn pawn = target.Pawn;
            AcceptanceReport acceptanceReport = this.CanInteract(pawn, true);
            if (!acceptanceReport.Accepted)
            {
                if (showMessages && !acceptanceReport.Reason.NullOrEmpty())
                {
                    Messages.Message("HVMP_CannotBD".Translate() + ": " + acceptanceReport.Reason.CapitalizeFirst(), pawn, MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            return true;
        }
        public AcceptanceReport CanInteract(Pawn activateBy = null, bool checkOptionalItems = true)
        {
            if (!this.needsPsyConnection || this.linkedPawn != null)
            {
                return "AlreadyActive".Translate();
            }
            if (activateBy != null)
            {
                if (activateBy.Dead)
                {
                    return "PawnIsDead".Translate(activateBy);
                }
                if (activateBy.GetStatValue(StatDefOf.PsychicSensitivity) <= float.Epsilon || !activateBy.IsColonistPlayerControlled)
                {
                    return "Incapable".Translate().CapitalizeFirst();
                }
                if (activateBy.Downed)
                {
                    return "MessageRitualPawnDowned".Translate(activateBy);
                }
            }
            return true;
        }
        public void DrawHighlight(LocalTargetInfo target)
        {
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }
        public void OrderForceTarget(LocalTargetInfo target)
        {
            if (this.ValidateTarget(target, false) && target.Pawn != null)
            {
                this.PsychoConnectPawn(target.Pawn);
            }
        }
        public void PsychoConnectPawn(Pawn p)
        {
            Hediff h = HediffMaker.MakeHediff(this.Props.BD_hediff, p);
            p.health.AddHediff(h);
            HediffComp_BrainDrain hcbd = h.TryGetComp<HediffComp_BrainDrain>();
            if (hcbd != null)
            {
                hcbd.other = this.parent;
            }
            this.linkedPawn = p;
        }
        public void OnGUI(LocalTargetInfo target)
        {
            string text = "ChooseWhoShouldActivate".Translate();
            Widgets.MouseAttachedLabel(text, 0f, 0f, null);
            if (this.ValidateTarget(target, false) && this.Props.BD_targetingParameters.CanTarget(target.Pawn, this))
            {
                GenUI.DrawMouseAttachment(this.UIIcon);
                return;
            }
            GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
        }
        public bool CasterIsPawn
        {
            get
            {
                return true;
            }
        }
        public bool IsMeleeAttack
        {
            get
            {
                return false;
            }
        }
        public bool Targetable
        {
            get
            {
                return true;
            }
        }
        public bool MultiSelect
        {
            get
            {
                return false;
            }
        }
        public bool HidePawnTooltips
        {
            get
            {
                return false;
            }
        }
        public Thing Caster
        {
            get
            {
                return this.parent;
            }
        }
        public Pawn CasterPawn
        {
            get
            {
                return null;
            }
        }
        public Verb GetVerb
        {
            get
            {
                return null;
            }
        }
        public TargetingParameters targetParams
        {
            get
            {
                return this.Props.BD_targetingParameters;
            }
        }
        public ITargetingSource DestinationSelector
        {
            get
            {
                return null;
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            this.raidCD = 0;
        }
        public AcceptanceReport CanMakeProgress()
        {
            if (this.needsPsyConnection && this.linkedPawn == null)
            {
                return "HVMP_RequiresBD".Translate();
            }
            if (this.canBeHackedNow)
            {
                return "HVMP_RequiresPVYAH".Translate();
            }
            return true;
        }
        protected override void OnHacked(Pawn hacker = null, bool suppressMessages = false)
        {
            base.OnHacked(hacker, suppressMessages);
            CompGlower cg = this.parent.GetComp<CompGlower>();
            if (cg != null)
            {
                cg.GlowColor = cg.Props.glowColor;
            }
            this.canBeHackedNow = false;
        }
        protected override void OnLockedOut(Pawn hacker = null)
        {
            base.OnLockedOut(hacker);
            if (this.parent.Spawned)
            {
                if (Rand.Chance(0.5f))
                {
                    this.ResetStaticData();
                    CompChainComputer.Option option = CompChainComputer.options.RandomElement();
                    GameCondition gameCondition = GameConditionMaker.MakeCondition(option.gameCondition, (int)(option.durationDaysRange.RandomInRange * 60000f));
                    gameCondition.forceDisplayAsDuration = true;
                    gameCondition.Permanent = false;
                    List<Rule> listRule = new List<Rule>();
                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                    gameCondition.RandomizeSettings(1000f, this.parent.MapHeld, listRule, dictionary);
                    this.parent.MapHeld.gameConditionManager.RegisterCondition(gameCondition);
                    Find.LetterStack.ReceiveLetter(gameCondition.LabelCap, gameCondition.LetterText, gameCondition.def.letterDef, LookTargets.Invalid, null, gameCondition.quest, null, null, 0, true);
                }
                else
                {
                    MechClusterSketch mechClusterSketch = MechClusterGenerator.GenerateClusterSketch(StorytellerUtility.DefaultThreatPointsNow(this.parent.Map), this.parent.MapHeld, true, false);
                    IntVec3 intVec = MechClusterUtility.FindClusterPosition(this.parent.MapHeld, mechClusterSketch, 100, 0.5f);
                    if (!intVec.IsValid)
                    {
                        return;
                    }
                    IEnumerable<Thing> enumerable = from t in MechClusterUtility.SpawnCluster(intVec, this.parent.MapHeld, mechClusterSketch, true, true, null)
                                                    where t.def != ThingDefOf.Wall && t.def != ThingDefOf.Barricade
                                                    select t;
                    Find.LetterStack.ReceiveLetter("LetterLabelMechClusterArrived".Translate(), "LetterMechClusterArrived".Translate(), LetterDefOf.ThreatBig, new TargetInfo(intVec, this.parent.MapHeld, false), null, null, null, null, 0, true);
                }
            }
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.PowerTrader != null)
            {
                this.PowerTrader.PowerOutput = -this.curPowerConsumption;
            }
            if (!this.parent.questTags.NullOrEmpty())
            {
                AcceptanceReport acceptanceReport = this.CanMakeProgress();
                if (acceptanceReport)
                {
                    this.curProgress += this.Props.baseProgressPerTick * (float)delta;
                    if (!this.IsHacked && this.captchaThreshold > 0f && this.curProgress >= this.captchaThreshold)
                    {
                        Find.LetterStack.ReceiveLetter(this.Props.PVYAH_messageLabel, this.Props.PVYAH_messageText, LetterDefOf.NeutralEvent, this.parent);
                        CompGlower cg = this.parent.GetComp<CompGlower>();
                        if (cg != null)
                        {
                            cg.GlowColor = this.Props.PVYAH_mustHackColor;
                        }
                        this.canBeHackedNow = true;
                        return;
                    }
                    if (this.PowerTrader.PowerOn)
                    {
                        float chanceMod = this.curPowerConsumption / this.Props.maxExternalPower;
                        this.curProgress += this.Props.bonusProgressPerInterval * (float)delta * (this.curPowerConsumption / this.Props.powerInterval);
                        if (this.raidCD <= 0 && this.parent.IsHashIntervalTick(2500, delta) && this.parent.Spawned && Rand.Chance(chanceMod) && Rand.MTBEventOccurs(this.Props.raidMTBdays, 60000f, 2500f))
                        {
                            this.raidCD = this.Props.minTimeBetweenRaids;
                            IncidentParms incidentParms = new IncidentParms
                            {
                                target = this.parent.Map,
                                points = StorytellerUtility.DefaultThreatPointsNow(this.parent.Map) * this.Props.raidPointFactor,
                                raidStrategy = RaidStrategyDefOf.ImmediateAttack
                            };
                            IncidentDefOf.RaidEnemy.Worker.TryExecute(incidentParms);
                        }
                    }
                    if (this.raidCD > 0)
                    {
                        this.raidCD -= delta;
                    }
                    if (this.curProgress >= this.RequiredProgress)
                    {
                        this.FinishComputation();
                    }
                }
                if (this.parent.Spawned && !this.IsHacked)
                {
                    if (this.captchaThreshold <= 0f)
                    {
                        this.Hack(this.defence, null, true);
                    }
                    else if (!this.canBeHackedNow)
                    {
                        this.Hack(-this.defence, null, true);
                    }
                }
            }
        }
        public void FinishComputation()
        {
            this.curProgress = this.RequiredProgress;
            if (this.parent.SpawnedOrAnyParentSpawned)
            {
                GenExplosion.DoExplosion(this.parent.PositionHeld, this.parent.MapHeld, 2.4f, DamageDefOf.Smoke, this.parent, -1, -1f, this.parent.def.building.destroySound ?? null);
            }
            QuestUtility.SendQuestTargetSignals(this.parent.questTags, "FinishedChainComp", this.Named("SUBJECT"));
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (!this.parent.questTags.NullOrEmpty())
            {
                QuestUtility.SendQuestTargetSignals(this.parent.questTags, "DestroyedChainComp", this.Named("SUBJECT"), previousMap.Named("MAP"));
            }
        }
        public override string CompInspectStringExtra()
        {
            string desc = "HVMP_CCProgress".Translate(this.curProgress.ToStringByStyle(ToStringStyle.FloatOne), this.RequiredProgress.ToStringByStyle(ToStringStyle.FloatOne));
            AcceptanceReport acceptanceReport = this.CanMakeProgress();
            if (!acceptanceReport.Reason.NullOrEmpty())
            {
                desc += "\n" + "HVMP_CCpaused".Translate() + ": " + acceptanceReport.Reason.CapitalizeFirst();
            }
            if (this.canBeHackedNow)
            {
                desc += "\n" + base.CompInspectStringExtra();
            }
            return desc;
        }
        private CompPowerTrader PowerTrader
        {
            get
            {
                CompPowerTrader cpt = this.parent.TryGetComp<CompPowerTrader>();
                return cpt;
            }
        }
        private Texture2D TexUp
        {
            get
            {
                if (this.texUp == null)
                {
                    this.texUp = ContentFinder<Texture2D>.Get(this.Props.texUp, true);
                }
                return this.texUp;
            }
        }
        private Texture2D TexDown
        {
            get
            {
                if (this.texDown == null)
                {
                    this.texDown = ContentFinder<Texture2D>.Get(this.Props.texDown, true);
                }
                return this.texDown;
            }
        }
        public Texture2D UIIcon
        {
            get
            {
                if (this.texBD == null)
                {
                    this.texBD = ContentFinder<Texture2D>.Get(this.Props.texBD, true);
                }
                return this.texBD;
            }
        }
        public float RequiredProgress
        {
            get
            {
                return this.Props.maxProgress * this.challengeRating;
            }
        }
        public void ResetStaticData()
        {
            CompChainComputer.options = new List<CompChainComputer.Option>
            {
                new CompChainComputer.Option(GameConditionDefOf.VolcanicWinter, new FloatRange(10f, 20f)),
                new CompChainComputer.Option(GameConditionDefOf.WeatherController, new FloatRange(5f, 20f)),
                new CompChainComputer.Option(GameConditionDefOf.HeatWave, new FloatRange(4f, 8f)),
                new CompChainComputer.Option(GameConditionDefOf.ColdSnap, new FloatRange(4f, 8f)),
                new CompChainComputer.Option(GameConditionDefOf.ToxicFallout, new FloatRange(5f, 20f)),
                new CompChainComputer.Option(GameConditionDefOf.PsychicSuppression, new FloatRange(4f, 8f)),
                new CompChainComputer.Option(GameConditionDefOf.EMIField, new FloatRange(4f, 8f)),
                new CompChainComputer.Option(GameConditionDefOf.PsychicDrone, new FloatRange(4f, 8f))
            };
        }
        private struct Option
        {
            public Option(GameConditionDef gameCondition, FloatRange durationDaysRange)
            {
                this.gameCondition = gameCondition;
                this.durationDaysRange = durationDaysRange;
            }
            public GameConditionDef gameCondition;
            public FloatRange durationDaysRange;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref this.curPowerConsumption, "curPowerConsumption", 0f, false);
            Scribe_Values.Look<float>(ref this.curProgress, "curProgress", 0f, false);
            Scribe_Values.Look<int>(ref this.challengeRating, "challengeRating", 0, false);
            Scribe_Values.Look<int>(ref this.raidCD, "raidCD", 180000, false);
            Scribe_Values.Look<bool>(ref this.needsPsyConnection, "needsPsyConnection", false, false);
            Scribe_References.Look<Pawn>(ref this.linkedPawn, "linkedPawn", false);
            Scribe_Values.Look<float>(ref this.captchaThreshold, "captchaThreshold", -1f, false);
            Scribe_Values.Look<bool>(ref this.canBeHackedNow, "canBeHackedNow", false, false);
        }
        private Texture2D texUp;
        private Texture2D texDown;
        private Texture2D texBD;
        public float curPowerConsumption;
        public float curProgress;
        public int challengeRating;
        public int raidCD;
        public bool needsPsyConnection;
        public Pawn linkedPawn;
        public float captchaThreshold;
        public bool canBeHackedNow;
        private static List<CompChainComputer.Option> options;
    }
    /*only pushes heat if flicked on in the quest node. Being enabled causes tons of other stuff too.
     * Heat pushing amount increases by OI_heatRatePerHour each hour, up to OI_heatRateMax. Won't push heat if MaxTemperature is exceeded in its cell; this value increases by OI_heatlImitPerHOur, up to OI_heatLimitMax
     * Once the chaincomp has been alive for a random number of hours within OI_hoursUntilHaywireRisk, bad shit starts happening BEYOND just the heat pushing, and it starts emitting OI_effecterDef.
     * OI_haywireChances are the chance each hour for each bad possible thing to happen. These "haywire effects" can't occur within OI_haywireCooldownTicks of each other.
     * CompBreak: causes a random breakdownable building connected to the same power grid (if any) to have its internal components break down
     * Explosion: unleash a OI_haywireExplosionRadius-radius fire explosion which deals OI_haywireExplosionDamage.
     * EMI: causes an EMI field for OI_EMIduration*/
    public class CompProperties_HeatPusher_OI : CompProperties_HeatPusher
    {
        public CompProperties_HeatPusher_OI()
        {
            this.compClass = typeof(CompHeatPusher_OI);
        }
        public float OI_heatRatePerHour;
        public float OI_heatLimitPerHour;
        public float OI_heatRateMax;
        public float OI_heatLimitMax;
        public FloatRange OI_hoursUntilHaywireRisk;
        public float OI_haywireChancePerHour_CompBreak;
        public float OI_haywireChancePerHour_Explosion;
        public float OI_haywireExplosionRadius;
        public IntRange OI_haywireExplosionDamage;
        public float OI_haywireChancePerHour_EMI;
        public IntRange OI_EMIduration;
        public int OI_haywireCooldownTicks;
        public string OI_haywireMessageLabel;
        public string OI_haywireMessageText;
        public EffecterDef OI_effecterDef;
    }
    public class CompHeatPusher_OI : CompHeatPusher
    {
        public new CompProperties_HeatPusher_OI Props
        {
            get
            {
                return (CompProperties_HeatPusher_OI)this.props;
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            this.enabled = false;
            this.OI_haywireThreshold = (int)(this.Props.OI_hoursUntilHaywireRisk.RandomInRange * 2500);
        }
        public override bool ShouldPushHeatNow
        {
            get
            {
                if (this.enabled)
                {
                    float ambientTemperature = this.parent.AmbientTemperature;
                    return this.enabled && ambientTemperature < this.MaxTemperature;
                }
                return false;
            }
        }
        public float EnergyPerPush
        {
            get
            {
                return Math.Min(this.Props.OI_heatRateMax, this.Props.heatPerSecond + (this.Props.OI_heatRatePerHour * this.ticksActive / 2500));
            }
        }
        public float MaxTemperature
        {
            get
            {
                return Math.Min(this.Props.OI_heatLimitMax, this.Props.heatPushMaxTemperature + (this.Props.OI_heatLimitPerHour * this.ticksActive / 2500));
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (this.enabled && this.parent.SpawnedOrAnyParentSpawned)
            {
                this.ticksActive++;
                if (this.OI_haywireCooldown > 0)
                {
                    this.OI_haywireCooldown--;
                }
                if (this.parent.IsHashIntervalTick(60) && this.ShouldPushHeatNow)
                {
                    GenTemperature.PushHeat(this.parent.PositionHeld, this.parent.MapHeld, this.EnergyPerPush);
                }
                if (this.OI_goneHaywire)
                {
                    if (this.effecter == null)
                    {
                        this.effecter = this.Props.OI_effecterDef.Spawn();
                        this.effecter.Trigger(this.parent, this.parent);
                    }
                    if (this.effecter != null)
                    {
                        this.effecter.EffectTick(this.parent, this.parent);
                    }
                }
                if (this.ticksActive >= this.OI_haywireThreshold)
                {
                    if (!this.OI_goneHaywire)
                    {
                        Find.LetterStack.ReceiveLetter(this.Props.OI_haywireMessageLabel, this.Props.OI_haywireMessageText, LetterDefOf.NegativeEvent, this.parent);
                        this.OI_goneHaywire = true;
                    }
                    if (this.OI_goneHaywire && this.OI_haywireCooldown <= 0 && this.parent.IsHashIntervalTick(250))
                    {
                        if (Rand.Chance(this.Props.OI_haywireChancePerHour_CompBreak * 0.1f))
                        {
                            CompPower power = this.parent.TryGetComp<CompPower>();
                            if (power != null)
                            {
                                PowerNet pn = power.PowerNet;
                                if (pn != null)
                                {
                                    List<CompPower> connectors = pn.connectors.InRandomOrder().ToList();
                                    if (!connectors.NullOrEmpty())
                                    {
                                        foreach (CompPower p2 in connectors)
                                        {
                                            CompBreakdownable cbd = p2.parent.GetComp<CompBreakdownable>();
                                            if (cbd != null && !cbd.BrokenDown)
                                            {
                                                GenExplosion.DoExplosion(this.parent.Position, this.parent.Map, this.Props.OI_haywireExplosionRadius, DamageDefOf.Smoke, this.parent);
                                                cbd.DoBreakdown();
                                                this.OI_haywireCooldown = this.Props.OI_haywireCooldownTicks;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (Rand.Chance(this.Props.OI_haywireChancePerHour_Explosion * 0.1f))
                        {
                            GenExplosion.DoExplosion(this.parent.Position, this.parent.Map, this.Props.OI_haywireExplosionRadius, DamageDefOf.Flame, this.parent, this.Props.OI_haywireExplosionDamage.RandomInRange);
                            Find.TickManager.slower.SignalForceNormalSpeed();
                            this.OI_haywireCooldown = this.Props.OI_haywireCooldownTicks;
                        }
                        if (Rand.Chance(this.Props.OI_haywireChancePerHour_EMI * 0.1f))
                        {
                            GenExplosion.DoExplosion(this.parent.Position, this.parent.Map, this.Props.OI_haywireExplosionRadius, DamageDefOf.EMP, this.parent);
                            GameCondition gameCondition = GameConditionMaker.MakeCondition(GameConditionDefOf.EMIField, this.Props.OI_EMIduration.RandomInRange);
                            gameCondition.forceDisplayAsDuration = true;
                            gameCondition.Permanent = false;
                            List<Rule> listRule = new List<Rule>();
                            Dictionary<string, string> dictionary = new Dictionary<string, string>();
                            this.parent.Map.gameConditionManager.RegisterCondition(gameCondition);
                            Find.LetterStack.ReceiveLetter(gameCondition.LabelCap, gameCondition.LetterText, gameCondition.def.letterDef, LookTargets.Invalid, null, null, null, null, 0, true);
                            this.OI_haywireCooldown = this.Props.OI_haywireCooldownTicks;
                        }
                    }
                }
            }
        }
        public override void CompTickRare()
        {
            base.CompTickRare();
            if (this.parent.SpawnedOrAnyParentSpawned)
            {
                if (this.ShouldPushHeatNow)
                {
                    GenTemperature.PushHeat(this.parent.PositionHeld, this.parent.MapHeld, this.EnergyPerPush * 4.1666665f);
                }
                Log.Error("meow?");
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.OI_goneHaywire, "OI_goneHaywire", false, false);
            Scribe_Values.Look<int>(ref this.OI_haywireThreshold, "OI_haywireThreshold", 3000000, false);
            Scribe_Values.Look<int>(ref this.OI_haywireCooldown, "OI_haywireCooldown", 0, false);
            Scribe_Values.Look<int>(ref this.ticksActive, "ticksActive", 0, false);
        }
        public bool OI_goneHaywire;
        public int OI_haywireThreshold;
        public int OI_haywireCooldown;
        public int ticksActive;
        public Effecter effecter;
    }
    //when removed, the linked chaincomputer loses a random amount of progress from within BD_progressLossOnDeath
    public class HediffCompProperties_BrainDrain : HediffCompProperties_Link
    {
        public HediffCompProperties_BrainDrain()
        {
            this.compClass = typeof(HediffComp_BrainDrain);
        }
        public FloatRange BD_progressLossOnDeath;
    }
    public class HediffComp_BrainDrain : HediffComp_Link
    {
        public new HediffCompProperties_BrainDrain Props
        {
            get
            {
                return (HediffCompProperties_BrainDrain)this.props;
            }
        }
        public override void CompPostPostRemoved()
        {
            if (this.other != null)
            {
                CompChainComputer ccc = this.other.TryGetComp<CompChainComputer>();
                if (ccc != null)
                {
                    ccc.linkedPawn = null;
                    if (ccc.curProgress <= ccc.RequiredProgress)
                    {
                        ccc.curProgress -= this.Props.BD_progressLossOnDeath.RandomInRange;
                        if (ccc.curProgress <= 0f)
                        {
                            ccc.curProgress = 0f;
                        }
                    }
                }
            }
            base.CompPostPostRemoved();
        }
    }
}
