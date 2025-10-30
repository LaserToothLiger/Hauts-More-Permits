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
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Grammar;
using Verse.Noise;
using Verse.Sound;
using static RimWorld.QuestPart;
using static System.Net.Mime.MediaTypeNames;
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
    public class RoyalTitlePermitWorker_HealStim : RoyalTitlePermitWorker_GiveHediffs_PTargFriendly
    {
        public override void GiveHediffInCaravanInner(Pawn caller, Faction faction, bool free, Caravan caravan)
        {
            Pawn bestPawn = caller;
            float bestScore = 0f;
            foreach (Pawn p in caravan.pawns)
            {
                float score = 0f;
                foreach (Hediff h in p.health.hediffSet.hediffs)
                {
                    if (h is Hediff_Injury)
                    {
                        score += h.Severity;
                    }
                }
                score /= Math.Max(0.5f,p.GetStatValue(StatDefOf.InjuryHealingFactor));
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPawn = p;
                }
            }
            this.AffectPawn(bestPawn, faction);
        }
    }
    public class RoyalTitlePermitWorker_ImmunityStim : RoyalTitlePermitWorker_GiveHediffs_PTargFriendly
    {
        public override void GiveHediffInCaravanInner(Pawn caller, Faction faction, bool free, Caravan caravan)
        {
            Pawn bestPawn = caller;
            float bestScore = 0f;
            foreach (Pawn p in caravan.pawns)
            {
                float score = 0f;
                foreach (Hediff h in p.health.hediffSet.hediffs)
                {
                    HediffComp_Immunizable hci = h.TryGetComp<HediffComp_Immunizable>();
                    if (hci != null)
                    {
                        score += Math.Max(0f,(h.Severity/h.def.lethalSeverity) - hci.Immunity);
                    }
                }
                score /= Math.Max(0.5f, p.GetStatValue(StatDefOf.ImmunityGainSpeed));
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPawn = p;
                }
            }
            this.AffectPawn(bestPawn, faction);
        }
    }
    public class RoyalTitlePermitWorker_TempregStim : RoyalTitlePermitWorker_GiveHediffs_PTargFriendly
    {
        public override void GiveHediffInCaravanInner(Pawn caller, Faction faction, bool free, Caravan caravan)
        {
            Pawn bestPawn = caller;
            float bestScore = 0f;
            foreach (Pawn p in caravan.pawns)
            {
                float burnup = Math.Max(0f, p.AmbientTemperature - p.GetStatValue(StatDefOf.ComfyTemperatureMax));
                Hediff heatstroke = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Heatstroke);
                if (heatstroke != null)
                {
                    burnup *= (1f+heatstroke.Severity);
                }
                float cooldown = Math.Max(0f, p.GetStatValue(StatDefOf.ComfyTemperatureMin) - p.AmbientTemperature);
                Hediff hypothermia = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Hypothermia);
                if (hypothermia != null)
                {
                    burnup *= (1f + hypothermia.Severity);
                }
                float score = burnup + cooldown;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPawn = p;
                }
            }
            this.AffectPawn(bestPawn, faction);
        }
    }
    public class RoyalTitlePermitWorker_ToxtolStim : RoyalTitlePermitWorker_GiveHediffs_PTargFriendly
    {
        public override void GiveHediffInCaravanInner(Pawn caller, Faction faction, bool free, Caravan caravan)
        {
            Pawn bestPawn = caller;
            float bestScore = 0f;
            foreach (Pawn p in caravan.pawns)
            {
                float score = p.GetStatValue(StatDefOf.ToxicEnvironmentResistance);
                Hediff toxBuildup = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup);
                if (toxBuildup != null)
                {
                    score *= (1f + toxBuildup.Severity);
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPawn = p;
                }
            }
            this.AffectPawn(bestPawn, faction);
        }
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
        public float dustRange;
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
                if (this.Props.dustRange > 0f)
                {
                    int maxDust = 10;
                    foreach (IntVec3 intVec in GenRadial.RadialCellsAround(this.parent.Position, this.Props.dustRange, true).InRandomOrder())
                    {
                        if (maxDust > 0 && Rand.Chance(0.6f))
                        {
                            FleckMaker.ThrowDustPuffThick(intVec.ToVector3Shifted(), this.parent.Map, Rand.Range(1f, 3f), CompAbilityEffect_Wallraise.DustColor);
                            maxDust--;
                        }
                    }
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
    public class QuestNode_AddNeopathy : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            if (this.pawns.GetValue(slate) == null || this.hediffDef.GetValue(slate) == null)
            {
                return;
            }
            QuestPart_AddNeopathy questPart_AddHediff = new QuestPart_AddNeopathy();
            questPart_AddHediff.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
            questPart_AddHediff.hediffDef = this.hediffDef.GetValue(slate);
            questPart_AddHediff.pawns.AddRange(this.pawns.GetValue(slate));
            questPart_AddHediff.addToHyperlinks = this.addToHyperlinks.GetValue(slate);
            bool mayhemMode = HVMP_Mod.settings.csX;
            bool DD_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.cs1, mayhemMode);
            bool DEM_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.cs2, mayhemMode);
            bool LD_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.cs3, mayhemMode);
            if (DD_on)
            {
                questPart_AddHediff.DD_bonusComplications += this.DD_bonusComplications.RandomInRange;
                questPart_AddHediff.DD_on = true;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_DD_info", this.DD_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_DD_info", " ") });
            }
            if (DEM_on)
            {
                questPart_AddHediff.DEM_failureChance = this.DEM_failureChance;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_DEM_info", this.DEM_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_DEM_info", " ") });
            }
            if (LD_on)
            {
                questPart_AddHediff.LD_bonusComplications += this.LD_bonusComplications.RandomInRange;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_LD_info", this.LD_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_LD_info", " ") });
            }
            QuestGen.quest.AddPart(questPart_AddHediff);
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        public SlateRef<IEnumerable<Pawn>> pawns;
        public SlateRef<HediffDef> hediffDef;
        public SlateRef<IEnumerable<BodyPartDef>> partsToAffect;
        public SlateRef<bool> checkDiseaseContractChance;
        public SlateRef<bool> addToHyperlinks;
        public IntRange DD_bonusComplications;
        [MustTranslate]
        public string DD_description;
        public float DEM_failureChance;
        [MustTranslate]
        public string DEM_description;
        public IntRange LD_bonusComplications;
        [MustTranslate]
        public string LD_description;
    }
    public class QuestPart_AddNeopathy : QuestPart
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
                for (int i = 0; i < this.pawns.Count; i = num + 1)
                {
                    yield return this.pawns[i];
                    num = i;
                }
                yield break;
            }
        }
        public override IEnumerable<Dialog_InfoCard.Hyperlink> Hyperlinks
        {
            get
            {
                foreach (Dialog_InfoCard.Hyperlink hyperlink in base.Hyperlinks)
                {
                    yield return hyperlink;
                }
                if (this.addToHyperlinks)
                {
                    yield return new Dialog_InfoCard.Hyperlink(this.hediffDef, -1);
                }
                yield break;
            }
        }
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                for (int i = 0; i < this.pawns.Count; i++)
                {
                    if (!this.pawns[i].DestroyedOrNull())
                    {
                        Hediff h = HediffMaker.MakeHediff(this.hediffDef, this.pawns[i]);
                        HediffComp_NeopathyComplications hcnp = h.TryGetComp<HediffComp_NeopathyComplications>();
                        if (hcnp != null)
                        {
                            if (this.DD_on)
                            {
                                hcnp.DD_on = true;
                            }
                            hcnp.DEM_failureChance = this.DEM_failureChance;
                            hcnp.maxComplications += this.DD_bonusComplications + this.LD_bonusComplications;
                            for (int j = this.LD_bonusComplications; j > 0; j--)
                            {
                                hcnp.GainComplication();
                            }
                        }
                        this.pawns[i].health.AddHediff(h);
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<Pawn>(ref this.pawns, "pawns", LookMode.Reference, Array.Empty<object>());
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_Defs.Look<HediffDef>(ref this.hediffDef, "hediffDef");
            Scribe_Values.Look<bool>(ref this.addToHyperlinks, "addToHyperlinks", false, false);
            Scribe_Values.Look<bool>(ref this.DD_on, "DD_on", false, false);
            Scribe_Values.Look<float>(ref this.DEM_failureChance, "DEM_failureChance", 0f, false);
            Scribe_Values.Look<int>(ref this.LD_bonusComplications, "LD_bonusComplications", 0, false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.pawns.RemoveAll((Pawn x) => x == null);
            }
        }
        public override void AssignDebugData()
        {
            base.AssignDebugData();
            this.inSignal = "DebugSignal" + Rand.Int.ToString();
            this.hediffDef = HediffDefOf.Anesthetic;
            this.pawns.Add(PawnsFinder.AllMaps_FreeColonists.FirstOrDefault<Pawn>());
        }
        public override void ReplacePawnReferences(Pawn replace, Pawn with)
        {
            this.pawns.Replace(replace, with);
        }
        public List<Pawn> pawns = new List<Pawn>();
        public string inSignal;
        public HediffDef hediffDef;
        public bool addToHyperlinks;
        public bool DD_on;
        public float DEM_failureChance;
        public int DD_bonusComplications;
        public int LD_bonusComplications;
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
            Hediff hediff = pawn.health.hediffSet.hediffs.Find((Hediff x) => x.def == this.recipe.removesHediff && x.Part == part && x.Visible);
            bool shouldGetRemoved = true;
            if (hediff != null)
            {
                HediffComp_NeopathyComplications hcnp = hediff.TryGetComp<HediffComp_NeopathyComplications>();
                if (hcnp != null && Rand.Chance(hcnp.DEM_failureChance))
                {
                    shouldGetRemoved = false;
                }
            }
            if (billDoer != null)
            {
                if (base.CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                {
                    return;
                }
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, new object[] { billDoer, pawn });
                if (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(billDoer))
                {
                    if (shouldGetRemoved)
                    {
                        string text;
                        if (!this.recipe.successfullyRemovedHediffMessage.NullOrEmpty())
                        {
                            text = this.recipe.successfullyRemovedHediffMessage.Formatted(billDoer.LabelShort, pawn.LabelShort);
                        } else {
                            text = "MessageSuccessfullyRemovedHediff".Translate(billDoer.LabelShort, pawn.LabelShort, this.recipe.removesHediff.label.Named("HEDIFF"), billDoer.Named("SURGEON"), pawn.Named("PATIENT"));
                        }
                        Messages.Message(text, pawn, MessageTypeDefOf.PositiveEvent, true);
                    } else {
                        string text = "HVMP_NeopathyCureFailed".Translate(billDoer.LabelShort, pawn.LabelShort);
                        Messages.Message(text, pawn, MessageTypeDefOf.PositiveEvent, true);
                    }
                }
            }
            if (hediff != null && shouldGetRemoved)
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
        public List<HediffDef> DD_complications;
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
                    this.GainComplication();
                }
            }
        }
        public void GainComplication()
        {
            if (!this.Props.possibleComplications.NullOrEmpty())
            {
                List<HediffDef> compPool = this.Props.possibleComplications;
                if (this.DD_on)
                {
                    compPool.AddRange(this.Props.DD_complications);
                }
                Hediff complication = HediffMaker.MakeHediff(compPool.RandomElement(), this.Pawn);
                this.Pawn.health.AddHediff(complication);
                this.complicationsCreated++;
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
                if (this.Props.possibleComplications.Contains(h.def) || this.Props.DD_complications.Contains(h.def))
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
            Scribe_Values.Look<bool>(ref this.DD_on, "DD_on", false, false);
            Scribe_Values.Look<float>(ref this.DEM_failureChance, "DEM_failureChance", 0f, false);
        }
        public int maxComplications;
        public int ticksToNextComplication;
        public int complicationsCreated;
        public int numTends;
        public bool cureDiscovered;
        public bool DD_on;
        public float DEM_failureChance;
    }
    public class HediffCompProperties_AcceleratedAging : HediffCompProperties
    {
        public HediffCompProperties_AcceleratedAging()
        {
            this.compClass = typeof(HediffComp_AcceleratedAging);
        }
        public int daysPerDay;
    }
    public class HediffComp_AcceleratedAging : HediffComp
    {
        public HediffCompProperties_AcceleratedAging Props
        {
            get
            {
                return (HediffCompProperties_AcceleratedAging)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(60000,delta))
            {
                this.Pawn.ageTracker.AgeBiologicalTicks += (this.Props.daysPerDay*60000);
            }
        }
    }
    public class HediffCompProperties_Infestation : HediffCompProperties
    {
        public HediffCompProperties_Infestation()
        {
            this.compClass = typeof(HediffComp_Infestation);
        }
        public float MTBdays;
    }
    public class HediffComp_Infestation : HediffComp
    {
        public HediffCompProperties_Infestation Props
        {
            get
            {
                return (HediffCompProperties_Infestation)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(2500, delta) && this.Pawn.SpawnedOrAnyParentSpawned && Rand.MTBEventOccurs(this.Props.MTBdays, 60000f, 2500))
            {
                IncidentParms incidentParms = new IncidentParms();
                incidentParms.target = this.Pawn.MapHeld;
                incidentParms.points = StorytellerUtility.DefaultThreatPointsNow(this.Pawn.MapHeld);
                incidentParms.infestationLocOverride = new IntVec3?(this.Pawn.PositionHeld);
                incidentParms.forced = true;
                IncidentDefOf.Infestation.Worker.TryExecute(incidentParms);
            }
        }
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
    //environmental control
    public class QuestNode_DNLU_SG : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            for (int i = 0; i < this.GB_LoopCount; i++)
            {
                if (this.storeLoopCounterAs.GetValue(slate) != null)
                {
                    slate.Set<int>(this.storeLoopCounterAs.GetValue(slate), i, false);
                }
                try
                {
                    if (!this.node.TestRun(slate))
                    {
                        return false;
                    }
                } finally {
                    slate.PopPrefix();
                }
            }
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            bool mayhemMode = HVMP_Mod.settings.ecX;
            bool DNLU_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.ec1, mayhemMode);
            bool GB_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.ec2, mayhemMode);
            int counter = GB_on ?this.GB_LoopCount:this.nonGB_LoopCount;
            if (DNLU_on)
            {
                QuestGen.slate.Set<bool>(this.DNLU_saveAs.GetValue(slate), true, false);
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_DNLU_info", this.DNLU_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_DNLU_info", " ") });
            }
            if (GB_on)
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_GB_info", this.GB_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_GB_info", " ") });
            }
            for (int i = 0; i < counter; i++)
            {
                if (this.storeLoopCounterAs.GetValue(slate) != null)
                {
                    QuestGen.slate.Set<int>(this.storeLoopCounterAs.GetValue(slate), i, false);
                }
                try
                {
                    this.node.Run();
                } finally {
                    QuestGen.slate.PopPrefix();
                }
            }
        }
        public QuestNode node;
        public int nonGB_LoopCount;
        public int GB_LoopCount;
        [NoTranslate]
        public SlateRef<string> storeLoopCounterAs;
        [MustTranslate]
        public string GB_description;
        [NoTranslate]
        public SlateRef<string> DNLU_saveAs;
        [MustTranslate]
        public string DNLU_description;
    }
    public class QuestNode_MultiProblemCauserGenerator : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            if (!this.TryFindTile(slate, out PlanetTile planetTile))
            {
                return false;
            }
            bool? value = this.clampRangeBySiteParts.GetValue(slate);
            if (((value.GetValueOrDefault()) & (value != null)) && this.sitePartDefs.GetValue(slate) == null)
            {
                return false;
            }
            this.SetVars(QuestGen.slate, planetTile, out List<SitePartDefWithParams> spdwp);
            if (!Find.Storyteller.difficulty.allowViolentQuests && !spdwp.NullOrEmpty())
            {
                using (IEnumerator<SitePartDefWithParams> enumerator = spdwp.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.def.wantsThreatPoints)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;
            PlanetTile planetTile;
            if (this.TryFindTile(QuestGen.slate, out planetTile))
            {
                this.SetVars(slate,planetTile, out List<SitePartDefWithParams> spdwp);
                Site site = QuestGen_Sites.GenerateSite(spdwp, planetTile , this.faction.GetValue(slate), this.hiddenSitePartsPossible.GetValue(slate), this.SingleSitePartRules, this.worldObjectDef.GetValue(slate));
                site.SetFaction(this.faction.GetValue(slate));
                quest.AddPart(new QuestPart_LookOverHere(site));
                IEnumerable<WorldObject> wosIEnum = this.worldObjects.GetValue(slate);
                List<WorldObject> wos = wosIEnum != null ? wosIEnum.ToList(): new List<WorldObject>();
                if (wos != null)
                {
                    wos.Add(site);
                } else {
                    wos = new List<WorldObject> { site };
                }
                slate.Set<List<WorldObject>>(this.storeAs.GetValue(slate), wos, false);
                Thing conditionCauser = slate.Get<Thing>("conditionCauser", null, false);
                if (conditionCauser != null)
                {
                    string text = QuestGen.GenerateNewSignal("AllConditionCausersDestroyed", false);
                    string text2 = QuestGen.GenerateNewSignal("ConditionCauserHacked", false);
                    IEnumerable<string> dsIEnum = this.destroyedStrings.GetValue(slate);
                    List<string> ds = dsIEnum != null ? dsIEnum.ToList() : new List<String>();
                    int i = this.iterator.GetValue(slate);
                    string text3 = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("terminal" + i.ToString());
                    QuestUtility.AddQuestTag(conditionCauser, text3);
                    string text4 = QuestGenUtility.HardcodedSignalWithQuestID(text3 + ".Destroyed");
                    ds.Add(text4);
                    slate.Set<int>(this.storeIteratorAs.GetValue(slate),i + 1);
                    slate.Set<List<string>>(this.storeStringsAs.GetValue(slate), ds, false);
                    QuestPart_PassAllActivable questPart_PassAllActivable;
                    if (!quest.TryGetFirstPartOfType<QuestPart_PassAllActivable>(out questPart_PassAllActivable))
                    {
                        questPart_PassAllActivable = quest.AddPart<QuestPart_PassAllActivable>();
                        questPart_PassAllActivable.inSignalEnable = QuestGen.slate.Get<string>("inSignal", null, false);
                    }
                    questPart_PassAllActivable.inSignals = ds;
                    questPart_PassAllActivable.outSignalsCompleted.Add(text);
                    questPart_PassAllActivable.outSignalAny = text2;
                }
                if (this.DNLU_flag.GetValue(slate))
                {
                    Mutator_DNLU component = site.GetComponent<Mutator_DNLU>();
                    if (component != null)
                    {
                        component.DNLU_on = true;
                    }
                }
            }
        }
        private RulePack SingleSitePartRules
        {
            get
            {
                Slate slate = QuestGen.slate;
                QuestScriptDef qsd = DefDatabase<QuestScriptDef>.GetNamed("Util_GenerateSite");
                if (qsd != null && qsd.root is QuestNode_GenerateSite qngs)
                {
                    return qngs.singleSitePartRules.GetValue(slate);
                }
                return this.singleSitePartRules.GetValue(slate);
            }
        }
        private bool TryFindTile(Slate slate, out PlanetTile tile)
        {
            bool value = this.canSelectSpace.GetValue(slate);
            Map map = slate.Get<Map>("map", null, false) ?? (value ? Find.RandomPlayerHomeMap : Find.RandomSurfacePlayerHomeMap);
            PlanetTile planetTile = ((map != null) ? map.Tile : PlanetTile.Invalid);
            if (planetTile.Valid && planetTile.LayerDef.isSpace && !value)
            {
                planetTile = PlanetTile.Invalid;
            }
            int num = int.MaxValue;
            bool? value2 = this.clampRangeBySiteParts.GetValue(slate);
            if (value2 != null && value2.Value)
            {
                foreach (SitePartDef sitePartDef in this.sitePartDefs.GetValue(slate))
                {
                    if (sitePartDef.conditionCauserDef != null)
                    {
                        num = Mathf.Min(num, sitePartDef.conditionCauserDef.GetCompProperties<CompProperties_CausesGameCondition>().worldRange);
                    }
                }
            }
            TileFinderMode tileFinderMode = (this.preferCloserTiles.GetValue(slate) ? TileFinderMode.Near : TileFinderMode.Random);
            float num2 = ((!ModsConfig.OdysseyActive) ? 0f : (this.selectLandmarkChance.GetValue(slate) ?? 0.5f));
            return TileFinder.TryFindNewSiteTile(out tile, planetTile, Mathf.Min(siteDistRange.min,num), Mathf.Min(siteDistRange.max,num), this.allowCaravans.GetValue(slate), this.allowedLandmarks.GetValue(slate), num2, this.canSelectComboLandmarks.GetValue(slate), tileFinderMode, false, value, null, null);
        }
        private void SetVars(Slate slate, PlanetTile planetTile, out List<SitePartDefWithParams> spdwp)
        {
            List<SitePartDefWithParams> list;
            SiteMakerHelper.GenerateDefaultParams(slate.Get<float>("points", 0f, false), planetTile, this.faction.GetValue(slate), this.sitePartDefs.GetValue(slate), out list);
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].def == SitePartDefOf.PreciousLump)
                    {
                        list[i].parms.preciousLumpResources = slate.Get<ThingDef>("targetMineable", null, false);
                    }
                }
            }
            slate.Set<List<SitePartDefWithParams>>(this.storeSitePartsParamsAs.GetValue(slate), list, false);
            spdwp = list;
        }
        public SlateRef<bool> preferCloserTiles;
        public SlateRef<bool> allowCaravans;
        public SlateRef<bool> canSelectSpace;
        public SlateRef<bool?> clampRangeBySiteParts;
        public SlateRef<IEnumerable<SitePartDef>> sitePartDefs;
        public SlateRef<List<LandmarkDef>> allowedLandmarks;
        public SlateRef<float?> selectLandmarkChance;
        public SlateRef<bool> canSelectComboLandmarks;
        public SlateRef<Faction> faction;
        [NoTranslate]
        public SlateRef<string> storeSitePartsParamsAs;
        public SlateRef<bool> hiddenSitePartsPossible;
        public SlateRef<RulePack> singleSitePartRules;
        public SlateRef<WorldObjectDef> worldObjectDef;
        public SlateRef<IEnumerable<WorldObject>> worldObjects;
        [NoTranslate]
        public SlateRef<string> storeAs;
        public SlateRef<IEnumerable<string>> destroyedStrings;
        [NoTranslate]
        public SlateRef<string> storeStringsAs;
        public SlateRef<int> iterator;
        [NoTranslate]
        public SlateRef<string> storeIteratorAs;
        public IntRange siteDistRange;
        public SlateRef<bool> DNLU_flag;
    }
    public class QuestNode_LAS : QuestNode_Delay
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.ec3, HVMP_Mod.settings.ecX))
            {
                base.RunInt();
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_LAS_info", this.LAS_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_LAS_info", " ") });
            }
            slate.Set<Faction>(this.storeMechFactionAs.GetValue(slate), Faction.OfMechanoids ?? HVMP_Utility.GetAnEnemyFaction(), false);
        }
        [MustTranslate]
        public string LAS_description;
        [NoTranslate]
        public SlateRef<string> storeMechFactionAs;
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
            float baseSize = largestSize * (0.6f + (0.4f * Rand.Value));
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.fw1, HVMP_Mod.settings.fwX))
            {
                baseSize = Math.Max(baseSize*this.C2C_factor,baseSize+this.C2C_minBonus);
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
            return Mathf.Min(new int[] { (int)(cellRect.Width/2), (int)(cellRect.Height/2), value });
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
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.fw3, mayhemMode))
                {
                    np.ticksRemaining = (int)(np.ticksRemaining * this.S2S_factor);
                }
                np.initialTicks = np.ticksRemaining;
                slate.Set<float>("preserveDays", np.ticksRemaining/60000f, false);
                slate.Set<Thing>(this.storeAs.GetValue(slate), np, false);
                QuestGen.quest.AddPart(new QuestPart_LookAtThis(np));
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.fw2, mayhemMode))
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
            return Find.Storyteller.difficulty.allowViolentQuests && slate.Exists("map", false) && AggressiveAnimalIncidentUtility.TryFindAggressiveAnimalKind(slate.Get<float>("points", 0f, false), slate.Get<Map>("map", null, false), out PawnKindDef pawnKindDef);
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
            bool mayhemMode = HVMP_Mod.settings.hdX;
            QuestPart_IncidentMutantMPs questPart_Incident = new QuestPart_IncidentMutantMPs
            {
                incident = HVMPDefOf.HVMP_MutantManhunterPack,
                mutation = hd
            };
            HediffDef hd2 = null;
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.hd1, mayhemMode))
            {
                List<HediffDef> possibleSecondMutations = mp.mutations;
                possibleSecondMutations.Remove(hd);
                hd2 = possibleSecondMutations.RandomElement();
                questPart_Incident.DT_mutation = hd2;
                slate.Set<string>("DT_mutationLabel", hd2.label, false);
                slate.Set<string>("DT_mutationDesc", hd2.description, false);
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_DT_info", this.DT_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_DT_info", " ") });
            }
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.hd3, mayhemMode))
            {
                questPart_Incident.UP_chance = this.UP_chance;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_UP_info", this.UP_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_UP_info", " ") });
            }
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
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.hd2, mayhemMode))
            {
                incidentParms.points *= this.MM_factor;
            }
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
            List<Rule> list = new List<Rule>
            {
                new Rule_String("animalKind_label", pawnKindDef.label),
                new Rule_String("animalKind_labelPlural", pawnKindDef.GetLabelPlural(num2))
            };
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
        [MustTranslate]
        public string DT_description;
        public float MM_factor;
        public float UP_chance;
        [MustTranslate]
        public string UP_description;
    }
    public class QuestPart_IncidentMutantMPs : QuestPart_Incident
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<HediffDef>(ref this.mutation, "mutation");
            Scribe_Defs.Look<HediffDef>(ref this.DT_mutation, "DT_mutation");
            Scribe_Values.Look<float>(ref this.UP_chance, "UP_chance", 0f, false);
        }
        public HediffDef mutation;
        public HediffDef DT_mutation;
        public float UP_chance;
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
                MutationsPool mp = HVMPDefOf.HVMP_MutantManhunterPack.GetModExtension<MutationsPool>();
                List<HediffDef> possibleMutations = mp.mutations;
                possibleMutations.Remove(qpimp.mutation);
                if (qpimp.DT_mutation != null && possibleMutations.Contains(qpimp.DT_mutation))
                {
                    possibleMutations.Remove(qpimp.DT_mutation);
                }
                for (int i = 0; i < list.Count; i++)
                {
                    Pawn pawn = list[i];
                    IntVec3 intVec = CellFinder.RandomClosewalkCellNear(spawnCenter, map, 10, null);
                    QuestUtility.AddQuestTag(GenSpawn.Spawn(pawn, intVec, map, rot, WipeMode.Vanish, false, false), parms.questTag);
                    pawn.health.AddHediff(HediffDefOf.Scaria, null, null, null);
                    pawn.health.AddHediff(qpimp.mutation, null, null, null);
                    if (qpimp.DT_mutation != null)
                    {
                        pawn.health.AddHediff(qpimp.DT_mutation, null, null, null);
                    }
                    if (Rand.Chance(qpimp.UP_chance))
                    {
                        pawn.health.AddHediff(possibleMutations.RandomElement());
                    }
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
                bool mayhemMode = HVMP_Mod.settings.raX;
                CompRetroviralInjection cri = thing.TryGetComp<CompRetroviralInjection>();
                if (cri != null)
                {
                    if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.ra1, mayhemMode))
                    {
                        cri.BSL4_on = true;
                        QuestGen.AddQuestDescriptionRules(new List<Rule>
                        {
                            new Rule_String("mutator_BSL4_info", this.BSL4_description.Formatted())
                        });
                    } else {
                        QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_BSL4_info", " ") });
                    }
                    if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.ra2, mayhemMode))
                    {
                        CompHackableRA chra = thing.TryGetComp<CompHackableRA>();
                        if (chra != null)
                        {
                            chra.CC_on = true;
                        }
                        QuestGen.AddQuestDescriptionRules(new List<Rule>
                        {
                            new Rule_String("mutator_CC_info", this.CC_description.Formatted())
                        });
                    } else {
                        QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_CC_info", " ") });
                    }
                    if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.ra3, mayhemMode))
                    {
                        cri.PG_on = true;
                        cri.effectOnInjection = cri.Props.possibleInjectionEffects.Where((RetroviralEffectDef red) => red.isBad).RandomElement();
                        QuestGen.AddQuestDescriptionRules(new List<Rule>
                        {
                            new Rule_String("mutator_PG_info", this.PG_description.Formatted())
                        });
                    } else {
                        QuestGen.AddQuestDescriptionRules(new List<Rule>
                        {
                            new Rule_String("mutator_PG_info", this.nonPG_description.Formatted())
                        });
                    }
                }
                QuestGen.quest.AddPart(new QuestPart_LookAtThis(thing));
            }
        }
        [NoTranslate]
        public SlateRef<string> storeAs;
        public ThingDef thingDef;
        [MustTranslate]
        public string BSL4_description;
        [MustTranslate]
        public string CC_description;
        [MustTranslate]
        public string nonPG_description;
        [MustTranslate]
        public string PG_description;
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
        public bool isBad;
    }
    public class CompProperties_HackableRA : CompProperties_Hackable
    {
        public CompProperties_HackableRA()
        {
            this.compClass = typeof(CompHackableRA);
        }
    }
    public class CompHackableRA : CompHackable
    {
        public new CompProperties_HackableRA Props
        {
            get
            {
                return (CompProperties_HackableRA)this.props;
            }
        }
        public override string CompInspectStringExtra()
        {
            if (!this.CC_on)
            {
                return null;
            }
            return base.CompInspectStringExtra();
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.parent.Spawned && !this.IsHacked && !this.CC_on)
            {
                this.Hack(this.defence, null, true);
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!this.CC_on)
            {
                yield break;
            }
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            yield break;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.CC_on, "CC_on", false, false);
        }
        public bool CC_on;
    }
    public class CompProperties_RetroviralInjection : CompProperties_UseEffect
    {
        public CompProperties_RetroviralInjection()
        {
            this.compClass = typeof(CompRetroviralInjection);
        }
        public float chanceForInjectionEffect;
        public List<RetroviralEffectDef> possibleInjectionEffects;
        public float BSL4_diseaseMTBdays;
        public int PG_extraGoodwillPenalty;
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
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.parent.IsHashIntervalTick(2500,delta) && Rand.MTBEventOccurs(this.Props.BSL4_diseaseMTBdays, 60000f, 2500) && this.parent.SpawnedOrAnyParentSpawned && this.BSL4_on)
            {
                CompStudiableQuestItem csqi = this.parent.GetComp<CompStudiableQuestItem>();
                if (csqi != null && csqi.curProgress > 0f)
                {
                    HautsUtility.DoRandomDiseaseOutbreak(this.parent);
                }
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
            if (this.PG_on)
            {
                this.effectOnInjection = this.Props.possibleInjectionEffects.Where((RetroviralEffectDef red) => red.isBad).RandomElement();
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look<RetroviralEffectDef>(ref this.effectOnInjection, "effectOnInjection");
            Scribe_Values.Look<bool>(ref this.BSL4_on, "BSL4_on", false, false);
            Scribe_Values.Look<bool>(ref this.PG_on, "PG_on", false, false);
        }
        public RetroviralEffectDef effectOnInjection;
        public bool BSL4_on;
        public bool PG_on;
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
            if (this.Item != null)
            {
                CompHackableRA chra = this.Item.TryGetComp<CompHackableRA>();
                if (chra != null && chra.CC_on && !chra.IsHacked)
                {
                    Messages.Message("HVMP_MustHackToInject".Translate(), this.Pawn, MessageTypeDefOf.NeutralEvent, true);
                    yield break;
                }
            }
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
                if (rinject.BSL4_on)
                {
                    HautsUtility.DoRandomDiseaseOutbreak(this.Pawn);
                }
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
                    Faction.OfPlayer.TryAffectGoodwillWith(this.Pawn.HomeFaction, -(70+(rinject.PG_on?rinject.Props.PG_extraGoodwillPenalty :0)), true, !this.Pawn.HomeFaction.temporary, HVMP_BDefOf.HVMP_PerformedHarmfulInjection, null);
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
