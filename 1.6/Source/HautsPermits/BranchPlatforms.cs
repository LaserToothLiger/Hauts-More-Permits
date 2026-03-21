using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace HautsPermits
{
    /*they're basically like settlements. However, you can never visit them peaceably, and the effects that periodically remove all branch faction-owned Settlements do not apply to BranchPlatforms.
     * Attackable and IncidentTargetTags are defined here, despite being the same as Settlement, to prevent other mods' Harmony patches to those from applying to branch platforms.*/
    [StaticConstructorOnStartup]
    public class BranchPlatform : Settlement, INameableWorldObject
    {
        public override bool Visitable
        {
            get
            {
                return false;
            }
        }
        public override bool Attackable
        {
            get
            {
                return base.Faction != Faction.OfPlayer;
            }
        }
        public BranchPlatform() {}
        public override IEnumerable<IncidentTargetTagDef> IncidentTargetTags()
        {
            foreach (IncidentTargetTagDef incidentTargetTagDef in base.IncidentTargetTags())
            {
                yield return incidentTargetTagDef;
            }
            if (base.Faction == null || base.Faction == Faction.OfPlayer || SettlementDefeatUtility.IsDefeated(base.Map, base.Faction))
            {
                yield return IncidentTargetTagDefOf.Map_PlayerHome;
            } else {
                yield return IncidentTargetTagDefOf.Map_Misc;
            }
            yield break;
        }
    }
    /*GO BIG MODE. Big as the mechhive platform, in fact, but using the shapes of the traders' guild platforms
     * Fog of war is similar to the faction's color*/
    public class GenStep_BigassPlatform : GenStep
    {
        public override int SeedPart
        {
            get
            {
                return 8256453;
            }
        }
        protected Faction GetFaction(Map map)
        {
            if (this.useSiteFaction && map.Parent.Faction != null)
            {
                return map.Parent.Faction;
            }
            if (this.factionDef != null)
            {
                return Find.FactionManager.FirstFactionOfDef(this.factionDef);
            }
            Faction f2 = Find.FactionManager.AllFactionsVisible.Where((Faction f3) => f3.def.HasModExtension<EBranchQuests>()).RandomElement();
            if (f2 != null)
            {
                return f2;
            }
            return null;
        }
        public override void Generate(Map map, GenStepParams parms)
        {
            if (!ModLister.CheckOdyssey("Orbital Platform"))
            {
                return;
            }
            float? num = null;
            if (parms.sitePart != null)
            {
                num = new float?(parms.sitePart.parms.points);
            }
            if (num == null)
            {
                RimWorld.Planet.Site site = map.Parent as RimWorld.Planet.Site;
                if (site != null)
                {
                    num = new float?(site.ActualThreatPoints);
                }
            }
            Faction faction = this.GetFaction(map);
            CellRect cellRect = this.GeneratePlatform(map, faction, num);
            if (Rand.Chance(0.33f))
            {
                this.DoRing(map, cellRect);
            } else if (Rand.Chance(0.5f)) {
                this.DoLargePlatforms(map, cellRect);
            } else {
                this.DoSmallPlatforms(map, cellRect);
            }
            this.SpawnCannons(map, cellRect.ExpandedBy(6));
            Faction f = this.GetFaction(map);
            Color colorPrep = this.fogOfWarColor.ToColor;
            if (f != null)
            {
                float fr = 1f, fg = 1f, fb = 1f;
                if (f.Color.r / f.Color.g >= 2f)
                {
                    fr += 0.25f;
                } else if (f.Color.r / f.Color.g >= 1.25f) {
                    fr += 0.1f;
                }
                if (f.Color.r / f.Color.b >= 2f)
                {
                    fr += 0.25f;
                } else if (f.Color.r / f.Color.b >= 1.25f) {
                    fr += 0.1f;
                }
                if (f.Color.g / f.Color.r >= 2f)
                {
                    fg += 0.25f;
                } else if (f.Color.g / f.Color.r >= 1.25f) {
                    fg += 0.1f;
                }
                if (f.Color.g / f.Color.b >= 2f)
                {
                    fg += 0.25f;
                } else if (f.Color.g / f.Color.b >= 1.25f) {
                    fg += 0.1f;
                }
                if (f.Color.b / f.Color.r >= 2f)
                {
                    fg += 0.25f;
                } else if (f.Color.b / f.Color.r >= 1.25f) {
                    fg += 0.1f;
                }
                if (f.Color.b / f.Color.g >= 2f)
                {
                    fg += 0.25f;
                } else if (f.Color.b / f.Color.g >= 1.25f) {
                    fg += 0.1f;
                }
                colorPrep.r *= fr;
                colorPrep.g *= fg;
                colorPrep.b *= fb;
            }
            map.FogOfWarColor = colorPrep;
            map.OrbitalDebris = this.orbitalDebrisDef;
            this.SpawnExteriorPrefabs(map, cellRect.ExpandedBy(6), faction);
        }
        private CellRect GeneratePlatform(Map map, Faction faction, float? threatPoints)
        {
            IntVec2 intVec = new IntVec2(GenStep_BigassPlatform.SizeRange.RandomInRange, GenStep_BigassPlatform.SizeRange.RandomInRange);
            Rot4 random = Rot4.Random;
            CellRect cellRect = map.Center.RectAbout(intVec, random).ClipInsideMap(map);
            StructureGenParams structureGenParams = new StructureGenParams
            {
                size = cellRect.Size
            };
            LayoutWorker worker = this.layoutDef.Worker;
            LayoutStructureSketch layoutStructureSketch = worker.GenerateStructureSketch(structureGenParams);
            map.layoutStructureSketches.Add(layoutStructureSketch);
            worker.Spawn(layoutStructureSketch, map, cellRect.Min, threatPoints, null, true, false, faction);
            MapGenerator.SetVar<CellRect>("SpawnRect", cellRect);
            MapGenerator.UsedRects.Add(cellRect);
            return cellRect;
        }
        private void DoRing(Map map, CellRect rect)
        {
            float num = Mathf.Sqrt((float)(rect.Width * rect.Width + rect.Height * rect.Height)) - (float)rect.Width - 12f;
            SpaceGenUtility.GenerateRing(map, rect, this.platformTerrain, Mathf.RoundToInt(num / 2f), 0, 13.9f, 0.5f, 0f);
        }
        private void DoLargePlatforms(Map map, CellRect rect)
        {
            int randomInRange = GenStep_BigassPlatform.LargeDockRange.RandomInRange;
            List<Rot4> list = new List<Rot4>
            {
                Rot4.North,
                Rot4.East,
                Rot4.South,
                Rot4.West
            };
            int num = 0;
            while (num < randomInRange && list.Any<Rot4>())
            {
                Rot4 rot = list.RandomElement<Rot4>();
                list.Remove(rot);
                SpaceGenUtility.GenerateConnectedPlatform(map, this.platformTerrain, rect, GenStep_BigassPlatform.LargeLandingAreaWidthRange, GenStep_BigassPlatform.LargeLandingAreaHeightRange, rot, 14, 0.2f, 2, null, null, null, null);
                num++;
            }
        }
        private void DoSmallPlatforms(Map map, CellRect rect)
        {
            ValueTuple<CellRect, CellRect, CellRect, CellRect> valueTuple = rect.Subdivide(1);
            int randomInRange = GenStep_BigassPlatform.SmallPlatformRange.RandomInRange;
            List<ValueTuple<CellRect, Rot4>> list = new List<ValueTuple<CellRect, Rot4>>
            {
                new ValueTuple<CellRect, Rot4>(valueTuple.Item1, Rot4.South),
                new ValueTuple<CellRect, Rot4>(valueTuple.Item1, Rot4.West),
                new ValueTuple<CellRect, Rot4>(valueTuple.Item3, Rot4.South),
                new ValueTuple<CellRect, Rot4>(valueTuple.Item3, Rot4.East),
                new ValueTuple<CellRect, Rot4>(valueTuple.Item2, Rot4.North),
                new ValueTuple<CellRect, Rot4>(valueTuple.Item2, Rot4.West),
                new ValueTuple<CellRect, Rot4>(valueTuple.Item4, Rot4.North),
                new ValueTuple<CellRect, Rot4>(valueTuple.Item4, Rot4.East)
            };
            int num = 0;
            while (num < randomInRange && list.Any<ValueTuple<CellRect, Rot4>>())
            {
                ValueTuple<CellRect, Rot4> valueTuple2 = list.RandomElement<ValueTuple<CellRect, Rot4>>();
                list.Remove(valueTuple2);
                ValueTuple<CellRect, Rot4> valueTuple3 = valueTuple2;
                CellRect item = valueTuple3.Item1;
                Rot4 item2 = valueTuple3.Item2;
                SpaceGenUtility.GenerateConnectedPlatform(map, this.platformTerrain, item, GenStep_BigassPlatform.SmallPlatformSizeRange, GenStep_BigassPlatform.SmallPlatformSizeRange, item2, GenStep_BigassPlatform.SmallPlatformDistanceRange.RandomInRange, 0.2f, 2, null, null, null, null);
                num++;
            }
        }
        private void SpawnExteriorPrefabs(Map map, CellRect rect, Faction faction)
        {
            using (List<GenStep_BigassPlatform.PrefabRange>.Enumerator enumerator = this.exteriorPrefabs.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    int randomInRange = enumerator.Current.countRange.RandomInRange;
                    for (int i = 0; i < randomInRange; i++)
                    {
                        IntVec3 intVec;
                        if (rect.TryFindRandomCell(out intVec, null))
                        {
                            Rot4 opposite = rect.GetClosestEdge(intVec).Opposite;
                            PrefabUtility.SpawnPrefab(enumerator.Current.prefab, map, intVec, opposite, faction, null, null, null, false);
                        }
                    }
                }
            }
        }
        private void SpawnCannons(Map map, CellRect rect)
        {
            if (this.cannonDef == null)
            {
                return;
            }
            for (int i = 0; i < 4; i++)
            {
                Rot4 rot = new Rot4(i);
                IntVec3 corner = rect.GetCorner(rot, true);
                int num = Mathf.Max(this.cannonDef.size.x, this.cannonDef.size.z) + 4;
                CellRect cellRect = corner.RectAbout(num, num);
                MapGenUtility.Line_NewTemp(this.platformTerrain, map, corner, rect.CenterCell, 6f, true, TerrainDefOf.Space);
                foreach (IntVec3 intVec in cellRect.Cells)
                {
                    if (intVec.InHorDistOf(corner, (float)num / 2f - 0.6f))
                    {
                        map.terrainGrid.SetTerrain(intVec, TerrainDefOf.AncientTile);
                    }
                    else if (intVec.InHorDistOf(corner, (float)num / 2f + 0.5f))
                    {
                        map.terrainGrid.SetTerrain(intVec, this.platformTerrain);
                    }
                }
                GenSpawn.Spawn(ThingMaker.MakeThing(this.cannonDef, null), corner, map, new Rot4(i % 4), WipeMode.Vanish, false, false);
                MapGenerator.UsedRects.Add(cellRect);
            }
        }
        protected float SpawnTemp
        {
            get
            {
                float? num = this.temperature;
                if (num == null)
                {
                    return -75f;
                }
                return num.GetValueOrDefault();
            }
        }
        public override void PostMapInitialized(Map map, GenStepParams parms)
        {
            MapGenUtility.SetMapRoomTemperature(map, this.layoutDef, this.SpawnTemp);
            if (this.spawnSentryDrones)
            {
                BaseGenUtility.ScatterSentryDronesInMap(GenStep_BigassPlatform.SentryCountFromPointsCurve, map, this.GetFaction(map), parms);
            }
        }
        private FactionDef factionDef;
        private bool useSiteFaction;
        private LayoutDef layoutDef;
        private TerrainDef platformTerrain;
        private ThingDef cannonDef;
        private ColorInt fogOfWarColor = new ColorInt(43, 46, 47);
        private OrbitalDebrisDef orbitalDebrisDef;
        private float? temperature;
        private bool spawnSentryDrones;
        private static readonly IntRange SizeRange = new IntRange(130, 160);
        private static readonly IntRange LargeDockRange = new IntRange(1, 2);
        private static readonly IntRange SmallPlatformRange = new IntRange(4, 6);
        private static readonly IntRange SmallPlatformSizeRange = new IntRange(16, 20);
        private static readonly IntRange SmallPlatformDistanceRange = new IntRange(10, 18);
        private static readonly IntRange LargeLandingAreaWidthRange = new IntRange(30, 40);
        private static readonly IntRange LargeLandingAreaHeightRange = new IntRange(50, 60);
        public static readonly IntRange LandingPadBorderLumpLengthRange = new IntRange(6, 10);
        public static readonly IntRange LandingPadBorderLumpOffsetRange = new IntRange(-1, 1);
        private static readonly SimpleCurve SentryCountFromPointsCurve = new SimpleCurve(new CurvePoint[]
        {
            new CurvePoint(0f, 0f),
            new CurvePoint(100f, 5f),
            new CurvePoint(1000f, 20f),
            new CurvePoint(5000f, 40f)
        });
        private class PrefabRange
        {
            public void LoadDataFromXmlCustom(XmlNode xmlRoot)
            {
                XmlHelper.ParseElements(this, xmlRoot, "prefab", "countRange");
            }
            public PrefabDef prefab;
            public IntRange countRange;
        }
        private List<GenStep_BigassPlatform.PrefabRange> exteriorPrefabs = new List<GenStep_BigassPlatform.PrefabRange>();
    }
    /*handles the spawning of a Branch Platform's inhabitants. If for some reason you want to scale the population up beyond what the mod settings will let you, you can sub out the defenderMulti in XML for EVEN MORE.
     *   (That will make Branch Platforms take even longer to load, though, so maybe don't do that.)
     * Spawned humanlike pawns have the DefendBase Lord, but each spawned defender only has this for 4-6 hours. That randomness means that if you don't provoke them, they'll go attack you in staggered groups, rather than all at once.
     * Their Lords are set to defend whatever point they spawned at, so that the defenders remain spread through the base and don't all aggro at once.
     * Also handles the generation of extra miscellaneous loot items, the way you'd find in regular settlements.*/
    public class GenStep_BigassPawnDefendersAndLoot : GenStep
    {
        public override int SeedPart
        {
            get
            {
                return 4217397;
            }
        }
        public override void Generate(Map map, GenStepParams parms)
        {
            CellRect cellRect;
            if (!MapGenerator.TryGetVar<CellRect>("SpawnRect", out cellRect))
            {
                Log.Error("GenStep_BigassPawnLoot tried to execute but no SpawnRect was found in the map generator. This CellRect must be set.");
                return;
            }
            Faction faction = this.GetFaction(map);
            if (this.generatePawns)
            {
                int defMulti = this.defenderMulti.RandomInRange;
                PawnGroupMakerParms pawnGroupMakerParms = new PawnGroupMakerParms
                {
                    tile = map.Tile,
                    faction = faction,
                    points = this.pointsPerPawnGen.RandomInRange,
                    inhabitants = true,
                    seed = null,
                    ignoreGroupCommonality = true
                };
                for (int i = 0; i < defMulti; i++)
                {
                    pawnGroupMakerParms.points = this.pointsPerPawnGen.RandomInRange;
                    CellRect cellRect2 = cellRect;
                    Faction faction2 = faction;
                    PawnGroupKindDef settlement = PawnGroupKindDefOf.Settlement;
                    MapGenUtility.GeneratePawns(map, cellRect2, faction2, null, settlement, pawnGroupMakerParms, null, null, null, this.requiresRoof);
                    pawnGroupMakerParms.points = this.pointsPerPawnGen.RandomInRange;
                    CellRect cellRect3 = cellRect;
                    PawnGroupKindDef settlement2 = PawnGroupKindDefOf.Settlement_RangedOnly;
                    MapGenUtility.GeneratePawns(map, cellRect3, faction2, null, settlement2, pawnGroupMakerParms, null, null, null, this.requiresRoof);
                }
                int combMulti = (int)Math.Ceiling(HVMP_Mod.settings.platformDefenderScale);
                for (int i = 0; i < combMulti; i++)
                {
                    pawnGroupMakerParms.points = this.pointsPerPawnGen.RandomInRange;
                    CellRect cellRect2 = cellRect;
                    Faction faction2 = faction;
                    PawnGroupKindDef settlement = PawnGroupKindDefOf.Combat;
                    MapGenUtility.GeneratePawns(map, cellRect2, faction2, null, settlement, pawnGroupMakerParms, null, null, null, this.requiresRoof);
                }
                foreach (Pawn p in map.mapPawns.PawnsInFaction(faction).InRandomOrder())
                {
                    if (p.RaceProps.Humanlike)
                    {
                        if (p.lord != null && !(p.lord.LordJob is LordJob_DefendBase))
                        {
                            p.GetLord().RemovePawn(p);
                        }
                        if (p.lord == null)
                        {
                            Lord lord = LordMaker.MakeNewLord(faction, new LordJob_DefendBase(faction, p.PositionHeld, Rand.RangeInclusive(4, 6) * 2500, false), map, null);
                            lord.AddPawn(p);
                        }
                    }
                }
            }
            FloatRange? floatRange = this.lootMarketValue;
            if (floatRange == null || !floatRange.GetValueOrDefault().IsZeros)
            {
                ThingSetMakerDef thingSetMakerDef;
                if ((thingSetMakerDef = this.lootThingSetMaker) == null)
                {
                    thingSetMakerDef = faction.def.settlementLootMaker ?? ThingSetMakerDefOf.MapGen_AbandonedColonyStockpile;
                }
                ThingSetMakerDef thingSetMakerDef2 = thingSetMakerDef;
                CellRect cellRect3 = cellRect;
                ThingSetMakerDef thingSetMakerDef3 = thingSetMakerDef2;
                FloatRange? floatRange2 = this.lootMarketValue;
                Faction faction3 = faction;
                bool flag = this.requiresRoof;
                MapGenUtility.GenerateLoot(map, cellRect3, thingSetMakerDef3, floatRange2, null, faction3, flag);
            }
        }
        private Faction GetFaction(Map map)
        {
            Faction faction;
            if (this.factionDef != null)
            {
                faction = Find.FactionManager.FirstFactionOfDef(this.factionDef);
            } else if (map.ParentFaction == null || map.ParentFaction == Faction.OfPlayer) {
                faction = Find.FactionManager.RandomEnemyFaction(false, false, true, TechLevel.Undefined);
            } else {
                faction = map.ParentFaction;
            }
            return faction;
        }
        public FactionDef factionDef;
        public bool generatePawns = true;
        public ThingSetMakerDef lootThingSetMaker;
        public FloatRange? lootMarketValue;
        public bool requiresRoof;
        public IntRange defenderMulti;
        public FloatRange pointsPerPawnGen;
    }
    /*Regular trader guild platforms sometimes have rooms of sterile tile flooring that only contain a couple cryptosleep caskets, which in turn have ancient soldiers in them.
     * I don't care for these, so the structure layout def for Branch Platforms sub them out for these babies, which are the entire point of raiding such a platform.
     * The code is basically the same, but we then sub out the cryptosleep caskets for ESCs, which contain platform-exclusive loot.*/
    public class RoomContents_HVMP_RewardVault : RoomContentsWorker
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
        {
            RoomGenUtility.FillWithPadding(HVMPDefOf.HVMP_EnterpriseSecurityCrate, RoomContents_HVMP_RewardVault.LootRange.RandomInRange, room, map, null, null, null, 1, null, false, false, null, null);
            base.FillRoom(map, room, faction, threatPoints);
        }
        private static readonly IntRange LootRange = new IntRange(2, 4);
    }
}
