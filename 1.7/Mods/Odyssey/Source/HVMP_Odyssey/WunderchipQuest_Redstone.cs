using HautsPermits;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HVMP_Odyssey
{
    //quest
    public class QuestNode_Root_Wunderchip_Redstone : QuestNode_Root_Gravcore
    {
        protected override bool TestRunInt(Slate slate)
        {
            return Faction.OfInsects != null && base.TestRunInt(slate);
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;
            PlanetTile planetTile;
            if (!this.TryFindSiteTile(out planetTile))
            {
                Log.Error("Could not find valid site tile for magmaformer spire quest.");
                return;
            }
            string text = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("MechanoidRelay");
            string text2 = QuestGenUtility.HardcodedSignalWithQuestID("site.MapGenerated");
            string text3 = QuestGenUtility.HardcodedSignalWithQuestID("site.MapRemoved");
            string text4 = QuestGenUtility.HardcodedSignalWithQuestID("magmaformer.FinishedMagmaformerRetraction");
            string text5 = QuestGenUtility.HardcodedSignalWithQuestID("magmaformer.Destroyed");
            Site site = QuestGen_Sites.GenerateSite(new SitePartDefWithParams[]
            {
                new SitePartDefWithParams(HVMPDefOf_Odyssey.HVMP_RedstoneSite, new SitePartParams
                {
                    points = slate.Get<float>("points", 0f, false),
                    threatPoints = slate.Get<float>("points", 0f, false)
                })
            }, planetTile, null, false, null, WorldObjectDefOf.ClaimableSite);
            slate.Set<Site>("site", site, false);
            quest.SpawnWorldObject(site, null, null);
            for (int i = 0; i < site.parts[0].things.Count; i++)
            {
                Thing thing = site.parts[0].things[i];
                QuestUtility.AddQuestTag(ref thing.questTags, text);
                slate.Set<Thing>("magmaformer", thing, false);
                //quest.Message("HVMP_RedstoneMessageSuccess".Translate(), MessageTypeDefOf.PositiveEvent, true, null, thing, text4);
            }
            quest.Letter(LetterDefOf.NeutralEvent, text2, null, null, null, false, QuestPart.SignalListenMode.OngoingOnly, null, false, "[letterTextMapGenerated]", null, "[letterLabelMapGenerated]", null, null);
            QuestPart_Choice.Choice choice = new QuestPart_Choice.Choice();
            choice.rewards.Add(new Reward_DefinedThingDef(HVMPDefOf.HVMP_Wunderchip));
            quest.RewardChoice(null, null).choices.Add(choice);
            quest.End(QuestEndOutcome.Success, 0, null, text4, QuestPart.SignalListenMode.OngoingOnly, true, false);
            quest.End(QuestEndOutcome.Fail, 0, null, text3, QuestPart.SignalListenMode.OngoingOnly, true, false);
            quest.End(QuestEndOutcome.Fail, 0, null, text5, QuestPart.SignalListenMode.OngoingOnly, true, false);
        }
    }
    //magmaformer spire's comps
    public class CompHackable_RetaliationOnLockout : CompHackable
    {
        protected override void OnLockedOut(Pawn hacker = null)
        {
            base.OnLockedOut(hacker);
            if (this.parent.Map != null)
            {
                IncidentParms ip = new IncidentParms
                {
                    target = this.parent.Map,
                    points = StorytellerUtility.DefaultThreatPointsNow(this.parent.Map)*1.15f,
                    infestationLocOverride = new IntVec3?(this.parent.Position),
                    forced = true
                };
                if (IncidentDefOf.RaidEnemy.Worker.CanFireNow(ip))
                {
                    IncidentDefOf.RaidEnemy.Worker.TryExecute(ip);
                }
            }
        }
    }
    public class CompProperties_MagmaformerRetraction : CompProperties
    {
        public CompProperties_MagmaformerRetraction()
        {
            this.compClass = typeof(CompMagmaformerRetraction);
        }
        public int ticksToRetract;
        public float deepDrillSpeedEffectiveness;
        public float raidMTBdays;
        public List<IncidentDef> tectonicIncidents;
        public float tectonicIncidentMTBdays;
        [MustTranslate]
        public string finishedLetterLabel;
        [MustTranslate]
        public string finishedLetterText;
        public LetterDef finishedLetterDef;
        public List<ThingDefCountClass> spawnOnFinish = new List<ThingDefCountClass>();
        public string displayWhileRetracting;
        public string displayWhenFinished;
    }
    [StaticConstructorOnStartup]
    public class CompMagmaformerRetraction : ThingComp
    {
        public CompProperties_MagmaformerRetraction Props
        {
            get
            {
                return (CompProperties_MagmaformerRetraction)this.props;
            }
        }
        public bool Retracting
        {
            get
            {
                return this.retracting;
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            this.ticksRemaining = this.Props.ticksToRetract;
            this.LeakLava();
        }
        public void LeakLava()
        {
            if (this.parent.Spawned)
            {
                int tries = 100;
                Map map = this.parent.Map;
                IEnumerable<IntVec3> iv3s = map.AllCells.Where((IntVec3 iv3) => iv3.IsValid && !iv3.Fogged(map) && iv3.GetEdifice(map) == null && (!iv3.GetTerrain(map).IsWater || Rand.Chance(0.1f)));
                while (tries > 0)
                {
                    IntVec3 lavaSpot = iv3s.RandomElement();
                    if (lavaSpot.IsValid)
                    {
                        map.terrainGrid.SetTerrain(lavaSpot, TerrainDefOf.LavaDeep);
                        break;
                    }
                    tries--;
                }
            }
        }
        public override void Notify_Hacked(Pawn hacker = null)
        {
            base.Notify_Hacked(hacker);
            this.retracting = true;
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.retracting)
            {
                this.ticksRemaining -= delta;
                if (this.ticksRemaining <= 0)
                {
                    this.FinishRetraction();
                }
                if (this.parent.Spawned)
                {
                    if (this.parent.IsHashIntervalTick(360,delta) && Rand.MTBEventOccurs(this.Props.raidMTBdays / Math.Max(0.5f, HVMP_Mod.settings.wunderQuestDifficulty), 60000, 360))
                    {
                        Map map = this.parent.Map;
                        IncidentParms ip = new IncidentParms
                        {
                            target = this.parent.Map,
                            points = StorytellerUtility.DefaultThreatPointsNow(this.parent.Map) * 1.15f,
                            infestationLocOverride = new IntVec3?(this.parent.Position),
                            forced = true
                        };
                        if (Rand.Chance(0.5f))
                        {
                            IntVec3 intVec = this.parent.InteractionCell;
                            IntVec3 loc;
                            Faction hostFaction = map.ParentFaction ?? Faction.OfPlayer;
                            int tries = 300;
                            while (tries > 0)
                            {
                                tries--;
                                intVec = CellFinder.RandomCell(map);
                                if (!intVec.Fogged(map) && intVec.Standable(map))
                                {
                                    loc = intVec;
                                    break;
                                }
                            }
                            loc = intVec;
                            TunnelHiveSpawner tunnelHiveSpawner = (TunnelHiveSpawner)ThingMaker.MakeThing(ThingDefOf.TunnelHiveSpawner, null);
                            tunnelHiveSpawner.spawnHive = false;
                            tunnelHiveSpawner.insectsPoints = Mathf.Clamp(StorytellerUtility.DefaultSiteThreatPointsNow() * Rand.Range(0.5f, 1f), 200f, 1000f);
                            GenSpawn.Spawn(tunnelHiveSpawner, loc, map, WipeMode.FullRefund);
                            IncidentWorker.SendIncidentLetter(IncidentDefOf.Infestation.letterLabel, IncidentDefOf.Infestation.letterText, IncidentDefOf.Infestation.letterDef, ip, new TargetInfo(tunnelHiveSpawner.Position, map, false), IncidentDefOf.Infestation, Array.Empty<NamedArgument>());
                        } else {
                            if (IncidentDefOf.RaidEnemy.Worker.CanFireNow(ip))
                            {
                                IncidentDefOf.RaidEnemy.Worker.TryExecute(ip);
                            }
                        }
                    }
                    if (this.parent.IsHashIntervalTick(495,delta) && Rand.MTBEventOccurs(this.Props.tectonicIncidentMTBdays / Math.Max(0.5f, HVMP_Mod.settings.wunderQuestDifficulty), 60000, 495) && !this.Props.tectonicIncidents.NullOrEmpty())
                    {
                        this.LeakLava();
                        IncidentParms ip = new IncidentParms
                        {
                            target = this.parent.Map,
                            points = StorytellerUtility.DefaultThreatPointsNow(this.parent.Map) * 1.15f,
                            infestationLocOverride = new IntVec3?(this.parent.Position),
                            forced = true
                        };
                        foreach (IncidentDef id in this.Props.tectonicIncidents.InRandomOrder())
                        {
                            if (id.Worker.CanFireNow(ip))
                            {
                                id.Worker.TryExecute(ip);
                                break;
                            }
                        }
                    }
                }
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            Command_Action command_Action = new Command_Action
            {
                defaultLabel = "HVMP_RedstoneGuideRetractionLabel".Translate(),
                defaultDesc = "HVMP_RedstoneGuideRetractionDesc".Translate(),
                icon = CompMagmaformerRetraction.GuideTexture,
                action = () =>
                {
                    Find.Targeter.BeginTargeting(TargetingParameters.ForColonist(), (LocalTargetInfo target) =>
                    {
                        target.Pawn?.jobs.TryTakeOrderedJob(JobMaker.MakeJob(HVMPDefOf_Odyssey.HVMP_GuideMagmaformer, this.parent), JobTag.Misc);
                    }, (LocalTargetInfo target) =>
                    {
                        Pawn pawn = target.Pawn;
                        if (pawn != null && pawn.IsColonistPlayerControlled)
                        {
                            GenDraw.DrawTargetHighlight(target);
                        }
                    }, (LocalTargetInfo target) => this.ValidateGuide(target).Accepted, null, null, CompMagmaformerRetraction.GuideTexture, playSoundOnAction: true, (LocalTargetInfo target) =>
                    {
                        AcceptanceReport acceptanceReport2 = this.ValidateGuide(target);
                        Pawn pawn = target.Pawn;
                        if (pawn != null && pawn.IsColonistPlayerControlled && !acceptanceReport2.Accepted)
                        {
                            Widgets.MouseAttachedLabel(("HVMP_RedstoneCannotChooseGuide".Translate() + ": " + acceptanceReport2.Reason.CapitalizeFirst()).Colorize(ColorLibrary.RedReadable));
                        } else {
                            Widgets.MouseAttachedLabel("HVMP_RedstoneCommandChooseGuide".Translate());
                        }
                    });
                }
            };
            AcceptanceReport acceptanceReport = this.CanGuideNow();
            if (!acceptanceReport.Accepted)
            {
                command_Action.Disable(acceptanceReport.Reason.CapitalizeFirst());
            }
            yield return command_Action;
            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Finish retraction",
                    action = delegate
                    {
                        this.ticksRemaining = -1;
                        this.FinishRetraction();
                    }
                };
            }
            foreach (Gizmo gizmo3 in QuestUtility.GetQuestRelatedGizmos(this.parent))
            {
                yield return gizmo3;
            }
            yield break;
        }
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            AcceptanceReport acceptanceReport = this.CanGuideNow();
            if (!acceptanceReport.Accepted)
            {
                yield return new FloatMenuOption("HVMP_RedstoneCannotChooseGuide".Translate() + ": " + acceptanceReport.Reason.CapitalizeFirst(), null);
                yield break;
            }
            CompMagmaformerRetraction.tmpAllowedPawns.Clear();
            if (!selPawn.CanReach(parent, PathEndMode.Touch, Danger.Deadly))
            {
                yield return new FloatMenuOption("HVMP_RedstoneCannotChooseGuide".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                yield break;
            }
            if (!CompMagmaformerRetraction.IsCapableOfGuidingRetraction(selPawn))
            {
                yield return new FloatMenuOption("HVMP_RedstoneCannotChooseGuide".Translate() + ": " + "IncapableOfMining".Translate(), null);
                yield break;
            }
            yield return new FloatMenuOption("HVMP_RedstoneGuideRetractionLabel".Translate(), () =>
            {
                selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(HVMPDefOf_Odyssey.HVMP_GuideMagmaformer, parent), JobTag.Misc);
            });
        }
        public override IEnumerable<FloatMenuOption> CompMultiSelectFloatMenuOptions(IEnumerable<Pawn> selPawns)
        {
            AcceptanceReport acceptanceReport = this.CanGuideNow();
            if (!acceptanceReport.Accepted)
            {
                yield return new FloatMenuOption("HVMP_RedstoneCannotChooseGuide".Translate() + ": " + acceptanceReport.Reason.CapitalizeFirst(), null);
                yield break;
            }
            CompMagmaformerRetraction.tmpAllowedPawns.Clear();
            foreach (Pawn selPawn in selPawns)
            {
                if (selPawn.CanReach(parent, PathEndMode.Touch, Danger.Deadly))
                {
                    CompMagmaformerRetraction.tmpAllowedPawns.Add(selPawn);
                }
            }
            if (CompMagmaformerRetraction.tmpAllowedPawns.Count <= 0)
            {
                yield return new FloatMenuOption("HVMP_RedstoneCannotChooseGuide".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                yield break;
            }
            CompMagmaformerRetraction.tmpAllowedPawns.Clear();
            foreach (Pawn selPawn2 in selPawns)
            {
                if (CompMagmaformerRetraction.IsCapableOfGuidingRetraction(selPawn2))
                {
                    CompMagmaformerRetraction.tmpAllowedPawns.Add(selPawn2);
                }
            }
            if (CompMagmaformerRetraction.tmpAllowedPawns.Count <= 0)
            {
                yield return new FloatMenuOption("HVMP_RedstoneCannotChooseGuide".Translate() + ": " + "IncapableOfMining".Translate(), null);
                yield break;
            }
            CompMagmaformerRetraction.tmpAllowedPawns.Clear();
            foreach (Pawn selPawn4 in selPawns)
            {
                if (CompMagmaformerRetraction.IsCapableOfGuidingRetraction(selPawn4) && selPawn4.CanReach(parent, PathEndMode.Touch, Danger.Deadly))
                {
                    CompMagmaformerRetraction.tmpAllowedPawns.Add(selPawn4);
                }
            }
            if (CompMagmaformerRetraction.tmpAllowedPawns.Count <= 0)
            {
                yield break;
            }
            yield return new FloatMenuOption("HVMP_RedstoneGuideRetractionLabel".Translate(), () =>
            {
                CompMagmaformerRetraction.tmpAllowedPawns[0].jobs.TryTakeOrderedJob(JobMaker.MakeJob(HVMPDefOf_Odyssey.HVMP_GuideMagmaformer, parent), JobTag.Misc);
                for (int i = 1; i < CompMagmaformerRetraction.tmpAllowedPawns.Count; i++)
                {
                    FloatMenuOptionProvider_DraftedMove.PawnGotoAction(parent.Position, CompMagmaformerRetraction.tmpAllowedPawns[i], RCellFinder.BestOrderedGotoDestNear(parent.Position, CompMagmaformerRetraction.tmpAllowedPawns[i]));
                }
            });
        }
        private AcceptanceReport ValidateGuide(LocalTargetInfo target)
        {
            Pawn pawn = target.Thing as Pawn;
            if (pawn == null || pawn.IsSubhuman)
            {
                return false;
            }
            if (!pawn.CanReach(this.parent, PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn))
            {
                return "NoPath".Translate();
            }
            if (!CompMagmaformerRetraction.IsCapableOfGuidingRetraction(pawn))
            {
                return "IncapableOfMining".Translate();
            }
            return true;
        }
        public static bool IsCapableOfGuidingRetraction(Pawn pawn)
        {
            return !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Mining) && (pawn.workSettings == null || pawn.workSettings.WorkIsActive(WorkTypeDefOf.Mining));
        }
        private AcceptanceReport CanGuideNow()
        {
            if (this.ticksRemaining <= 0)
            {
                return "HVMP_RedstoneRetractionFinished".Translate();
            } else if (!this.retracting)
            {
                return "HVMP_RequiresPVYAH".Translate();
            }
            return true;
        }
        public void GuidanceWorkDone(Pawn driller, int delta)
        {
            float num = driller.GetStatValue(StatDefOf.DeepDrillingSpeed, true, -1) * (float)delta * this.Props.deepDrillSpeedEffectiveness;
            this.ticksRemaining -= num;
        }
        public override string CompInspectStringExtra()
        {
            if (!this.retracting)
            {
                CompHackable ch = this.parent.TryGetComp<CompHackable>();
                if (ch != null && !ch.IsHacked)
                {
                    return null;
                }
                return this.Props.displayWhenFinished.Translate();
            } else {
                int displayTicks = (int)this.ticksRemaining;
                if (this.ticksRemaining < 2500)
                {
                    return this.Props.displayWhileRetracting.Translate(displayTicks.ToStringSecondsFromTicks("F0"));
                } else {
                    return this.Props.displayWhileRetracting.Translate(displayTicks.ToStringTicksToPeriod(true, false, true, true));
                }
            }
        }
        public void FinishRetraction()
        {
            if (this.parent.Spawned)
            {
                if (this.parent.InteractionCell != null && this.parent.InteractionCell.Walkable(this.parent.Map))
                {
                    GenLeaving.DropThingsNear(this.parent, this.Props.spawnOnFinish, CompMagmaformerRetraction.tmpSpawnedThings, true, (IntVec3 iv3) => iv3 == this.parent.InteractionCell);
                } else {
                    GenLeaving.DropThingsNear(this.parent, this.Props.spawnOnFinish, CompMagmaformerRetraction.tmpSpawnedThings, true, null);
                }
                if (!this.sentLetter && !this.Props.finishedLetterLabel.NullOrEmpty() && !this.Props.finishedLetterText.NullOrEmpty())
                {
                    this.sentLetter = true;
                    Find.LetterStack.ReceiveLetter(this.Props.finishedLetterLabel, this.Props.finishedLetterText, this.Props.finishedLetterDef, CompMagmaformerRetraction.tmpSpawnedThings, null, null, null, null, 0, true);
                }
                CompMagmaformerRetraction.tmpSpawnedThings.Clear();
                this.retracting = false;
                QuestUtility.SendQuestTargetSignals(this.parent.questTags, "FinishedMagmaformerRetraction", this.Named("SUBJECT"));
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref this.ticksRemaining, "ticksRemaining", 180000, false);
            Scribe_Values.Look<bool>(ref this.sentLetter, "sentLetter", false, false);
            Scribe_Values.Look<bool>(ref this.retracting, "retracting", false, false);
        }
        public float ticksRemaining;
        private bool sentLetter;
        private bool retracting;
        private static readonly List<Thing> tmpSpawnedThings = new List<Thing>();
        private static readonly Texture2D GuideTexture = ContentFinder<Texture2D>.Get("UI/Commands/Hack", true);
        private static readonly List<Pawn> tmpAllowedPawns = new List<Pawn>();
    }
    //jobs
    public class JobDriver_GuideMagmaformer : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null, errorOnFailed, false);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            this.FailOnThingHavingDesignation(TargetIndex.A, DesignationDefOf.Uninstall);
            this.FailOn(() => !job.targetA.Thing.TryGetComp<CompMagmaformerRetraction>().Retracting);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            Toil work = ToilMaker.MakeToil("MakeNewToils");
            work.handlingFacing = true;
            work.tickAction = () =>
            {
                Pawn actor = work.actor;
                ((Building)actor.CurJob.targetA.Thing).GetComp<CompMagmaformerRetraction>().GuidanceWorkDone(actor, 1);
                actor.skills?.Learn(SkillDefOf.Mining, 0.065f);
                actor.rotationTracker.FaceTarget(actor.CurJob.targetA.Thing);
            };
            work.defaultCompleteMode = ToilCompleteMode.Never;
            work.WithEffect(EffecterDefOf.Drill, TargetIndex.A);
            work.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            work.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            work.activeSkill = () => SkillDefOf.Mining;
            yield return work;
        }
    }
    //volcanic incident workers - 'lava surge' is lava flow, but it has far less restrictions for the kinds of terrain it can start on or pgoress to
    public class IncidentWorker_LavaFlowAnyEdge : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            ((Map)parms.target).GameConditionManager.RegisterCondition(GameConditionMaker.MakeCondition(HVMPDefOf_Odyssey.HVMP_LavaFlowAnyEdge,Rand.Range(2500,12500)));
            return true;
        }
    }
    public class GameCondition_LavaFlowAnyEdge : GameCondition
    {
        private int FlowDurationTicks
        {
            get
            {
                return this.maxFlow * 5;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<IntVec3>(ref this.openCells, "openCells", LookMode.Value);
            Scribe_Values.Look<int>(ref this.maxFlow, "maxFlow", 0, false);
        }
        public override void Init()
        {
            base.Init();
            this.maxFlow = GameCondition_LavaFlowAnyEdge.MaxFlowRange.RandomInRange;
            foreach (IntVec3 intVec in this.GetInitialCells(base.SingleMap))
            {
                base.SingleMap.terrainGrid.SetTempTerrain(intVec, TerrainDefOf.LavaShallow);
                base.SingleMap.tempTerrain.QueueRemoveTerrain(intVec, Find.TickManager.TicksGame + this.FlowDurationTicks + 60000);
                this.openCells.Add(intVec);
            }
            if (this.openCells.Count == 0)
            {
                this.End();
                return;
            }
            Find.LetterStack.ReceiveLetter(HVMPDefOf_Odyssey.HVMP_LavaFlowAnyEdge.LabelCap, HVMPDefOf_Odyssey.HVMP_LavaFlowAnyEdge.description, LetterDefOf.ThreatSmall, new LookTargets(this.openCells.First<IntVec3>(), base.SingleMap), null, null, null, null, 0, true);
        }
        private IEnumerable<IntVec3> GetInitialCells(Map map)
        {
            Rot4 edge = Rot4.Random;
            int count = GameCondition_LavaFlowAnyEdge.StartPositionCountRange.RandomInRange;
            for (int i = 0; i < count; i++)
            {
                if (CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) =>
                {
                    if (c.Fogged(map) || c.GetTerrain(map).IsWater)
                    {
                        return false;
                    }
                    return c.GetEdifice(map) == null;
                }, map, edge, 0f, out var result))
                {
                    yield return result;
                }
            }
        }
        public override void GameConditionTick()
        {
            if (!base.SingleMap.IsHashIntervalTick(5))
            {
                return;
            }
            this.SpreadLava();
            if (Find.TickManager.TicksGame > this.startTick + this.FlowDurationTicks || this.openCells.Count == 0)
            {
                this.End();
            }
        }
        private void SpreadLava()
        {
            IntVec3 intVec;
            if (!this.openCells.TryRandomElementByWeight((IntVec3 c) => (float)(Mathf.Max(this.AdjacentLavaCells(c), 1) * 4), out intVec))
            {
                intVec = this.openCells.RandomElement<IntVec3>();
            }
            if (this.tempAdjCells == null)
            {
                this.tempAdjCells = GenAdj.CardinalDirections.ToList<IntVec3>();
            }
            this.tempAdjCells.Shuffle<IntVec3>();
            bool flag = true;
            foreach (IntVec3 intVec2 in this.tempAdjCells)
            {
                IntVec3 intVec3 = intVec + intVec2;
                if (this.CanLavaPotentiallySpreadInto(intVec3))
                {
                    flag = false;
                }
                if (this.CanLavaSpreadInto(intVec3))
                {
                    base.SingleMap.terrainGrid.SetTempTerrain(intVec3, TerrainDefOf.LavaShallow);
                    base.SingleMap.tempTerrain.QueueRemoveTerrain(intVec3, Find.TickManager.TicksGame + this.FlowDurationTicks + 60000);
                    this.openCells.Add(intVec3);
                    break;
                }
            }
            if (flag)
            {
                this.openCells.Remove(intVec);
            }
        }
        private bool CanLavaPotentiallySpreadInto(IntVec3 c)
        {
            return c.InBounds(base.SingleMap) && !this.openCells.Contains(c) && base.SingleMap.terrainGrid.FoundationAt(c) == null && !base.SingleMap.terrainGrid.TerrainAt(c).IsWater && base.SingleMap.terrainGrid.TerrainAt(c) != TerrainDefOf.LavaShallow;
        }
        private bool CanLavaSpreadInto(IntVec3 c)
        {
            if (!this.CanLavaPotentiallySpreadInto(c))
            {
                return false;
            }
            Building edifice = c.GetEdifice(base.SingleMap);
            return edifice == null || edifice.IsClearableFreeBuilding;
        }
        private int AdjacentLavaCells(IntVec3 c)
        {
            int num = 0;
            foreach (IntVec3 intVec in GenAdj.CardinalDirections)
            {
                IntVec3 intVec2 = c + intVec;
                if (intVec2.InBounds(base.SingleMap) && intVec2.GetTerrain(base.SingleMap) == TerrainDefOf.LavaShallow)
                {
                    num++;
                }
            }
            return num;
        }
        private HashSet<IntVec3> openCells = new HashSet<IntVec3>();
        private int maxFlow;
        private static readonly IntRange StartPositionCountRange = new IntRange(3, 6);
        private static readonly IntRange MaxFlowRange = new IntRange(20000, 32000);
        private List<IntVec3> tempAdjCells;
    }
    //site
    public class SitePartWorker_RedstoneSite : SitePartWorker
    {
        public override void Init(Site site, SitePart sitePart)
        {
            base.Init(site, sitePart);
            sitePart.things = new ThingOwner<Thing>(sitePart);
            sitePart.things.TryAdd(ThingMaker.MakeThing(HVMPDefOf_Odyssey.HVMP_Magmaformer, null), true);
        }
    }
    public class GenStep_RedstoneSite : GenStep
    {
        public override int SeedPart
        {
            get
            {
                return 184069412;
            }
        }
        public override void Generate(Map map, GenStepParams parms)
        {
            List<CellRect> usedRects = MapGenerator.GetOrGenerateVar<List<CellRect>>("UsedRects");
            using (IEnumerator<Thing> enumerator = ((IEnumerable<Thing>)parms.sitePart.things).GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Thing thing = enumerator.Current;
                    ResolveParams resolveParams = default(ResolveParams);
                    resolveParams.singleThingToSpawn = thing;
                    if (!RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith((IntVec3 x) => this.CellValid(x, map, thing.def.Size, usedRects), map, out IntVec3 intVec))
                    {
                        intVec = CellFinderLoose.RandomCellWith((IntVec3 x) => this.CellValid(x, map, thing.def.Size, usedRects), map, 1000);
                    }
                    if (!intVec.IsValid)
                    {
                        Log.Warning("Could not find valid cell for HVMP_Magmaformer.");
                    }
                    resolveParams.rect = GenAdj.OccupiedRect(intVec, thing.def.defaultPlacingRot, thing.def.size);
                    this.ChangeTerrainAround(resolveParams, map, thing, usedRects);
                    BaseGen.symbolStack.Push("thing", resolveParams, null);
                }
            }
            BaseGen.globalSettings.map = map;
            BaseGen.Generate();
        }
        private void ChangeTerrainAround(ResolveParams thingParms, Map map, Thing thing, List<CellRect> usedRects)
        {
            CellRect cellRect = thingParms.rect.ExpandedBy(1);
            foreach (IntVec3 intVec in cellRect)
            {
                if (map.terrainGrid.TerrainAt(intVec).changeable)
                {
                    map.terrainGrid.SetTerrain(intVec, TerrainDefOf.AncientConcrete);
                }
            }
            CellRect cellRect2 = cellRect.ExpandedBy(1);
            IEnumerable<IntVec3> corners = cellRect2.Corners;
            foreach (IntVec3 intVec2 in cellRect2.EdgeCells)
            {
                map.terrainGrid.SetTerrain(intVec2, TerrainDefOf.AncientTile);
            }
            usedRects.Add(cellRect2);
            foreach (IntVec3 intVec3 in corners)
            {
                if (GenConstruct.TerrainCanSupport(CellRect.CenteredOn(intVec3, 1), map, ThingDefOf.AncientMechDropBeacon))
                {
                    GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.AncientMechDropBeacon, null), intVec3, map, ThingDefOf.AncientMechDropBeacon.defaultPlacingRot, WipeMode.Vanish, false, false);
                }
            }
        }
        private bool CellValid(IntVec3 cell, Map map, IntVec2 size, List<CellRect> usedRects)
        {
            CellRect cellRect = CellRect.CenteredOn(cell, Mathf.Max(size.x, size.z)).ExpandedBy(2);
            foreach (CellRect cellRect2 in usedRects)
            {
                if (cellRect2.Overlaps(cellRect))
                {
                    return false;
                }
            }
            foreach (IntVec3 intVec in cellRect)
            {
                if (!intVec.InBounds(map))
                {
                    return false;
                }
                if (intVec.Fogged(map))
                {
                    return false;
                }
                if (!intVec.Walkable(map))
                {
                    return false;
                }
            }
            return cell.GetAffordances(map).Contains(TerrainAffordanceDefOf.Heavy);
        }
    }
}
