using HautsPermits;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace HautsPermits_Biotech
{
    /*almost QuestNode_GetLargestClearArea verbatim, buuuut also handles the mutator fw1: Coast to Coast increases size by C2C_factor or offsets it by C2C_minBonus, whichever is more inconvenient(ly big)
     * and can possibly be smaller by default. This determines the max radius of the preserve marker*/
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
            float baseSize = largestSize * (0.6f + (0.4f * Rand.Value));
            if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.fw1, HVMP_Mod.settings.fwX))
            {
                baseSize = Math.Max(baseSize * this.C2C_factor, baseSize + this.C2C_minBonus);
            }
            slate.Set<int>(this.storeAs.GetValue(slate), (int)baseSize, false);
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
            return Mathf.Min(new int[] { (int)(cellRect.Width / 2), (int)(cellRect.Height / 2), value });
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
        public int C2C_minBonus;
        public float C2C_factor = 1f;
    }
    /*make the preserve marker, and handle the other two mutators
     * fw2: Insects of Unusual Size gives the marker an IOUS_infestationMTBdays, which enables its infestation-spawning abilities
     * fw3: Season to Season: multiplies the marker's timer by S2S_factor*/
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
                bool mayhemMode = HVMP_Mod.settings.fwX;
                np.radius = slate.Get<int>("preserveRadius", 10, false);
                np.ticksRemaining = slate.Get<int>("preserveTicks", 900000, false);
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.fw3, mayhemMode))
                {
                    np.ticksRemaining = (int)(np.ticksRemaining * this.S2S_factor);
                }
                np.initialTicks = np.ticksRemaining;
                slate.Set<float>("preserveDays", np.ticksRemaining / 60000f, false);
                slate.Set<Thing>(this.storeAs.GetValue(slate), np, false);
                QuestGen.quest.AddPart(new QuestPart_LookAtThis(np));
                if (BranchQuestSetupUtility.MutatorEnabled(HVMP_Mod.settings.fw2, mayhemMode))
                {
                    np.IOUS_infestationMTBdays = this.IOUS_infestationMTBdays;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_IOUS_info", this.IOUS_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_IOUS_info", " ") });
                }
            }
        }
        [NoTranslate]
        public SlateRef<string> storeAs;
        public ThingDef thingDef;
        [NoTranslate]
        public SlateRef<string> storeReqProgressAs;
        public float IOUS_infestationMTBdays;
        [MustTranslate]
        public string IOUS_description;
        public float S2S_factor;
    }
    //i give you the marker now yay : )
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
    /*the marker's class. Handles the timer, the radius, the periodic scans (every 177 ticks) for whether or not there's any pollution or artificial buildings or floors or (somehow) another preserve marker,
     * and also the MTB infestations centered on self
     * Despite sharing a name with the Bad Juju mutator, AnyBadJuju and FirstBadJuju are wholly unrelated to the ARCHIVE Remnant quest*/
    [StaticConstructorOnStartup]
    public class NaturePreserve : Thing
    {
        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (this.Spawned && this.IsHashIntervalTick(177, delta))
            {
                if (this.IOUS_infestationMTBdays > 0f && this.Spawned && Rand.MTBEventOccurs(this.IOUS_infestationMTBdays, 60000f, 177))
                {
                    IncidentParms incidentParms = new IncidentParms();
                    incidentParms.target = this.MapHeld;
                    incidentParms.points = StorytellerUtility.DefaultThreatPointsNow(this.MapHeld);
                    incidentParms.infestationLocOverride = new IntVec3?(this.PositionHeld);
                    incidentParms.forced = true;
                    IncidentDefOf.Infestation.Worker.TryExecute(incidentParms);
                }
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
                } else {
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
            Scribe_Values.Look<float>(ref this.IOUS_infestationMTBdays, "IOUS_infestationMTBdays", -1f, false);
        }
        public float radius;
        public int ticksRemaining;
        public int initialTicks;
        public int ticksSinceDisallowedAnything;
        public float IOUS_infestationMTBdays;
    }
    //you can't place down a nature preserve if that would put a floor, pollution, an artificial building, or another preserve marker in its radius
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
                if (center.CloseToEdge(map, (int)np.radius))
                {
                    return "HVMPCannotPlacePreserve".Translate();
                }
                bool badJuju = false;
                List<Thing> forCell = map.listerArtificialBuildingsForMeditation.GetForCell(center, np.radius);
                forCell.AddRange(map.listerThings.ThingsOfDef(HVMP_BDefOf.HVMP_PreserveMarker).Where((Thing t) => t.Position.DistanceTo(center) <= np.radius));
                if (forCell.Count > 0)
                {
                    badJuju = true;
                } else {
                    foreach (Thing t in map.listerThings.ThingsOfDef(HVMP_BDefOf.HVMP_PreserveMarker))
                    {
                        if (t.Spawned && t is NaturePreserve np2 && np2.Position.DistanceTo(center) <= np2.radius)
                        {
                            badJuju = true;
                            break;
                        }
                    }
                    if (!badJuju)
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
                }
                if (badJuju)
                {
                    return "HVMPCannotPlacePreserve".Translate();
                }
            }
            return true;
        }
    }
    /*ARGH PERFORMANCE. I think this is about as performant as this alert can be, but having to search all maps' listerThings is still one of the pain points in HEMP. I guess alerts in general are, especially ones about buildings.
     * If a Nature Preserve in any map has turned on what is basically its "fuck, I'm in distress" flag, you get the alert. Thankfully this is effectively cached and only reupdated every so often.*/
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
}
