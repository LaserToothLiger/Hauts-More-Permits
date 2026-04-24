using DelaunatorSharp;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace HautsPermits_Occult
{
    /*Anomaly is a really good DLC that was really well thought out. You can tell this is true because of how much crap is hardcoded.
     * Something seems cool and you want to remix it? Fuck you, it's hardcoded. Make it again from SCRATCH if you want to change like, two goddamn lines.
     *                                     anyways
     * When you visit WorldObject_Hypercube, it pulls you into its gray labyrinth map. As per the "extradimensional" fluff about it, this map does not actually correspond to the Hypercube's world tile. It doesn't have one.
     * It also has variables of interest for all three Natali mutators; in particular, if AOEAH_condition is assigned, it will register that as a permanent condition in the labyrinth.
     * Only one visitation works. Once the map has been initialized, further visitations will not bring the visitors into the map. Hence the warning label that appears in the inspection tab.*/
    public class WorldObject_Hypercube : WorldObject
    {
        public override Material Material
        {
            get
            {
                if (this.cachedMat == null)
                {
                    this.cachedMat = MaterialPool.MatFrom(this.def.texture, ShaderDatabase.WorldOverlayTransparentLit, Color.white, WorldMaterials.WorldObjectRenderQueue);
                }
                return this.cachedMat;
            }
        }
        public override string GetInspectString()
        {
            return base.GetInspectString() + "HVMP_OnlyOnePartyLabel".Translate();
        }
        public void Notify_CaravanArrived(Caravan caravan)
        {
            if (this.labyrinthMap != null)
            {
                Messages.Message("HVMP_LabyrinthAlreadyGenerated".Translate(), this, MessageTypeDefOf.RejectInput, false);
                return;
            }
            this.generating = true;
            LongEventHandler.QueueLongEvent(delegate
            {
                if (this.labyrinthMap == null)
                {
                    this.labyrinthMap = PocketMapUtility.GeneratePocketMap(new IntVec3(90, 1, 90), HVMPDefOf_A.HVMP_Hypercube, null, Find.AnyPlayerHomeMap);
                }
            }, "GeneratingLabyrinth", true, new Action<Exception>(GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap), false, false, delegate
            {
                this.generating = false;
                HVMP_HypercubeMapComponent lmc = this.labyrinthMap.GetComponent<HVMP_HypercubeMapComponent>();
                if (lmc != null)
                {
                    lmc.spatialAnomaly = this;
                    List<Pawn> pawns = new List<Pawn>();
                    foreach (Pawn p in caravan.pawns)
                    {
                        pawns.Add(p);
                    }
                    CaravanEnterMapUtility.Enter(caravan, this.labyrinthMap, (Pawn p) => CellFinder.RandomClosewalkCellNear(lmc.GetDropPosition(), this.labyrinthMap, 12, null), CaravanDropInventoryMode.DoNotDrop, true);
                    foreach (Pawn p in pawns)
                    {
                        lmc.TeleportToLabyrinth(p);
                    }
                    if (this.AOEAH_condition != null && !this.labyrinthMap.gameConditionManager.ConditionIsActive(this.AOEAH_condition))
                    {
                        GameCondition gameCondition = GameConditionMaker.MakeCondition(this.AOEAH_condition, 99999);
                        gameCondition.Permanent = true;
                        if (gameCondition.CanApplyOnMap(this.labyrinthMap))
                        {
                            this.labyrinthMap.gameConditionManager.RegisterCondition(gameCondition);
                            Find.LetterStack.ReceiveLetter(gameCondition.LabelCap, gameCondition.LetterText, gameCondition.def.letterDef, LookTargets.Invalid, null, null, null, null, 0, true);
                        }
                    }
                    Find.TickManager.Pause();
                }
                IEnumerable<Building> obelisksE = this.labyrinthMap.listerBuildings.AllBuildingsNonColonistOfDef(HVMPDefOf_A.HVMP_WarpedObelisk_Hypercube);
                if (obelisksE != null)
                {
                    List<Building> obelisks = obelisksE.ToList();
                    foreach (Building b in obelisks)
                    {
                        CompObelisk_Hypercube coh = b.TryGetComp<CompObelisk_Hypercube>();
                        if (coh != null)
                        {
                            coh.spatialAnomaly = this;
                        }
                    }
                }
            });
        }
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(caravan))
            {
                yield return floatMenuOption;
            }
            foreach (FloatMenuOption floatMenuOption2 in CaravanArrivalAction_VisitHypercube.GetFloatMenuOptions(caravan, this))
            {
                yield return floatMenuOption2;
            }
            yield break;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Map>(ref this.labyrinthMap, "labyrinthMap", false);
            Scribe_Defs.Look<GameConditionDef>(ref this.AOEAH_condition, "AOEAH_condition");
            Scribe_Values.Look<bool>(ref this.COL_on, "COL_on", false, false);
            Scribe_Defs.Look<PawnKindDef>(ref this.DF_monsterKind, "DF_monsterKind");
        }
        private Material cachedMat;
        public Map labyrinthMap;
        public GameConditionDef AOEAH_condition;
        public bool COL_on;
        public PawnKindDef DF_monsterKind;
        private bool generating;

    }
    public class CaravanArrivalAction_VisitHypercube : CaravanArrivalAction
    {
        public override string Label
        {
            get
            {
                return "VisitPeaceTalks".Translate(this.hypercube.Label);
            }
        }
        public override string ReportString
        {
            get
            {
                return "CaravanVisiting".Translate(this.hypercube.Label);
            }
        }
        public CaravanArrivalAction_VisitHypercube()
        {
        }
        public CaravanArrivalAction_VisitHypercube(WorldObject_Hypercube hypercube)
        {
            this.hypercube = hypercube;
        }
        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(caravan, destinationTile);
            if (!floatMenuAcceptanceReport)
            {
                return floatMenuAcceptanceReport;
            }
            if (this.hypercube != null && this.hypercube.Tile != destinationTile)
            {
                return false;
            }
            return CaravanArrivalAction_VisitHypercube.CanVisit(caravan, this.hypercube);
        }
        public override void Arrived(Caravan caravan)
        {
            this.hypercube.Notify_CaravanArrived(caravan);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<WorldObject_Hypercube>(ref this.hypercube, "hypercube", false);
        }
        public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, WorldObject_Hypercube hypercube)
        {
            return hypercube != null && hypercube.Spawned;
        }
        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, WorldObject_Hypercube hypercube)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions<CaravanArrivalAction_VisitHypercube>(() => CaravanArrivalAction_VisitHypercube.CanVisit(caravan, hypercube), () => new CaravanArrivalAction_VisitHypercube(hypercube), "VisitPeaceTalks".Translate(hypercube.Label), caravan, hypercube.Tile, hypercube, null);
        }
        private WorldObject_Hypercube hypercube;
    }
    /*"Why, isn't this almost identical to LabyrinthMapComponent?" You are very perceptive and astute, and I am very tired. Anomaly is many peoples' least favorite DLC because of its lack of content and replayability.
     *   Anomaly is my least favorite DLC because of the limited backend extensibility. We are not the same.
     * The chief difference is that there is no abductorObelisk, so pawns get teleported back to a player home map. If none exists, NOBODY LEAVES, but that should be a remarkably rare and stupid occurrence.
     * Also, instead of destroying the obelisk, we destroy the world object, obviously.
     * Also also, TeleportToLabyrinth gains a "statue" bool field. If you invoke it, it will teleport the target to a random room instead of its own position*/
    public class HVMP_HypercubeMapComponent : CustomMapComponent
    {
        public HVMP_HypercubeMapComponent(Map map)
            : base(map)
        {
        }
        public override void MapComponentTick()
        {
            if (!this.closing && GenTicks.IsTickInterval(300) && !this.map.mapPawns.AnyColonistSpawned)
            {
                PocketMapUtility.DestroyPocketMap(this.map);
            }
            this.TeleportPawnsClosing();
        }
        private void TeleportPawnsClosing()
        {
            if (!this.closing || GenTicks.TicksGame < this.nextTeleportTick)
            {
                return;
            }
            Map dest = null;
            this.nextTeleportTick = GenTicks.TicksGame + HVMP_HypercubeMapComponent.TeleportDelayTicks.RandomInRange;
            foreach (Map map in Find.Maps)
            {
                if (map.IsPlayerHome)
                {
                    dest = map;
                    break;
                }
            }
            if (dest == null)
            {
                return;
            }
            IntVec3 intVec;
            if (!CellFinderLoose.TryGetRandomCellWith((IntVec3 pos) => HVMP_HypercubeMapComponent.IsValidTeleportCell(pos, dest), dest, 1000, out intVec))
            {
                return;
            }
            using (List<Pawn>.Enumerator enumerator2 = this.map.mapPawns.AllPawns.GetEnumerator())
            {
                if (enumerator2.MoveNext())
                {
                    Pawn pawn = enumerator2.Current;
                    Pawn pawn2;
                    if ((pawn2 = SkipUtility.SkipTo(pawn, intVec, dest) as Pawn) != null && PawnUtility.ShouldSendNotificationAbout(pawn2))
                    {
                        Messages.Message("MessagePawnReappeared".Translate(pawn2.Named("PAWN")), pawn2, MessageTypeDefOf.NeutralEvent, false);
                    }
                    pawn.inventory.UnloadEverything = true;
                    return;
                }
            }
            foreach (Thing thing in (IEnumerable<Thing>)this.map.spawnedThings)
            {
                if (thing.def.category == ThingCategory.Item)
                {
                    SkipUtility.SkipTo(thing, intVec, dest);
                    return;
                }
            }
            Find.LetterStack.ReceiveLetter("LetterLabelLabyrinthExit".Translate(), "LetterLabyrinthExit".Translate(), LetterDefOf.NeutralEvent, null, 0, true);
            PocketMapUtility.DestroyPocketMap(this.map);
            if (this.spatialAnomaly != null)
            {
                this.spatialAnomaly.Destroy();
            }
        }
        private static bool IsValidTeleportCell(IntVec3 pos, Map dest)
        {
            return !pos.Fogged(dest) && pos.Standable(dest) && dest.reachability.CanReachColony(pos);
        }
        public void SetSpawnRooms(List<LayoutRoom> rooms)
        {
            this.spawnableRooms = rooms;
        }
        public void StartClosing()
        {
            this.closing = true;
        }
        public Thing TeleportToLabyrinth(Thing thing, bool statue = false)
        {
            IntVec3 dropPosition = this.GetDropPosition();
            Thing thing2 = SkipUtility.SkipTo(thing, (thing.Spawned && thing.Map == this.map && !statue) ? thing.Position : dropPosition, this.map);
            Pawn pawn;
            if ((pawn = thing as Pawn) != null)
            {
                Pawn_NeedsTracker needs = pawn.needs;
                if (needs != null)
                {
                    Need_Mood mood = needs.mood;
                    if (mood != null)
                    {
                        ThoughtHandler thoughts = mood.thoughts;
                        if (thoughts != null)
                        {
                            MemoryThoughtHandler memories = thoughts.memories;
                            if (memories != null)
                            {
                                memories.TryGainMemory(ThoughtDefOf.ObeliskAbduction, null, null);
                            }
                        }
                    }
                }
                if (PawnUtility.ShouldSendNotificationAbout(pawn))
                {
                    Messages.Message("MessagePawnVanished".Translate(pawn.Named("PAWN")), thing2, MessageTypeDefOf.NeutralEvent, false);
                }
            }
            return thing2;
        }
        public IntVec3 GetDropPosition()
        {
            foreach (CellRect cellRect in this.spawnableRooms.RandomElement<LayoutRoom>().rects)
            {
                HVMP_HypercubeMapComponent.tmpCells.AddRange(cellRect.ContractedBy(2));
            }
            IntVec3 intVec = HVMP_HypercubeMapComponent.tmpCells.RandomElement<IntVec3>();
            HVMP_HypercubeMapComponent.tmpCells.Clear();
            return CellFinder.StandableCellNear(intVec, this.map, 5f, null);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.closing, "closing", false, false);
            Scribe_Values.Look<int>(ref this.nextTeleportTick, "nextTeleportTick", 0, false);
            Scribe_References.Look<Building>(ref this.labyrinthObelisk, "labyrinthObelisk", false);
            Scribe_Deep.Look<LayoutStructureSketch>(ref this.structureSketch, "structureSketch", Array.Empty<object>());
            Scribe_Collections.Look<LayoutRoom>(ref this.spawnableRooms, "spawnableRects", LookMode.Deep, Array.Empty<object>());
            Scribe_References.Look<WorldObject>(ref this.spatialAnomaly, "spatialAnomaly", false);
        }
        public Building labyrinthObelisk;
        private LayoutStructureSketch structureSketch;
        private List<LayoutRoom> spawnableRooms;
        private bool closing;
        private int nextTeleportTick;
        private static readonly IntRange TeleportDelayTicks = new IntRange(6, 60);
        private const int IntervalCheckCloseTicks = 300;
        private static readonly List<IntVec3> tmpCells = new List<IntVec3>();
        public WorldObject spatialAnomaly;
    }
    /*GenStep_Labyrinth but we make our own bespoke exit obelisk instead of the normal one. Because the normal one is inextricably linked to the usual abductor obelisk.
     * Also, all other references to the regular labyrinth stuff have to be subbed out for the appropriate Hypercube stuff instead*/
    public class GenStep_ImTiredOfBeinCalmAllTheGaddamnTime : GenStep
    {
        public override int SeedPart
        {
            get
            {
                return 8797469;
            }
        }
        public override void Generate(Map map, GenStepParams parms)
        {
            if (!ModLister.CheckAnomaly("Labyrinth"))
            {
                return;
            }
            TerrainGrid terrainGrid = map.terrainGrid;
            foreach (IntVec3 intVec in map.AllCells)
            {
                terrainGrid.SetTerrain(intVec, TerrainDefOf.GraySurface);
            }
            CellRect cellRect = map.BoundsRect(0);
            GenStep_ImTiredOfBeinCalmAllTheGaddamnTime.FillEdges(cellRect, map);
            StructureGenParams structureGenParams = new StructureGenParams
            {
                size = cellRect.ContractedBy(2).Size
            };
            LayoutWorker worker = HVMPDefOf_A.HVMP_HypercubeLayout.Worker;
            int num = 10;
            do
            {
                this.structureSketch = worker.GenerateStructureSketch(structureGenParams);
            } while (!this.structureSketch.structureLayout.HasRoomWithDef(HVMPDefOf_A.HVMP_HypercubeObelisk) && num-- > 0);
            if (num == 0)
            {
                Log.ErrorOnce("Failed to generate labyrinth, guard exceeded. Check layout worker for errors placing minimum rooms", 9868797);
                return;
            }
            worker.Spawn(this.structureSketch, map, new IntVec3(2, 0, 2), null, null, false, false, null);
            map.layoutStructureSketches.Add(this.structureSketch);
            HVMP_HypercubeMapComponent component = map.GetComponent<HVMP_HypercubeMapComponent>();
            LayoutRoom firstRoomOfDef = this.structureSketch.structureLayout.GetFirstRoomOfDef(HVMPDefOf_A.HVMP_HypercubeObelisk);
            List<LayoutRoom> spawnableRooms = this.GetSpawnableRooms(firstRoomOfDef);
            component.SetSpawnRooms(spawnableRooms);
            MapGenerator.PlayerStartSpot = IntVec3.Zero;
            map.fogGrid.Refog(new CellRect(-1, -1, 999, 999).ClipInsideMap(map));
        }
        private List<LayoutRoom> GetSpawnableRooms(LayoutRoom obelisk)
        {
            List<LayoutRoom> list = new List<LayoutRoom>();
            list.AddRange(this.structureSketch.structureLayout.Rooms);
            list.Remove(obelisk);
            foreach (ValueTuple<LayoutRoom, CellRect, CellRect> valueTuple in this.structureSketch.structureLayout.GetLogicalRoomConnections(obelisk))
            {
                LayoutRoom item = valueTuple.Item1;
                if (list.Contains(item))
                {
                    list.Remove(item);
                    foreach (ValueTuple<LayoutRoom, CellRect, CellRect> valueTuple2 in this.structureSketch.structureLayout.GetLogicalRoomConnections(item))
                    {
                        LayoutRoom item2 = valueTuple2.Item1;
                        if (list.Contains(item2))
                        {
                            list.Remove(item2);
                        }
                    }
                }
            }
            for (int i = list.Count - 1; i >= 0; i--)
            {
                using (List<LayoutRoomDef>.Enumerator enumerator3 = list[i].defs.GetEnumerator())
                {
                    while (enumerator3.MoveNext())
                    {
                        if (!enumerator3.Current.isValidPlayerSpawnRoom)
                        {
                            list.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            if (list.Empty<LayoutRoom>())
            {
                list.Clear();
                list.AddRange(this.structureSketch.structureLayout.Rooms);
                list.Remove(obelisk);
            }
            return list;
        }
        private static void FillEdges(CellRect rect, Map map)
        {
            for (int i = 0; i < rect.Width; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    GenStep_ImTiredOfBeinCalmAllTheGaddamnTime.SpawnWall(new IntVec3(i, 0, j), map);
                    GenStep_ImTiredOfBeinCalmAllTheGaddamnTime.SpawnWall(new IntVec3(i, 0, rect.Height - j - 1), map);
                }
            }
            for (int k = 2; k < rect.Height - 2; k++)
            {
                for (int l = 0; l < 2; l++)
                {
                    GenStep_ImTiredOfBeinCalmAllTheGaddamnTime.SpawnWall(new IntVec3(l, 0, k), map);
                    GenStep_ImTiredOfBeinCalmAllTheGaddamnTime.SpawnWall(new IntVec3(rect.Width - l - 1, 0, k), map);
                }
            }
        }
        private static void SpawnWall(IntVec3 pos, Map map)
        {
            GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.GrayWall, ThingDefOf.LabyrinthMatter), pos, map, WipeMode.Vanish);
        }
        private LayoutStructureSketch structureSketch;
    }
    /*at this point you should not be surprised to learn this is nearly a replica of LayoutWorker_Labyrinth
     * extreeeeemely identical, literally just subbing out the generation of an exit obelisk for the new exit obelisk (which I might request a custom art asset for that is no longer an obelisk? Time will tell)*/
    public class HVMP_LayoutWorkerHypercube : LayoutWorker
    {
        public HVMP_LayoutWorkerHypercube(LayoutDef def)
            : base(def)
        {
        }
        protected override LayoutSketch GenerateSketch(StructureGenParams parms)
        {
            if (!ModLister.CheckAnomaly("Labyrinth"))
            {
                return null;
            }
            LayoutSketch layoutSketch = new LayoutSketch
            {
                wall = ThingDefOf.GrayWall,
                door = ThingDefOf.GrayDoor,
                floor = TerrainDefOf.GraySurface,
                defaultAffordanceTerrain = TerrainDefOf.GraySurface,
                wallStuff = ThingDefOf.LabyrinthMatter,
                doorStuff = ThingDefOf.LabyrinthMatter
            };
            using (new ProfilerBlock("Generate Labyrinth"))
            {
                layoutSketch.structureLayout = this.GenerateLabyrinth(parms);
            }
            return layoutSketch;
        }
        private StructureLayout GenerateLabyrinth(StructureGenParams parms)
        {
            CellRect cellRect = new CellRect(0, 0, parms.size.x, parms.size.z);
            StructureLayout structureLayout = new StructureLayout(parms.sketch, cellRect);
            HVMP_LayoutWorkerHypercube.PlaceObeliskRoom(cellRect, structureLayout);
            using (new ProfilerBlock("Scatter L Rooms"))
            {
                HVMP_LayoutWorkerHypercube.ScatterLRooms(cellRect, structureLayout);
            }
            using (new ProfilerBlock("Scatter Square Rooms"))
            {
                HVMP_LayoutWorkerHypercube.ScatterSquareRooms(cellRect, structureLayout);
            }
            using (new ProfilerBlock("Generate Graphs"))
            {
                HVMP_LayoutWorkerHypercube.GenerateGraphs(structureLayout);
            }
            structureLayout.FinalizeRooms(false);
            using (new ProfilerBlock("Create Doors"))
            {
                HVMP_LayoutWorkerHypercube.CreateDoors(structureLayout);
            }
            using (new ProfilerBlock("Create Corridors"))
            {
                HVMP_LayoutWorkerHypercube.CreateCorridorsAStar(structureLayout);
            }
            using (new ProfilerBlock("Fill Empty Spaces"))
            {
                HVMP_LayoutWorkerHypercube.FillEmptySpaces(structureLayout);
            }
            return structureLayout;
        }
        private static void FillEmptySpaces(StructureLayout layout)
        {
            HashSet<IntVec3> hashSet = new HashSet<IntVec3>();
            foreach (IntVec3 intVec in layout.container.Cells)
            {
                if (layout.IsEmptyAt(intVec) && !hashSet.Contains(intVec))
                {
                    foreach (IntVec3 intVec2 in GenAdjFast.AdjacentCells8Way(intVec))
                    {
                        if (layout.IsWallAt(intVec2))
                        {
                            hashSet.Add(intVec);
                            break;
                        }
                    }
                }
            }
            foreach (IntVec3 intVec3 in hashSet)
            {
                layout.Add(intVec3, RoomLayoutCellType.Wall);
            }
        }
        private static void GenerateGraphs(StructureLayout layout)
        {
            List<Vector2> list = new List<Vector2>();
            foreach (LayoutRoom layoutRoom in layout.Rooms)
            {
                Vector3 vector = Vector3.zero;
                foreach (CellRect cellRect in layoutRoom.rects)
                {
                    vector += cellRect.CenterVector3;
                }
                vector /= (float)layoutRoom.rects.Count;
                list.Add(new Vector2(vector.x, vector.z));
            }
            layout.delaunator = new Delaunator(list.ToArray());
            layout.neighbours = new RelativeNeighborhoodGraph(layout.delaunator);
        }
        private static void PlaceObeliskRoom(CellRect size, StructureLayout layout)
        {
            int num = Rand.Range(0, size.Width - 19);
            int num2 = Rand.Range(0, size.Height - 19);
            CellRect cellRect = new CellRect(num, num2, 19, 19);
            LayoutRoom layoutRoom = layout.AddRoom(new List<CellRect> { cellRect });
            layoutRoom.requiredDef = HVMPDefOf_A.HVMP_HypercubeObelisk;
            layoutRoom.entryCells = new List<IntVec3>();
            layoutRoom.entryCells.AddRange(cellRect.GetCenterCellsOnEdge(Rot4.North, 2));
            layoutRoom.entryCells.AddRange(cellRect.GetCenterCellsOnEdge(Rot4.East, 2));
            layoutRoom.entryCells.AddRange(cellRect.GetCenterCellsOnEdge(Rot4.South, 2));
            layoutRoom.entryCells.AddRange(cellRect.GetCenterCellsOnEdge(Rot4.West, 2));
        }
        private static void ScatterLRooms(CellRect size, StructureLayout layout)
        {
            int randomInRange = HVMP_LayoutWorkerHypercube.LShapeRoomRange.RandomInRange;
            int num = 0;
            int num2 = 0;
            while (num2 < 100 && num < randomInRange)
            {
                int randomInRange2 = HVMP_LayoutWorkerHypercube.RoomSizeRange.RandomInRange;
                int randomInRange3 = HVMP_LayoutWorkerHypercube.RoomSizeRange.RandomInRange;
                int num3 = Rand.Range(0, size.Width - randomInRange2);
                int num4 = Rand.Range(0, size.Height - randomInRange3);
                int num5 = HVMP_LayoutWorkerHypercube.LShapeRoomRange.RandomInRange;
                int num6 = HVMP_LayoutWorkerHypercube.LShapeRoomRange.RandomInRange;
                while (Mathf.Abs(num5 - randomInRange2) <= 2)
                {
                    num5 = HVMP_LayoutWorkerHypercube.LShapeRoomRange.RandomInRange;
                }
                while (Mathf.Abs(num6 - randomInRange3) <= 2)
                {
                    num6 = HVMP_LayoutWorkerHypercube.LShapeRoomRange.RandomInRange;
                }
                CellRect cellRect = new CellRect(num3, num4, randomInRange2, randomInRange3);
                CellRect cellRect2;
                if (Rand.Bool)
                {
                    cellRect2 = new CellRect(cellRect.maxX, cellRect.maxZ - num6 + 1, num5, num6);
                } else {
                    cellRect2 = new CellRect(cellRect.minX - num5, cellRect.minZ, num5 + 1, num6);
                }
                if (cellRect2.Width >= 4 && cellRect2.Height >= 4 && size.FullyContainedWithin(cellRect2) && !HVMP_LayoutWorkerHypercube.OverlapsWithAnyRoom(layout, cellRect) && !HVMP_LayoutWorkerHypercube.OverlapsWithAnyRoom(layout, cellRect2))
                {
                    layout.AddRoom(new List<CellRect> { cellRect, cellRect2 });
                    num++;
                }
                num2++;
            }
        }
        private static void ScatterSquareRooms(CellRect size, StructureLayout layout)
        {
            int randomInRange = HVMP_LayoutWorkerHypercube.RoomRange.RandomInRange;
            int num = 0;
            int num2 = 0;
            while (num2 < 300 && num < randomInRange)
            {
                int randomInRange2 = HVMP_LayoutWorkerHypercube.RoomSizeRange.RandomInRange;
                int randomInRange3 = HVMP_LayoutWorkerHypercube.RoomSizeRange.RandomInRange;
                int num3 = Rand.Range(0, size.Width - randomInRange2);
                int num4 = Rand.Range(0, size.Height - randomInRange3);
                CellRect cellRect = new CellRect(num3, num4, randomInRange2, randomInRange3);
                if (!HVMP_LayoutWorkerHypercube.OverlapsWithAnyRoom(layout, cellRect))
                {
                    layout.AddRoom(new List<CellRect> { cellRect });
                    num++;
                }
                num2++;
            }
        }
        private static void CreateCorridorsAStar(StructureLayout layout)
        {
            foreach (LayoutRoom layoutRoom in layout.Rooms)
            {
                foreach (ValueTuple<LayoutRoom, CellRect, CellRect> valueTuple in layout.GetLogicalRoomConnections(layoutRoom))
                {
                    LayoutRoom item = valueTuple.Item1;
                    if (!layoutRoom.connections.Contains(item))
                    {
                        HVMP_LayoutWorkerHypercube.ConnectRooms(layout, layoutRoom, item);
                    }
                }
            }
        }
        private static void ConnectRooms(StructureLayout layout, LayoutRoom a, LayoutRoom b)
        {
            PriorityQueue<ValueTuple<IntVec3, IntVec3>, int> priorityQueue = new PriorityQueue<ValueTuple<IntVec3, IntVec3>, int>();
            foreach (CellRect cellRect in a.rects)
            {
                foreach (CellRect cellRect2 in b.rects)
                {
                    IEnumerable<IntVec3> enumerable = a.entryCells;
                    foreach (IntVec3 intVec in (enumerable ?? cellRect.EdgeCells))
                    {
                        if (!cellRect.IsCorner(intVec) && !cellRect2.Contains(intVec))
                        {
                            Rot4 closestEdge = cellRect.GetClosestEdge(intVec);
                            enumerable = b.entryCells;
                            foreach (IntVec3 intVec2 in (enumerable ?? cellRect2.EdgeCells))
                            {
                                if (!cellRect2.IsCorner(intVec2) && !cellRect.Contains(intVec2))
                                {
                                    Rot4 closestEdge2 = cellRect2.GetClosestEdge(intVec2);
                                    int num = (intVec2 - intVec).LengthManhattan;
                                    RotationDirection relativeRotation = Rot4.GetRelativeRotation(closestEdge, closestEdge2);
                                    if (closestEdge == Rot4.East && intVec2.x < cellRect.maxX)
                                    {
                                        num += 4;
                                    } else if (closestEdge == Rot4.West && intVec2.x > cellRect.minX) {
                                        num += 4;
                                    }
                                    if (closestEdge == Rot4.North && intVec2.z < cellRect.maxZ)
                                    {
                                        num += 4;
                                    } else if (closestEdge == Rot4.South && intVec2.z > cellRect.minZ) {
                                        num += 4;
                                    }
                                    if (relativeRotation == RotationDirection.Clockwise || relativeRotation == RotationDirection.Counterclockwise)
                                    {
                                        num++;
                                    } else if (relativeRotation == RotationDirection.None) {
                                        num += 2;
                                    }
                                    priorityQueue.Enqueue(new ValueTuple<IntVec3, IntVec3>(intVec, intVec2), num);
                                }
                            }
                        }
                    }
                }
            }
            ValueTuple<IntVec3, IntVec3> valueTuple;
            while (priorityQueue.TryDequeue(out valueTuple, out int num2))
            {
                IntVec3 item = valueTuple.Item1;
                IntVec3 item2 = valueTuple.Item2;
                if (HVMP_LayoutWorkerHypercube.TryGetPath(layout, item, item2, num2 * 2, out List<IntVec3> list))
                {
                    IntVec3 intVec3 = item2 - item;
                    if (Mathf.Max(Mathf.Abs(intVec3.x), Mathf.Abs(intVec3.z)) <= 4)
                    {
                        layout.Add(item, RoomLayoutCellType.Floor);
                        layout.Add(item2, RoomLayoutCellType.Floor);
                        HVMP_LayoutWorkerHypercube.InflatePath(layout, list, 1);
                        int num3 = 1;
                        if (list.Count == 1 || !layout.IsGoodForDoor(list[num3]))
                        {
                            num3 = 0;
                        }
                        if (a.requiredDef == HVMPDefOf_A.HVMP_HypercubeObelisk)
                        {
                            layout.Add(item, RoomLayoutCellType.Door);
                        } else if (b.requiredDef == HVMPDefOf_A.HVMP_HypercubeObelisk) {
                            layout.Add(item2, RoomLayoutCellType.Door);
                        } else {
                            layout.Add(list[num3], RoomLayoutCellType.Door);
                        }
                    } else {
                        layout.Add(item, RoomLayoutCellType.Door);
                        layout.Add(item2, RoomLayoutCellType.Door);
                        HVMP_LayoutWorkerHypercube.InflatePath(layout, list, Mathf.Min(Mathf.Max(1, Mathf.CeilToInt((float)list.Count / 3f)), 3));
                    }
                    a.connections.Add(b);
                    b.connections.Add(a);
                    return;
                }
            }
        }
        private static void InflatePath(StructureLayout layout, List<IntVec3> cells, int levels)
        {
            Queue<IntVec3> queue = new Queue<IntVec3>();
            HashSet<IntVec3> hashSet = new HashSet<IntVec3>();
            IntVec3 intVec = cells[0];
            IntVec3 last = cells.GetLast<IntVec3>();
            IntVec3 intVec2 = new IntVec3(Mathf.Min(intVec.x, last.x), 0, Mathf.Min(intVec.z, last.z));
            IntVec3 intVec3 = new IntVec3(Mathf.Max(intVec.x, last.x), 0, Mathf.Max(intVec.z, last.z));
            CellRect cellRect = new CellRect
            {
                minX = intVec2.x,
                minZ = intVec2.z,
                maxX = intVec3.x,
                maxZ = intVec3.z
            };
            cellRect = cellRect.ExpandedBy(levels);
            using (List<IntVec3>.Enumerator enumerator = cells.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    IntVec3 intVec4 = enumerator.Current;
                    if (layout.IsEmptyAt(intVec4))
                    {
                        queue.Enqueue(intVec4);
                        break;
                    }
                }
                goto IL_19B;
            }
        IL_103:
            IntVec3 intVec5 = queue.Dequeue();
            bool flag = cellRect.IsOnEdge(intVec5) && !cells.Contains(intVec5);
            layout.Add(intVec5, (!flag) ? RoomLayoutCellType.Floor : RoomLayoutCellType.Wall);
            if (!flag)
            {
                foreach (IntVec3 intVec6 in HVMP_LayoutWorkerHypercube.Neighbours8Way(layout, intVec5))
                {
                    if (layout.IsEmptyAt(intVec6) && cellRect.Contains(intVec6) && !hashSet.Contains(intVec6))
                    {
                        queue.Enqueue(intVec6);
                        hashSet.Add(intVec6);
                    }
                }
            }
        IL_19B:
            if (queue.Count == 0)
            {
                bool flag2;
                do
                {
                    flag2 = false;
                    foreach (IntVec3 intVec7 in hashSet)
                    {
                        if (!layout.IsWallAt(intVec7) && !cells.Contains(intVec7) && HVMP_LayoutWorkerHypercube.CountAdjacentWalls(layout, intVec7) == 3)
                        {
                            layout.Add(intVec7, RoomLayoutCellType.Wall);
                            flag2 = true;
                        }
                    }
                } while (flag2);
                foreach (IntVec3 intVec8 in cellRect.ExpandedBy(1).EdgeCells)
                {
                    LayoutRoom layoutRoom;
                    if (intVec8.x > 0 && intVec8.z > 0 && intVec8.x < layout.Width && intVec8.z < layout.Height && !layout.TryGetRoom(intVec8, out layoutRoom) && layout.IsEmptyAt(intVec8))
                    {
                        layout.Add(intVec8, RoomLayoutCellType.Wall);
                    }
                }
                return;
            }
            goto IL_103;
        }
        private static int CountAdjacentWalls(StructureLayout layout, IntVec3 cell)
        {
            int num = 0;
            for (int i = 0; i < 4; i++)
            {
                IntVec3 intVec = cell + new Rot4(i).FacingCell;
                if (intVec.x > 0 && intVec.z > 0 && intVec.x < layout.Width && intVec.z < layout.Height && layout.IsWallAt(intVec))
                {
                    num++;
                }
            }
            return num;
        }
        private static List<IntVec3> ReconstructPath(Dictionary<IntVec3, IntVec3> from, IntVec3 current)
        {
            List<IntVec3> list = new List<IntVec3> { current };
            while (from.ContainsKey(current))
            {
                current = from[current];
                list.Add(current);
            }
            list.Reverse();
            return list;
        }
        private static void ResetPathVars()
        {
            HVMP_LayoutWorkerHypercube.openSet.Clear();
            HVMP_LayoutWorkerHypercube.cameFrom.Clear();
            HVMP_LayoutWorkerHypercube.gScore.Clear();
            HVMP_LayoutWorkerHypercube.fScore.Clear();
            HVMP_LayoutWorkerHypercube.toEnqueue.Clear();
        }
        private static bool TryGetPath(StructureLayout layout, IntVec3 start, IntVec3 goal, int max, out List<IntVec3> path)
        {
            HVMP_LayoutWorkerHypercube.ResetPathVars();
            HVMP_LayoutWorkerHypercube.gScore.Add(start, 0);
            HVMP_LayoutWorkerHypercube.fScore.Add(start, HVMP_LayoutWorkerHypercube.Heuristic(start, goal));
            HVMP_LayoutWorkerHypercube.openSet.Enqueue(start, HVMP_LayoutWorkerHypercube.fScore[start]);
            while (HVMP_LayoutWorkerHypercube.openSet.Count != 0)
            {
                IntVec3 intVec = HVMP_LayoutWorkerHypercube.openSet.Dequeue();
                if (intVec == goal)
                {
                    path = HVMP_LayoutWorkerHypercube.ReconstructPath(HVMP_LayoutWorkerHypercube.cameFrom, intVec);
                    HVMP_LayoutWorkerHypercube.ResetPathVars();
                    return true;
                }
                HVMP_LayoutWorkerHypercube.toEnqueue.Clear();
                foreach (IntVec3 intVec2 in HVMP_LayoutWorkerHypercube.Neighbours(layout, intVec, goal))
                {
                    if (intVec2 == goal)
                    {
                        HVMP_LayoutWorkerHypercube.cameFrom[intVec2] = intVec;
                        path = HVMP_LayoutWorkerHypercube.ReconstructPath(HVMP_LayoutWorkerHypercube.cameFrom, intVec2);
                        HVMP_LayoutWorkerHypercube.ResetPathVars();
                        return true;
                    }
                    int num = HVMP_LayoutWorkerHypercube.gScore[intVec] + 1;
                    if (num > max)
                    {
                        break;
                    }
                    if (!HVMP_LayoutWorkerHypercube.gScore.ContainsKey(intVec2) || num < HVMP_LayoutWorkerHypercube.gScore[intVec2])
                    {
                        HVMP_LayoutWorkerHypercube.cameFrom[intVec2] = intVec;
                        HVMP_LayoutWorkerHypercube.gScore[intVec2] = num;
                        HVMP_LayoutWorkerHypercube.fScore[intVec2] = num + HVMP_LayoutWorkerHypercube.Heuristic(intVec2, goal);
                        HVMP_LayoutWorkerHypercube.toEnqueue.Add(intVec2);
                    }
                }
                List<IntVec3> list = HVMP_LayoutWorkerHypercube.toEnqueue;
                Comparison<IntVec3> comparison = delegate (IntVec3 x, IntVec3 z)
                {
                    if (x == z)
                    {
                        return 0;
                    }
                    IntVec3 intVec4 = x - start;
                    if (intVec4.x == 0 || intVec4.z == 0)
                    {
                        return -1;
                    }
                    intVec4 = x - goal;
                    if (intVec4.x == 0 || intVec4.z == 0)
                    {
                        return -1;
                    }
                    intVec4 = z - start;
                    if (intVec4.x == 0 || intVec4.z == 0)
                    {
                        return 1;
                    }
                    intVec4 = z - goal;
                    if (intVec4.x == 0 || intVec4.z == 0)
                    {
                        return 1;
                    }
                    return 0;
                };
                list.Sort(comparison);
                foreach (IntVec3 intVec3 in HVMP_LayoutWorkerHypercube.toEnqueue)
                {
                    HVMP_LayoutWorkerHypercube.openSet.Enqueue(intVec3, HVMP_LayoutWorkerHypercube.fScore[intVec3]);
                }
            }
            HVMP_LayoutWorkerHypercube.ResetPathVars();
            path = null;
            return false;
        }
        private static IEnumerable<IntVec3> Neighbours8Way(StructureLayout layout, IntVec3 cell)
        {
            foreach (IntVec3 intVec in GenAdj.AdjacentCellsAround)
            {
                IntVec3 intVec2 = cell + intVec;
                if (intVec2.x > 0 && intVec2.z > 0 && intVec2.x < layout.Width && intVec2.z < layout.Height && layout.IsEmptyAt(intVec2))
                {
                    yield return intVec2;
                }
            }
            yield break;
        }
        private static IEnumerable<IntVec3> Neighbours(StructureLayout layout, IntVec3 cell, IntVec3 goal)
        {
            int num;
            for (int i = 0; i < 4; i = num + 1)
            {
                IntVec3 intVec = cell + new Rot4(i).FacingCell;
                if (intVec.x > 0 && intVec.z > 0 && intVec.x < layout.Width && intVec.z < layout.Height && (!(intVec != goal) || layout.IsEmptyAt(intVec)))
                {
                    yield return intVec;
                }
                num = i;
            }
            yield break;
        }
        private static int Heuristic(IntVec3 pos, IntVec3 goal)
        {
            return (goal - pos).LengthManhattan;
        }
        private static bool OverlapsWithAnyRoom(StructureLayout layout, CellRect rect)
        {
            foreach (LayoutRoom layoutRoom in layout.Rooms)
            {
                foreach (CellRect cellRect in layoutRoom.rects)
                {
                    if (cellRect.Overlaps(rect.ContractedBy(1)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private static void CreateDoors(StructureLayout layout)
        {
            HVMP_LayoutWorkerHypercube.tmpCells.Clear();
            HVMP_LayoutWorkerHypercube.tmpCells.AddRange(layout.container.Cells.InRandomOrder(null));
            for (int i = 0; i < HVMP_LayoutWorkerHypercube.tmpCells.Count; i++)
            {
                IntVec3 intVec = HVMP_LayoutWorkerHypercube.tmpCells[i];
                if (layout.IsWallAt(intVec))
                {
                    if (layout.IsGoodForHorizontalDoor(intVec))
                    {
                        HVMP_LayoutWorkerHypercube.TryConnectAdjacentRooms(layout, intVec, IntVec3.North);
                    }
                    if (layout.IsGoodForVerticalDoor(intVec))
                    {
                        HVMP_LayoutWorkerHypercube.TryConnectAdjacentRooms(layout, intVec, IntVec3.East);
                    }
                }
            }
            HVMP_LayoutWorkerHypercube.tmpCells.Clear();
        }
        private static void TryConnectAdjacentRooms(StructureLayout layout, IntVec3 p, IntVec3 dir)
        {
            LayoutRoom layoutRoom;
            if (!layout.TryGetRoom(p + dir, out layoutRoom))
            {
                return;
            }
            LayoutRoom layoutRoom2;
            if (!layout.TryGetRoom(p - dir, out layoutRoom2))
            {
                return;
            }
            if (layoutRoom.connections.Contains(layoutRoom2))
            {
                return;
            }
            bool flag = false;
            using (IEnumerator<ValueTuple<LayoutRoom, CellRect, CellRect>> enumerator = layout.GetLogicalRoomConnections(layoutRoom).GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.Item1 == layoutRoom2)
                    {
                        flag = true;
                        break;
                    }
                }
            }
            if (!flag)
            {
                return;
            }
            if (layoutRoom.entryCells != null && !layoutRoom.entryCells.Contains(p))
            {
                return;
            }
            if (layoutRoom2.entryCells != null && !layoutRoom2.entryCells.Contains(p))
            {
                return;
            }
            layout.Add(p, RoomLayoutCellType.Door);
            layoutRoom.connections.Add(layoutRoom2);
            layoutRoom2.connections.Add(layoutRoom);
        }
        private static readonly IntRange RoomSizeRange = new IntRange(8, 12);
        private static readonly IntRange LShapeRoomRange = new IntRange(6, 12);
        private static readonly IntRange RoomRange = new IntRange(32, 48);
        private const int Border = 2;
        private const int CorridorInflation = 3;
        private const int ObeliskRoomSize = 19;
        private static readonly PriorityQueue<IntVec3, int> openSet = new PriorityQueue<IntVec3, int>();
        private static readonly Dictionary<IntVec3, IntVec3> cameFrom = new Dictionary<IntVec3, IntVec3>();
        private static readonly Dictionary<IntVec3, int> gScore = new Dictionary<IntVec3, int>();
        private static readonly Dictionary<IntVec3, int> fScore = new Dictionary<IntVec3, int>();
        private static readonly List<IntVec3> toEnqueue = new List<IntVec3>();
        private static readonly List<IntVec3> tmpCells = new List<IntVec3>();
    }
    /*NOW WE'RE AT ONE THOUSAND LINES OF CODE, MOST OF IT? WRITTEN BY LUDEON STUDIOS. You didn't see it but I just glared so hard at a squirrel it fell out of a tree.
     * The og version of this is RoomContents_Obelisk. Natali labyrinths have a lot fewer gray boxes (handled in XML), but I think guaranteeing that there are two at the end is still fine, so I didn't remove those from this room.*/
    public class HVMP_RoomContentsObelisk : RoomContentsWorker
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
        {
            base.FillRoom(map, room, faction, threatPoints);
            CellRect cellRect = room.rects[0];
            foreach (IntVec3 intVec in cellRect.Cells)
            {
                if (intVec.GetFirstBuilding(map) == null)
                {
                    if (!(intVec - cellRect.CenterCell).IsCardinal && cellRect.CenterCell.DistanceTo(intVec) >= 8.9f)
                    {
                        GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.GrayWall, ThingDefOf.LabyrinthMatter), intVec, map, WipeMode.Vanish);
                    }
                    if (cellRect.CenterCell.DistanceTo(intVec) < 3.9f)
                    {
                        map.terrainGrid.SetTerrain(intVec, TerrainDefOf.Voidmetal);
                    }
                }
            }
            Building building = (Building)GenSpawn.Spawn(ThingMaker.MakeThing(HVMPDefOf_A.HVMP_WarpedObelisk_Hypercube, null), cellRect.CenterCell, map, WipeMode.Vanish);
            map.GetComponent<HVMP_HypercubeMapComponent>().labyrinthObelisk = building;
            string text = "ThingDiscovered" + Find.UniqueIDsManager.GetNextSignalTagID().ToString();
            SignalAction_Letter signalAction_Letter = (SignalAction_Letter)ThingMaker.MakeThing(ThingDefOf.SignalAction_Letter, null);
            signalAction_Letter.signalTag = text;
            signalAction_Letter.letterDef = LetterDefOf.PositiveEvent;
            signalAction_Letter.letterLabelKey = "LetterLabelObeliskDiscovered";
            signalAction_Letter.letterMessageKey = "HVMP_LetterObeliskDiscovered";
            GenSpawn.Spawn(signalAction_Letter, building.Position, map, WipeMode.Vanish);
            room.SpawnRectTriggersForAction(signalAction_Letter, map);
            RoomContents_GrayBox.SpawnBoxInRoom(cellRect.CenterCell + Rot4.East.FacingCell * 3, map, null, true);
            RoomContents_GrayBox.SpawnBoxInRoom(cellRect.CenterCell + Rot4.West.FacingCell * 3, map, null, true);
        }
        private const float WallRadius = 8.9f;
        private const float MetalRadius = 3.9f;
    }
    /*finally, something that isn't MOSTLY Ludeon's code! This is the Natali-exit's replacement for CompObelisk_Labyrinth, yes, but it has to send a quest signal so you can win the quest,
     * which requires a reference to the Hypercube world object (spatialAnomaly) to do so.
     * Also, while the map component handled the permanent game condition from Absence of Energy and Happiness, this handles the other two mutators
     * -Chaos of Limbo: shortly after the map is created, it will pick one of five random effects (see DoCOL()) to apply that make the labyrinth worser and less easy. Chimeras near the exit is my favorite one.
     * -Don't Forget: once you interact with the exit to go home, plop down the spatialAnomaly's DF_monsterKind pawn, briefly stun it, play cool S/VFX, and let the exit teleport carry it back home with ya.*/
    public class CompProperties_ObeliskHypercube : CompProperties_Interactable
    {
        public CompProperties_ObeliskHypercube()
        {
            this.compClass = typeof(CompObelisk_Hypercube);
        }
        [MustTranslate]
        public string messageActivating;
    }
    public class CompObelisk_Hypercube : CompInteractable
    {
        public new CompProperties_ObeliskHypercube Props
        {
            get
            {
                return (CompProperties_ObeliskHypercube)this.props;
            }
        }
        protected override void OnInteracted(Pawn caster)
        {
            Messages.Message(this.Props.messageActivating, this.parent, MessageTypeDefOf.NeutralEvent, false);
            Map map = this.parent.Map;
            if (map != null && !spatialAnomaly.questTags.NullOrEmpty())
            {
                QuestUtility.SendQuestTargetSignals(spatialAnomaly.questTags, "NataliResolved", spatialAnomaly.Named("SUBJECT"));
            }
            HVMP_HypercubeMapComponent hmc = map.GetComponent<HVMP_HypercubeMapComponent>();
            if (hmc != null && hmc.spatialAnomaly != null && hmc.spatialAnomaly is WorldObject_Hypercube woh && woh.DF_monsterKind != null)
            {
                PawnKindDef pkd = woh.DF_monsterKind;
                Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(pkd, pkd.RaceProps.Humanlike ? Faction.OfHoraxCult : Faction.OfEntities, PawnGenerationContext.NonPlayer, new PlanetTile?(map.Tile), false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
                pawn.health.overrideDeathOnDownedChance = 0f;
                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(this.parent.Position, map, 12, null), map);
                EffecterDefOf.MonolithLevelChanged.Spawn().Trigger(new TargetInfo(pawn.Position, map, false), new TargetInfo(pawn.Position, map, false), -1);
                if (pawn.stances != null && pawn.stances.stunner != null)
                {
                    pawn.stances.stunner.StunFor(600, null);
                }
                FleckMaker.Static(pawn.Position, map, FleckDefOf.PsycastSkipInnerExit, 1f);
                FleckMaker.Static(pawn.Position, map, FleckDefOf.PsycastSkipOuterRingExit, 1f);
                SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(pawn.Position, map, false));
            }
            map.GetComponent<HVMP_HypercubeMapComponent>().StartClosing();
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (!this.didCOL)
            {
                HVMP_HypercubeMapComponent hmc = this.parent.Map.GetComponent<HVMP_HypercubeMapComponent>();
                if (hmc != null && hmc.spatialAnomaly != null && hmc.spatialAnomaly is WorldObject_Hypercube woh && woh.COL_on)
                {
                    this.DoCOL();
                }
                this.didCOL = true;
            }
        }
        public void DoCOL()
        {
            int outcome = Rand.RangeInclusive(1, 5);
            Map m = this.parent.Map;
            switch (outcome)
            {
                case 1:
                    foreach (IntVec3 iv3 in m.AllCells)
                    {
                        if (m.terrainGrid.TerrainAt(iv3) == TerrainDefOf.GraySurface)
                        {
                            m.terrainGrid.SetTerrain(iv3, HVMPDefOf_A.HVMP_BurntSurface);
                        }
                    }
                    break;
                case 2:
                    List<Pawn> pawns = m.mapPawns.AllPawnsSpawned.ToList();
                    if (!pawns.NullOrEmpty())
                    {
                        for (int i = pawns.Count - 1; i >= 0; i--)
                        {
                            if (pawns[i].kindDef == PawnKindDefOf.Fingerspike)
                            {
                                this.GenerateStrongerFleshbeast(pawns[i]);
                            }
                        }
                    }
                    List<Thing> things = m.listerThings.ThingsOfDef(ThingDefOf.GrayBox);
                    if (!things.NullOrEmpty())
                    {
                        for (int i = things.Count - 1; i >= 0; i--)
                        {
                            if (Rand.Chance(0.5f))
                            {
                                this.GenerateStrongerFleshbeast(things[i]);
                            }
                        }
                    }
                    break;
                case 3:
                    foreach (IntVec3 iv3 in m.AllCells)
                    {
                        m.roofGrid.SetRoof(iv3, null);
                    }
                    GameCondition gameCondition = GameConditionMaker.MakeCondition(GameConditionDefOf.DeathPall, 99999);
                    gameCondition.Permanent = true;
                    if (gameCondition.CanApplyOnMap(m))
                    {
                        m.gameConditionManager.RegisterCondition(gameCondition);
                        Find.LetterStack.ReceiveLetter(gameCondition.LabelCap, gameCondition.LetterText, gameCondition.def.letterDef, LookTargets.Invalid, null, null, null, null, 0, true);
                    }
                    break;
                case 4:
                    int chimeraCount = Rand.RangeInclusive(1, 5);
                    Lord lord = LordMaker.MakeNewLord(Faction.OfEntities, new LordJob_DefendPoint(this.parent.Position, 12f, 25f, false, false), m, null);
                    for (int i = chimeraCount; i > 0; i--)
                    {
                        Pawn chimera = PawnGenerator.GeneratePawn(PawnKindDefOf.Chimera, Faction.OfEntities, null);
                        GenSpawn.Spawn(chimera, CellFinder.RandomClosewalkCellNear(this.parent.Position, m, 10), m, WipeMode.Vanish);
                        if (!chimera.Downed)
                        {
                            Lord lord2 = chimera.lord;
                            if (lord2 != null)
                            {
                                lord2.RemovePawn(chimera);
                            }
                            lord.AddPawn(chimera);
                        }
                    }
                    break;
                case 5:
                    GenSpawn.Spawn(HVMPDefOf_A.HVMP_LivingCloudkill, CellFinder.RandomClosewalkCellNear(this.parent.Position, m, 10), m);
                    break;
                default:
                    break;
            }
        }
        public void GenerateStrongerFleshbeast(Thing originalThing)
        {
            CompCanBeDormant compCanBeDormant;
            if (GenSpawn.Spawn(PawnGenerator.GeneratePawn(FleshbeastUtility.AllFleshbeasts.Where((PawnKindDef pkd) => pkd.race.race.baseBodySize > ThingDefOf.Fingerspike.race.baseBodySize).RandomElement(), Faction.OfEntities, null), originalThing.Position, originalThing.Map, WipeMode.Vanish).TryGetComp(out compCanBeDormant))
            {
                compCanBeDormant.ToSleep();
            }
            for (int i = 0; i < 3; i++)
            {
                FilthMaker.TryMakeFilth(CellFinder.RandomClosewalkCellNear(originalThing.Position, originalThing.Map, 3, null), originalThing.Map, ThingDefOf.Filth_Blood, 1, FilthSourceFlags.None, true);
            }
            Thing.allowDestroyNonDestroyable = true;
            originalThing.Destroy();
            Thing.allowDestroyNonDestroyable = false;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look<WorldObject>(ref this.spatialAnomaly, "spatialAnomaly", false);
            Scribe_Values.Look<bool>(ref this.didCOL, "didCOL", false, false);
        }
        public WorldObject spatialAnomaly;
        public bool didCOL;
    }
}
