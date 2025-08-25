using HautsPermits;
using RimWorld.QuestGen;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld.Planet;
using UnityEngine;
using Verse.Grammar;
using Verse.Sound;
using HautsFramework;
using static UnityEngine.GraphicsBuffer;
using System.Reflection;
using static RimWorld.PsychicRitualRoleDef;
using Verse.AI.Group;
using System.Collections;
using System.Text.RegularExpressions;
using Verse.AI;
using LudeonTK;
using System.Security.Cryptography;
using Verse.Noise;
using DelaunatorSharp;
using HarmonyLib;
using VFECore.Abilities;

namespace HautsPermits_Occult
{
    [StaticConstructorOnStartup]
    public class HautsPermits_Occult
    {
        private static readonly Type patchType = typeof(HautsPermits_Occult);
        static HautsPermits_Occult()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautspermits.anomaly");
            harmony.Patch(AccessTools.Method(typeof(CompFloorEtching), nameof(CompFloorEtching.PostSpawnSetup)),
                          postfix: new HarmonyMethod(patchType, nameof(HVMPCompFloorEtching_PostSpawnSetupPostfix)));
            MethodInfo methodInfo = typeof(CompGrayStatueTeleporter).GetMethod("Trigger", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo,
                          prefix: new HarmonyMethod(patchType, nameof(HVMPCompGrayStatueTeleporter_TriggerPrefix)));
        }
        public static void HVMPCompFloorEtching_PostSpawnSetupPostfix(CompFloorEtching __instance)
        {
            HVMP_HypercubeMapComponent component = __instance.parent.Map.GetComponent<HVMP_HypercubeMapComponent>();
            if (component == null)
            {
                return;
            }
            float angleFlat = (component.labyrinthObelisk.Position - __instance.parent.Position).AngleFlat;
            Rot4 direction = Rot4.North;
            if (angleFlat >= 315f || angleFlat < 45f)
            {
                direction = Rot4.North;
            } else if (angleFlat >= 45f && angleFlat < 135f) {
                direction = Rot4.East;
            } else if (angleFlat >= 135f && angleFlat < 225f) {
                direction = Rot4.South;
            } else if (angleFlat >= 225f && angleFlat < 315f) {
                direction = Rot4.West;
            }
            if (__instance.GetType().GetField("direction", BindingFlags.NonPublic | BindingFlags.Instance) != null)
            {
                __instance.GetType().GetField("direction", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, direction);
            }
        }
        public static bool HVMPCompGrayStatueTeleporter_TriggerPrefix(CompGrayStatueTeleporter __instance, Pawn target)
        {
            HVMP_HypercubeMapComponent component = __instance.parent.Map.GetComponent<HVMP_HypercubeMapComponent>();
            if (component != null)
            {
                __instance.parent.MapHeld.GetComponent<HVMP_HypercubeMapComponent>().TeleportToLabyrinth(target,true);
                return false;
            }
            return true;
        }
    }
    [DefOf]
    public static class HVMPDefOf_A
    {
        static HVMPDefOf_A()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HVMPDefOf));
        }
        public static GameConditionDef HVMP_LovecraftAlignmentCond;
        public static GameConditionDef HVMP_LovecraftHypnoLight;

        public static HediffDef HVMP_LightHypnotized;

        public static IncidentDef HVMP_LovecraftAlignment;

        public static LayoutDef HVMP_HypercubeLayout;
        public static LayoutRoomDef HVMP_HypercubeObelisk;
        public static MapGeneratorDef HVMP_Hypercube;
        public static ThingDef HVMP_WarpedObelisk_Hypercube;

        public static PawnKindDef HVMP_FleshmassNucleus;
    }
    //permits
    public class RoyalTitlePermitWorker_SelfSkip : RoyalTitlePermitWorker_Targeted
    {
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (target.IsValid)
            {
                if (!base.CanHitTarget(target) || !target.Cell.Standable(this.map))
                {
                    if (showMessages)
                    {
                        Messages.Message(this.def.LabelCap + ": " + "HVMP_BadSkipSpot".Translate(), MessageTypeDefOf.RejectInput, true);
                    }
                    return false;
                }
                return true;
            }
            return false;
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            FleckCreationData dataAttachedOverlay = FleckMaker.GetDataAttachedOverlay(this.caller, FleckDefOf.PsycastSkipFlashEntry, new Vector3(-0.5f, 0f, -0.5f), 1f, -1f);
            dataAttachedOverlay.link.detachAfterTicks = 5;
            this.caller.Map.flecks.CreateFleck(dataAttachedOverlay);
            FleckMaker.Static(target.Cell, this.caller.Map, FleckDefOf.PsycastSkipInnerExit, 1f);
            SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(this.caller.Position, this.caller.Map, false));
            SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(target.Cell, this.caller.Map, false));
            CompCanBeDormant compCanBeDormant = this.caller.TryGetComp<CompCanBeDormant>();
            if (compCanBeDormant != null)
            {
                compCanBeDormant.WakeUp();
            }
            this.caller.Position = target.Cell;
            if ((this.caller.Faction == Faction.OfPlayer || this.caller.IsPlayerControlled) && this.caller.Position.Fogged(this.caller.Map))
            {
                FloodFillerFog.FloodUnfog(this.caller.Position, this.caller.Map);
            }
            this.caller.stances.stunner.StunFor(60, this.caller, false, false, false);
            this.caller.Notify_Teleported(true, true);
            CompAbilityEffect_Teleport.SendSkipUsedSignal(this.caller.Position, this.caller);
            GenClamor.DoClamor(this.caller, target.Cell, 10f, ClamorDefOf.Ability);
            this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
            }
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (map.generatorDef.isUnderground)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (faction.HostileTo(Faction.OfPlayer))
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            string text = this.def.LabelCap + ": ";
            Action action = null;
            bool free;
            if (base.FillAidOption(pawn, faction, ref text, out free))
            {
                action = delegate
                {
                    this.BeginSelfSkip(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        private void BeginSelfSkip(Pawn caller, Faction faction, Map map, bool free)
        {
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetSelf = true;
            this.targetingParameters.canTargetFires = true;
            this.targetingParameters.canTargetItems = true;
            this.caller = caller;
            this.map = map;
            this.faction = faction;
            this.free = free;
            this.targetingParameters.validator = delegate (TargetInfo target)
            {
                if (this.def.royalAid.targetingRange > 0f && target.Cell.DistanceTo(caller.Position) > this.def.royalAid.targetingRange)
                {
                    return false;
                }
                if (target.Cell.Fogged(map))
                {
                    return false;
                }
                RoofDef roof = target.Cell.GetRoof(map);
                return roof == null || !roof.isThickRoof;
            };
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private Faction faction;
    }
    public class Hediff_Rehumanizer : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.Severity == this.def.minSeverity)
            {
                if (this.pawn.Inhumanized())
                {
                    this.pawn.Rehumanize();
                }
                this.pawn.health.RemoveHediff(this);
            }
        }
    }
    public class RoyalTitlePermitWorker_Rehumanizer : RoyalTitlePermitWorker_GiveHediffs
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return pawn.Inhumanized() && base.IsGoodPawn(pawn);
        }
    }
    public class Hediff_Deghoulizer : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.Severity == this.def.minSeverity)
            {
                if (this.pawn.IsGhoul)
                {
                    this.pawn.mutant.Revert();
                }
                this.pawn.health.RemoveHediff(this);
            }
        }
    }
    public class RoyalTitlePermitWorker_Deghoulizer : RoyalTitlePermitWorker_GiveHediffs
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return pawn.IsGhoul && base.IsGoodPawn(pawn);
        }
    }
    public class Hediff_Psylinker : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.Severity == this.def.minSeverity)
            {
                this.pawn.ChangePsylinkLevel(1, PawnUtility.ShouldSendNotificationAbout(this.pawn));
                this.pawn.health.RemoveHediff(this);
            }
        }
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            this.Severity += this.def.initialSeverity * this.pawn.GetPsylinkLevel();
        }
    }
    public class RoyalTitlePermitWorker_Psylinker : RoyalTitlePermitWorker_GiveHediffs
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return !pawn.HostileTo(this.caller) && pawn.RaceProps.Humanlike && pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon && base.IsGoodPawn(pawn);
        }
    }
    public class Hediff_InstantKillMode : Hediff
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            this.activated = false;
        }
        public override void PostTick()
        {
            base.PostTick();
            CompActivity ca = this.pawn.GetComp<CompActivity>();
            if (ca != null)
            {
                if (!ca.IsActive)
                {
                    if (this.activated)
                    {
                        NociosphereUtility.SkipTo(this.pawn, this.pawn.Position);
                        if (!this.pawn.DestroyedOrNull())
                        {
                            this.pawn.DeSpawn(DestroyMode.Vanish);
                            Lord lord = this.pawn.GetLord();
                            if (lord != null)
                            {
                                lord.Notify_PawnLost(this.pawn, PawnLostCondition.ExitedMap, null);
                            }
                            Find.WorldPawns.PassToWorld(this.pawn, PawnDiscardDecideMode.Discard);
                            return;
                        }
                    } else {
                        ca.SetActivity(ca.ActivityLevel + (1f / this.ticksToKillMode));
                    }
                } else {
                    this.activated = true;
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.activated, "activated", false, false);
        }
        public bool activated = false;
        public float ticksToKillMode = 300f;
    }
    //quest-stuff
    public class QuestNode_OccultIntermediary : QuestNode_Sequence
    {
        protected override void RunInt()
        {
            if (HVMP_Utility.TryFindOccultFaction(out Faction occultFaction))
            {
                Slate slate = QuestGen.slate;
                slate.Set<Thing>("asker", occultFaction.leader, false);
                slate.Set<Faction>("faction", occultFaction, false);
                Map map = HVMP_Utility.TryGetMap();
                slate.Set<Map>("map", map, false);
                QuestPart_BranchGoodwillFailureHandler qpbgfh = new QuestPart_BranchGoodwillFailureHandler();
                qpbgfh.faction = occultFaction;
                qpbgfh.goodwill = HVMP_Utility.ExpectationBasedGoodwillLoss(map, true, true, occultFaction);
                QuestGen.quest.AddPart(qpbgfh);
                HVMP_Utility.SetSettingScalingRewardValue(slate);
            }
            base.RunInt();
        }
        protected override bool TestRunInt(Slate slate)
        {
            return HVMP_Utility.TryFindOccultFaction(out Faction occultFaction);
        }
    }
    //barker
    public class QuestNode_GiveRewardsBarker : QuestNode_GiveRewardsBranch
    {
        protected override bool TestRunInt(Slate slate)
        {
            return base.TestRunInt(slate);
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;
            QuestPart_MakeAndAcceptNewQuest qppd = new QuestPart_MakeAndAcceptNewQuest
            {
                inSignal = QuestGenUtility.HardcodedSignalWithQuestID(quest.InitiateSignal),
                questToMake = this.possibleQuests.RandomElement(),
            };
            quest.AddPart(qppd);
            base.RunInt();
        }
        public List<QuestScriptDef> possibleQuests;
    }
    public class QuestPart_MakeAndAcceptNewQuest : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                if (this.questToMake != null && this.questToMake.CanRun(this.QuestPoints))
                {
                    Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(questToMake, this.QuestPoints);
                    quest.SetInitiallyAccepted();
                    quest.Initiate();
                }
            }
        }
        public float QuestPoints
        {
            get
            {
                return Math.Max(QuestGen.slate.Get<float>("points"), 1f);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_Defs.Look<QuestScriptDef>(ref this.questToMake, "questToMake");
        }
        public string inSignal;
        public QuestScriptDef questToMake;
    }
    public abstract class QuestNode_Root_BarkerBox : QuestNode
    {
        protected virtual bool RequiresPawn { get; } = true;
        protected override bool TestRunInt(Slate slate)
        {
            Map map = QuestGen_Get.GetMap(false, null);
            Pawn pawn;
            return map != null && (!this.RequiresPawn || QuestUtility.TryGetIdealColonist(out pawn, map, new Func<Pawn, bool>(this.ValidatePawn)));
        }
        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            quest.hiddenInUI = true;
            Map map = QuestGen_Get.GetMap(false, null);
            float points = slate.Get<float>("points", 0f, false);
            slate.Set<Map>("map", map, false);
            Pawn asker = this.FindAsker();
            Pawn pawn = null;
            if (this.RequiresPawn && !QuestUtility.TryGetIdealColonist(out pawn, map, new Func<Pawn, bool>(this.ValidatePawn)))
            {
                Log.ErrorOnce("Attempted to create a mysterious cargo quest but no valid pawns or world pawns could be found", 94657346);
                quest.End(QuestEndOutcome.InvalidPreAcceptance, true, true);
            }
            Thing thing = this.GenerateThing(pawn);
            quest.Delay(120, delegate
            {
                quest.DropPods(map.Parent, new List<Thing> { thing }, "[deliveredLetterLabel]", null, "[deliveredLetterText]", null, new bool?(true), false, false, false, null, null, QuestPart.SignalListenMode.OngoingOnly, null, true, true, false, null);
                this.AddPostDroppedQuestParts(pawn, thing, quest);
                quest.End(QuestEndOutcome.Success, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, false, false);
            }, null, null, null, false, null, null, false, null, null, null, false, QuestPart.SignalListenMode.OngoingOnly, false);
            slate.Set<Pawn>("asker", asker, false);
            slate.Set<bool>("askerIsNull", asker == null, false);
            slate.Set<Pawn>("pawn", pawn, false);
            Slate slate2 = slate;
            string text = "pawnOnMap";
            Pawn pawn2 = pawn;
            slate2.Set<bool>(text, ((pawn2 != null) ? pawn2.MapHeld : null) == map, false);
        }
        protected abstract Thing GenerateThing(Pawn pawn);
        protected virtual void AddPostDroppedQuestParts(Pawn pawn, Thing thing, Quest quest){}
        protected virtual bool ValidatePawn(Pawn pawn)
        {
            return pawn.IsColonist || pawn.IsSlaveOfColony;
        }
        private Pawn FindAsker()
        {
            return null;
        }
    }
    public class QuestNode_Root_BarkerCorpse : QuestNode_Root_BarkerBox
    {
        protected override Thing GenerateThing(Pawn pawn)
        {
            return AnomalyUtility.MakeUnnaturalCorpse(pawn);
        }
        protected override bool ValidatePawn(Pawn pawn)
        {
            return base.ValidatePawn(pawn) && pawn.RaceProps.unnaturalCorpseDef != null && !Find.Anomaly.PawnHasUnnaturalCorpse(pawn);
        }
        protected override void AddPostDroppedQuestParts(Pawn pawn, Thing thing, Quest quest)
        {
            quest.PawnDestroyed(pawn, null, delegate
            {
                quest.LinkUnnaturalCorpse(pawn, thing as UnnaturalCorpse, null);
            }, null, null, null, QuestPart.SignalListenMode.OngoingOnly);
        }
    }
    public class QuestNode_Root_BarkerCube : QuestNode_Root_BarkerBox
    {
        protected override Thing GenerateThing(Pawn pawn)
        {
            return ThingMaker.MakeThing(ThingDefOf.GoldenCube, null);
        }
        protected override void AddPostDroppedQuestParts(Pawn pawn, Thing thing, Quest quest)
        {
            quest.PawnDestroyed(pawn, null, delegate
            {
                quest.GiveHediff(pawn, HediffDefOf.CubeInterest, null);
            }, null, null, null, QuestPart.SignalListenMode.OngoingOnly);
        }
        protected override bool ValidatePawn(Pawn pawn)
        {
            return base.ValidatePawn(pawn) && !pawn.health.hediffSet.HasHediff(HediffDefOf.CubeInterest, false) && !pawn.health.hediffSet.HasHediff(HediffDefOf.CubeComa, false);
        }
    }
    public class QuestNode_Root_BarkerSphere : QuestNode_Root_BarkerBox
    {
        protected override bool RequiresPawn { get; }
        protected override Thing GenerateThing(Pawn pawn)
        {
            return PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Nociosphere, Faction.OfEntities, PawnGenerationContext.NonPlayer, -1, false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
        }
    }
    public class QuestNode_Root_BarkerNucleus : QuestNode_Root_BarkerBox
    {
        protected override bool RequiresPawn { get; }
        protected override Thing GenerateThing(Pawn pawn)
        {
            return PawnGenerator.GeneratePawn(new PawnGenerationRequest(HVMPDefOf_A.HVMP_FleshmassNucleus, Faction.OfEntities, PawnGenerationContext.NonPlayer, -1, false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
        }
    }
    public class QuestNode_Root_BarkerSpine : QuestNode_Root_BarkerBox
    {
        protected override bool RequiresPawn { get; }
        protected override Thing GenerateThing(Pawn _)
        {
            return ThingMaker.MakeThing(ThingDefOf.RevenantSpine, null);
        }
    }
    /*brevik
    public class QuestNode_GenerateVoidLens : QuestNode
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
            CompActivityVoidLens cavl = thing.TryGetComp<CompActivityVoidLens>();
            if (cavl != null)
            {
                int toDestabilize = (int)(cavl.Props.destabilizationDays.RandomInRange * 60000f);
                slate.Set<string>(this.storeReqProgressAs.GetValue(slate), toDestabilize.ToStringTicksToPeriod(), false);
                cavl.ticksToDestabilize = toDestabilize;
                cavl.initToDestabilize = cavl.ticksToDestabilize;
            }
            slate.Set<Thing>(this.storeAs.GetValue(slate), thing, false);
            QuestGen.quest.AddPart(new QuestPart_LookAtThis(thing));
        }
        [NoTranslate]
        public SlateRef<string> storeAs;
        public ThingDef thingDef;
        [NoTranslate]
        public SlateRef<string> storeReqProgressAs;
    }
    public class CompProperties_ActivityVoidLens : CompProperties_Activity
    {
        public CompProperties_ActivityVoidLens()
        {
            this.compClass = typeof(CompActivityVoidLens);
        }
        public FloatRange destabilizationDays;
        public float dustRadius;
        public DamageDef dustDamageDef;
        public float activityThresholdForMentalStates;
        public float mtbMentalStatesDays;
        public List<MentalStateDef> possibleMentalStates;
        public float pmsWeight_HG;
        public float pmsWeight_Fleshmass;
        public float pmsWeight_MS;
        public List<HediffGiver> pmsHediffGivers;
        public List<MentalStateDef> pmsMentalStates;
    }
    public class CompActivityVoidLens : CompActivity
    {
        public new CompProperties_ActivityVoidLens Props
        {
            get
            {
                return (CompProperties_ActivityVoidLens)this.props;
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            this.ticksToDestabilize = (int)(this.Props.destabilizationDays.RandomInRange*60000f);
            this.initToDestabilize = this.ticksToDestabilize;
        }
        public override void CompTick()
        {
            if (this.parent.Spawned)
            {
                base.CompTick();
                this.ticksToDestabilize--;
                if (this.ticksToDestabilize <= 0)
                {
                    this.Destabilize(true);
                    return;
                }
                if (this.ActivityLevel >= 1f)
                {
                    this.Destabilize(false);
                    return;
                }
                if (this.parent.IsHashIntervalTick(100) && this.ActivityLevel >= this.Props.activityThresholdForMentalStates)
                {
                    if (Rand.MTBEventOccurs(this.Props.mtbMentalStatesDays, 60000f, 100f))
                    {
                        List<Pawn> pawns = this.parent.Map.mapPawns.AllHumanlikeSpawned;
                        if (pawns.Count > 0)
                        {
                            Pawn p = pawns.Where((Pawn p2)=>!p2.InMentalState).RandomElement();
                            p?.mindState.mentalStateHandler.TryStartMentalState(this.Props.possibleMentalStates.RandomElement(), null, false, true, false, null, false, false, false);
                        }
                    }
                }
            }
        }
        public void Destabilize(bool safely)
        {
            if (!this.destabilized || !this.parent.questTags.NullOrEmpty())
            {
                if (this.parent.SpawnedOrAnyParentSpawned)
                {
                    GenExplosion.DoExplosion(this.parent.PositionHeld, this.parent.MapHeld, this.Props.dustRadius, this.Props.dustDamageDef, this.parent);
                    if (!safely)
                    {
                        List<Pawn> pawns = this.parent.MapHeld.mapPawns.AllHumanlikeSpawned;
                        if (pawns.Count > 0)
                        {
                            for (int i = pawns.Count - 1; i >= 0; i--)
                            {
                                Pawn p = pawns[i];
                                float totalWeight = this.Props.pmsWeight_Fleshmass + this.Props.pmsWeight_MS + this.Props.pmsWeight_HG;
                                float randVal = Rand.Value * totalWeight;
                                if (randVal <= this.Props.pmsWeight_Fleshmass)
                                {
                                    HediffDef mutation = DefDatabase<HediffDef>.AllDefsListForReading.Where((HediffDef hd) => hd.HasComp(typeof(HediffComp_FleshbeastEmerge)) && hd.defaultInstallPart != null).RandomElement();
                                    if (mutation != null)
                                    {
                                        FleshbeastUtility.TryGiveMutation(p, mutation);
                                        Find.LetterStack.ReceiveLetter("HVMP_VoidLensMutationLabel".Translate().CapitalizeFirst().Formatted(p.Named("PAWN")).AdjustedFor(p, "PAWN", true).Resolve(), "HVMP_VoidLensMutationText".Translate().CapitalizeFirst().Formatted(p.Named("PAWN")).AdjustedFor(p, "PAWN", true).Resolve(), LetterDefOf.NeutralEvent, p, null, null, null, null, 0, true);
                                    }
                                } else if (randVal <= this.Props.pmsWeight_Fleshmass + this.Props.pmsWeight_MS) {
                                    p.mindState.mentalStateHandler.TryStartMentalState(this.Props.pmsMentalStates.RandomElement(), null, false, true, false, null, false, false, false);
                                } else {
                                    HediffGiver hg = this.Props.pmsHediffGivers.RandomElement();
                                    if (hg != null)
                                    {
                                        hg.TryApply(p, null);
                                        string diseaseLabel = hg.hediff.label + ": " + (p.Name != null ? p.Name.ToStringShort : p.Label);
                                        Find.LetterStack.ReceiveLetter(diseaseLabel, "HVMP_VoidLensDiseaseText".Translate().CapitalizeFirst().Formatted(p.Named("PAWN")).AdjustedFor(p, "PAWN", true).Resolve(), LetterDefOf.NeutralEvent, p, null, null, null, null, 0, true);
                                    }
                                }
                            }
                        }
                    }
                    this.destabilized = true;
                    GenLeaving.DoLeavingsFor(this.parent, this.parent.MapHeld, DestroyMode.KillFinalize);
                    QuestUtility.SendQuestTargetSignals(this.parent.questTags, "DestabilizedVoidLens", this.Named("SUBJECT"));
                }
            }
        }
        public override string CompInspectStringExtra()
        {
            return "HVMP_CCProgress".Translate(this.ticksToDestabilize.ToStringTicksToPeriod(), this.initToDestabilize.ToStringTicksToPeriod());
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (!this.parent.questTags.NullOrEmpty())
            {
                QuestUtility.SendQuestTargetSignals(this.parent.questTags, "DestroyedVoidLens", this.Named("SUBJECT"), previousMap.Named("MAP"));
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.ticksToDestabilize, "ticksToDestabilize", 0, false);
            Scribe_Values.Look<int>(ref this.initToDestabilize, "initToDestabilize", 0, false);
            Scribe_Values.Look<bool>(ref this.destabilized, "destabilized", false, false);
        }
        public int ticksToDestabilize;
        public int initToDestabilize;
        public bool destabilized = false;
    }
    public class CompProperties_UseEffectInstallSoulstone : CompProperties_UseEffect
    {
        public CompProperties_UseEffectInstallSoulstone()
        {
            this.compClass = typeof(CompUseEffect_InstallSoulstone);
        }
        public HediffDef hediffDef;
        public BodyPartDef bodyPart;
        public bool allowNonColonists;
        public bool requiresPsychicallySensitive;
    }
    public class CompUseEffect_InstallSoulstone : CompUseEffect
    {
        public CompProperties_UseEffectInstallSoulstone Props
        {
            get
            {
                return (CompProperties_UseEffectInstallSoulstone)this.props;
            }
        }
        public override void DoEffect(Pawn user)
        {
            BodyPartRecord bodyPartRecord = user.RaceProps.body.GetPartsWithDef(this.Props.bodyPart).FirstOrFallback(null);
            if (bodyPartRecord == null)
            {
                return;
            }
            int ttd = 0;
            int itd = 0;
            bool dsd = false;
            List<string> questTags = this.parent.questTags;
            CompActivityVoidLens cavl = this.parent.TryGetComp<CompActivityVoidLens>();
            if (cavl != null)
            {
                ttd = cavl.ticksToDestabilize;
                itd = cavl.initToDestabilize;
                dsd = cavl.destabilized;
            }
            Hediff firstHediffOfDef = user.health.hediffSet.GetFirstHediffOfDef(this.Props.hediffDef, false);
            if (firstHediffOfDef == null)
            {
                Hediff hediff = HediffMaker.MakeHediff(this.Props.hediffDef,user);
                user.health.AddHediff(hediff);
                HediffComp_BlackSoulstone hcbs = hediff.TryGetComp<HediffComp_BlackSoulstone>();
                if (hcbs != null)
                {
                    hcbs.ticksToDestabilize = ttd;
                    hcbs.initToDestabilize = itd;
                    hcbs.destabilized = dsd;
                    hcbs.questTags = questTags;
                }
                return;
            }
        }
        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            if ((!p.IsFreeColonist || p.IsQuestLodger()) && !this.Props.allowNonColonists)
            {
                return "InstallImplantNotAllowedForNonColonists".Translate();
            }
            if (p.RaceProps.body.GetPartsWithDef(this.Props.bodyPart).FirstOrFallback(null) == null)
            {
                return "InstallImplantNoBodyPart".Translate() + ": " + this.Props.bodyPart.LabelShort;
            }
            if (this.Props.requiresPsychicallySensitive && p.psychicEntropy != null && !p.psychicEntropy.IsPsychicallySensitive)
            {
                return "InstallImplantPsychicallyDeaf".Translate();
            }
            Hediff existingImplant = this.GetExistingImplant(p);
            if (existingImplant != null)
            {
                return "InstallImplantAlreadyInstalled".Translate();
            }
            return true;
        }
        public Hediff GetExistingImplant(Pawn p)
        {
            for (int i = 0; i < p.health.hediffSet.hediffs.Count; i++)
            {
                Hediff hediff = p.health.hediffSet.hediffs[i];
                if (hediff.def == this.Props.hediffDef && hediff.Part == p.RaceProps.body.GetPartsWithDef(this.Props.bodyPart).FirstOrFallback(null))
                {
                    return hediff;
                }
            }
            return null;
        }
    }
    public class HediffCompProperties_BlackSoulstone : HediffCompProperties
    {
        public HediffCompProperties_BlackSoulstone()
        {
            this.compClass = typeof(HediffComp_BlackSoulstone);
        }
        public LetterDef letterDef;
        [MustTranslate]
        public string letterText;
        [MustTranslate]
        public string letterLabel;
        public ThingDef thingForm;
    }
    public class HediffComp_BlackSoulstone : HediffComp
    {
        public HediffCompProperties_BlackSoulstone Props
        {
            get
            {
                return (HediffCompProperties_BlackSoulstone)this.props;
            }
        }
        private bool ShouldSendLetter
        {
            get
            {
                return (this.Pawn == null || !PawnGenerator.IsBeingGenerated(this.Pawn));
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            this.ticksToDestabilize--;
            if (this.ticksToDestabilize <= 0)
            {
                this.Destabilize(true);
                return;
            }
            if (this.parent.Severity == this.parent.def.maxSeverity)
            {
                this.Destabilize(false);
                return;
            }
        }
        public void Destabilize(bool safely)
        {
            if (!this.destabilized || !this.questTags.NullOrEmpty())
            {
                if (this.Pawn.SpawnedOrAnyParentSpawned)
                {
                    if (!safely)
                    {
                        int switcher = Rand.RangeInclusive(1, 3);
                        switch(switcher)
                        {
                            case 1:
                                this.Pawn.health.AddHediff(HediffDefOf.CrumblingMind,this.Pawn.health.hediffSet.GetBrain());
                                break;
                            case 2:
                                FleshbeastUtility.MeatSplatter(Rand.RangeInclusive(3,4), this.Pawn.PositionHeld, this.Pawn.MapHeld, FleshbeastUtility.MeatExplosionSize.Large);
                                FleshbeastUtility.SpawnFleshbeastFromPawn(this.Pawn,true);
                                break;
                            case 3:
                                this.Pawn.SetFaction(Find.FactionManager.OfEntities);
                                this.Pawn.guest.Recruitable = false;
                                if (this.Pawn.GetLord() != null)
                                {
                                    this.Pawn.GetLord().Notify_PawnLost(this.Pawn, PawnLostCondition.Undefined, null);
                                }
                                LordMaker.MakeNewLord(Faction.OfEntities, new LordJob_AssaultColony(Faction.OfEntities, false, false, false, false, false, false, false), this.Pawn.MapHeld, null).AddPawn(this.Pawn);
                                Lord lord = LordMaker.MakeNewLord(Faction.OfEntities, new LordJob_SightstealerAssault(), this.Pawn.MapHeld, null);
                                FloatRange spawnPointScale = new FloatRange(0.25f, 0.65f);
                                float num = StorytellerUtility.DefaultThreatPointsNow(this.Pawn.MapHeld) * spawnPointScale.RandomInRange;
                                num = Mathf.Max(Faction.OfEntities.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Sightstealers, null), num);
                                List<Pawn> list = PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
                                {
                                    faction = Faction.OfEntities,
                                    groupKind = PawnGroupKindDefOf.Sightstealers,
                                    points = num,
                                    tile = this.Pawn.MapHeld.Tile
                                }, true).ToList<Pawn>();
                                foreach (Pair<List<Pawn>, IntVec3> pair in PawnsArrivalModeWorkerUtility.SplitIntoRandomGroupsNearMapEdge(list, this.Pawn.MapHeld, false))
                                {
                                    foreach (Thing thing in pair.First)
                                    {
                                        IntVec3 intVec = CellFinder.RandomClosewalkCellNear(pair.Second, this.Pawn.MapHeld, 8, null);
                                        GenSpawn.Spawn(thing, intVec, this.Pawn.MapHeld, WipeMode.Vanish);
                                    }
                                }
                                foreach (Pawn pawn in list)
                                {
                                    lord.AddPawn(pawn);
                                }
                                SoundDefOf.Sightstealer_SummonedHowl.PlayOneShot(this.Pawn);
                                break;
                            default:
                                break;
                        }
                    }
                    this.destabilized = true;
                    QuestUtility.SendQuestTargetSignals(this.questTags, "DestabilizedVoidLens", this.Named("SUBJECT"));
                }
            }
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (this.ShouldSendLetter)
            {
                Find.LetterStack.ReceiveLetter(this.Props.letterLabel.Formatted(this.parent.Named("HEDIFF")), this.Props.letterText.Formatted(this.parent.pawn.Named("PAWN"), this.parent.Named("HEDIFF")), this.Props.letterDef ?? LetterDefOf.NeutralEvent, this.parent.pawn, null, null, null, null, 0, true);
            }
            if (this.Pawn.SpawnedOrAnyParentSpawned)
            {
                Thing thing;
                if (!GenDrop.TryDropSpawn(ThingMaker.MakeThing(this.Props.thingForm, null), this.Pawn.PositionHeld, this.Pawn.MapHeld, ThingPlaceMode.Near, out thing, null, null, true))
                {
                    QuestUtility.SendQuestTargetSignals(this.questTags, "DestroyedVoidLens", this.Named("SUBJECT"), this.Pawn.MapHeld.Named("MAP"));
                    return;
                }
                CompActivityVoidLens cavl = thing.TryGetComp<CompActivityVoidLens>();
                if (cavl != null)
                {
                    cavl.ticksToDestabilize = this.ticksToDestabilize;
                    cavl.initToDestabilize = this.initToDestabilize;
                    cavl.destabilized = this.destabilized;
                }
                thing.questTags = this.questTags;
                thing.SetForbidden(!thing.MapHeld.areaManager.Home[thing.PositionHeld], true);
            }
        }
        public override string CompTipStringExtra { 
            get {
                return "HVMP_CCProgress".Translate(this.ticksToDestabilize.ToStringTicksToPeriod(), this.initToDestabilize.ToStringTicksToPeriod());
            } 
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.ticksToDestabilize, "ticksToDestabilize", 0, false);
            Scribe_Values.Look<int>(ref this.initToDestabilize, "initToDestabilize", 0, false);
            Scribe_Values.Look<bool>(ref this.destabilized, "destabilized", false, false);
        }
        public int ticksToDestabilize;
        public int initToDestabilize;
        public bool destabilized = false;
        public List<string> questTags;
    }
    public class MentalState_WanderBrainwipe : MentalState_WanderPsychotic
    {
        public override void PostStart(string reason)
        {
            base.PostStart(reason);
            if (this.pawn.Inhumanized())
            {
                this.pawn.Rehumanize();
            }
            this.pawn.guest.Recruitable = true;
            MemoryThoughtHandler memories = this.pawn.needs.mood.thoughts.memories;
            if (memories != null)
            {
                List<Thought_Memory> list = new List<Thought_Memory>();
                List<Thought_Memory> list2 = new List<Thought_Memory>();
                foreach (Thought_Memory thought_Memory in memories.Memories)
                {
                    if (thought_Memory.MoodOffset() != 0f && thought_Memory.DurationTicks > 0)
                    {
                        list.Add(thought_Memory);
                    }
                    if (thought_Memory is ISocialThought)
                    {
                        list2.Add(thought_Memory);
                    }
                }
                foreach (Thought_Memory thought_Memory2 in list)
                {
                    memories.RemoveMemory(thought_Memory2);
                }
                foreach (Thought_Memory thought_Memory3 in list2)
                {
                    memories.RemoveMemory(thought_Memory3);
                }
            }
            this.pawn.guest.resistance = 0f;
            this.pawn.guest.will = 0f;
            if (ModsConfig.IdeologyActive && this.pawn.Ideo != null)
            {
                this.pawn.ideo.OffsetCertainty(-this.pawn.ideo.Certainty);
            }
        }
    }*/
    //fuller
    public class QuestNode_Fuller : QuestNode_OccultIntermediary
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            PawnKindDef pawnKindDef;
            if (pawnKinds.TryRandomElement(out pawnKindDef))
            {
                slate.Set<PawnKindDef>(this.storePawnKindAs.GetValue(slate), pawnKindDef, false);
                slate.Set<Faction>(this.storePrisonerFactionAs.GetValue(slate), Find.FactionManager.FirstFactionOfDef(this.prisonerFaction) ??Find.FactionManager.OfEntities, false);
            }
            base.RunInt();
        }
        [NoTranslate]
        public SlateRef<string> storePawnKindAs;
        public List<PawnKindDef> pawnKinds;
        [NoTranslate]
        public SlateRef<string> storePrisonerFactionAs;
        public FactionDef prisonerFaction;
    }
    //lovecraft
    public class QuestNode_GiveRewardsLovecraft : QuestNode_GiveRewardsBranch
    {
        protected override bool TestRunInt(Slate slate)
        {
            return base.TestRunInt(slate);
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;
            QuestPart_PsychicDischarge qppd = new QuestPart_PsychicDischarge
            {
                inSignal = QuestGenUtility.HardcodedSignalWithQuestID(quest.InitiateSignal),
                cooldownBetweenLovecrafts = this.cooldownBetweenLovecrafts
            };
            IncidentParms ip = new IncidentParms
            {
                target = slate.Get<Map>("map", Find.AnyPlayerHomeMap, false),
                faction = slate.Get<Faction>("faction",null,false),
                points = slate.Get<float>("points", 350f, false),
                forced = true,
                quest = quest,
                bypassStorytellerSettings = true
            };
            IncidentDef incident = this.possibleIncidents.RandomElement();
            if (incident == null)
            {
                incident = HVMPDefOf_A.HVMP_LovecraftAlignment;
            }
            if (incident != null)
            {
                qppd.incidentParms = ip;
                qppd.incident = incident;
                List<IncidentDef> incidents = this.possibleIncidents;
                incidents.Remove(incident);
                qppd.otherIncidents = incidents;
            }
            qppd.SetIncidentParmsAndRemoveTarget(ip);
            quest.AddPart(qppd);
            base.RunInt();
        }
        public List<IncidentDef> possibleIncidents;
        public int cooldownBetweenLovecrafts;
    }
    public class QuestPart_PsychicDischarge : QuestPart_RequirementsToAccept
    {
        public override AcceptanceReport CanAccept()
        {
            WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
            if (wcbs != null)
            {
                if (wcbs.lovecraftEventTimer <= 0)
                {
                    return true;
                }
                return new AcceptanceReport("HVMP_LovecraftTooSoon".Translate(wcbs.lovecraftEventTimer.ToStringTicksToPeriodVerbose(true, true).Colorize(ColoredText.DateTimeColor)));
            }
            return new AcceptanceReport("HVMP_LovecraftTooSoon".Translate(0));
        }
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                if (this.incident != null && this.incident.Worker.CanFireNow(this.incidentParms))
                {
                    this.incident.Worker.TryExecute(this.incidentParms);
                } else if (!this.otherIncidents.NullOrEmpty()) {
                    while (this.otherIncidents.Count > 0)
                    {
                        IncidentDef id = this.otherIncidents.RandomElement();
                        if (id.Worker.CanFireNow(this.incidentParms))
                        {
                            id.Worker.TryExecute(this.incidentParms);
                            break;
                        } else {
                            this.otherIncidents.Remove(id);
                        }
                    }
                }
                WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
                if (wcbs != null)
                {
                    wcbs.lovecraftEventTimer = this.cooldownBetweenLovecrafts;
                }
            }
        }
        public void SetIncidentParmsAndRemoveTarget(IncidentParms value)
        {
            this.incidentParms = value;
            Map map = this.incidentParms.target as Map;
            if (map != null)
            {
                this.mapParent = map.Parent;
                //this.incidentParms.target = null;
                return;
            }
            this.mapParent = null;
        }
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
                yield break;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_Defs.Look<IncidentDef>(ref this.incident, "incident");
            Scribe_Deep.Look<IncidentParms>(ref this.incidentParms, "incidentParms", Array.Empty<object>());
            Scribe_References.Look<MapParent>(ref this.mapParent, "mapParent", false);
            Scribe_Collections.Look<IncidentDef>(ref this.otherIncidents, "otherIncidents", LookMode.Def, Array.Empty<object>());
            Scribe_Values.Look<int>(ref this.cooldownBetweenLovecrafts, "cooldownBetweenLovecrafts", 60000, false);
        }
        public string inSignal;
        public IncidentDef incident;
        public List<IncidentDef> otherIncidents;
        public IncidentParms incidentParms;
        private MapParent mapParent;
        public int cooldownBetweenLovecrafts;
    }
    public class IncidentWorker_Align : IncidentWorker_MakeGameCondition
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            this.DoConditionAndLetter(parms, map, Mathf.RoundToInt(this.def.durationDays.RandomInRange * 60000f), parms.points);
            SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera((Map)parms.target);
            return true;
        }
        protected void DoConditionAndLetter(IncidentParms parms, Map map, int duration, float points)
        {
            GameCondition gc = GameConditionMaker.MakeCondition(this.def.gameCondition, duration);
            map.gameConditionManager.RegisterCondition(gc);
            base.SendStandardLetter(gc.LabelCap, gc.LetterText, gc.def.letterDef, parms, LookTargets.Invalid, Array.Empty<NamedArgument>());
        }
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return true;
        }
    }
    public class GameCondition_Alignment : GameCondition
    {
        public override int TransitionTicks
        {
            get
            {
                return 60000;
            }
        }
    }
    public class StatPart_PsychicAlignment : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing && req.Thing is Pawn p && p.MapHeld != null && p.MapHeld.gameConditionManager.ConditionIsActive(HVMPDefOf_A.HVMP_LovecraftAlignmentCond))
            {
                val += this.offset;
                val *= this.factor;
            }
        }
        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing && req.Thing is Pawn p && p.MapHeld != null && p.MapHeld.gameConditionManager.ConditionIsActive(HVMPDefOf_A.HVMP_LovecraftAlignmentCond))
            {
                string explanation = "HVMP_StatsReport_PsychicAlignment".Translate(HVMPDefOf_A.HVMP_LovecraftAlignmentCond.LabelCap,this.offset.ToStringPercent(),this.factor.ToStringPercent());
                return explanation;
            }
            return null;
        }
        public float offset;
        public float factor = 1f;
    }
    public class IncidentWorker_Hypnolight : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return !map.gameConditionManager.ConditionIsActive(HVMPDefOf_A.HVMP_LovecraftHypnoLight);
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            this.DoConditionAndLetter(parms, map, Mathf.RoundToInt(this.def.durationDays.RandomInRange * 60000f), parms.points);
            SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera((Map)parms.target);
            return true;
        }
        protected void DoConditionAndLetter(IncidentParms parms, Map map, int duration, float points)
        {
            if (points < 0f)
            {
                points = StorytellerUtility.DefaultThreatPointsNow(map);
            }
            GameCondition gameCondition = GameConditionMaker.MakeCondition(HVMPDefOf_A.HVMP_LovecraftHypnoLight, duration);
            map.gameConditionManager.RegisterCondition(gameCondition);
            base.SendStandardLetter(gameCondition.LabelCap, gameCondition.LetterText, gameCondition.def.letterDef, parms, LookTargets.Invalid, Array.Empty<NamedArgument>());
        }
    }
    public class GameCondition_HypnoLight : GameCondition
    {
        public Color CurrentColor
        {
            get
            {
                return Color.Lerp(GameCondition_HypnoLight.Colors[this.prevColorIndex], GameCondition_HypnoLight.Colors[this.curColorIndex], this.curColorTransition);
            }
        }
        private int GetNewColorIndex()
        {
            return (from x in Enumerable.Range(0, GameCondition_HypnoLight.Colors.Length)
                    where x != this.curColorIndex
                    select x).RandomElement<int>();
        }
        private int TransitionDurationTicks
        {
            get
            {
                if (!base.Permanent)
                {
                    return 280;
                }
                return 3750;
            }
        }
        public override float SkyGazeChanceFactor(Map map)
        {
            return 10f;
        }
        public override float SkyGazeJoyGainFactor(Map map)
        {
            return 5f;
        }
        public override float SkyTargetLerpFactor(Map map)
        {
            return GameConditionUtility.LerpInOutValue(this, (float)this.TransitionTicks, 1f);
        }
        public override SkyTarget? SkyTarget(Map map)
        {
            Color currentColor = this.CurrentColor;
            SkyColorSet skyColorSet = new SkyColorSet(Color.Lerp(Color.white, currentColor, 0.075f) * this.Brightness(map), new Color(0.92f, 0.92f, 0.92f), Color.Lerp(Color.white, currentColor, 0.025f) * this.Brightness(map), 1f);
            return new SkyTarget?(new SkyTarget(Mathf.Max(GenCelestial.CurCelestialSunGlow(map), 0.25f), skyColorSet, 1f, 1f));
        }
        private float Brightness(Map map)
        {
            return Mathf.Max(0.73f, GenCelestial.CurCelestialSunGlow(map));
        }
        public override void Init()
        {
            base.Init();
            this.curColorIndex = Rand.Range(0, GameCondition_HypnoLight.Colors.Length);
            this.prevColorIndex = this.curColorIndex;
            this.curColorTransition = 1f;
        }
        public override void GameConditionTick()
        {
            this.curColorTransition += 1f / (float)this.TransitionDurationTicks;
            if (this.curColorTransition >= 1f)
            {
                this.prevColorIndex = this.curColorIndex;
                this.curColorIndex = this.GetNewColorIndex();
                this.curColorTransition = 0f;
            }
            if (Find.TickManager.TicksGame % 100 == 0)
            {
                foreach (Map m in base.AffectedMaps)
                {
                    foreach (Pawn p in m.mapPawns.AllPawnsSpawned)
                    {
                        if (p.needs.outdoors != null)
                        {
                            p.needs.outdoors.CurLevel -= 0.01f;
                        }
                        if (!p.PositionHeld.Roofed(p.MapHeld) && m.glowGrid.GroundGlowAt(p.PositionHeld) > float.Epsilon && !PawnUtility.IsBiologicallyOrArtificiallyBlind(p) && !p.IsMutant && !p.IsEntity)
                        {
                            p.health.hediffSet.TryGetHediff(HVMPDefOf_A.HVMP_LightHypnotized, out Hediff lh);
                            if (lh == null)
                            {
                                Hediff hediff = HediffMaker.MakeHediff(HVMPDefOf_A.HVMP_LightHypnotized,p);
                                p.health.AddHediff(hediff);
                            }
                        }
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.curColorIndex, "curColorIndex", 0, false);
            Scribe_Values.Look<int>(ref this.prevColorIndex, "prevColorIndex", 0, false);
            Scribe_Values.Look<float>(ref this.curColorTransition, "curColorTransition", 0f, false);
        }
        private float curColorTransition;
        private int curColorIndex = -1;
        private int prevColorIndex = -1;
        private static readonly Color[] Colors = new Color[]
        {
            new Color(0f, 1f, 0f),
            new Color(0.3f, 1f, 0f),
            new Color(0f, 1f, 0.7f),
            new Color(0.3f, 1f, 0.7f),
            new Color(0f, 0.5f, 1f),
            new Color(0f, 0f, 1f),
            new Color(0.87f, 0f, 1f),
            new Color(0.75f, 0f, 1f)
        };
    }
    public class Hediff_HypnoLight : Hediff
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.Spawned)
            {
                GameCondition_HypnoLight activeCondition = pawn.MapHeld.gameConditionManager.GetActiveCondition<GameCondition_HypnoLight>();
                if (activeCondition != null)
                {
                    if (!this.pawn.PositionHeld.Roofed(this.pawn.MapHeld) && this.pawn.Map.glowGrid.GroundGlowAt(this.pawn.PositionHeld) > float.Epsilon && !PawnUtility.IsBiologicallyOrArtificiallyBlind(this.pawn) && !this.pawn.IsMutant && !this.pawn.IsEntity)
                    {
                        this.Severity += 0.0001f;
                        return;
                    }
                }
            }
            this.Severity -= 0.001f;
        }
    }
    public class IncidentWorker_ManhunterPulse : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
            {
                if ((p.Faction == null || p.Faction != Faction.OfPlayer) && p.IsNonMutantAnimal && p.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon && !p.InAggroMentalState)
                {
                    return true;
                }
            }
            return false;
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            int insaneAnimals = 0;
            SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera((Map)parms.target);
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
            {
                if ((p.Faction == null || p.Faction != Faction.OfPlayer) && p.IsNonMutantAnimal && p.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon && (insaneAnimals <= 2 || Rand.Chance(0.9f)))
                {
                    if (!p.Awake())
                    {
                        RestUtility.WakeUp(p, true);
                    }
                    p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, null, false, false, false, null, false, false, false);
                    insaneAnimals++;
                }
            }
            base.SendStandardLetter("HVMP_LovecraftManhunterLabel".Translate(), "HVMP_LovecraftManhunterText".Translate(), LetterDefOf.ThreatBig, parms, LookTargets.Invalid, Array.Empty<NamedArgument>());
            return true;
        }
    }
    public class IncidentWorker_Suppression : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return !map.gameConditionManager.ConditionIsActive(GameConditionDefOf.PsychicSuppression) && map.mapPawns.FreeColonistsCount != 0;
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            this.DoConditionAndLetter(parms, map, Mathf.RoundToInt(this.def.durationDays.RandomInRange * 60000f), map.mapPawns.FreeColonists.RandomElement<Pawn>().gender, parms.points);
            SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera((Map)parms.target);
            return true;
        }
        protected void DoConditionAndLetter(IncidentParms parms, Map map, int duration, Gender gender, float points)
        {
            if (points < 0f)
            {
                points = StorytellerUtility.DefaultThreatPointsNow(map);
            }
            GameCondition_PsychicSuppression gameCondition = (GameCondition_PsychicSuppression)GameConditionMaker.MakeCondition(GameConditionDefOf.PsychicSuppression, duration);
            gameCondition.gender = gender;
            map.gameConditionManager.RegisterCondition(gameCondition);
            base.SendStandardLetter(gameCondition.LabelCap, gameCondition.LetterText, gameCondition.def.letterDef, parms, LookTargets.Invalid, Array.Empty<NamedArgument>());
        }
    }
    //natali
    public class WorldObject_Hypercube : WorldObject
    {
        public override Material Material
        {
            get
            {
                if (this.cachedMat == null)
                {
                    Color color;
                    if (base.Faction != null)
                    {
                        color = base.Faction.Color;
                    } else {
                        color = Color.white;
                    }
                    this.cachedMat = MaterialPool.MatFrom(this.def.texture, ShaderDatabase.WorldOverlayTransparentLit, color, WorldMaterials.WorldObjectRenderQueue);
                }
                return this.cachedMat;
            }
        }
        public void Notify_CaravanArrived(Caravan caravan)
        {
            this.generating = true;
            LongEventHandler.QueueLongEvent(delegate
            {
                if (this.labyrinthMap == null)
                {
                    this.labyrinthMap = PocketMapUtility.GeneratePocketMap(new IntVec3(90, 1, 90), HVMPDefOf_A.HVMP_Hypercube, null, Find.AnyPlayerHomeMap);
                }
            }, "GeneratingLabyrinth", true, new Action<Exception>(GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap), false, delegate
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
        }
        private Material cachedMat;
        public Map labyrinthMap;
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
        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, int destinationTile)
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
            foreach (Thing thing in ((IEnumerable<Thing>)this.map.spawnedThings))
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
        public override void MapComponentOnGUI()
        {
            if (DebugViewSettings.drawMapGraphs)
            {
                foreach (KeyValuePair<Vector2, List<Vector2>> keyValuePair in this.map.layoutStructureSketch.structureLayout.neighbours.connections)
                {
                    foreach (Vector2 vector in keyValuePair.Value)
                    {
                        Vector2 vector2 = new Vector2(2f, 2f);
                        Vector2 vector3 = vector2 + keyValuePair.Key;
                        Vector2 vector4 = vector2 + vector;
                        Vector2 vector5 = new Vector3(vector3.x, 0f, vector3.y).MapToUIPosition();
                        Vector2 vector6 = new Vector3(vector4.x, 0f, vector4.y).MapToUIPosition();
                        DevGUI.DrawLine(vector5, vector6, Color.green, 2f);
                    }
                }
            }
            if (DebugViewSettings.drawMapRooms)
            {
                foreach (LayoutRoom layoutRoom in this.map.layoutStructureSketch.structureLayout.Rooms)
                {
                    string text = "NA";
                    if (!layoutRoom.defs.NullOrEmpty<LayoutRoomDef>())
                    {
                        text = layoutRoom.defs.Select((LayoutRoomDef x) => x.defName).ToCommaList(false, false);
                    }
                    float widthCached = text.GetWidthCached();
                    Vector2 vector7 = (layoutRoom.rects[0].Min + IntVec3.NorthEast * 2).ToVector3().MapToUIPosition();
                    DevGUI.Label(new Rect(vector7.x - widthCached / 2f, vector7.y, widthCached, 20f), text);
                    foreach (CellRect cellRect in layoutRoom.rects)
                    {
                        IntVec3 min = cellRect.Min;
                        IntVec3 intVec = cellRect.Max + new IntVec3(1, 0, 1);
                        IntVec3 intVec2 = new IntVec3(min.x, 0, min.z);
                        IntVec3 intVec3 = new IntVec3(intVec.x, 0, min.z);
                        IntVec3 intVec4 = new IntVec3(min.x, 0, intVec.z);
                        IntVec3 intVec5 = new IntVec3(intVec.x, 0, intVec.z);
                        this.TryDrawLine(intVec2, intVec3);
                        this.TryDrawLine(intVec2, intVec4);
                        this.TryDrawLine(intVec4, intVec5);
                        this.TryDrawLine(intVec3, intVec5);
                    }
                }
            }
        }
        private void TryDrawLine(IntVec3 a, IntVec3 b)
        {
            Vector2 vector = a.ToVector3().MapToUIPosition();
            Vector2 vector2 = b.ToVector3().MapToUIPosition();
            DevGUI.DrawLine(vector, vector2, Color.blue, 2f);
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
    public class GenStep_ImTiredOfBeinCalmAllTheGaddamnTime : GenStep
    {
        public override int SeedPart
        {
            get
            {
                return 8767466;
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
            StructureGenParams structureGenParams = new StructureGenParams
            {
                size = new IntVec2(map.Size.x, map.Size.z)
            };
            LayoutWorker worker = HVMPDefOf_A.HVMP_HypercubeLayout.Worker;
            int num = 10;
            do
            {
                this.structureSketch = worker.GenerateStructureSketch(structureGenParams);
            }
            while (!this.structureSketch.structureLayout.HasRoomWithDef(HVMPDefOf_A.HVMP_HypercubeObelisk) && num-- > 0);
            if (num == 0)
            {
                Log.ErrorOnce("Failed to generate labyrinth, guard exceeded. Check layout worker for errors placing minimum rooms", 9868797);
                return;
            }
            worker.Spawn(this.structureSketch, map, IntVec3.Zero, null, null, false);
            worker.FillRoomContents(this.structureSketch, map);
            map.layoutStructureSketch = this.structureSketch;
            HVMP_HypercubeMapComponent component = map.GetComponent<HVMP_HypercubeMapComponent>();
            LayoutRoom firstRoomOfDef = this.structureSketch.structureLayout.GetFirstRoomOfDef(HVMPDefOf_A.HVMP_HypercubeObelisk);
            List<LayoutRoom> spawnableRooms = this.GetSpawnableRooms(firstRoomOfDef);
            component.SetSpawnRooms(spawnableRooms);
            MapGenerator.PlayerStartSpot = IntVec3.Zero;
            map.fogGrid.Refog(new CellRect(-1,-1,999,999).ClipInsideMap(map));
        }
        private List<LayoutRoom> GetSpawnableRooms(LayoutRoom obelisk)
        {
            List<LayoutRoom> list = new List<LayoutRoom>();
            list.AddRange(this.structureSketch.structureLayout.Rooms);
            list.Remove(obelisk);
            foreach (LayoutRoom layoutRoom in this.structureSketch.structureLayout.GetLogicalRoomConnections(obelisk))
            {
                if (list.Contains(layoutRoom))
                {
                    list.Remove(layoutRoom);
                    foreach (LayoutRoom layoutRoom2 in this.structureSketch.structureLayout.GetLogicalRoomConnections(layoutRoom))
                    {
                        if (list.Contains(layoutRoom2))
                        {
                            list.Remove(layoutRoom2);
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
        private LayoutStructureSketch structureSketch;
    }
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
                wallStuff = ThingDefOf.LabyrinthMatter,
                doorStuff = ThingDefOf.LabyrinthMatter
            };
            CellRect cellRect = new CellRect(0, 0, parms.size.x, parms.size.z);
            HVMP_LayoutWorkerHypercube.FillEdges(cellRect, layoutSketch);
            cellRect = cellRect.ContractedBy(2);
            parms.size = new IntVec2(cellRect.Width, cellRect.Height);
            using (new ProfilerBlock("Generate Labyrinth"))
            {
                layoutSketch.layout = this.GenerateLabyrinth(parms);
            }
            using (new ProfilerBlock("Flush"))
            {
                layoutSketch.FlushLayoutToSketch(new IntVec3(2, 0, 2));
            }
            return layoutSketch;
        }
        private StructureLayout GenerateLabyrinth(StructureGenParams parms)
        {
            StructureLayout structureLayout = new StructureLayout();
            CellRect cellRect = new CellRect(0, 0, parms.size.x, parms.size.z);
            structureLayout.Init(cellRect);
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
                }
                else
                {
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
                foreach (LayoutRoom layoutRoom2 in layout.GetLogicalRoomConnections(layoutRoom))
                {
                    if (!layoutRoom.connections.Contains(layoutRoom2))
                    {
                        HVMP_LayoutWorkerHypercube.ConnectRooms(layout, layoutRoom, layoutRoom2);
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
                                    }
                                    else if (closestEdge == Rot4.West && intVec2.x > cellRect.minX)
                                    {
                                        num += 4;
                                    }
                                    if (closestEdge == Rot4.North && intVec2.z < cellRect.maxZ)
                                    {
                                        num += 4;
                                    }
                                    else if (closestEdge == Rot4.South && intVec2.z > cellRect.minZ)
                                    {
                                        num += 4;
                                    }
                                    if (relativeRotation == RotationDirection.Clockwise || relativeRotation == RotationDirection.Counterclockwise)
                                    {
                                        num++;
                                    }
                                    else if (relativeRotation == RotationDirection.None)
                                    {
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
            int num2;
            while (priorityQueue.TryDequeue(out valueTuple, out num2))
            {
                IntVec3 item = valueTuple.Item1;
                IntVec3 item2 = valueTuple.Item2;
                List<IntVec3> list;
                if (HVMP_LayoutWorkerHypercube.TryGetPath(layout, item, item2, num2 * 2, out list))
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
                }
                while (flag2);
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
            using (IEnumerator<LayoutRoom> enumerator = layout.GetLogicalRoomConnections(layoutRoom).GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current == layoutRoom2)
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
        private static void FillEdges(CellRect rect, Sketch sketch)
        {
            for (int i = 0; i < rect.Width; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    HVMP_LayoutWorkerHypercube.SpawnWall(new IntVec3(i, 0, j), sketch);
                    HVMP_LayoutWorkerHypercube.SpawnWall(new IntVec3(i, 0, rect.Height - j - 1), sketch);
                }
            }
            for (int k = 2; k < rect.Height - 2; k++)
            {
                for (int l = 0; l < 2; l++)
                {
                    HVMP_LayoutWorkerHypercube.SpawnWall(new IntVec3(l, 0, k), sketch);
                    HVMP_LayoutWorkerHypercube.SpawnWall(new IntVec3(rect.Width - l - 1, 0, k), sketch);
                }
            }
        }
        private static void SpawnWall(IntVec3 pos, Sketch sketch)
        {
            sketch.AddThing(ThingDefOf.GrayWall, pos, Rot4.North, ThingDefOf.LabyrinthMatter, 1, null, null, true);
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
    public class HVMP_RoomContentsObelisk : RoomContentsWorker
    {
        public override void FillRoom(Map map, LayoutRoom room)
        {
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
            string text = "ThingDiscovered" + Find.UniqueIDsManager.GetNextSignalTagID();
            SignalAction_Letter signalAction_Letter = (SignalAction_Letter)ThingMaker.MakeThing(ThingDefOf.SignalAction_Letter, null);
            signalAction_Letter.signalTag = text;
            signalAction_Letter.letterDef = LetterDefOf.PositiveEvent;
            signalAction_Letter.letterLabelKey = "LetterLabelObeliskDiscovered";
            signalAction_Letter.letterMessageKey = "LetterObeliskDiscovered";
            GenSpawn.Spawn(signalAction_Letter, building.Position, map, WipeMode.Vanish);
            room.SpawnRectTriggersForAction(signalAction_Letter, map);
            RoomContentsGrayBox.SpawnBoxInRoom(cellRect.CenterCell + Rot4.East.FacingCell * 3, map, null, true);
            RoomContentsGrayBox.SpawnBoxInRoom(cellRect.CenterCell + Rot4.West.FacingCell * 3, map, null, true);
        }
        private const float WallRadius = 8.9f;
        private const float MetalRadius = 3.9f;
    }
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
            if (this.parent.Map != null && !spatialAnomaly.questTags.NullOrEmpty())
            {
                QuestUtility.SendQuestTargetSignals(spatialAnomaly.questTags, "NataliResolved", spatialAnomaly.Named("SUBJECT"));
            }
            this.parent.Map.GetComponent<HVMP_HypercubeMapComponent>().StartClosing();
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look<WorldObject>(ref this.spatialAnomaly, "spatialAnomaly", false);
        }
        public WorldObject spatialAnomaly;
    }
    //romero
    public class QuestNode_GetShamblerFaction : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return Find.FactionManager.FirstFactionOfDef(FactionDefOf.Entities) != null;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Entities);
            if (faction == null)
            {
                this.TryFindFaction(out faction, slate);
            }
            if (faction != null)
            {
                QuestGen.slate.Set<Faction>(this.storeAs.GetValue(slate), faction, false);
                if (!faction.Hidden)
                {
                    QuestPart_InvolvedFactions questPart_InvolvedFactions = new QuestPart_InvolvedFactions();
                    questPart_InvolvedFactions.factions.Add(faction);
                    QuestGen.quest.AddPart(questPart_InvolvedFactions);
                }
            }
        }
        private bool TryFindFaction(out Faction faction, Slate slate)
        {
            return (from x in Find.FactionManager.GetFactions(true, false, true, TechLevel.Undefined, false)
                    where this.IsGoodFaction(x, slate)
                    select x).TryRandomElement(out faction);
        }
        private bool IsGoodFaction(Faction faction, Slate slate)
        {
            return faction.HostileTo(Faction.OfPlayer);
        }
        [NoTranslate]
        public SlateRef<string> storeAs;
    }
    public class QuestNode_Romero : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return slate.Exists("map", false);
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Map map = QuestGen.slate.Get<Map>("map", null, false);
            float num = QuestGen.slate.Get<float>("points", 0f, false);
            Faction faction = Faction.OfEntities ?? QuestGen.slate.Get<Faction>("enemyFaction", null, false);
            QuestPart_Incident questPart_Incident = new QuestPart_Incident
            {
                debugLabel = "raid",
                incident = HVMPDefOf.HVMP_ShamblerAssault
            };
            IncidentParms incidentParms = this.GenerateIncidentParms(map, num, faction, slate, questPart_Incident);
            PawnGroupMakerParms defaultPawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Shamblers, incidentParms, true);
            defaultPawnGroupMakerParms.points = IncidentWorker_Raid.AdjustedRaidPoints(defaultPawnGroupMakerParms.points, incidentParms.raidArrivalMode, incidentParms.raidStrategy, defaultPawnGroupMakerParms.faction, PawnGroupKindDefOf.Shamblers, null);
            questPart_Incident.SetIncidentParmsAndRemoveTarget(incidentParms);
            questPart_Incident.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
            QuestGen.quest.AddPart(questPart_Incident);
        }
        private IncidentParms GenerateIncidentParms(Map map, float points, Faction faction, Slate slate, QuestPart_Incident questPart)
        {
            IncidentParms incidentParms = new IncidentParms();
            incidentParms.forced = true;
            incidentParms.target = map;
            incidentParms.points = Mathf.Max(points, faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat, null));
            incidentParms.faction = faction;
            incidentParms.pawnGroupMakerSeed = new int?(Rand.Int);
            incidentParms.inSignalEnd = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignalLeave.GetValue(slate));
            incidentParms.questTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(this.tag.GetValue(slate));
            incidentParms.canTimeoutOrFlee = this.canTimeoutOrFlee.GetValue(slate) ?? true;
            if (this.raidPawnKind.GetValue(slate) != null)
            {
                incidentParms.pawnKind = this.raidPawnKind.GetValue(slate);
                incidentParms.pawnCount = Mathf.Max(1, Mathf.RoundToInt(incidentParms.points / incidentParms.pawnKind.combatPower));
            }
            if (this.arrivalMode.GetValue(slate) != null)
            {
                incidentParms.raidArrivalMode = this.arrivalMode.GetValue(slate);
            }
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
            IncidentWorker_Raid incidentWorker_Raid = (IncidentWorker_Raid)questPart.incident.Worker;
            incidentWorker_Raid.ResolveRaidStrategy(incidentParms, PawnGroupKindDefOf.Combat);
            incidentWorker_Raid.ResolveRaidArriveMode(incidentParms);
            incidentWorker_Raid.ResolveRaidAgeRestriction(incidentParms);
            if (incidentParms.raidArrivalMode.walkIn)
            {
                incidentParms.spawnCenter = this.walkInSpot.GetValue(slate) ?? QuestGen.slate.Get<IntVec3?>("walkInSpot", null, false) ?? IntVec3.Invalid;
            }
            else
            {
                incidentParms.spawnCenter = this.dropSpot.GetValue(slate) ?? QuestGen.slate.Get<IntVec3?>("dropSpot", null, false) ?? IntVec3.Invalid;
            }
            return incidentParms;
        }

        [NoTranslate]
        public SlateRef<string> inSignal;
        public SlateRef<IntVec3?> walkInSpot;
        public SlateRef<IntVec3?> dropSpot;
        public SlateRef<string> customLetterLabel;
        public SlateRef<string> customLetterText;
        public SlateRef<RulePack> customLetterLabelRules;
        public SlateRef<RulePack> customLetterTextRules;
        public SlateRef<PawnsArrivalModeDef> arrivalMode;
        public SlateRef<PawnKindDef> raidPawnKind;
        public SlateRef<bool?> canTimeoutOrFlee;
        [NoTranslate]
        public SlateRef<string> inSignalLeave;
        [NoTranslate]
        public SlateRef<string> tag;
        private const string RootSymbol = "root";
    }
}
