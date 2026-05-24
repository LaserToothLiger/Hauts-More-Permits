using HautsFramework;
using HautsPermits;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HVMP_Odyssey
{
    //as Ancient Reactor, but with a different building and different text
    public class QuestNode_Root_Wunderchip_Korhal : QuestNode_Root_Gravcore
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;
            PlanetTile planetTile;
            if (!this.TryFindSiteTile(out planetTile))
            {
                Log.Error("Could not find valid site tile for lockdown zone quest.");
                return;
            }
            string text = QuestGenUtility.HardcodedSignalWithQuestID("site.MapGenerated");
            string text2 = QuestGenUtility.HardcodedSignalWithQuestID("site.MapRemoved");
            Site site = QuestGen_Sites.GenerateSite(new SitePartDefWithParams[]
            {
                new SitePartDefWithParams(HVMPDefOf_Odyssey.HVMP_KorhalSite, new SitePartParams
                {
                    points = slate.Get<float>("points", 0f, false),
                    threatPoints = slate.Get<float>("points", 0f, false)
                })
            }, planetTile, null, false, null, WorldObjectDefOf.ClaimableSite);
            slate.Set<Site>("site", site, false);
            quest.SpawnWorldObject(site, null, null);
            QuestPart_Choice.Choice choice = new QuestPart_Choice.Choice();
            choice.rewards.Add(new Reward_DefinedThingDef(HVMPDefOf.HVMP_Wunderchip));
            quest.RewardChoice(null, null).choices.Add(choice);
            bool flag = false;
            QuestPart.SignalListenMode signalListenMode = QuestPart.SignalListenMode.OngoingOnly;
            string text5 = "HVMP_KorhalSiteArrivedLetter".Translate();
            string text6 = "HVMP_KorhalSiteArrivedLetterText".Translate();
            quest.Letter(LetterDefOf.NeutralEvent, text, null, null, null, flag, signalListenMode, Gen.YieldSingle<Map>(site.Map), false, text6, null, text5, null, null);
            quest.End(QuestEndOutcome.Success, 0, null, text, QuestPart.SignalListenMode.OngoingOnly, false, false);
            quest.End(QuestEndOutcome.Unknown, 0, null, text2, QuestPart.SignalListenMode.OngoingOnly, false, false);
        }
    }
    public class GenStep_KorhalSite : GenStep_LargeRuins
    {
        public override int SeedPart
        {
            get
            {
                return 19882678;
            }
        }
        protected override int RegionSize
        {
            get
            {
                return 45;
            }
        }
        protected override FloatRange DefaultMapFillPercentRange
        {
            get
            {
                return new FloatRange(0.6f, 0.75f);
            }
        }
        protected override FloatRange MergeRange
        {
            get
            {
                return new FloatRange(1f, 1f);
            }
        }
        protected override int MoveRangeLimit
        {
            get
            {
                return 6;
            }
        }
        protected override int ContractLimit
        {
            get
            {
                return 6;
            }
        }
        protected override int MinRegionSize
        {
            get
            {
                return 15;
            }
        }
        protected override IntRange RuinsMinMaxRange
        {
            get
            {
                return new IntRange(2, 6);
            }
        }
        protected override LayoutDef LayoutDef
        {
            get
            {
                return LayoutDefOf.AncientRuinsReactor_Standard;
            }
        }
        protected override Faction Faction
        {
            get
            {
                return Faction.OfAncientsHostile;
            }
        }
        public override void Generate(Map map, GenStepParams parms)
        {
            this.placedBeaconLayout = false;
            base.Generate(map, parms);
        }
        protected override LayoutStructureSketch GenerateAndSpawn(CellRect rect, Map map, GenStepParams parms, LayoutDef layoutDef)
        {
            if (!this.placedBeaconLayout)
            {
                this.placedBeaconLayout = true;
                layoutDef = HVMPDefOf_Odyssey.HVMP_AncientRuins_KorhalSite;
                MapGenerator.SetVar<CellRect>("SpawnRect", rect.ExpandedBy(1));
            }
            return base.GenerateAndSpawn(rect, map, parms, layoutDef);
        }
        public override void PostMapInitialized(Map map, GenStepParams parms)
        {
            BaseGenUtility.ScatterSentryDronesInMap(GenStep_KorhalSite.SentryCountFromPointsCurve, map, Faction.OfAncientsHostile, parms);
        }
        private bool placedBeaconLayout;
        private static readonly SimpleCurve SentryCountFromPointsCurve = new SimpleCurve(new CurvePoint[]
        {
            new CurvePoint(0f, 0f),
            new CurvePoint(100f, 2f*HVMP_Mod.settings.wunderQuestDifficulty),
            new CurvePoint(1000f, 10f*HVMP_Mod.settings.wunderQuestDifficulty),
            new CurvePoint(5000f, 20f*HVMP_Mod.settings.wunderQuestDifficulty)
        });
    }
    public class RoomContents_OrbitalTargetingBeacon : RoomContentsWorker
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
        {
            CellRect cellRect = (from r in room.rects
                                 where r.Width >= 6 && r.Height >= 6
                                 orderby r.Area descending
                                 select r).FirstOrDefault<CellRect>();
            if (cellRect == default(CellRect))
            {
                Log.Error("Failed to place generator.");
                return;
            }
            RoomContents_OrbitalTargetingBeacon.SpawnBeacon(map, cellRect, faction);
            base.FillRoom(map, room, faction, threatPoints);
            RoomContents_OrbitalTargetingBeacon.SpawnDestroyedConsoles(map, room);
        }

        // Token: 0x0600B093 RID: 45203 RVA: 0x00325374 File Offset: 0x00323574
        private static void SpawnDestroyedConsoles(Map map, LayoutRoom room)
        {
            float num = (float)room.rects.Sum((CellRect r) => r.ContractedBy(1).EdgeCellsCount) / 10f;
            int num2 = Mathf.Max(Mathf.RoundToInt(RoomContents_OrbitalTargetingBeacon.DestroyedConsolesPer10EdgeCells.RandomInRange * num), 1);
            RoomGenUtility.FillAroundEdges(ThingDefOf.AncientDestroyedConsole, num2, RoomContents_OrbitalTargetingBeacon.DestroyedConsolesGroupSize, room, map, null, null, 1, 0, null, true, RotationDirection.Opposite, null, null);
        }
        private static void SpawnBeacon(Map map, CellRect largest, Faction faction)
        {
            IntVec3 zero = IntVec3.Zero;
            if (largest.Width % 2 == 0)
            {
                zero.x = 1;
            }
            if (largest.Height % 2 == 0)
            {
                zero.z = 1;
            }
            Thing thing = ThingMaker.MakeThing(HVMPDefOf_Odyssey.HVMP_OrbitalTargetingBeacon, null);
            thing.SetFaction(faction ?? Faction.OfAncientsHostile, null);
            GenSpawn.Spawn(thing, largest.CenterCell - zero, map, Rot4.Random, WipeMode.Vanish, false, false);
        }
        private static readonly FloatRange DestroyedConsolesPer10EdgeCells = new FloatRange(1f, 3f);
        private static readonly IntRange DestroyedConsolesGroupSize = new IntRange(1);
    }
    //orbital targeting hediff gains or loses severity depending on whether or not the pawn is under a roof, in a room, and/or moving. At any stage past 1st, it begins spawning blastShapes at the victim's position (frequency linearly scales w severity to maxBlastFrequencyFactor), and at final stage it can create blast shapes it couldn't b4
    public class GameCondition_OrbitalTargeting : GameCondition_InflictHediff
    {
        public override void AddHediff(Pawn pawn, InflictedHediff ih)
        {
            if (ih.hediff != null && !pawn.RaceProps.IsDrone && pawn.Faction != Faction.OfAncientsHostile)
            {
                Hediff h = HediffMaker.MakeHediff(ih.hediff,pawn);
                HediffComp_ReliantOnOrbitalTargeting hcroot = h.TryGetComp<HediffComp_ReliantOnOrbitalTargeting>();
                if (hcroot != null)
                {
                    hcroot.instigator = this.conditionCauser;
                }
                pawn.health.AddHediff(h, null, null, null);
            }
        }
    }
    public class HediffCompProperties_ReliantOnOrbitalTargeting : HediffCompProperties_ReliantOnGameCondition
    {
        public HediffCompProperties_ReliantOnOrbitalTargeting()
        {
            this.compClass = typeof(HediffComp_ReliantOnOrbitalTargeting);
        }
        public float severityPerHourSafe;
        public float severityPerHourUnroofedOrOutdoors;
        public float severityPerHourMoving;
        public float severityPerHourUnroofedAndOutdoors;
        public List<ThingDef> blastShapes;
        public List<ThingDef> blastShapesUnlockedAtFinalStage;
        public IntRange blastFrequencyTicks = new IntRange(2500);
        public float maxBlastFrequencyFactor = 3f;
    }
    public class HediffComp_ReliantOnOrbitalTargeting : HediffComp_ReliantOnGameCondition
    {
        public new HediffCompProperties_ReliantOnOrbitalTargeting Props
        {
            get
            {
                return (HediffCompProperties_ReliantOnOrbitalTargeting)this.props;
            }
        }
        public override string CompLabelInBracketsExtra
        {
            get
            {
                if (this.parent.CurStageIndex == 0)
                {
                    return (this.parent.Severity - this.parent.CurStage.minSeverity).ToStringPercent();
                } else {
                    return this.timeToNextBlast.TicksToSeconds().ToStringByStyle(ToStringStyle.Integer);
                }
            }
        }
        public override bool MeetsGameConditionQualifiers(GameCondition gc)
        {
            return base.MeetsGameConditionQualifiers(gc) && (!this.Pawn.RaceProps.IsDrone || this.Pawn.Faction == Faction.OfAncientsHostile);
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            float sevAdjustment = 0f;
            if (this.Pawn.Spawned)
            {
                IntVec3 iv3 = this.Pawn.Position;
                Map map = this.Pawn.Map;
                if (iv3.GetRoof(map) == null)
                {
                    sevAdjustment += iv3.GetRoom(map) == null ? this.Props.severityPerHourUnroofedAndOutdoors : this.Props.severityPerHourUnroofedOrOutdoors;
                    if (this.Pawn.pather.MovingNow)
                    {
                        sevAdjustment += this.Props.severityPerHourMoving;
                    }
                } else if (iv3.GetRoom(map) == null) {
                    sevAdjustment += this.Props.severityPerHourUnroofedOrOutdoors;
                    if (this.Pawn.pather.MovingNow)
                    {
                        sevAdjustment += this.Props.severityPerHourMoving;
                    }
                } else {
                    sevAdjustment += this.Props.severityPerHourSafe;
                }
                if (this.parent.CurStageIndex > 0)
                {
                    if (this.timeToNextBlast < 0)
                    {
                        List<ThingDef> blastShapes = this.Props.blastShapes;
                        if (this.parent.CurStageIndex == this.parent.def.stages.Count - 1)
                        {
                            blastShapes.AddRange(this.Props.blastShapesUnlockedAtFinalStage);
                        }
                        if (blastShapes.Count > 0)
                        {
                            Thing thing = ThingMaker.MakeThing(blastShapes.RandomElement());
                            if (thing is OrbitalStrike os)
                            {
                                os.instigator = this.instigator;
                            }
                            GenSpawn.Spawn(blastShapes.RandomElement(), this.Pawn.Position, this.Pawn.Map);
                            this.timeToNextBlast = (int)(this.Props.blastFrequencyTicks.RandomInRange/Math.Max(1f,this.parent.CurStageIndex));
                        }
                    }
                    this.timeToNextBlast -= delta;
                }
            } else {
                sevAdjustment += this.Props.severityPerHourSafe;
            }
            this.parent.Severity += sevAdjustment*delta/2500f;
            base.CompPostTickInterval(ref severityAdjustment, delta);
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.timeToNextBlast, "timeToNextBlast", -1, false);
        }
        public int timeToNextBlast = -1;
        public Thing instigator;
    }
    /*bombardments are, for some godforsaken reason, not governed by comps or really anything else modifiable. Except for the laser pointer color. This is stupid, so let's fix that. UNFORTUNATELY comp projectile interceptors' interaction with bombardments is hardcoded so the parent still needs to technically BE a bombardment... le sigh
     * Anyways, this doesn't work entirely the way I want it to, but the infinity damage vaporization blast is adequate for my purposes.*/
    public class CustomizableBombardment : Bombardment
    {
        protected override void Tick() {
            CompCustomizableBombardment ccb = this.TryGetComp<CompCustomizableBombardment>();
            if (ccb != null)
            {
                ccb.CompTick();
            }
        }
    }
    public class CompProperties_CustomizableBombardment : CompProperties
    {
        public CompProperties_CustomizableBombardment()
        {
            this.compClass = typeof(CompCustomizableBombardment);
        }
        public int shots;
        public int warmupTicks = 60;
        public int ticksBetweenShots = 18;
        public float shotSpread;
        public FloatRange explosionRadiusRange = new FloatRange(6f, 8f);
        public DamageDef explosionType;
        public int explosionAmount;
        public float explosionArmorPen;
        public float thickRoofInterceptChance = 0.5f;
        public SoundDef preImpactSound;
    }
    public class CompCustomizableBombardment : ThingComp
    {
        public CompProperties_CustomizableBombardment Props
        {
            get
            {
                return (CompProperties_CustomizableBombardment)this.props;
            }
        }
        protected int TicksPassed
        {
            get
            {
                return Find.TickManager.TicksGame - this.startTick;
            }
        }
        protected int TicksLeft
        {
            get
            {
                return (this.duration + this.warmupTicks) - this.TicksPassed;
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            this.warmupTicks = this.Props.warmupTicks;
        }
        public void StartStrike()
        {
            if (this.parent.Spawned)
            {
                this.duration = this.Props.ticksBetweenShots * this.Props.shots;
                this.angle = CompCustomizableBombardment.AngleRange.RandomInRange;
                this.startTick = Find.TickManager.TicksGame;
                CompAffectsSky cas = this.parent.GetComp<CompAffectsSky>();
                if (cas != null)
                {
                    cas.StartFadeInHoldFadeOut(30, this.duration - 30 - 15, 15, 1f);
                }
            }
        }
        public void GetNextExplosionCell()
        {
            if (this.Props.shotSpread < 1f)
            {
                this.nextExplosionCell = this.parent.Position;
            } else {
                this.nextExplosionCell = (from x in GenRadial.RadialCellsAround(this.parent.Position, this.Props.shotSpread, true)
                                          where x.InBounds(this.parent.Map)
                                          select x).RandomElementByWeight((IntVec3 x) => Bombardment.DistanceChanceFactor.Evaluate(x.DistanceTo(this.parent.Position) / this.Props.shotSpread));
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                this.GetNextExplosionCell();
            }
            CompOrbitalBeam cob = this.parent.GetComp<CompOrbitalBeam>();
            if (cob != null)
            {
                cob.StartAnimation(this.warmupTicks + this.duration, 10, this.angle);
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (this.warmupTicks > 0)
            {
                this.warmupTicks--;
                if (this.warmupTicks <= 0)
                {
                    this.StartStrike();
                }
            } else {
                if (this.TicksPassed > this.duration + this.warmupTicks)
                {
                    this.parent.Destroy();
                }
            }
            this.EffectTick();
        }
        public void EffectTick()
        {
            if (!this.nextExplosionCell.IsValid)
            {
                this.ticksToNextEffect = this.warmupTicks - this.Props.ticksBetweenShots;
                this.GetNextExplosionCell();
            }
            this.ticksToNextEffect--;
            if (this.ticksToNextEffect <= 0 && this.TicksLeft >= this.Props.ticksBetweenShots)
            {
                this.Props.preImpactSound.PlayOneShot(new TargetInfo(this.nextExplosionCell, this.parent.Map, false));
                this.TryDoExplosion(new Bombardment.BombardmentProjectile(1, this.nextExplosionCell));
                this.ticksToNextEffect = this.Props.ticksBetweenShots;
                this.GetNextExplosionCell();
            }
        }
        public void TryDoExplosion(Bombardment.BombardmentProjectile proj)
        {
            IntVec3 targetCell = this.nextExplosionCell;
            Map map = this.parent.Map;
            if (Rand.Chance(this.Props.thickRoofInterceptChance))
            {
                RoofDef rd = targetCell.GetRoof(map);
                if (rd != null && rd.isThickRoof)
                {
                    return;
                }
            }
            if (this.parent is Bombardment bobby)
            {
                List<Thing> list = this.parent.Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].TryGetComp<CompProjectileInterceptor>().CheckBombardmentIntercept(bobby, proj))
                    {
                        return;
                    }
                }
            }
            GenExplosion.DoExplosion(targetCell, map, this.Props.explosionRadiusRange.RandomInRange, this.Props.explosionType, this.compInstigator, this.Props.explosionAmount, this.Props.explosionArmorPen, null, this.weaponDef, this.parent.def, null, null, 0f, 1, null, null, 255, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f, null, null, null, null);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look<Thing>(ref this.compInstigator, "compInstigator", false);
            Scribe_Defs.Look<ThingDef>(ref this.weaponDef, "weaponDef");
            Scribe_Values.Look<int>(ref this.duration, "duration", 0, false);
            Scribe_Values.Look<float>(ref this.angle, "angle", 0f, false);
            Scribe_Values.Look<int>(ref this.startTick, "startTick", 0, false);
            Scribe_Values.Look<int>(ref this.warmupTicks, "warmupTicks", 0, false);
            Scribe_Values.Look<int>(ref this.ticksToNextEffect, "ticksToNextEffect", 0, false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (!this.nextExplosionCell.IsValid)
                {
                    this.GetNextExplosionCell();
                }
            }
        }
        public IntVec3 nextExplosionCell = IntVec3.Invalid;
        public Thing compInstigator;
        public ThingDef weaponDef;
        public int duration;
        public float angle;
        public int startTick;
        public int warmupTicks = 60;
        public int ticksToNextEffect;
        protected static readonly FloatRange AngleRange = new FloatRange(-12f, 12f);
    }
}
