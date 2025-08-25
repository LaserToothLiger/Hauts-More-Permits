using HautsFramework;
using HautsPermits;
using Ionic.Zlib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Grammar;
using Verse.Noise;
using Verse.Sound;
using static RimWorld.QuestPart;
using static UnityEngine.GraphicsBuffer;

namespace HautsPermits_Biotech
{

    [StaticConstructorOnStartup]
    public class HautsPermits_Biotech
    {
        static HautsPermits_Biotech()
        {
        }
    }
    [DefOf]
    public static class HVMP_BDefOf
    {
        static HVMP_BDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HVMP_BDefOf));
        }
        public static HistoryEventDef HVMP_PerformedHarmfulInjection;

        public static JobDef HVMP_InjectRetroviralPackage;

        public static ThingDef HVMP_PreserveMarker;
    }
    //permits
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_DropHatchery : RoyalTitlePermitWorker_Targeted
    {
        public override void DrawHighlight(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(this.caller.Position, this.def.royalAid.targetingRange, Color.white, null);
            if (this.def.royalAid.radius > 0f)
            {
                GenDraw.DrawRadiusRing(target.Cell, this.def.royalAid.radius, Color.red, null);
            }
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            if (target.IsValid)
            {
                if (target.Cell.InBounds(map))
                {
                    WaterBody wb = target.Cell.GetWaterBody(this.map);
                    if (wb != null)
                    {
                        if (wb.MaxPopulation <= 0)
                        {
                            Messages.Message(this.def.LabelCap + ": " + "HVMP_WaterUnsuitableForFish".Translate(), MessageTypeDefOf.RejectInput, true);
                            return;
                        }
                    } else {
                        Messages.Message(this.def.LabelCap + ": " + "HVMP_NoWaterForFish".Translate(), MessageTypeDefOf.RejectInput, true);
                        return;
                    }
                }
                foreach (Building b in GenRadial.RadialDistinctThingsAround(target.Cell, this.map, this.def.royalAid.radius, true).OfType<Building>().Distinct<Building>())
                {
                    if (this.def.royalAid.itemsToDrop[0].thingDef == b.def)
                    {
                        Messages.Message(this.def.LabelCap + ": " + "HVMP_TooCloseToHatchery".Translate(), MessageTypeDefOf.RejectInput, true);
                        return;
                    }
                }
                this.CallResources(target.Cell);
            }
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (map.generatorDef.isUnderground)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            bool free;
            if (HVMP_Utility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free))
            {
                action = delegate
                {
                    this.BeginCallResources(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            yield break;
        }
        private void BeginCallResources(Pawn caller, Faction faction, Map map, bool free)
        {
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetPawns = false;
            this.caller = caller;
            this.map = map;
            this.faction = faction;
            this.free = free;
            this.targetingParameters.validator = (TargetInfo target) => (this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(caller.Position) <= this.def.royalAid.targetingRange) && !target.Cell.Fogged(map) && target.Cell.GetTerrain(map).IsWater && (!target.Cell.Roofed(map) || !target.Cell.GetRoof(map).isThickRoof);
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void CallResources(IntVec3 cell)
        {
            foreach (ThingDefCountClass tdcc in this.def.royalAid.itemsToDrop)
            {
                for (int i = 0; i < tdcc.count; i++)
                {
                    IntVec3 intVec;
                    if (i == 0 || !DropCellFinder.TryFindDropSpotNear(cell, this.map, out intVec, false, false, false, new IntVec2?(new IntVec2(1, 1)), false))
                    {
                        intVec = cell;
                    }
                    DropPodIncomingOfFaction dp = (DropPodIncomingOfFaction)SkyfallerMaker.MakeSkyfaller(HVMPDefOf.HVMP_DropPodOfFaction);
                    dp.thing = tdcc.thingDef;
                    dp.faction = this.faction;
                    dp.placeDirect = true;
                    GenSpawn.Spawn(dp, intVec, this.map, WipeMode.Vanish);
                }
            }
            Messages.Message("MessagePermitTransportDrop".Translate(this.faction.Named("FACTION")), new LookTargets(cell, this.map), MessageTypeDefOf.NeutralEvent, true);
            this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
            }
            HVMP_Utility.DoPTargeterCooldown(this.faction,this.caller,this);
        }
        private Faction faction;
    }
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_DropGD : RoyalTitlePermitWorker_Targeted
    {
        public override void DrawHighlight(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(this.caller.Position, this.def.royalAid.targetingRange, Color.white, null);
            if (this.def.royalAid.radius > 0f)
            {
                GenDraw.DrawRadiusRing(target.Cell, this.def.royalAid.radius, Color.red, null);
            }
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            if (target.IsValid)
            {
                foreach (IntVec3 intVec in GenRadial.RadialCellsAround(target.Cell, 4f, true))
                {
                    if (intVec.InBounds(map))
                    {
                        TerrainDef td = intVec.GetTerrain(this.map);
                        if (td != null && !td.affordances.NullOrEmpty() && (!td.affordances.Contains(TerrainAffordanceDefOf.Heavy) || !td.affordances.Contains(DefDatabase<TerrainAffordanceDef>.GetNamedSilentFail("Diggable"))))
                        {
                            Messages.Message(this.def.LabelCap + ": " + "HVMP_PoorTerrainForGeyser".Translate(), MessageTypeDefOf.RejectInput, true);
                            return;
                        }
                    }
                }
                foreach (Building b in GenRadial.RadialDistinctThingsAround(target.Cell, this.map, this.def.royalAid.radius, true).OfType<Building>().Distinct<Building>())
                {
                    if (b.def == ThingDefOf.SteamGeyser || this.def.royalAid.itemsToDrop[0].thingDef == b.def)
                    {
                        Messages.Message(this.def.LabelCap + ": " + "HVMP_TooCloseToGeyser".Translate(), MessageTypeDefOf.RejectInput, true);
                        return;
                    }
                }
                this.CallResources(target.Cell);
            }
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (map.generatorDef.isUnderground)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (map.TileInfo.WaterCovered)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "HVMP_CommandCallRoyalAidMapTooWatery".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            bool free;
            if (HVMP_Utility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free))
            {
                action = delegate
                {
                    this.BeginCallResources(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            yield break;
        }
        private void BeginCallResources(Pawn caller, Faction faction, Map map, bool free)
        {
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetPawns = false;
            this.caller = caller;
            this.map = map;
            this.faction = faction;
            this.free = free;
            this.targetingParameters.validator = (TargetInfo target) => (this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(caller.Position) <= this.def.royalAid.targetingRange) && !target.Cell.Fogged(map) && DropCellFinder.CanPhysicallyDropInto(target.Cell, map, true, true);
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void CallResources(IntVec3 cell)
        {
            foreach (ThingDefCountClass tdcc in this.def.royalAid.itemsToDrop)
            {
                for (int i = 0; i < tdcc.count; i++)
                {
                    IntVec3 intVec;
                    if (i == 0 || !DropCellFinder.TryFindDropSpotNear(cell, this.map, out intVec, false, false, false, new IntVec2?(new IntVec2(1, 1)), false))
                    {
                        intVec = cell;
                    }
                    DropPodIncomingOfFaction dp = (DropPodIncomingOfFaction)SkyfallerMaker.MakeSkyfaller(HVMPDefOf.HVMP_DropPodOfFaction);
                    dp.thing = tdcc.thingDef;
                    dp.faction = this.faction;
                    dp.placeDirect = true;
                    GenSpawn.Spawn(dp, intVec, this.map, WipeMode.Vanish);
                }
            }
            Messages.Message("MessagePermitTransportDrop".Translate(this.faction.Named("FACTION")), new LookTargets(cell, this.map), MessageTypeDefOf.NeutralEvent, true);
            this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
            }
            HVMP_Utility.DoPTargeterCooldown(this.faction,this.caller,this);
        }
        private Faction faction;
    }
    public class CompProperties_GTDrill : CompProperties
    {
        public CompProperties_GTDrill()
        {
            this.compClass = typeof(CompGTDrill);
        }
        public int ticksToFinish;
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
    //quest nodes
    public class QuestNode_EcosphereIntermediary : QuestNode_Sequence
    {
        protected override void RunInt()
        {
            if (HVMP_Utility.TryFindEcosphereFaction(out Faction faction))
            {
                Slate slate = QuestGen.slate;
                slate.Set<Thing>("asker", faction.leader, false);
                QuestGen.slate.Set<Faction>("faction", faction, false);
                Map map = HVMP_Utility.TryGetMap();
                slate.Set<Map>("map", map, false);
                PlanetTile tile = HVMP_Utility.TryGetPlanetTile();
                slate.Set<PlanetTile>("pTile", tile, false);
                QuestPart_BranchGoodwillFailureHandler qpbgfh = new QuestPart_BranchGoodwillFailureHandler();
                qpbgfh.faction = faction;
                qpbgfh.goodwill = HVMP_Utility.ExpectationBasedGoodwillLoss(map, true, true, faction);
                QuestGen.quest.AddPart(qpbgfh);
                HVMP_Utility.SetSettingScalingRewardValue(slate);
            }
            base.RunInt();
        }
        protected override bool TestRunInt(Slate slate)
        {
            HVMP_Utility.SetSettingScalingRewardValue(slate);
            return HVMP_Utility.TryFindEcosphereFaction(out Faction ecosphereFaction) && base.TestRunInt(slate);
        }
    }
    //case study
    public class QuestNode_CaseStudy : QuestNode_EcosphereIntermediary
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            PawnKindDef pawnKindDef;
            if (pawnKinds.TryRandomElement(out pawnKindDef))
            {
                slate.Set<PawnKindDef>(this.storePawnKindAs.GetValue(slate), pawnKindDef, false);
            }
            base.RunInt();
        }
        [NoTranslate]
        public SlateRef<string> storePawnKindAs;
        public List<PawnKindDef> pawnKinds;
    }
    public class QuestNode_ShuttleWhenCured : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return this.node == null || this.node.TestRun(slate);
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            QuestPart_ShuttleWhenCured qpswc = new QuestPart_ShuttleWhenCured();
            qpswc.inSignalComplete = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignalComplete.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
            if (this.lodgers.GetValue(slate) != null)
            {
                qpswc.lodgers.AddRange(this.lodgers.GetValue(slate));
            }
            if (this.node != null)
            {
                QuestGenUtility.RunInnerNode(this.node, qpswc);
            }
            if (!this.outSignalComplete.GetValue(slate).NullOrEmpty())
            {
                qpswc.outSignalsCompleted.Add(QuestGenUtility.HardcodedSignalWithQuestID(this.outSignalComplete.GetValue(slate)));
            }
            QuestGen.quest.AddPart(qpswc);
        }
        [NoTranslate]
        public SlateRef<string> inSignalComplete;
        [NoTranslate]
        public SlateRef<string> outSignalComplete;
        public SlateRef<IEnumerable<Pawn>> lodgers;
        public QuestNode node;
    }
    public class QuestPart_ShuttleWhenCured : QuestPartActivable
    {
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo globalTargetInfo in base.QuestLookTargets)
                {
                    yield return globalTargetInfo;
                }
                int num;
                for (int i = 0; i < this.lodgers.Count; i = num + 1)
                {
                    yield return this.lodgers[i];
                    num = i;
                }
                yield break;
            }
        }
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignalComplete)
            {
                this.Enable(signal.args);
                this.Complete();
            }
        }
        public override AlertReport AlertReport
        {
            get
            {
                if (!this.alert || base.State != QuestPartState.Enabled)
                {
                    return false;
                }
                return AlertReport.CulpritsAre(this.lodgers);
            }
        }
        public override string AlertLabel
        {
            get
            {
                return "HVMP_QuestPartShuttleArriveOnCure".Translate();
            }
        }
        public override string AlertExplanation
        {
            get
            {
                if (this.quest.hidden)
                {
                    return "HVMP_QuestPartShuttleArriveOnCure".Translate();
                }
                return "HVMP_QuestPartShuttleArriveOnCureDesc".Translate(this.quest.name, this.lodgers.Select((Pawn p) => p.LabelShort).ToLineList("- ", false));
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignalComplete, "inSignalComplete", null, false);
            Scribe_Collections.Look<Pawn>(ref this.lodgers, "lodgers", LookMode.Reference, Array.Empty<object>());
            Scribe_Values.Look<bool>(ref this.alert, "alert", false, false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.lodgers.RemoveAll((Pawn x) => x == null);
            }
        }
        public override void AssignDebugData()
        {
            base.AssignDebugData();
            if (Find.AnyPlayerHomeMap != null)
            {
                this.lodgers.AddRange(Find.RandomPlayerHomeMap.mapPawns.FreeColonists);
            }
        }
        public override void ReplacePawnReferences(Pawn replace, Pawn with)
        {
            this.lodgers.Replace(replace, with);
        }
        public string inSignalComplete;
        public List<Pawn> lodgers = new List<Pawn>();
        public bool alert;
    }
    public class Recipe_RemoveNeopathy : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (!base.AvailableOnNow(thing, part))
            {
                return false;
            }
            if (!(thing is Pawn pawn))
            {
                return false;
            }
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i].def == this.recipe.removesHediff)
                {
                    HediffComp_NeopathyComplications hcnc = hediffs[i].TryGetComp<HediffComp_NeopathyComplications>();
                    if (hcnc != null && hcnc.cureDiscovered)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            List<Hediff> allHediffs = pawn.health.hediffSet.hediffs;
            int num;
            for (int i = 0; i < allHediffs.Count; i = num + 1)
            {
                if (allHediffs[i].Part != null && allHediffs[i].def == recipe.removesHediff && allHediffs[i].Visible)
                {
                    yield return allHediffs[i].Part;
                }
                num = i;
            }
            yield break;
        }
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null)
            {
                if (base.CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                {
                    return;
                }
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, new object[] { billDoer, pawn });
                if (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(billDoer))
                {
                    string text;
                    if (!this.recipe.successfullyRemovedHediffMessage.NullOrEmpty())
                    {
                        text = this.recipe.successfullyRemovedHediffMessage.Formatted(billDoer.LabelShort, pawn.LabelShort);
                    } else {
                        text = "MessageSuccessfullyRemovedHediff".Translate(billDoer.LabelShort, pawn.LabelShort, this.recipe.removesHediff.label.Named("HEDIFF"), billDoer.Named("SURGEON"), pawn.Named("PATIENT"));
                    }
                    Messages.Message(text, pawn, MessageTypeDefOf.PositiveEvent, true);
                }
            }
            Hediff hediff = pawn.health.hediffSet.hediffs.Find((Hediff x) => x.def == this.recipe.removesHediff && x.Part == part && x.Visible);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }
    }
    public class HediffCompProperties_NeopathyComplications : HediffCompProperties_TendDuration
    {
        public HediffCompProperties_NeopathyComplications()
        {
            this.compClass = typeof(HediffComp_NeopathyComplications);
        }
        public IntRange complicationCount;
        public IntRange nextComplicationTimer;
        public List<HediffDef> possibleComplications;
        public float chanceTendRevealsCureMethod;
        public FloatRange tendDiscoveryRange;
    }
    public class HediffComp_NeopathyComplications : HediffComp_TendDuration
    {
        public HediffCompProperties_NeopathyComplications Props
        {
            get
            {
                return (HediffCompProperties_NeopathyComplications)this.props;
            }
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
            this.maxComplications = this.Props.complicationCount.RandomInRange;
            this.ticksToNextComplication = this.Props.nextComplicationTimer.RandomInRange;
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.complicationsCreated < this.maxComplications)
            {
                this.ticksToNextComplication -= delta;
                if (this.ticksToNextComplication <= 0)
                {
                    this.ticksToNextComplication = this.Props.nextComplicationTimer.RandomInRange;
                    if (!this.Props.possibleComplications.NullOrEmpty())
                    {
                        Hediff complication = HediffMaker.MakeHediff(this.Props.possibleComplications.RandomElement(), this.Pawn);
                        this.Pawn.health.AddHediff(complication);
                        this.complicationsCreated++;
                    }
                }
            }
        }
        public override void CompTended(float quality, float maxQuality, int batchPosition = 0)
        {
            base.CompTended(quality, maxQuality, batchPosition);
            this.numTends++;
            if (this.numTends >= this.Props.tendDiscoveryRange.min && !this.cureDiscovered && (this.numTends >= this.Props.tendDiscoveryRange.max || Rand.Chance(this.Props.chanceTendRevealsCureMethod)))
            {
                this.cureDiscovered = true;
                Messages.Message("HVMP_NeopathyCureDiscovered".Translate().CapitalizeFirst().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve(), this.Pawn,MessageTypeDefOf.PositiveEvent,true);
            }
        }
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (!this.Pawn.questTags.NullOrEmpty())
            {
                QuestUtility.SendQuestTargetSignals(this.Pawn.questTags, "NeopathyCured", this.Named("SUBJECT"));
            }
            List<Hediff> toRemove = new List<Hediff>();
            foreach (Hediff h in this.Pawn.health.hediffSet.hediffs)
            {
                if (this.Props.possibleComplications.Contains(h.def))
                {
                    toRemove.Add(h);
                }
            }
            foreach (Hediff h in toRemove)
            {
                this.Pawn.health.RemoveHediff(h);
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.maxComplications, "maxComplications", 5, false);
            Scribe_Values.Look<int>(ref this.ticksToNextComplication, "ticksToNextComplication", 60000, false);
            Scribe_Values.Look<int>(ref this.complicationsCreated, "complicationsCreated", 0, false);
            Scribe_Values.Look<int>(ref this.numTends, "numTends", 0, false);
            Scribe_Values.Look<bool>(ref this.cureDiscovered, "cureDiscovered", false, false);
        }
        public int maxComplications;
        public int ticksToNextComplication;
        public int complicationsCreated;
        public int numTends;
        public bool cureDiscovered;
    }
    public class Hediff_MiasmaticRot : Hediff
    {
        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (this.pawn.IsHashIntervalTick(2000, delta) && this.pawn.SpawnedOrAnyParentSpawned)
            {
                GasUtility.AddGas(this.pawn.PositionHeld, this.pawn.MapHeld, GasType.RotStink, 4444);
            }
        }
    }
    //field work
    public class QuestNode_GetLargestClearAreaOrSlightlySmaller : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            int largestSize = this.GetLargestSize(slate);
            slate.Set<int>(this.storeAs.GetValue(slate), largestSize, false);
            return largestSize >= this.failIfSmaller.GetValue(slate);
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            int largestSize = this.GetLargestSize(slate);
            slate.Set<int>(this.storeAs.GetValue(slate), (int)(largestSize*(0.6f+(0.4f*Rand.Value))), false);
        }
        private int GetLargestSize(Slate slate)
        {
            Map mapResolved = this.map.GetValue(slate) ?? slate.Get<Map>("map", null, false);
            if (mapResolved == null)
            {
                return 0;
            }
            int value = this.max.GetValue(slate);
            CellRect cellRect = LargestAreaFinder.FindLargestRect(mapResolved, (IntVec3 x) => this.IsClear(x, mapResolved), value);
            return Mathf.Min(new int[] { cellRect.Width, cellRect.Height, value });
        }
        private bool IsClear(IntVec3 c, Map map)
        {
            if (!c.GetTerrain(map).affordances.Contains(TerrainAffordanceDefOf.Heavy))
            {
                return false;
            }
            List<Thing> thingList = c.GetThingList(map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (thingList[i].def.IsBuildingArtificial && thingList[i].Faction == Faction.OfPlayer)
                {
                    return false;
                }
                if (thingList[i].def.mineable)
                {
                    bool flag = false;
                    for (int j = 0; j < 8; j++)
                    {
                        IntVec3 intVec = c + GenAdj.AdjacentCells[j];
                        if (intVec.InBounds(map) && intVec.GetFirstMineable(map) == null)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public SlateRef<Map> map;
        [NoTranslate]
        public SlateRef<string> storeAs;
        public SlateRef<int> failIfSmaller;
        public SlateRef<int> max;
    }
    public class QuestNode_GeneratePreserveMarker : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Thing thing = ThingMaker.MakeThing(this.thingDef, null);
            if (thing is NaturePreserve np)
            {
                np.radius = slate.Get<int>("preserveRadius", 10, false);
                np.ticksRemaining = slate.Get<int>("preserveTicks", 900000, false);
                np.initialTicks = slate.Get<int>("preserveTicks", 900000, false);
                slate.Set<Thing>(this.storeAs.GetValue(slate), np, false);
                QuestGen.quest.AddPart(new QuestPart_LookAtThis(np));
            }
        }
        [NoTranslate]
        public SlateRef<string> storeAs;
        public ThingDef thingDef;
        [NoTranslate]
        public SlateRef<string> storeReqProgressAs;
    }
    public class QuestNode_DropPreserveMarkerCopy : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return slate.Exists("map", false);
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            QuestPart_DropPreserveMarkerCopy qpdpmc = new QuestPart_DropPreserveMarkerCopy();
            qpdpmc.mapParent = slate.Get<Map>("map", null, false).Parent;
            qpdpmc.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
            qpdpmc.outSignalResult = QuestGenUtility.HardcodedSignalWithQuestID(this.outSignalResult.GetValue(slate));
            qpdpmc.destroyOrPassToWorldOnCleanup = this.destroyOrPassToWorldOnCleanup.GetValue(slate);
            qpdpmc.thingDef = this.thingDef;
            QuestGen.quest.AddPart(qpdpmc);
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        [NoTranslate]
        public SlateRef<string> outSignalResult;
        public SlateRef<bool> destroyOrPassToWorldOnCleanup;
        public ThingDef thingDef;
    }
    public class QuestPart_DropPreserveMarkerCopy : QuestPart
    {
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo globalTargetInfo in base.QuestLookTargets)
                {
                    yield return globalTargetInfo;
                }
                if (this.mapParent != null)
                {
                    yield return this.mapParent;
                }
                if (this.copy != null)
                {
                    yield return this.copy;
                }
                yield break;
            }
        }
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                this.copy = null;
                NaturePreserve arg = signal.args.GetArg<NaturePreserve>("SUBJECT");
                if (arg != null && this.mapParent != null && this.mapParent.HasMap)
                {
                    Map map = this.mapParent.Map;
                    IntVec3 intVec = DropCellFinder.RandomDropSpot(map, true);
                    this.copy = (NaturePreserve)ThingMaker.MakeThing(this.thingDef, null);
                    this.copy.radius = arg.radius;
                    this.copy.ticksRemaining = arg.ticksRemaining;
                    this.copy.initialTicks = arg.initialTicks;
                    if (!arg.questTags.NullOrEmpty<string>())
                    {
                        this.copy.questTags = new List<string>();
                        this.copy.questTags.AddRange(arg.questTags);
                    }
                    DropPodUtility.DropThingsNear(intVec, map, Gen.YieldSingle<Thing>(this.copy.MakeMinified()), 110, false, false, true, false, true, null);
                }
                if (!this.outSignalResult.NullOrEmpty())
                {
                    if (this.copy != null)
                    {
                        Find.SignalManager.SendSignal(new Signal(this.outSignalResult, this.copy.Named("SUBJECT")));
                        return;
                    }
                    Find.SignalManager.SendSignal(new Signal(this.outSignalResult, false));
                }
            }
        }
        public override void Cleanup()
        {
            base.Cleanup();
            if (this.destroyOrPassToWorldOnCleanup && this.copy != null)
            {
                QuestPart_DestroyThingsOrPassToWorld.Destroy(this.copy);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_Values.Look<string>(ref this.outSignalResult, "outSignalResult", null, false);
            Scribe_References.Look<MapParent>(ref this.mapParent, "mapParent", false);
            Scribe_References.Look<NaturePreserve>(ref this.copy, "copy", false);
            Scribe_Defs.Look<ThingDef>(ref thingDef, "thingDef");
            Scribe_Values.Look<bool>(ref this.destroyOrPassToWorldOnCleanup, "destroyOrPassToWorldOnCleanup", false, false);
        }
        public override void AssignDebugData()
        {
            base.AssignDebugData();
            this.inSignal = "DebugSignal" + Rand.Int;
            if (Find.AnyPlayerHomeMap != null)
            {
                this.mapParent = Find.RandomPlayerHomeMap.Parent;
            }
        }
        public MapParent mapParent;
        public string inSignal;
        public string outSignalResult;
        public bool destroyOrPassToWorldOnCleanup;
        public ThingDef thingDef;
        private NaturePreserve copy;
    }
    [StaticConstructorOnStartup]
    public class NaturePreserve : Thing
    {
        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (this.Spawned && this.IsHashIntervalTick(177, delta))
            {
                if (this.AnyBadJuju)
                {
                    this.ticksSinceDisallowedAnything += 177;
                    if (this.ticksSinceDisallowedAnything >= 60000)
                    {
                        Messages.Message("HVMP_PreserveDestroyedBecauseOfViolation".Translate(), new TargetInfo(base.Position, base.Map, false), MessageTypeDefOf.NegativeEvent, true);
                        QuestUtility.SendQuestTargetSignals(this.questTags, "PreserveDestroyed", this.Named("SUBJECT"));
                        if (!base.Destroyed)
                        {
                            this.Destroy(DestroyMode.Vanish);
                        }
                    }
                }
                else
                {
                    this.ticksSinceDisallowedAnything = 0;
                    this.ticksRemaining -= 177;
                    if (this.ticksRemaining <= 0)
                    {
                        QuestUtility.SendQuestTargetSignals(this.questTags, "FinishedPreserve", this.Named("SUBJECT"));
                    }
                }
            }
        }
        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            GenDraw.DrawRadiusRing(this.Position, this.radius);
        }
        public IntVec3 FirstBadJuju
        {
            get
            {
                if (this.Spawned)
                {
                    List<Thing> forCell = this.Map.listerArtificialBuildingsForMeditation.GetForCell(this.Position, this.radius);
                    foreach (Thing np in this.Map.listerThings.ThingsOfDef(HVMP_BDefOf.HVMP_PreserveMarker))
                    {
                        if (np != this && np.Position.DistanceTo(this.Position) <= this.radius)
                        {
                            forCell.Add(np);
                        }
                    }
                    if (forCell.Count > 0)
                    {
                        return forCell[0].Position;
                    } else {
                        foreach (IntVec3 intVec in GenRadial.RadialCellsAround(this.Position, this.radius, true))
                        {
                            if (intVec.InBounds(this.Map) && (intVec.IsPolluted(this.Map) || intVec.GetTerrain(this.Map).IsFloor))
                            {
                                return intVec;
                            }
                        }
                    }
                }
                return IntVec3.Invalid;
            }
        }
        public bool AnyBadJuju
        {
            get
            {
                return this.FirstBadJuju.IsValid;
            }
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Finish",
                    action = delegate
                    {
                        this.ticksRemaining = 0;
                    }
                };
            }
            foreach (Gizmo gizmo3 in QuestUtility.GetQuestRelatedGizmos(this))
            {
                yield return gizmo3;
            }
            yield break;
        }
        public float InitialDays
        {
            get
            {
                return this.initialTicks / 60000f;
            }
        }
        public int TicksBeforeDefilementFailure
        {
            get
            {
                return 60000 - this.ticksSinceDisallowedAnything;
            }
        }
        public float DaysElapsed
        {
            get
            {
                return this.InitialDays - (this.ticksRemaining / 60000f);
            }
        }
        public override string GetInspectString()
        {
            string toDisplay = "HVMP_CCProgress".Translate(this.DaysElapsed.ToStringByStyle(ToStringStyle.FloatTwo), this.InitialDays.ToStringByStyle(ToStringStyle.FloatOne));
            if (this.ticksSinceDisallowedAnything > 0)
            {
                toDisplay += "\n" + "HVMP_AlertPreserveViolation".Translate(this.TicksBeforeDefilementFailure.ToStringTicksToPeriod());
            }
            return toDisplay;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.radius, "radius", 40f, false);
            Scribe_Values.Look<int>(ref this.ticksRemaining, "ticksRemaining", 900000, false);
            Scribe_Values.Look<int>(ref this.initialTicks, "initialTicks", 900000, false);
            Scribe_Values.Look<int>(ref this.ticksSinceDisallowedAnything, "ticksSinceDisallowedAnything", 0, false);
        }
        public float radius;
        public int ticksRemaining;
        public int initialTicks;
        public int ticksSinceDisallowedAnything;
    }
    public class PlaceWorker_NoPollutionFloorsStructuresOrOtherPreserves : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            if (thing != null && thing is NaturePreserve np)
            {
                GenDraw.DrawRadiusRing(center, np.radius, Color.white, null);
            }
        }
        public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            if (thing != null && thing is NaturePreserve np)
            {
                bool badJuju = false;
                List<Thing> forCell = map.listerArtificialBuildingsForMeditation.GetForCell(center, np.radius);
                forCell.AddRange(map.listerThings.ThingsOfDef(HVMP_BDefOf.HVMP_PreserveMarker).Where((Thing t) => t.Position.DistanceTo(center) <= np.radius));
                if (forCell.Count > 0)
                {
                    badJuju = true;
                }
                else
                {
                    foreach (IntVec3 intVec in GenRadial.RadialCellsAround(center, np.radius, true))
                    {
                        if (intVec.InBounds(map) && (intVec.IsPolluted(map) || intVec.GetTerrain(map).IsFloor))
                        {
                            badJuju = true;
                            break;
                        }
                    }
                }
                if (badJuju)
                {
                    return "HVMPCannotPlacePreserve".Translate();
                }
            }
            return true;
        }
    }
    public class Alert_DisallowedInsidePreserve : Alert_Critical
    {
        private int MinTicksLeft
        {
            get
            {
                int num = int.MaxValue;
                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    List<Thing> list = maps[i].listerThings.ThingsOfDef(HVMP_BDefOf.HVMP_PreserveMarker);
                    for (int j = 0; j < list.Count; j++)
                    {
                        NaturePreserve np = (NaturePreserve)list[j];
                        if (np.ticksSinceDisallowedAnything > 0 && np.TicksBeforeDefilementFailure < num)
                        {
                            num = np.TicksBeforeDefilementFailure;
                        }
                    }
                }
                return num;
            }
        }
        private List<Thing> DefiledCells
        {
            get
            {
                this.defiledMarkers.Clear();
                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    List<Thing> list = maps[i].listerThings.ThingsOfDef(HVMP_BDefOf.HVMP_PreserveMarker);
                    for (int j = 0; j < list.Count; j++)
                    {
                        NaturePreserve np = (NaturePreserve)list[j];
                        if (np.ticksSinceDisallowedAnything > 0)
                        {
                            this.defiledMarkers.Add(np);
                        }
                    }
                }
                return this.defiledMarkers;
            }
        }

        public Alert_DisallowedInsidePreserve()
        {
            this.defaultLabel = "HVMP_AlertPreserveViolationFlat".Translate();
        }
        public override AlertReport GetReport()
        {
            return AlertReport.CulpritsAre(this.DefiledCells);
        }
        public override TaggedString GetExplanation()
        {
            return "HVMP_AlertPreserveViolationDesc".Translate(this.MinTicksLeft.ToStringTicksToPeriodVerbose(true, true));
        }
        private List<Thing> defiledMarkers = new List<Thing>();
    }
    //hazard disposal
    public class QuestNode_MutantManhunterPack : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return slate.Exists("map", false) && AggressiveAnimalIncidentUtility.TryFindAggressiveAnimalKind(slate.Get<float>("points", 0f, false), slate.Get<Map>("map", null, false), out PawnKindDef pawnKindDef);
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Map map = QuestGen.slate.Get<Map>("map", null, false);
            float num = QuestGen.slate.Get<float>("points", 0f, false);
            MutationsPool mp = HVMPDefOf.HVMP_MutantManhunterPack.GetModExtension<MutationsPool>();
            HediffDef hd = mp.mutations.RandomElement();
            slate.Set<string>("mutationLabel", hd.label, false);
            slate.Set<string>("mutationDesc", hd.description, false);
            QuestPart_IncidentMutantMPs questPart_Incident = new QuestPart_IncidentMutantMPs
            {
                incident = HVMPDefOf.HVMP_MutantManhunterPack,
                mutation = hd
            };
            IncidentParms incidentParms = new IncidentParms
            {
                forced = true,
                target = map,
                points = num,
                quest = QuestGen.quest,
                questTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(this.tag.GetValue(slate)),
                spawnCenter = this.walkInSpot.GetValue(slate) ?? QuestGen.slate.Get<IntVec3?>("walkInSpot", null, false) ?? IntVec3.Invalid,
                pawnCount = this.animalCount.GetValue(slate)
            };
            if (AggressiveAnimalIncidentUtility.TryFindAggressiveAnimalKind(num, map, out PawnKindDef pawnKindDef))
            {
                incidentParms.pawnKind = pawnKindDef;
            }
            slate.Set<PawnKindDef>("animalKindDef", pawnKindDef, false);
            int num2 = ((incidentParms.pawnCount > 0) ? incidentParms.pawnCount : AggressiveAnimalIncidentUtility.GetAnimalsCount(pawnKindDef, num));
            QuestGen.slate.Set<int>("animalCount", num2, false);
            if (!this.customLetterLabel.GetValue(slate).NullOrEmpty() || this.customLetterLabelRules.GetValue(slate) != null)
            {
                QuestGen.AddTextRequest("root", delegate (string x)
                {
                    incidentParms.customLetterLabel = x;
                }, QuestGenUtility.MergeRules(this.customLetterLabelRules.GetValue(slate), this.customLetterLabel.GetValue(slate), "root"));
            }
            if (!this.customLetterText.GetValue(slate).NullOrEmpty() || this.customLetterTextRules.GetValue(slate) != null)
            {
                QuestGen.AddTextRequest("root", delegate (string x)
                {
                    incidentParms.customLetterText = x;
                }, QuestGenUtility.MergeRules(this.customLetterTextRules.GetValue(slate), this.customLetterText.GetValue(slate), "root"));
            }
            questPart_Incident.SetIncidentParmsAndRemoveTarget(incidentParms);
            questPart_Incident.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
            QuestGen.quest.AddPart(questPart_Incident);
            List<Rule> list = new List<Rule>();
            list.Add(new Rule_String("animalKind_label", pawnKindDef.label));
            list.Add(new Rule_String("animalKind_labelPlural", pawnKindDef.GetLabelPlural(num2)));
            QuestGen.AddQuestDescriptionRules(list);
            QuestGen.AddQuestNameRules(list);
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        public SlateRef<string> customLetterLabel;
        public SlateRef<string> customLetterText;
        public SlateRef<RulePack> customLetterLabelRules;
        public SlateRef<RulePack> customLetterTextRules;
        public SlateRef<IntVec3?> walkInSpot;
        public SlateRef<int> animalCount;
        [NoTranslate]
        public SlateRef<string> tag;
        private const string RootSymbol = "root";
    }
    public class QuestPart_IncidentMutantMPs : QuestPart_Incident
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<HediffDef>(ref this.mutation, "mutation");
        }
        public HediffDef mutation;
    }
    public class MutationsPool : DefModExtension
    {
        public MutationsPool()
        {

        }
        public List<HediffDef> mutations;
    }
    public class IncidentWorker_HazardDisposal : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
            {
                return false;
            }
            Map map = (Map)parms.target;
            return this.def.HasModExtension<MutationsPool>() && AggressiveAnimalIncidentUtility.TryFindAggressiveAnimalKind(parms.points, map, out PawnKindDef pawnKindDef) && RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 intVec, map, CellFinder.EdgeRoadChance_Animal, false, null);
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (parms.quest == null)
            {
                return false;
            }
            Map map = (Map)parms.target;
            PawnKindDef pawnKind = parms.pawnKind;
            if ((pawnKind == null && !AggressiveAnimalIncidentUtility.TryFindAggressiveAnimalKind(parms.points, map, out pawnKind)) || AggressiveAnimalIncidentUtility.GetAnimalsCount(pawnKind, parms.points) == 0)
            {
                return false;
            }
            IntVec3 spawnCenter = parms.spawnCenter;
            if (!spawnCenter.IsValid && !RCellFinder.TryFindRandomPawnEntryCell(out spawnCenter, map, CellFinder.EdgeRoadChance_Animal, false, null))
            {
                return false;
            }
            List<Pawn> list = AggressiveAnimalIncidentUtility.GenerateAnimals(pawnKind, map.Tile, parms.points * 1f, parms.pawnCount);
            Rot4 rot = Rot4.FromAngleFlat((map.Center - spawnCenter).AngleFlat);
            QuestPart_IncidentMutantMPs qpimp = parms.quest.GetFirstPartOfType<QuestPart_IncidentMutantMPs>();
            if (qpimp != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Pawn pawn = list[i];
                    IntVec3 intVec = CellFinder.RandomClosewalkCellNear(spawnCenter, map, 10, null);
                    QuestUtility.AddQuestTag(GenSpawn.Spawn(pawn, intVec, map, rot, WipeMode.Vanish, false, false), parms.questTag);
                    pawn.health.AddHediff(HediffDefOf.Scaria, null, null, null);
                    pawn.health.AddHediff(qpimp.mutation, null, null, null);
                    pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent, null, false, false, false, null, false, false, false);
                    pawn.mindState.exitMapAfterTick = Find.TickManager.TicksGame + Rand.Range(60000, 120000);
                }
            }
            if (ModsConfig.AnomalyActive)
            {
                if (this.def == IncidentDefOf.FrenziedAnimals)
                {
                    base.SendStandardLetter("FrenziedAnimalsLabel".Translate(), "FrenziedAnimalsText".Translate(pawnKind.GetLabelPlural(-1)), LetterDefOf.ThreatBig, parms, list[0], Array.Empty<NamedArgument>());
                } else {
                    base.SendStandardLetter("LetterLabelManhunterPackArrived".Translate(), "ManhunterPackArrived".Translate(pawnKind.GetLabelPlural(-1)), LetterDefOf.ThreatBig, parms, list[0], Array.Empty<NamedArgument>());
                }
            } else {
                base.SendStandardLetter("LetterLabelManhunterPackArrived".Translate(), "ManhunterPackArrived".Translate(pawnKind.GetLabelPlural(-1)), LetterDefOf.ThreatBig, parms, list[0], Array.Empty<NamedArgument>());
            }
            Find.TickManager.slower.SignalForceNormalSpeedShort();
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.ForbiddingDoors, OpportunityType.Critical);
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.AllowedAreas, OpportunityType.Important);
            return true;
        }
    }
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
    public class Hediff_Broodlings : Hediff
    {
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (this.pawn.SpawnedOrAnyParentSpawned && Rand.Chance(this.pawn.BodySize/2f))
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
    //retroviral agent
    public class QuestNode_GenerateRetroviralPackage : QuestNode
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
            slate.Set<Thing>(this.storeAs.GetValue(slate), thing, false);
            CompStudiableQuestItem cda = thing.TryGetComp<CompStudiableQuestItem>();
            if (cda != null)
            {
                cda.challengeRating = challengeRating;
                cda.relevantSkills = new List<SkillDef>();
                if (!cda.Props.requiredSkillDefs.NullOrEmpty())
                {
                    cda.relevantSkills.AddRange(cda.Props.requiredSkillDefs);
                }
                if (!cda.Props.possibleExtraSkillDefs.NullOrEmpty())
                {
                    SkillDef extraSkill = cda.Props.possibleExtraSkillDefs.RandomElement();
                    cda.relevantSkills.Add(extraSkill);
                }
                cda.relevantStats = new List<StatDef>();
                if (!cda.Props.requiredStatDefs.NullOrEmpty())
                {
                    cda.relevantStats.AddRange(cda.Props.requiredStatDefs);
                }
                if (cda.Props.extraStat)
                {
                    float secondStatDeterminer = Rand.Value;
                    StatDef secondStat;
                    if (secondStatDeterminer <= 0.6f)
                    {
                        secondStat = cda.Props.possibleExtraStatDefsLikely.RandomElement();
                    } else if (secondStatDeterminer <= 0.9f) {
                        secondStat = cda.Props.possibleExtraStatDefsProbable.NullOrEmpty() ? cda.Props.possibleExtraStatDefsLikely.RandomElement() : cda.Props.possibleExtraStatDefsProbable.RandomElement();
                    } else {
                        secondStat = cda.Props.possibleExtraStatDefsUnlikely.NullOrEmpty() ? cda.Props.possibleExtraStatDefsLikely.RandomElement() : cda.Props.possibleExtraStatDefsUnlikely.RandomElement();
                    }
                    cda.relevantStats.Add(secondStat);
                }
                QuestGen.quest.AddPart(new QuestPart_LookAtThis(thing));
            }
        }
        [NoTranslate]
        public SlateRef<string> storeAs;
        public ThingDef thingDef;
    }
    public class RetroviralEffectDef : Def
    {
        public List<HediffDef> inflictedHediffPool;
        public bool addXenogene;
        public int removeXenogeneIfOverComplexity = -1;
        public List<ThingDef> droppedItemPool;
        public bool healWorstInjury;
        public HediffGiverSetDef inflictedChronicHediffPool;
        public IncidentDef inflictedDisease;
    }
    public class CompProperties_RetroviralInjection : CompProperties_UseEffect
    {
        public CompProperties_RetroviralInjection()
        {
            this.compClass = typeof(CompRetroviralInjection);
        }
        public float chanceForInjectionEffect;
        public List<RetroviralEffectDef> possibleInjectionEffects;
    }
    public class CompRetroviralInjection : CompTargetEffect
    {
        public CompProperties_RetroviralInjection Props
        {
            get
            {
                return (CompProperties_RetroviralInjection)this.props;
            }
        }
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (!user.IsColonistPlayerControlled)
            {
                return;
            }
            Job job = JobMaker.MakeJob(HVMP_BDefOf.HVMP_InjectRetroviralPackage, target, this.parent);
            job.count = 1;
            job.playerForced = true;
            user.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            this.effectOnInjection = Rand.Chance(this.Props.chanceForInjectionEffect) ? this.Props.possibleInjectionEffects.RandomElement() : null;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look<RetroviralEffectDef>(ref this.effectOnInjection, "effectOnInjection");
        }
        public RetroviralEffectDef effectOnInjection;
    }
    public class JobDriver_InjectRpackage : JobDriver
    {
        private Pawn Pawn
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
            return this.pawn.Reserve(this.Pawn, this.job, 1, -1, null, errorOnFailed, false) && this.pawn.Reserve(this.Item, this.job, 1, -1, null, errorOnFailed, false);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false, true, false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.A);
            Toil toil = Toils_General.Wait(600, TargetIndex.None);
            toil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            toil.tickIntervalAction = delegate(int delta)
            {
                CompUsable compUsable = this.Item.TryGetComp<CompUsable>();
                if (compUsable != null && this.warmupMote == null && compUsable.Props.warmupMote != null)
                {
                    this.warmupMote = MoteMaker.MakeAttachedOverlay(this.Pawn, compUsable.Props.warmupMote, Vector3.zero, 1f, -1f);
                }
                Mote mote = this.warmupMote;
                if (mote == null)
                {
                    return;
                }
                mote.Maintain();
            };
            yield return toil;
            yield return Toils_General.Do(new Action(this.DoInjectionEffects));
            yield break;
        }
        private void DoInjectionEffects()
        {
            CompRetroviralInjection rinject = this.Item.TryGetComp<CompRetroviralInjection>();
            if (rinject != null && this.Pawn != null)
            {
                SoundDefOf.MechSerumUsed.PlayOneShot(SoundInfo.InMap(this.Pawn, MaintenanceType.None));
                TaggedString taggedString = "";
                RetroviralEffectDef red = rinject.effectOnInjection;
                if (red != null)
                {
                    if (red.healWorstInjury)
                    {
                        taggedString = HealthUtility.FixWorstHealthCondition(this.Pawn, Array.Empty<HediffDef>());
                    }
                    if (!red.droppedItemPool.NullOrEmpty())
                    {
                        Thing thing = ThingMaker.MakeThing(red.droppedItemPool.RandomElement());
                        GenSpawn.Spawn(thing, this.Pawn.Position, this.Pawn.Map, WipeMode.Vanish);
                        taggedString = "HVMP_RetroviralItem".Translate(this.Pawn, thing.Label);
                    }
                    if (red.addXenogene && this.Pawn.genes != null)
                    {
                        int xenogeneComplexity = 0;
                        if (!this.Pawn.genes.Xenogenes.NullOrEmpty())
                        {
                            foreach (Gene g in this.Pawn.genes.Xenogenes)
                            {
                                xenogeneComplexity += g.def.biostatCpx;
                            }
                        }
                        if (red.removeXenogeneIfOverComplexity >= 0 && xenogeneComplexity > red.removeXenogeneIfOverComplexity)
                        {
                            if (!this.Pawn.genes.Xenogenes.NullOrEmpty())
                            {
                                List<Gene> nonArcGenes = this.Pawn.genes.Xenogenes.Where((Gene g) => g.def.biostatArc <= 0).ToList();
                                if (nonArcGenes.Count > 0)
                                {
                                    Gene toRemove = nonArcGenes.RandomElement();
                                    taggedString = "HVMP_RetroviralRemovedGene".Translate(this.Pawn, toRemove.Label);
                                    this.Pawn.genes.Xenogenes.Remove(toRemove);
                                }
                            }
                        } else {
                            List<GeneDef> possibleGenes = DefDatabase<GeneDef>.AllDefsListForReading.Where((GeneDef g) => g.biostatArc <= 0 && (red.removeXenogeneIfOverComplexity < 0 || g.biostatCpx + xenogeneComplexity <= red.removeXenogeneIfOverComplexity) && !this.Pawn.genes.HasActiveGene(g)).ToList();
                            if (!possibleGenes.NullOrEmpty())
                            {
                                GeneDef toAdd = possibleGenes.RandomElement();
                                taggedString = "HVMP_RetroviralGainedGene".Translate(this.Pawn, toAdd.label);
                                this.Pawn.genes.AddGene(toAdd, true);
                            }
                        }
                    }
                    if (!red.inflictedHediffPool.NullOrEmpty())
                    {
                        Hediff hediff = HediffMaker.MakeHediff(red.inflictedHediffPool.RandomElement(), this.Pawn);
                        this.Pawn.health.AddHediff(hediff);
                        taggedString = "HVMP_RetroviralHediff".Translate(this.Pawn, hediff.Label);
                    }
                    if (red.inflictedChronicHediffPool != null)
                    {
                        List<HediffGiver> hgivers = new List<HediffGiver>();
                        foreach (HediffGiver hg in red.inflictedChronicHediffPool.hediffGivers)
                        {
                            if (hg is HediffGiver_Birthday || hg is HediffGiver_RandomAgeCurved)
                            {
                                hgivers.Add(hg);
                            }
                        }
                        if (hgivers.Count > 0)
                        {
                            List<Hediff> oah = new List<Hediff>();
                            HediffGiver toGive = hgivers.RandomElement();
                            if (toGive.TryApply(this.Pawn, oah))
                            {
                                taggedString = "HVMP_RetroviralHediff".Translate(this.Pawn, oah[0].Label);
                            }
                        }
                    }
                    if (red.inflictedDisease != null)
                    {
                        IncidentDef incidentDef = red.inflictedDisease;
                        string text;
                        List<Pawn> list = ((IncidentWorker_Disease)incidentDef.Worker).ApplyToPawns(Gen.YieldSingle<Pawn>(this.pawn), out text);
                        if (PawnUtility.ShouldSendNotificationAbout(this.pawn))
                        {
                            if (list.Contains(this.pawn))
                            {
                                Find.LetterStack.ReceiveLetter("LetterLabelTraitDisease".Translate(incidentDef.diseaseIncident.label), "LetterTraitDisease".Translate(this.pawn.LabelCap, incidentDef.diseaseIncident.label, this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true), LetterDefOf.NegativeEvent, this.pawn, null, null, null, null, 0, true);
                            }
                            else if (!text.NullOrEmpty())
                            {
                                Messages.Message(text, this.pawn, MessageTypeDefOf.NeutralEvent, true);
                            }
                        }
                    }
                }
                if ((this.Pawn.Faction != null && this.pawn.Faction != null && this.Pawn.Faction != this.pawn.Faction) || this.Pawn.IsQuestLodger())
                {
                    Faction.OfPlayer.TryAffectGoodwillWith(this.Pawn.HomeFaction, -70, true, !this.Pawn.HomeFaction.temporary, HVMP_BDefOf.HVMP_PerformedHarmfulInjection, null);
                    QuestUtility.SendQuestTargetSignals(pawn.questTags, "SurgeryViolation", pawn.Named("SUBJECT"));
                }
                if (PawnUtility.ShouldSendNotificationAbout(this.Pawn))
                {
                    Messages.Message(taggedString, this.Pawn, MessageTypeDefOf.PositiveEvent, true);
                }
                if (!this.Item.questTags.NullOrEmpty())
                {
                    QuestUtility.SendQuestTargetSignals(this.Item.questTags, "StudiableItemFinished", this.Named("SUBJECT"));
                }
            }
        }
        private Mote warmupMote;
    }
    public class CompTargetable_AnyHumanlikeWillDo : CompTargetable
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
            TargetingParameters targetingParameters = new TargetingParameters();
            targetingParameters.canTargetPawns = true;
            targetingParameters.canTargetSelf = false;
            targetingParameters.canTargetItems = false;
            targetingParameters.canTargetBuildings = false;
            targetingParameters.mapObjectTargetsMustBeAutoAttackable = false;
            targetingParameters.validator = (TargetInfo x) => x.Thing is Pawn p && p.RaceProps.Humanlike;
            return targetingParameters;
        }
        public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
        {
            yield return targetChosenByPlayer;
            yield break;
        }
    }
}
