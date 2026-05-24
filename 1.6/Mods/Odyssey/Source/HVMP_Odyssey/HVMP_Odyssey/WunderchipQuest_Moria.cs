using HautsPermits;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace HVMP_Odyssey
{
    //as Mechanoid Relay, except the stabilizers aren't guarded by dormant mechs (damaging them still calls mechs down though), and the relay is a monster spawner
    public class QuestNode_Root_Wunderchip_Moria : QuestNode_Root_Gravcore
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
                Log.Error("Could not find valid site tile for mineral extractor quest.");
                return;
            }
            string text = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("MechanoidRelay");
            string text2 = QuestGenUtility.HardcodedSignalWithQuestID("site.MapGenerated");
            string text3 = QuestGenUtility.HardcodedSignalWithQuestID("site.MapRemoved");
            string text4 = QuestGen.GenerateNewSignal("StabilizerDealtWith", true);
            string text5 = QuestGen.GenerateNewSignal("AllStabilizersDealtWith", true);
            Site site = QuestGen_Sites.GenerateSite(new SitePartDefWithParams[]
            {
                new SitePartDefWithParams(HVMPDefOf_Odyssey.HVMP_MoriaSite, new SitePartParams
                {
                    points = slate.Get<float>("points", 0f, false),
                    threatPoints = slate.Get<float>("points", 0f, false),
                    stabilizerCount = 3
                })
            }, planetTile, Faction.OfMechanoids, false, null, WorldObjectDefOf.ClaimableSite);
            slate.Set<Site>("site", site, false);
            quest.SpawnWorldObject(site, null, null);
            List<Thing> list = new List<Thing>();
            List<string> list2 = new List<string>();
            for (int i = 0; i < site.parts[0].things.Count; i++)
            {
                Thing thing = site.parts[0].things[i];
                QuestUtility.AddQuestTag(ref thing.questTags, text);
                if (thing.def == ThingDefOf.MechStabilizer)
                {
                    this.AddStabilizer(thing, site, quest, slate, text2, list, list2);
                } else if (thing.def == HVMPDefOf_Odyssey.HVMP_MineralExtractor) {
                    slate.Set<Thing>("relay", thing, false);
                }
            }
            quest.SignalPassAny(null, list2, text4);
            QuestPart_Filter_AllThingsHackedOrDestroyed questPart_Filter_AllThingsHackedOrDestroyed = new QuestPart_Filter_AllThingsHackedOrDestroyed();
            questPart_Filter_AllThingsHackedOrDestroyed.things.AddRange(list);
            questPart_Filter_AllThingsHackedOrDestroyed.inSignal = text4;
            questPart_Filter_AllThingsHackedOrDestroyed.outSignal = text5;
            quest.AddPart(questPart_Filter_AllThingsHackedOrDestroyed);
            quest.AddPart(new QuestPart_Moria
            {
                inSignal = text5,
                relay = slate.Get<Thing>("relay", null, false)
            });
            quest.Delay(10000, delegate
            {
                quest.RandomRaid(site, FloatRange.One, Faction.OfInsects, null, PawnsArrivalModeDefOf.EdgeWalkIn, RaidStrategyDefOf.ImmediateAttack, "HVMP_MineralExtractorRaidLetterLabel".Translate(), "HVMP_MineralExtractorRaidLetterText".Translate(), true);
            }, text5, null, null, false, null, null, false, null, null, null, false, QuestPart.SignalListenMode.OngoingOnly, false);
            quest.Letter(LetterDefOf.NeutralEvent, text2, null, null, null, false, QuestPart.SignalListenMode.OngoingOnly, null, false, "[letterTextMapGenerated]", null, "[letterLabelMapGenerated]", null, null);
            QuestPart_RelayStabilizersRemainingAlert questPart_RelayStabilizersRemainingAlert = new QuestPart_RelayStabilizersRemainingAlert(site, text2, text5, text4, list.Count);
            questPart_RelayStabilizersRemainingAlert.things.AddRange(list);
            quest.AddPart(questPart_RelayStabilizersRemainingAlert);
            QuestPart_Choice.Choice choice = new QuestPart_Choice.Choice();
            choice.rewards.Add(new Reward_DefinedThingDef(HVMPDefOf.HVMP_Wunderchip));
            quest.RewardChoice(null, null).choices.Add(choice);
            quest.End(QuestEndOutcome.Success, 0, null, text5, QuestPart.SignalListenMode.OngoingOnly, false, false);
            quest.End(QuestEndOutcome.Unknown, 0, null, text3, QuestPart.SignalListenMode.OngoingOnly, false, false);
        }
        private void AddStabilizer(Thing thing, Site site, Quest quest, Slate slate, string mapGeneratedSignal, List<Thing> stabilizers, List<string> signalsDealtWith)
        {
            string text = string.Format("{0}{1}", "stabilizer", stabilizers.Count);
            slate.Set<Thing>(text, thing, false);
            string text2 = QuestGenUtility.HardcodedSignalWithQuestID(text + ".Destroyed");
            string text3 = QuestGenUtility.HardcodedSignalWithQuestID(text + ".Hacked");
            string text4 = QuestGenUtility.HardcodedSignalWithQuestID(text + ".TookDamage");
            string text5 = QuestGenUtility.HardcodedSignalWithQuestID(text + ".LockedOut");
            signalsDealtWith.Add(text2);
            signalsDealtWith.Add(text3);
            QuestPart_Filter_Hacked questPart_Filter_Hacked = new QuestPart_Filter_Hacked();
            questPart_Filter_Hacked.inSignal = text2;
            questPart_Filter_Hacked.outSignalElse = QuestGen.GenerateNewSignal("SendRaidStabilizerDestroyed", true);
            quest.AddPart(questPart_Filter_Hacked);
            quest.Message("MessageStabilizerDeactivated".Translate(), MessageTypeDefOf.PositiveEvent, true, null, null, text3);
            quest.RandomRaid(site, FloatRange.One, Faction.OfInsects, questPart_Filter_Hacked.outSignalElse, PawnsArrivalModeDefOf.EdgeWalkIn, RaidStrategyDefOf.ImmediateAttack, "HVMP_MineralExtractorRaidLetterLabel".Translate(), "HVMP_MineralExtractorRaidLetterText".Translate(), true);
            stabilizers.Add(thing);
        }
    }
    public class CompProperties_WunderExtractor : CompProperties_MechRelay
    {
        public CompProperties_WunderExtractor()
        {
            this.compClass = typeof(CompWunderExtractor);
        }
        public int pawnSpawnMTBhours;
        public List<PawnGroupMaker> pawnGroupMakers;
        public int maxConcurrentPawns;
        public SimpleCurve pawnPointsGivenExistingPawns;
    }
    public class CompWunderExtractor : CompMechRelay
    {
        public new CompProperties_WunderExtractor Props
        {
            get
            {
                return (CompProperties_WunderExtractor)this.props;
            }
        }
        public Faction PawnFaction
        {
            get
            {
                return Faction.OfInsects ?? Faction.OfAncientsHostile;
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad && this.parent.Spawned)
            {
                Map map = this.parent.Map;
                TerrainDef terrainDef = map.Biome.TerrainForAffordance(this.parent.def.terrainAffordanceNeeded);
                foreach (IntVec3 intVec in GenAdj.OccupiedRect(this.parent.TrueCenter().ToIntVec3(), Rot4.North, this.parent.def.Size))
                {
                    map.terrainGrid.RemoveTopLayer(intVec, false);
                    if (!intVec.GetAffordances(map).Contains(this.parent.def.terrainAffordanceNeeded))
                    {
                        map.terrainGrid.SetTerrain(intVec, terrainDef);
                    }
                }
            }
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.parent.Spawned && this.parent.IsHashIntervalTick(500) && Rand.MTBEventOccurs(this.Props.pawnSpawnMTBhours, 2500,500))
            {
                int pawnCount = this.parent.Map.mapPawns.PawnsInFaction(this.PawnFaction).Count;
                if (pawnCount < this.Props.maxConcurrentPawns)
                {
                    this.SpawnPawns(pawnCount);
                }
            }
        }
        public void SpawnPawns(int extantPawnCount)
        {
            CellRect cellRect = GenAdj.OccupiedRect(this.parent.Position, Rot4.North, ThingDefOf.PitBurrow.Size).ContractedBy(2);
            List<PawnFlyer> list = new List<PawnFlyer>();
            List<IntVec3> list2 = new List<IntVec3>();
            List<Pawn> pawns = this.GetPawnsForPoints(extantPawnCount, this.parent.Map);
            if (pawns.Count > 0)
            {
                foreach (Pawn pawn in pawns)
                {
                    IntVec3 randomCell = cellRect.RandomCell;
                    GenSpawn.Spawn(pawn, randomCell, this.parent.Map, WipeMode.Vanish);
                    IntVec3 intVec;
                    if (CellFinder.TryFindRandomCellNear(this.parent.Position, this.parent.Map, this.parent.def.size.x / 2 + 1, (IntVec3 c) => !c.Fogged(this.parent.Map) && c.Walkable(this.parent.Map) && !c.Impassable(this.parent.Map), out intVec, -1))
                    {
                        pawn.rotationTracker.FaceCell(intVec);
                        list.Add(PawnFlyer.MakeFlyer(ThingDefOf.PawnFlyer_Stun, pawn, intVec, null, null, false, new Vector3?(randomCell.ToVector3() + new Vector3(0f, 0f, -1f)), null, default(LocalTargetInfo)));
                        list2.Add(randomCell);
                    }
                }
                if (list2.Count == 0)
                {
                    return;
                }
                SpawnRequest spawnRequest = new SpawnRequest(list.Cast<Thing>().ToList<Thing>(), list2, 1, 1f, null);
                spawnRequest.initialDelay = 420;
                spawnRequest.lord = LordMaker.MakeNewLord(this.PawnFaction, new LordJob_DefendPoint(this.parent.Position,12f,65f,false,false), this.parent.Map, null);
                this.parent.Map.deferredSpawner.AddRequest(spawnRequest, true);
                pawns.Clear();
            }
        }
        public List<Pawn> GetPawnsForPoints(int pawnCount, Map map)
        {
            List<Pawn> pawns = new List<Pawn>();
            if (this.groupMaker == null)
            {
                this.groupMaker = this.Props.pawnGroupMakers.RandomElement();
            }
            float points = this.Props.pawnPointsGivenExistingPawns.Evaluate(pawnCount)*HVMP_Mod.settings.wunderQuestDifficulty;
            while (points > 0)
            {
                PawnGenOption pgo = this.groupMaker.options.RandomElementByWeight((PawnGenOption option)=>option.selectionWeight);
                pawns.Add(this.GeneratePawn(pgo.kind));
                points -= pgo.Cost;
            }
            return pawns;
        }
        public Pawn GeneratePawn(PawnKindDef kind)
        {
            PawnGenerationRequest pawnGenerationRequest = new PawnGenerationRequest(kind, this.PawnFaction, PawnGenerationContext.NonPlayer, this.parent.Tile, false, false, false, true, true, 1f, false, true, true, false, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false);
            return PawnGenerator.GeneratePawn(pawnGenerationRequest);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<PawnGroupMaker>(ref this.groupMaker, "groupMaker", null, false);
        }
        public PawnGroupMaker groupMaker;
    }
    public class CompProperties_YouShouldDeconstructYourselfNow : CompProperties
    {
        public CompProperties_YouShouldDeconstructYourselfNow()
        {
            this.compClass = typeof(CompYouShouldDeconstructYourselfNow);
        }
    }
    public class CompYouShouldDeconstructYourselfNow : ThingComp {
        public override void CompTick()
        {
            base.CompTick();
            this.parent.Kill();
        }
    }
    public class QuestPart_Moria : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag == this.inSignal && this.relay != null)
            {
                CompWunderExtractor cwe = this.relay.TryGetComp<CompWunderExtractor>();
                if (cwe != null)
                {
                    cwe.Deactivate();
                }
            }
        }
        public override void ExposeData()
        {
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_References.Look<Thing>(ref this.relay, "relay", false);
        }
        public string inSignal;
        public Thing relay;
    }
    public class SitePartWorker_MoriaSite : SitePartWorker
    {
        public override void Init(Site site, SitePart sitePart)
        {
            base.Init(site, sitePart);
            sitePart.things = new ThingOwner<Thing>(sitePart);
            sitePart.things.TryAdd(ThingMaker.MakeThing(HVMPDefOf_Odyssey.HVMP_MineralExtractor, null), true);
            for (int i = 0; i < sitePart.parms.stabilizerCount; i++)
            {
                sitePart.things.TryAdd(ThingMaker.MakeThing(ThingDefOf.MechStabilizer, null), true);
            }
        }
    }
    public class GenStep_MoriaSite : GenStep
    {
        public override int SeedPart
        {
            get
            {
                return 583369484;
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
                    if (thing.def != HVMPDefOf_Odyssey.HVMP_MineralExtractor || !RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith((IntVec3 x) => this.CellValid(x, map, thing.def.Size, usedRects), map, out IntVec3 intVec))
                    {
                        intVec = CellFinderLoose.RandomCellWith((IntVec3 x) => this.CellValid(x, map, thing.def.Size, usedRects), map, 1000);
                    }
                    if (!intVec.IsValid)
                    {
                        Log.Warning("Could not find valid cell for HVMP_MineralExtractor/Stabilizer.");
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
            bool flag = thing.def == HVMPDefOf_Odyssey.HVMP_MineralExtractor;
            IEnumerable<IntVec3> corners = cellRect2.Corners;
            foreach (IntVec3 intVec2 in cellRect2.EdgeCells)
            {
                if ((flag || !corners.Contains(intVec2)) && map.terrainGrid.TerrainAt(intVec2).changeable)
                {
                    map.terrainGrid.SetTerrain(intVec2, TerrainDefOf.AncientTile);
                }
            }
            usedRects.Add(cellRect2);
            if (flag)
            {
                foreach (IntVec3 intVec3 in corners)
                {
                    if (GenConstruct.TerrainCanSupport(CellRect.CenteredOn(intVec3, 1), map, ThingDefOf.AncientMechDropBeacon))
                    {
                        GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.AncientMechDropBeacon, null), intVec3, map, ThingDefOf.AncientMechDropBeacon.defaultPlacingRot, WipeMode.Vanish, false, false);
                    }
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
