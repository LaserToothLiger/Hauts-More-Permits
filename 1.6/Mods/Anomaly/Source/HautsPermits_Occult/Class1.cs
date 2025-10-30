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
using VEF.Abilities;
using static System.Net.Mime.MediaTypeNames;

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
            harmony.Patch(AccessTools.Method(typeof(UnnaturalCorpseTracker), nameof(UnnaturalCorpseTracker.CorpseTick)),
                          postfix: new HarmonyMethod(patchType, nameof(HVMPCorpseTickPostfix)));
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
        public static void HVMPCorpseTickPostfix(UnnaturalCorpseTracker __instance)
        {
            if (__instance.Corpse != null && __instance.Corpse.InnerPawn != null && __instance.Corpse.IsHashIntervalTick(7500))
            {
                Hediff h = __instance.Corpse.InnerPawn.health.hediffSet.GetFirstHediffOfDef(HVMPDefOf_A.HVMP_LGTH_Corpse);
                if (h != null)
                {
                    __instance.GetType().GetField("awakenTick", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, GenTicks.TicksGame + Mathf.CeilToInt(Rand.Range(h.def.minSeverity,h.def.maxSeverity) * 60000f));
                    __instance.Corpse.InnerPawn.health.RemoveHediff(h);
                }
            }
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
        public static HediffDef HVMP_LGTH;
        public static HediffDef HVMP_LGTH_Corpse;
        public static HediffDef HVMP_TOTU_PsychicViscosity;
        public static HediffDef HVMP_Mindfire_hediff;
        public static HediffDef HVMP_WeaknessOfFlesh_hediff;
        public static HediffDef HVMP_AOEAH_hediff;

        public static IncidentDef HVMP_LovecraftAlignment;

        public static LayoutDef HVMP_HypercubeLayout;
        public static LayoutRoomDef HVMP_HypercubeObelisk;
        public static MapGeneratorDef HVMP_Hypercube;
        public static ThingDef HVMP_WarpedObelisk_Hypercube;

        public static PawnKindDef HVMP_PrimedNociosphere;
        public static PawnKindDef HVMP_GhoulSuperEvil;

        public static ThingDef HVMP_LivingCloudkill;

        public static TerrainDef HVMP_BurntSurface;
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
            HVMP_Utility.DoPTargeterCooldown(this.faction,this.caller,this);
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
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            string text = this.def.LabelCap + ": ";
            Action action = null;
            bool free;
            if (HVMP_Utility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free))
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
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
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
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return HVMP_Utility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            HVMP_Utility.DoPTargeterCooldown(faction, caller, this);
        }
    }
    public class Hediff_Deghoulizer : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
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
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return HVMP_Utility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            HVMP_Utility.DoPTargeterCooldown(faction, caller, this);
        }
    }
    public class Hediff_Psylinker : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
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
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return HVMP_Utility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            HVMP_Utility.DoPTargeterCooldown(faction, caller, this);
        }
    }
    public class Hediff_InstantKillMode : Hediff
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            this.activated = false;
        }
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.Spawned)
            {
                if (this.pawn.Map.GetComponent<HVMP_HypercubeMapComponent>() == null)
                {
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
                            }
                            else
                            {
                                ca.SetActivity(ca.ActivityLevel + ((float)delta / this.ticksToKillMode));
                            }
                        }
                        else
                        {
                            this.activated = true;
                        }
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.activated, "activated", false, false);
        }
        public bool activated = false;
        public float ticksToKillMode = 900f;
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
                PlanetTile tile = HVMP_Utility.TryGetPlanetTile();
                slate.Set<PlanetTile>("pTile", tile, false);
                QuestPart_BranchGoodwillFailureHandler qpbgfh = new QuestPart_BranchGoodwillFailureHandler();
                qpbgfh.faction = occultFaction;
                QuestGen.quest.AddPart(qpbgfh);
                HVMP_Utility.SetSettingScalingRewardValue(slate);
            }
            base.RunInt();
        }
        protected override bool TestRunInt(Slate slate)
        {
            HVMP_Utility.SetSettingScalingRewardValue(slate);
            return HVMP_Utility.TryFindOccultFaction(out Faction occultFaction) && base.TestRunInt(slate);
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
            bool mayhemMode = HVMP_Mod.settings.barkerX;
            bool LGTH_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.barker1, mayhemMode);
            bool RR_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.barker2, mayhemMode);
            int TT_on = 1 + (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.barker3, mayhemMode) ? this.TT_bonusItems : 0);
            slate.Set<bool>(this.LGTH_storeAs.GetValue(slate), LGTH_on, false);
            if (LGTH_on)
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_LGTH_info", this.LGTH_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_LGTH_info", " ") });
            }
            if (RR_on)
            {
                QuestPart_Barker_RR qppd = new QuestPart_Barker_RR
                {
                    inSignal = QuestGenUtility.HardcodedSignalWithQuestID(quest.InitiateSignal),
                    provocationDelayHoursMin = this.RR_provocationDelayHoursRange.min,
                    provocationDelayHoursMax = this.RR_provocationDelayHoursRange.max
                };
                quest.AddPart(qppd);
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_RR_info", this.RR_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_RR_info", " ") });
            }
            if (TT_on > 1)
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_TT_info", this.TT_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_TT_info", this.nonTT_description.Formatted())
                });
            }
            while (TT_on > 0)
            {
                TT_on--;
                this.AddQuestlet(quest, LGTH_on);
            }
            base.RunInt();
        }
        public void AddQuestlet(Quest quest, bool LGTH_on)
        {
            QuestPart_MakeAndAcceptNewQuest qppd = new QuestPart_MakeAndAcceptNewQuest
            {
                inSignal = QuestGenUtility.HardcodedSignalWithQuestID(quest.InitiateSignal),
                questToMake = this.possibleQuests.RandomElement(),
                LGTH_on = LGTH_on
            };
            quest.AddPart(qppd);
        }
        public List<QuestScriptDef> possibleQuests;
        [MustTranslate]
        public string LGTH_description;
        [NoTranslate]
        public SlateRef<string> LGTH_storeAs;
        public IntRange RR_provocationDelayHoursRange;
        [MustTranslate]
        public string RR_description;
        public int TT_bonusItems;
        [MustTranslate]
        public string TT_description;
        [MustTranslate]
        public string nonTT_description;
    }
    public class QuestPart_MakeAndAcceptNewQuest : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                IIncidentTarget randomPlayerHomeMap = Find.RandomPlayerHomeMap;
                if (this.questToMake != null && this.questToMake.CanRun(this.QuestPoints, randomPlayerHomeMap ?? Find.World))
                {
                    Slate newSlate = new Slate();
                    newSlate.Set<float>("points", this.QuestPoints, false);
                    newSlate.Set<bool>("LGTH_on", QuestGen.slate.Get<bool>("LGTH_on", LGTH_on, false), false);
                    Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(this.questToMake, newSlate);
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
            Scribe_Values.Look<bool>(ref this.LGTH_on, "LGTH_on", false, false);
        }
        public string inSignal;
        public QuestScriptDef questToMake;
        public bool LGTH_on;
    }
    public class QuestPart_Barker_RR : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                Map map = Find.RandomPlayerHomeMap;
                List<IncidentDef> list = new List<IncidentDef>();
                bool flag = false;
                foreach (EntityCategoryDef entityCategoryDef in DefDatabase<EntityCategoryDef>.AllDefs.OrderBy((EntityCategoryDef x) => x.listOrder))
                {
                    foreach (EntityCodexEntryDef entityCodexEntryDef in DefDatabase<EntityCodexEntryDef>.AllDefs)
                    {
                        if (entityCodexEntryDef.category == entityCategoryDef && !entityCodexEntryDef.provocationIncidents.NullOrEmpty<IncidentDef>() && !entityCodexEntryDef.Discovered)
                        {
                            foreach (IncidentDef incidentDef in entityCodexEntryDef.provocationIncidents)
                            {
                                IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(incidentDef.category, map);
                                incidentParms.bypassStorytellerSettings = true;
                                if (incidentDef.Worker.CanFireNow(incidentParms))
                                {
                                    list.Add(incidentDef);
                                    flag = true;
                                }
                            }
                        }
                    }
                    if (flag)
                    {
                        break;
                    }
                }
                if (!list.Any<IncidentDef>())
                {
                    foreach (EntityCodexEntryDef entityCodexEntryDef2 in DefDatabase<EntityCodexEntryDef>.AllDefs)
                    {
                        if (!entityCodexEntryDef2.provocationIncidents.NullOrEmpty<IncidentDef>())
                        {
                            foreach (IncidentDef incidentDef2 in entityCodexEntryDef2.provocationIncidents)
                            {
                                IncidentParms incidentParms2 = StorytellerUtility.DefaultParmsNow(incidentDef2.category, map);
                                incidentParms2.bypassStorytellerSettings = true;
                                if (incidentDef2.Worker.CanFireNow(incidentParms2))
                                {
                                    list.Add(incidentDef2);
                                }
                            }
                        }
                    }
                }
                if (!list.NullOrEmpty() && list.TryRandomElement(out IncidentDef incidentDef3))
                {
                    IncidentParms incidentParms3 = StorytellerUtility.DefaultParmsNow(incidentDef3.category, map);
                    incidentParms3.bypassStorytellerSettings = true;
                    Find.Storyteller.incidentQueue.Add(incidentDef3, Find.TickManager.TicksGame + Mathf.RoundToInt(Rand.RangeInclusive(this.provocationDelayHoursMin,this.provocationDelayHoursMax) * 2500f), incidentParms3, 0);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_Values.Look<int>(ref this.provocationDelayHoursMin, "provocationDelayHoursMin", 1, false);
            Scribe_Values.Look<int>(ref this.provocationDelayHoursMax, "provocationDelayHoursMax", 3, false);
        }
        public string inSignal;
        public int provocationDelayHoursMin;
        public int provocationDelayHoursMax;
    }
    public abstract class QuestNode_Root_BarkerBox : QuestNode
    {
        protected virtual bool RequiresPawn { get; } = true;
        protected override bool TestRunInt(Slate slate)
        {
            Map map = HVMP_Utility.TryGetMap();
            return map != null && (!this.RequiresPawn || QuestUtility.TryGetIdealColonist(out Pawn pawn, map, new Func<Pawn, bool>(this.ValidatePawn)));
        }
        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            quest.hiddenInUI = true;
            Map map = HVMP_Utility.TryGetMap();
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
                quest.DropPods(map.Parent, new List<Thing> { thing }, "[deliveredLetterLabel]", null, "[deliveredLetterText]", null, new bool?(true), false, false, false, null, null, QuestPart.SignalListenMode.OngoingOnly, null, true, true, false, true, null);
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
            Slate slate = QuestGen.slate;
            quest.PawnDestroyed(pawn, null, delegate
            {
                quest.LinkUnnaturalCorpse(pawn, thing as UnnaturalCorpse, null);
            }, null, null, null, QuestPart.SignalListenMode.OngoingOnly);
            if (slate.Get<bool>("LGTH_on", false, false) && thing is UnnaturalCorpse uc && uc.InnerPawn != null)
            {
                uc.InnerPawn.health.AddHediff(HVMPDefOf_A.HVMP_LGTH_Corpse);
            }
        }
        public IntRange LGTH_daysToAwaken;
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
            if (QuestGen.slate.Get<bool>("LGTH_on", false, false) && pawn.MapHeld != null)
            {
                int pawnsToAffect = Math.Min(pawn.MapHeld.mapPawns.SlavesAndPrisonersOfColonySpawnedCount + pawn.MapHeld.mapPawns.ColonistCount - 1, this.LGTH_bonusInterests.RandomInRange);
                for (int i = pawnsToAffect; i > 0; i--)
                {
                    Pawn pawn2 = null;
                    int tries = 100;
                    while (tries > 0)
                    {
                        if (QuestUtility.TryGetIdealColonist(out pawn2, pawn.MapHeld, new Func<Pawn, bool>(this.ValidatePawn)))
                        {
                            if (pawn2 != pawn)
                            {
                                quest.GiveHediff(pawn2, HediffDefOf.CubeInterest, null);
                                break;
                            }
                        }
                        tries--;
                    }
                }
            }
        }
        protected override bool ValidatePawn(Pawn pawn)
        {
            return base.ValidatePawn(pawn) && !pawn.health.hediffSet.HasHediff(HediffDefOf.CubeInterest, false) && !pawn.health.hediffSet.HasHediff(HediffDefOf.CubeComa, false);
        }
        public IntRange LGTH_bonusInterests;
    }
    public class QuestNode_Root_BarkerSphere : QuestNode_Root_BarkerBox
    {
        protected override bool RequiresPawn { get; }
        protected override Thing GenerateThing(Pawn pawn)
        {
            Pawn thing = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Nociosphere, Faction.OfEntities, PawnGenerationContext.NonPlayer, -1, false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
            if (QuestGen.slate.Get<bool>("LGTH_on", false, false))
            {
                thing.health.AddHediff(HVMPDefOf_A.HVMP_LGTH);
            }
            return thing;
        }
    }
    public class QuestNode_Root_BarkerNucleus : QuestNode_Root_BarkerBox
    {
        protected override bool RequiresPawn { get; }
        protected override Thing GenerateThing(Pawn pawn)
        {
            Pawn thing = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.FleshmassNucleus, Faction.OfEntities, PawnGenerationContext.NonPlayer, -1, false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
            thing.health.AddHediff(this.hediff);
            if (QuestGen.slate.Get<bool>("LGTH_on", false, false))
            {
                thing.health.AddHediff(HVMPDefOf_A.HVMP_LGTH);
            }
            return thing;
        }
        public HediffDef hediff;
    }
    public class QuestNode_Root_BarkerSpine : QuestNode_Root_BarkerBox
    {
        protected override bool RequiresPawn { get; }
        protected override Thing GenerateThing(Pawn _)
        {
            return ThingMaker.MakeThing(ThingDefOf.RevenantSpine, null);
        }
    }
    public class HediffCompProperties_LGTH : HediffCompProperties
    {
        public HediffCompProperties_LGTH()
        {
            this.compClass = typeof(HediffComp_LGTH);
        }
        public float activityPerHour;
        public float activityPerDamage;
    }
    public class HediffComp_LGTH : HediffComp
    {
        public HediffCompProperties_LGTH Props
        {
            get
            {
                return (HediffCompProperties_LGTH)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            CompActivity ca = this.Pawn.GetComp<CompActivity>();
            if (ca != null)
            {
                if (ca.IsActive)
                {
                    this.Pawn.health.RemoveHediff(this.parent);
                    return;
                }
                ca.SetActivity(ca.ActivityLevel + (this.Props.activityPerHour * delta / 2500f));
            }
        }
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            if (totalDamageDealt > 0f && this.Props.activityPerDamage > 0f)
            {
                CompActivity ca = this.Pawn.GetComp<CompActivity>();
                if (ca != null && ca.IsDormant)
                {
                    ca.SetActivity(ca.ActivityLevel + this.Props.activityPerDamage, false);
                }
            }
        }
    }
    //fuller
    public class QuestNode_Fuller : QuestNode_OccultIntermediary
    {
        protected override bool TestRunInt(Slate slate)
        {
            HVMP_Utility.SetSettingScalingRewardValue(slate);
            return HVMP_Utility.TryFindOccultFaction(out Faction occultFaction);
        }
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
    public class QuestNode_GiveFullerMutators : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            if (this.pawns.GetValue(slate) == null)
            {
                return;
            }
            bool mayhemMode = HVMP_Mod.settings.fullerX;
            bool FWK_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.fuller1, mayhemMode);
            bool SRM_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.fuller2, mayhemMode);
            bool WLW_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.fuller3, mayhemMode);
            slate.Set<bool>(this.FWK_storeAs.GetValue(slate), FWK_on, false);
            QuestPart_GiveFullerMutators qpghef = new QuestPart_GiveFullerMutators
            {
                inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false)
            };
            if (FWK_on)
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_FWK_info", this.FWK_description.Formatted())
                });
                qpghef.bonusDeathRefusalsMin = this.FWK_deathRefusalCount.min;
                qpghef.bonusDeathRefusalsMax = this.FWK_deathRefusalCount.max;
                qpghef.FWK_hediffDef = this.FWK_hediffDef;
                slate.Set<int>(this.FWK_storeAsMin.GetValue(slate), this.FWK_deathRefusalCount.min, false);
                slate.Set<int>(this.FWK_storeAsMax.GetValue(slate), this.FWK_deathRefusalCount.max, false);
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_FWK_info", " ") });
            }
            if (SRM_on)
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_SRM_info", this.SRM_description.Formatted())
                });
                qpghef.SRM_hediffDef = this.SRM_hediffDef;
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_SRM_info", " ") });
            }
            if (WLW_on)
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_WLW_info", this.WLW_description.Formatted())
                });
                qpghef.WLW_hediffDef = this.WLW_hediffDef;
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_WLW_info", " ") });
            }
            qpghef.pawns.AddRange(this.pawns.GetValue(slate));
            QuestGen.quest.AddPart(qpghef);
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        public SlateRef<IEnumerable<Pawn>> pawns;
        public IntRange FWK_deathRefusalCount;
        [NoTranslate]
        public SlateRef<string> FWK_storeAs;
        [NoTranslate]
        public SlateRef<string> FWK_storeAsMin;
        [NoTranslate]
        public SlateRef<string> FWK_storeAsMax;
        public HediffDef FWK_hediffDef;
        [MustTranslate]
        public string FWK_description;
        public HediffDef SRM_hediffDef;
        [MustTranslate]
        public string SRM_description;
        public HediffDef WLW_hediffDef;
        [MustTranslate]
        public string WLW_description;
    }
    public class QuestPart_GiveFullerMutators : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                for (int i = 0; i < this.pawns.Count; i++)
                {
                    if (this.FWK_hediffDef != null)
                    {
                        Hediff hediff = this.pawns[i].health.hediffSet.GetFirstHediffOfDef(HediffDefOf.DeathRefusal);
                        if (hediff == null)
                        {
                            hediff = HediffMaker.MakeHediff(HediffDefOf.DeathRefusal, this.pawns[i]);
                            this.pawns[i].health.AddHediff(hediff, null);
                        }
                        if (hediff is Hediff_DeathRefusal hdr)
                        {
                            hdr.SetUseAmountDirect(hdr.UsesLeft + Rand.RangeInclusive(this.bonusDeathRefusalsMin, this.bonusDeathRefusalsMax), true);
                        }
                        this.pawns[i].health.AddHediff(this.FWK_hediffDef);
                    }
                    if (this.SRM_hediffDef != null)
                    {
                        this.pawns[i].health.AddHediff(this.SRM_hediffDef);
                    }
                    if (this.WLW_hediffDef != null)
                    {
                        this.pawns[i].health.AddHediff(this.WLW_hediffDef);
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_Collections.Look<Pawn>(ref this.pawns, "pawns", LookMode.Reference, Array.Empty<object>());
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.pawns.RemoveAll((Pawn x) => x == null);
            }
            Scribe_Values.Look<int>(ref this.bonusDeathRefusalsMin, "bonusDeathRefusalsMin", 0, false);
            Scribe_Values.Look<int>(ref this.bonusDeathRefusalsMax, "bonusDeathRefusalsMax", 0, false);
            Scribe_Defs.Look<HediffDef>(ref this.FWK_hediffDef, "FWK_hediffDef");
            Scribe_Defs.Look<HediffDef>(ref this.SRM_hediffDef, "SRM_hediffDef");
            Scribe_Defs.Look<HediffDef>(ref this.WLW_hediffDef, "WLW_hediffDef");
        }
        public override void ReplacePawnReferences(Pawn replace, Pawn with)
        {
            this.pawns.Replace(replace, with);
        }
        public string inSignal;
        public List<Pawn> pawns = new List<Pawn>();
        public int bonusDeathRefusalsMin;
        public int bonusDeathRefusalsMax;
        public HediffDef FWK_hediffDef;
        public HediffDef SRM_hediffDef;
        public HediffDef WLW_hediffDef;
    }
    public class HediffCompProperties_FWK : HediffCompProperties
    {
        public HediffCompProperties_FWK()
        {
            this.compClass = typeof(HediffComp_FWK);
        }
        public float MTBdays;
        public float raidPointFactor;
        public int maxPawns = 10;
        public int minPawns = 1;
    }
    public class HediffComp_FWK : HediffComp
    {
        public HediffCompProperties_FWK Props
        {
            get
            {
                return (HediffCompProperties_FWK)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.SpawnedOrAnyParentSpawned && this.Pawn.IsHashIntervalTick(250,delta) && Rand.MTBEventOccurs(this.Props.MTBdays, 60000f, 250))
            {
                this.SkipIn(this.Pawn.MapHeld, this.Pawn.PositionHeld);
            }
        }
        public void SkipIn(Map map, IntVec3 iv3)
        {
            Faction f = this.Pawn.Faction ?? Faction.OfHoraxCult;
            IncidentParms parms = new IncidentParms
            {
                target = map,
                faction = f,
                points = Math.Max(StorytellerUtility.DefaultThreatPointsNow(map)*this.Props.raidPointFactor,f.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat)),
                forced = true,
                bypassStorytellerSettings = true,
                spawnCenter = iv3,
                pawnGroupKind = PawnGroupKindDefOf.Combat
            };
            parms.pawnCount = Math.Max(this.Props.minPawns,Math.Min(parms.pawnCount, this.Props.maxPawns));
            List<Pawn> list = new List<Pawn>();
            PawnGroupMaker pgm = f.def.pawnGroupMakers.Where((PawnGroupMaker pgm2) => pgm2.kindDef == PawnGroupKindDefOf.Combat).RandomElement();
            for (int i = 0; i < parms.pawnCount; i++)
            {
                PawnKindDef pawnKind = pgm.options.RandomElementByWeight((PawnGenOption pgo)=>pgo.selectionWeight).kind;
                PawnGenerationContext pawnGenerationContext = PawnGenerationContext.NonPlayer;
                float biocodeWeaponsChance = parms.biocodeWeaponsChance;
                float biocodeApparelChance = parms.biocodeApparelChance;
                bool pawnsCanBringFood = false;
                Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(pawnKind, f, pawnGenerationContext, null, true, false, false, false, true, 1f, false, true, false, pawnsCanBringFood, true, false, false, false, false, biocodeWeaponsChance, biocodeApparelChance, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
                if (pawn != null)
                {
                    list.Add(pawn);
                }
            }
            if (list.Any())
            {
                for (int i = 0; i < list.Count; i++)
                {
                    IntVec3 loc = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 6, null);
                    while (loc == parms.spawnCenter)
                    {
                        loc = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 6, null);
                    }
                    GenSpawn.Spawn(list[i], loc, map, parms.spawnRotation, WipeMode.Vanish, false);
                    FleckMaker.Static(loc, map, FleckDefOf.PsycastSkipInnerExit, 1f);
                    FleckMaker.Static(loc, map, FleckDefOf.PsycastSkipOuterRingExit, 1f);
                    SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(loc, map, false));
                }
            }
            Lord lord = LordMaker.MakeNewLord(f, new LordJob_AssaultColony(f, false, true, false, false, false, false, false), map, null);
            lord.AddPawns(list, true);
            if (!this.Pawn.Downed)
            {
                if (this.Pawn.guest != null && this.Pawn.guest.IsPrisoner)
                {
                    this.Pawn.guest.SetGuestStatus(null, GuestStatus.Guest);
                }
                Lord lord2 = this.Pawn.lord;
                if (lord2 != null)
                {
                    lord2.RemovePawn(this.Pawn);
                }
                lord.AddPawn(this.Pawn);
            }
            Find.LetterStack.ReceiveLetter("HVMP_TeleportationRaidLabel".Translate(), "HVMP_TeleportationRaidText".Translate(), LetterDefOf.ThreatBig, list, null, null, null, null, 0, true);
        }
    }
    public class Hediff_SRM : Hediff
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(150) && (this.pawn.Spawned || this.pawn.GetCaravan() != null))
            {
                for (int i = this.pawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
                {
                    Hediff hediff = this.pawn.health.hediffSet.hediffs[i];
                    if (hediff.def == HediffDefOf.Anesthetic)
                    {
                        this.ThisOneWouldLikeToRage(hediff);
                        return;
                    }
                    if (hediff.def.isBad && hediff.CurStage != null && hediff.CurStage.capMods != null)
                    {
                        foreach (PawnCapacityModifier pcm in hediff.CurStage.capMods)
                        {
                            if (pcm.capacity == PawnCapacityDefOf.Consciousness && pcm.setMax <= 0.3f)
                            {
                                this.ThisOneWouldLikeToRage(hediff);
                                break;
                            }
                        }
                    }
                }
            }
        }
        public void ThisOneWouldLikeToRage(Hediff toRemove)
        {
            if (toRemove != null)
            {
                this.pawn.health.RemoveHediff(toRemove);
                if (this.pawn.guest != null && this.pawn.guest.IsPrisoner && PrisonBreakUtility.InitiatePrisonBreakMtbDays(this.pawn, null, true) > 0f)
                {
                    PrisonBreakUtility.StartPrisonBreak(this.pawn);
                }
            }
        }
    }
    public class ThoughtWorker_SRM : ThoughtWorker_Hediff
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.guest == null || !p.guest.IsPrisoner)
            {
                return ThoughtState.Inactive;
            }
            return base.CurrentStateInternal(p);
        }
    }
    public class Hediff_WLW : HediffWithComps {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsMutant)
            {
                this.pawn.health.RemoveHediff(this);
                return;
            }
            if (this.Severity == this.def.maxSeverity)
            {
                MutantUtility.SetPawnAsMutantInstantly(this.pawn, MutantDefOf.Ghoul, RotStage.Fresh);
                Faction f = Faction.OfEntities ?? this.pawn.Faction;
                this.pawn.SetFaction(f);
                Hediff anesthetic = this.pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Anesthetic);
                if (anesthetic != null)
                {
                    this.pawn.health.RemoveHediff(anesthetic);   
                }
                if (this.pawn.Spawned && !this.pawn.Downed)
                {
                    Lord lord = LordMaker.MakeNewLord(f, new LordJob_AssaultColony(f, false, true, false, false, false, false, false), this.pawn.Map, null);
                    if (!this.pawn.Downed)
                    {
                        Lord lord2 = this.pawn.lord;
                        if (lord2 != null)
                        {
                            lord2.RemovePawn(this.pawn);
                        }
                        lord.AddPawn(this.pawn);
                    }
                }
                this.pawn.health.RemoveHediff(this);
            }
        }
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
            bool mayhemMode = HVMP_Mod.settings.lcX;
            bool EFT_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.lc1, mayhemMode);
            bool NKNS_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.lc2, mayhemMode);
            bool TOTU_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.lc3, mayhemMode);
            List<IncidentDef> usableIncidents = new List<IncidentDef>();
            usableIncidents.AddRange(this.possibleBadIncidents);
            if (EFT_on)
            {
                if (Rand.Chance(this.EFT_chance))
                {
                    QuestPart_EFT qeft = new QuestPart_EFT
                    {
                        inSignal = QuestGenUtility.HardcodedSignalWithQuestID(quest.InitiateSignal)
                    };
                    IncidentDef ieft = this.EFT_incidents.RandomElement();
                    if (ieft != null)
                    {
                        qeft.incidentParms = ip;
                        qeft.incident = ieft;
                        List<IncidentDef> iefts = new List<IncidentDef>();
                        iefts.AddRange(this.EFT_incidents);
                        iefts.Remove(ieft);
                        qeft.otherIncidents = iefts;
                    }
                    qeft.SetIncidentParmsAndRemoveTarget(ip);
                    quest.AddPart(qeft);
                }
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_EFT_info", this.EFT_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_EFT_info", " ") });
            }
            if (NKNS_on)
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_NKNS_info", this.NKNS_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_NKNS_info", this.nonNKNS_description.Formatted())
                });
                usableIncidents.AddRange(this.possibleOkIncidents);
            }
            if (TOTU_on)
            {
                float num = slate.Get<float>("points", 0f, false);
                GameCondition gameCondition2 = GameConditionMaker.MakeCondition(this.TOTU_conditions.RandomElement(), this.TOTU_durationTicks.RandomInRange);
                QuestPart_GameCondition questPart_GameCondition2 = new QuestPart_GameCondition();
                questPart_GameCondition2.gameCondition = gameCondition2;
                List<Rule> list2 = new List<Rule>();
                Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
                Map map = HVMP_Utility.GetMap_QuestNodeGameCondition(slate);
                questPart_GameCondition2.mapParent = map.Parent;
                gameCondition2.RandomizeSettings(num, map, list2, dictionary2);
                questPart_GameCondition2.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
                questPart_GameCondition2.sendStandardLetter = false;
                quest.AddPart(questPart_GameCondition2);
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_TOTU_info", this.TOTU_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_TOTU_info", " ") });
            }
            IncidentDef incident = usableIncidents.RandomElement();
            if (incident == null)
            {
                incident = HVMPDefOf_A.HVMP_LovecraftAlignment;
            }
            if (incident != null)
            {
                qppd.incidentParms = ip;
                qppd.incident = incident;
                List<IncidentDef> incidents = usableIncidents;
                incidents.Remove(incident);
                qppd.otherIncidents = incidents;
            }
            qppd.SetIncidentParmsAndRemoveTarget(ip);
            quest.AddPart(qppd);
            base.RunInt();
        }
        public List<IncidentDef> possibleOkIncidents;
        public List<IncidentDef> possibleBadIncidents;
        public int cooldownBetweenLovecrafts;
        public float EFT_chance;
        public List<IncidentDef> EFT_incidents;
        [MustTranslate]
        public string EFT_description;
        [MustTranslate]
        public string nonNKNS_description;
        [MustTranslate]
        public string NKNS_description;
        public IntRange TOTU_durationTicks;
        public List<GameConditionDef> TOTU_conditions;
        [MustTranslate]
        public string TOTU_description;
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
    public class QuestPart_EFT : QuestPart
    {
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
            }
        }
        public void SetIncidentParmsAndRemoveTarget(IncidentParms value)
        {
            this.incidentParms = value;
            Map map = this.incidentParms.target as Map;
            if (map != null)
            {
                this.mapParent = map.Parent;
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
        }
        public string inSignal;
        public IncidentDef incident;
        public List<IncidentDef> otherIncidents;
        public IncidentParms incidentParms;
        private MapParent mapParent;
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
                string explanation = HVMPDefOf_A.HVMP_LovecraftAlignmentCond.LabelCap + ": +" + this.offset.ToStringPercent() + ", x" + this.factor.ToStringPercent();
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
                if ((p.Faction == null || p.Faction != Faction.OfPlayer) && p.IsAnimal && p.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon && !p.InAggroMentalState)
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
            List<Pawn> pawns = map.mapPawns.AllPawnsSpawned.ToList();
            for (int i = pawns.Count - 1; i >= 0; i--)
            {
                Pawn p = pawns[i];
                if ((p.Faction == null || p.Faction != Faction.OfPlayer) && p.IsAnimal && p.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon && (insaneAnimals <= 2 || Rand.Chance(0.9f)))
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
    public class IncidentWorker_MutationPulse : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
            {
                if ((p.Faction == null || p.Faction != Faction.OfPlayer) && p.IsAnimal)
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
            List<Pawn> pawns = map.mapPawns.AllPawnsSpawned.ToList();
            for (int i = pawns.Count - 1; i >= 0; i--)
            {
                Pawn p = pawns[i];
                if ((p.Faction == null || p.Faction != Faction.OfPlayer) && p.IsAnimal && p.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon && (insaneAnimals <= 2 || Rand.Chance(0.9f)))
                {
                    FleshbeastUtility.SpawnFleshbeastFromPawn(p, false, false, Array.Empty<PawnKindDef>());
                    insaneAnimals++;
                }
            }
            base.SendStandardLetter("HVMP_LovecraftManhunterLabel".Translate(), "HVMP_LovecraftManhunterText".Translate(), LetterDefOf.ThreatBig, parms, LookTargets.Invalid, Array.Empty<NamedArgument>());
            return true;
        }
    }
    public class IncidentWorker_PrimedNociosphere : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 spawnCenter = parms.spawnCenter;
            if (!spawnCenter.IsValid && !RCellFinder.TryFindRandomSpotJustOutsideColony(parms.spawnCenter, map, out spawnCenter))
            {
                return false;
            }
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(HVMPDefOf_A.HVMP_PrimedNociosphere, Faction.OfEntities, PawnGenerationContext.NonPlayer, new PlanetTile?(map.Tile), false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
            NociosphereUtility.SkipTo((Pawn)GenSpawn.Spawn(pawn, spawnCenter, map, WipeMode.Vanish), spawnCenter);
            base.SendStandardLetter(parms, pawn, Array.Empty<NamedArgument>());
            return true;
        }
    }
    public class IncidentWorker_SuperGhoul : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 spawnCenter = parms.spawnCenter;
            if (!spawnCenter.IsValid && !RCellFinder.TryFindRandomSpotJustOutsideColony(parms.spawnCenter, map, out spawnCenter))
            {
                return false;
            }
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(HVMPDefOf_A.HVMP_GhoulSuperEvil, Faction.OfEntities, PawnGenerationContext.NonPlayer, new PlanetTile?(map.Tile), false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
            pawn.health.overrideDeathOnDownedChance = 0f;
            if (AnomalyIncidentUtility.IncidentShardChance(parms.points))
            {
                AnomalyIncidentUtility.PawnShardOnDeath(pawn);
            }
            GenSpawn.Spawn(pawn,spawnCenter,map);
            FleckMaker.Static(pawn.Position, map, FleckDefOf.PsycastSkipInnerExit, 1f);
            FleckMaker.Static(pawn.Position, map, FleckDefOf.PsycastSkipOuterRingExit, 1f);
            SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(pawn.Position, map, false));
            //LordMaker.MakeNewLord(parms.faction, new LordJob_AssaultColony(Faction.OfEntities, false, false, false, false, false, false, false), map, Gen.YieldSingle<Pawn>(pawn));
            base.SendStandardLetter(parms, pawn, Array.Empty<NamedArgument>());
            return true;
        }
    }
    public class GameCondition_BloodBlight : GameCondition
    {
        public override void GameConditionTick()
        {
            base.GameConditionTick();
            if (Find.TickManager.TicksGame % 120 == 0 && Rand.MTBEventOccurs(3f, 2500f, 120))
            {
                foreach (Map map in base.AffectedMaps)
                {
                    foreach (Pawn p in map.mapPawns.AllPawnsSpawned.InRandomOrder())
                    {
                        if (p.RaceProps.IsFlesh && !p.IsMutant && !p.IsEntity)
                        {
                            //Log.Error("p: " + p.Label);
                            Hediff hediff = HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed("BloodRot"),p);
                            p.health.DropBloodFilth();
                            p.health.AddHediff(hediff);
                            HediffComp_Disappears hcd = hediff.TryGetComp<HediffComp_Disappears>();
                            if (hcd != null)
                            {
                                hcd.disappearsAfterTicks /= 3;
                                hcd.ticksToDisappear = hcd.disappearsAfterTicks;
                            }
                            FleshbeastUtility.MeatSplatter(2, p.PositionHeld, p.MapHeld, FleshbeastUtility.MeatExplosionSize.Normal);
                            break;
                        }
                    }
                }
            }
        }
    }
    public class GameCondition_PsychicViscosity : GameCondition
    {
        public override void Init()
        {
            base.Init();
        }
        public static void CheckPawn(Pawn pawn)
        {
            if (pawn.RaceProps.IsFlesh && !pawn.IsMutant && !pawn.IsEntity && !pawn.health.hediffSet.HasHediff(HVMPDefOf_A.HVMP_TOTU_PsychicViscosity, false))
            {
                pawn.health.AddHediff(HVMPDefOf_A.HVMP_TOTU_PsychicViscosity, null, null, null);
            }
        }
        public override void GameConditionTick()
        {
            foreach (Map map in base.AffectedMaps)
            {
                List<Pawn> allPawns = map.mapPawns.AllPawns;
                for (int i = 0; i < allPawns.Count; i++)
                {
                    GameCondition_PsychicViscosity.CheckPawn(allPawns[i]);
                }
            }
        }
    }
    public class GameCondition_Mindfire : GameCondition
    {
        public override void Init()
        {
            base.Init();
        }
        public static void CheckPawn(Pawn pawn)
        {
            if (pawn.RaceProps.IsFlesh && !pawn.IsMutant && !pawn.IsEntity && !pawn.health.hediffSet.HasHediff(HVMPDefOf_A.HVMP_Mindfire_hediff, false))
            {
                pawn.health.AddHediff(HVMPDefOf_A.HVMP_Mindfire_hediff, null, null, null);
            }
        }
        public override void GameConditionTick()
        {
            foreach (Map map in base.AffectedMaps)
            {
                List<Pawn> allPawns = map.mapPawns.AllPawns;
                for (int i = 0; i < allPawns.Count; i++)
                {
                    GameCondition_Mindfire.CheckPawn(allPawns[i]);
                }
            }
        }
    }
    public class GameCondition_WeaknessOfFlesh : GameCondition
    {
        public override void Init()
        {
            base.Init();
        }
        public static void CheckPawn(Pawn pawn)
        {
            if (pawn.RaceProps.IsFlesh && !pawn.IsMutant && !pawn.IsEntity && !pawn.health.hediffSet.HasHediff(HVMPDefOf_A.HVMP_WeaknessOfFlesh_hediff, false))
            {
                pawn.health.AddHediff(HVMPDefOf_A.HVMP_WeaknessOfFlesh_hediff, null, null, null);
            }
        }
        public override void GameConditionTick()
        {
            foreach (Map map in base.AffectedMaps)
            {
                List<Pawn> allPawns = map.mapPawns.AllPawns;
                for (int i = 0; i < allPawns.Count; i++)
                {
                    GameCondition_WeaknessOfFlesh.CheckPawn(allPawns[i]);
                }
            }
        }
    }
    //natali
    public class QuestNode_GenerateHypercube : QuestNode
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            WorldObject worldObject = WorldObjectMaker.MakeWorldObject(this.def.GetValue(slate));
            worldObject.Tile = this.tile.GetValue(slate);
            if (worldObject is WorldObject_Hypercube wohc)
            {
                bool mayhemMode = HVMP_Mod.settings.nataliX;
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.natali1,mayhemMode))
                {
                    wohc.AOEAH_condition = this.AOEAH_condition;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_AOEAH_info", this.AOEAH_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_AOEAH_info", " ") });
                }
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.natali2, mayhemMode))
                {
                    wohc.COL_on = true;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_COL_info", this.COL_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_COL_info", " ") });
                }
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.natali3, mayhemMode))
                {
                    wohc.DF_on = true;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_DF_info", this.DF_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_DF_info", " ") });
                }
            }
            if (this.faction.GetValue(slate) != null)
            {
                worldObject.SetFaction(this.faction.GetValue(slate));
            }
            if (this.storeAs.GetValue(slate) != null)
            {
                QuestGen.slate.Set<WorldObject>(this.storeAs.GetValue(slate), worldObject, false);
            }
        }
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        public SlateRef<WorldObjectDef> def;
        public SlateRef<PlanetTile> tile;
        public SlateRef<Faction> faction;
        [NoTranslate]
        public SlateRef<string> storeAs;
        public GameConditionDef AOEAH_condition;
        [MustTranslate]
        public string AOEAH_description;
        [MustTranslate]
        public string COL_description;
        [MustTranslate]
        public string DF_description;
    }
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
            Scribe_Values.Look<bool>(ref this.DF_on, "DF_on", false, false);
        }
        private Material cachedMat;
        public Map labyrinthMap;
        public GameConditionDef AOEAH_condition;
        public bool COL_on;
        public bool DF_on;
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
            }
            while (!this.structureSketch.structureLayout.HasRoomWithDef(HVMPDefOf_A.HVMP_HypercubeObelisk) && num-- > 0);
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
            map.fogGrid.Refog(new CellRect(-1,-1,999,999).ClipInsideMap(map));
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
    public class CompProperties_ObeliskHypercube : CompProperties_Interactable
    {
        public CompProperties_ObeliskHypercube()
        {
            this.compClass = typeof(CompObelisk_Hypercube);
        }
        [MustTranslate]
        public string messageActivating;
        public List<PawnKindDef> DF_pawnKinds;
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
            if (hmc != null && hmc.spatialAnomaly != null && hmc.spatialAnomaly is WorldObject_Hypercube woh && woh.DF_on)
            {
                Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(this.Props.DF_pawnKinds.RandomElement(), Faction.OfEntities, PawnGenerationContext.NonPlayer, new PlanetTile?(map.Tile), false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
                pawn.health.overrideDeathOnDownedChance = 0f;
                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(this.parent.Position, map, 12, null), map);
                EffecterDefOf.MonolithLevelChanged.Spawn().Trigger(new TargetInfo(pawn.Position, map, false), new TargetInfo(pawn.Position, map, false), -1);
                if (pawn.stances != null && pawn.stances.stunner != null)
                {
                    pawn.stances.stunner.StunFor(600,null);
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
    public class GameCondition_AOEAH : GameCondition
    {
        public override void Init()
        {
            base.Init();
        }
        public static void CheckPawn(Pawn pawn)
        {
            if (pawn.RaceProps.IsFlesh && !pawn.IsMutant && !pawn.IsEntity && !pawn.health.hediffSet.HasHediff(HVMPDefOf_A.HVMP_AOEAH_hediff, false))
            {
                pawn.health.AddHediff(HVMPDefOf_A.HVMP_AOEAH_hediff, null, null, null);
            }
        }
        public override void GameConditionTick()
        {
            foreach (Map map in base.AffectedMaps)
            {
                List<Pawn> allPawns = map.mapPawns.AllPawns;
                for (int i = 0; i < allPawns.Count; i++)
                {
                    GameCondition_AOEAH.CheckPawn(allPawns[i]);
                }
            }
        }
    }
    public class Thought_AOEAH : Thought_Situational
    {
        protected override float BaseMoodOffset
        {
            get
            {
                float num = 0f;
                Hediff firstHediffOfDef = this.pawn.health.hediffSet.GetFirstHediffOfDef(this.def.hediff, false);
                if (firstHediffOfDef != null)
                {
                    num = firstHediffOfDef.Severity;
                }
                return -num;
            }
        }
    }
    public class LivingCloudkill : ThingWithComps
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<Vector2>(ref this.realPosition, "realPosition", default(Vector2), false);
            Scribe_Values.Look<float>(ref this.direction, "direction", 0f, false);
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                Vector3 vector = base.Position.ToVector3Shifted();
                this.realPosition = new Vector2(vector.x, vector.z);
                this.direction = Rand.Range(0f, 360f);
            }
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {

        }
        protected override void Tick()
        {
            if (!base.Spawned)
            {
                return;
            }
            if (LivingCloudkill.directionNoise == null)
            {
                LivingCloudkill.directionNoise = new Perlin(0.0020000000949949026, 2.0, 0.5, 4, 1948573612, QualityMode.Medium);
            }
            this.direction += (float)LivingCloudkill.directionNoise.GetValue((double)Find.TickManager.TicksAbs, (double)((float)(this.thingIDNumber % 500) * 1000f), 0.0) * 0.78f;
            this.realPosition = this.realPosition.Moved(this.direction, 0.028333334f);
            IntVec3 intVec = new Vector3(this.realPosition.x, 0f, this.realPosition.y).ToIntVec3();
            if (intVec.InBounds(base.Map))
            {
                base.Position = intVec;
                if (this.IsHashIntervalTick(15))
                {
                    this.DamageCloseThings();
                }
                if (Rand.MTBEventOccurs(15f, 1f, 1f))
                {
                    this.DamageFarThings();
                }
            } else {
                List<Building> obelisksE = base.Map.listerBuildings.AllBuildingsNonColonistOfDef(HVMPDefOf_A.HVMP_WarpedObelisk_Hypercube).ToList();
                if (!obelisksE.NullOrEmpty())
                {
                    base.Position = obelisksE.RandomElement().Position;
                    this.realPosition = base.Position.ToVector2();
                    SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
                } else {
                    this.Destroy();
                }
            }
        }
        private void DamageCloseThings()
        {
            if (ModsConfig.BiotechActive)
            {
                GasUtility.AddGas(base.Position, base.Map, GasType.ToxGas, 0.035f);
            } else {
                GasUtility.AddGas(base.Position, base.Map, GasType.RotStink, 0.035f);
            }
            int num = GenRadial.NumCellsInRadius(4.2f);
            for (int i = 0; i < num; i++)
            {
                IntVec3 intVec = base.Position + GenRadial.RadialPattern[i];
                if (intVec.InBounds(base.Map) && !this.CellImmuneToDamage(intVec))
                {
                    Pawn firstPawn = intVec.GetFirstPawn(base.Map);
                    if (firstPawn != null)
                    {
                        ToxicUtility.DoPawnToxicDamage(firstPawn, 1f);
                    }
                }
            }
        }
        private void DamageFarThings()
        {
            HautsDefOf.Hauts_ToxThornsMist.SpawnMaintained(base.Position, base.Map, 1f);
        }
        private bool CellImmuneToDamage(IntVec3 c)
        {
            if (c.Roofed(base.Map) && c.GetRoof(base.Map).isThickRoof)
            {
                return true;
            }
            Building edifice = c.GetEdifice(base.Map);
            return edifice != null && edifice.def.category == ThingCategory.Building && (edifice.def.building.isNaturalRock || (edifice.def == ThingDefOf.Wall && edifice.Faction == null));
        }
        private Vector2 realPosition;
        private float direction;
        private static ModuleBase directionNoise;
    }
    public class HediffCompProperties_Umbranarch : HediffCompProperties
    {
        public HediffCompProperties_Umbranarch()
        {
            this.compClass = typeof(HediffComp_Umbranarch);
        }
        public IntRange noctolCount;
        public int noctolCooldown;
        public PawnKindDef noctolDef;
        public string darknessLetterText;
    }
    public class HediffComp_Umbranarch : HediffComp
    {
        public HediffCompProperties_Umbranarch Props
        {
            get
            {
                return (HediffCompProperties_Umbranarch)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.spawnCooldown > 0)
            {
                this.spawnCooldown -= delta;
            }
            if (this.Pawn.Spawned && this.Pawn.IsHashIntervalTick(2500))
            {
                if (this.Pawn.Map.GetComponent<HVMP_HypercubeMapComponent>() == null)
                {
                    GameCondition gcud = this.Pawn.Map.gameConditionManager.GetActiveCondition(GameConditionDefOf.UnnaturalDarkness);
                    if (gcud == null)
                    {
                        GameCondition gameCondition = GameConditionMaker.MakeCondition(GameConditionDefOf.UnnaturalDarkness, 99999);
                        gameCondition.Duration = 4000;
                        if (gameCondition.CanApplyOnMap(this.Pawn.Map))
                        {
                            this.Pawn.Map.gameConditionManager.RegisterCondition(gameCondition);
                            Find.LetterStack.ReceiveLetter(gameCondition.LabelCap, this.Props.darknessLetterText, LetterDefOf.ThreatSmall, this.Pawn, null, null, null, null, 0, true);
                        }
                    } else {
                        gcud.TicksLeft = Math.Max(gcud.TicksLeft,4000);
                    }
                }
            }
        }
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            if (this.spawnCooldown <= 0 && this.Pawn.Spawned)
            {
                this.spawnCooldown = this.Props.noctolCooldown;
                int toSpawn = this.Props.noctolCount.RandomInRange;
                Faction f = this.Pawn.Faction ?? Faction.OfEntities;
                Lord lord = this.Pawn.lord??LordMaker.MakeNewLord(f, new LordJob_AssaultColony(f, false, false, false, false, false, false, false), this.Pawn.Map, null);
                while (toSpawn > 0)
                {
                    toSpawn--;
                    Pawn noctol = PawnGenerator.GeneratePawn(this.Props.noctolDef, f, null);
                    GenSpawn.Spawn(noctol, CellFinder.RandomClosewalkCellNear(this.Pawn.Position, this.Pawn.Map, 10), this.Pawn.Map, WipeMode.Vanish);
                    if (!noctol.Downed)
                    {
                        Lord lord2 = noctol.lord;
                        if (lord2 != null)
                        {
                            lord2.RemovePawn(noctol);
                        }
                        lord.AddPawn(noctol);
                    }
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.spawnCooldown, "spawnCooldown", 0, false);
        }
        public int spawnCooldown;
    }
    //romero
    public class QuestNode_Romero_BS : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.romero1, HVMP_Mod.settings.romeroX))
            {
                float num = slate.Get<float>("points", 0f, false);
                GameCondition gameCondition = GameConditionMaker.MakeCondition(GameConditionDefOf.DeathPall, (int)(this.durationFactor.RandomInRange * this.duration.GetValue(slate)));
                QuestPart_GameCondition questPart_GameCondition = new QuestPart_GameCondition();
                questPart_GameCondition.gameCondition = gameCondition;
                List<Rule> list = new List<Rule>();
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                Map map = QuestNode_Romero_BS.GetMap(slate);
                questPart_GameCondition.mapParent = map.Parent;
                questPart_GameCondition.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
                QuestGen.quest.AddPart(questPart_GameCondition);
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_BS_info", this.BS_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_BS_info", " ") });
            }
        }
        private static Map GetMap(Slate slate)
        {
            Map randomPlayerHomeMap;
            if (!slate.TryGet<Map>("map", out randomPlayerHomeMap, false))
            {
                randomPlayerHomeMap = Find.RandomPlayerHomeMap;
            }
            return randomPlayerHomeMap;
        }
        public SlateRef<int> duration;
        [NoTranslate]
        public SlateRef<string> inSignal;
        [MustTranslate]
        public string BS_description;
        public FloatRange durationFactor;
    }
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
            return Find.Storyteller.difficulty.allowViolentQuests && slate.Exists("map", false);
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;
            Map map = QuestGen.slate.Get<Map>("map", null, false);
            float num = QuestGen.slate.Get<float>("points", 0f, false);
            Faction faction = Faction.OfEntities ?? slate.Get<Faction>("enemyFaction", null, false);
            QuestPart_Incident questPart_Incident = new QuestPart_Incident
            {
                debugLabel = "raid",
                incident = HVMPDefOf.HVMP_ShamblerAssault
            };
            IncidentParms incidentParms = this.GenerateIncidentParms(map, num, faction, slate, questPart_Incident);
            PawnGroupMakerParms defaultPawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Shamblers, incidentParms, true);
            defaultPawnGroupMakerParms.points = IncidentWorker_Raid.AdjustedRaidPoints(defaultPawnGroupMakerParms.points, incidentParms.raidArrivalMode, incidentParms.raidStrategy, defaultPawnGroupMakerParms.faction, PawnGroupKindDefOf.Shamblers, map);
            questPart_Incident.SetIncidentParmsAndRemoveTarget(incidentParms);
            questPart_Incident.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
            quest.AddPart(questPart_Incident);
            QuestPart_RomeroMutators qprm = new QuestPart_RomeroMutators();
            bool mayhemMode = HVMP_Mod.settings.romeroX;
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.romero2, mayhemMode))
            {
                qprm.CAD_pawnType = this.CAD_pawnList.RandomElement();
                qprm.CAD_pointFactor = this.CAD_pointFactor;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_CAD_info", this.CAD_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_CAD_info", " ") });
            }
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.romero3, mayhemMode))
            {
                qprm.NOTED_hediff = this.NOTED_bonusHediff;
                qprm.NOTED_lifespanFactor = this.NOTED_lifespanFactor;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_NOTED_info", this.NOTED_description.Formatted())
                });
            } else {
                qprm.NOTED_lifespanFactor = 1f;
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_NOTED_info", " ") });
            }
            quest.AddPart(qprm);
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
            incidentParms.quest = QuestGen.quest;
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
            } else {
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
        public List<PawnKindDef> CAD_pawnList;
        public float CAD_pointFactor;
        [MustTranslate]
        public string CAD_description;
        public float NOTED_lifespanFactor;
        public HediffDef NOTED_bonusHediff;
        [MustTranslate]
        public string NOTED_description;
    }
    public class QuestPart_RomeroMutators : QuestPart
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<PawnKindDef>(ref this.CAD_pawnType, "CAD_pawnType");
            Scribe_Values.Look<float>(ref this.CAD_pointFactor, "CAD_pointFactor", 1f, false);
            Scribe_Defs.Look<HediffDef>(ref this.NOTED_hediff, "NOTED_hediff");
            Scribe_Values.Look<float>(ref this.NOTED_lifespanFactor, "NOTED_lifespanFactor", 1f, false);
        }
        public PawnKindDef CAD_pawnType;
        public float CAD_pointFactor;
        public HediffDef NOTED_hediff;
        public float NOTED_lifespanFactor;
    }
    public class IncidentWorker_RomeroAssault : IncidentWorker_ShamblerAssault
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (base.TryExecuteWorker(parms)) {
                if (parms.quest != null)
                {
                    QuestPart_RomeroMutators qprm = parms.quest.GetFirstPartOfType<QuestPart_RomeroMutators>();
                    if (qprm != null && qprm.CAD_pawnType != null)
                    {
                        Map map = (Map)parms.target;
                        if (map != null)
                        {
                            IntVec3 iv3 = IntVec3.Invalid;
                            Pawn p = null;
                            if (!parms.pawnGroups.NullOrEmpty())
                            {
                                p = parms.pawnGroups.RandomElement().Key;
                                if (p != null)
                                {
                                    iv3 = p.Position;
                                }
                            }
                            if (!iv3.IsValid || !iv3.InBounds(map))
                            {
                                RCellFinder.TryFindRandomPawnEntryCell(out iv3, map, CellFinder.EdgeRoadChance_Hostile, false, null);
                            }
                            if (iv3.IsValid && iv3.InBounds(map))
                            {
                                float combatPower = qprm.CAD_pawnType.combatPower;
                                float points = Math.Max(parms.points * qprm.CAD_pointFactor, combatPower);
                                Lord lord = parms.lord;
                                if (lord == null)
                                {
                                    if (p != null)
                                    {
                                        lord = p.lord;
                                    }
                                }
                                Lord backupLord = LordMaker.MakeNewLord(parms.faction, new LordJob_AssaultColony(parms.faction, false, false, false, false, false, false, false), map, null);
                                while (points >= combatPower)
                                {
                                    Pawn CADdie = PawnGenerator.GeneratePawn(qprm.CAD_pawnType, parms.faction, null);
                                    GenSpawn.Spawn(CADdie, CellFinder.RandomClosewalkCellNear(iv3, map, 10), map, WipeMode.Vanish);
                                    if (!CADdie.Downed)
                                    {
                                        Lord lord2 = CADdie.lord;
                                        if (lord2 != null && lord2 != lord)
                                        {
                                            lord2.RemovePawn(CADdie);
                                        }
                                        if (!lord.CanAddPawn(CADdie))
                                        {
                                            backupLord.AddPawn(CADdie);
                                        } else {
                                            lord.AddPawn(CADdie);
                                        }
                                    }
                                    points -= combatPower;
                                }
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }
        protected override void PostProcessSpawnedPawns(IncidentParms parms, List<Pawn> pawns)
        {
            base.PostProcessSpawnedPawns(parms, pawns);
            QuestPart_RomeroMutators qprm = parms.quest.GetFirstPartOfType<QuestPart_RomeroMutators>();
            if (qprm != null && qprm.NOTED_hediff != null)
            {
                foreach (Pawn p in pawns)
                {
                    p.health.AddHediff(qprm.NOTED_hediff);
                    if (qprm.NOTED_lifespanFactor != 1f)
                    {
                        Hediff shambler = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Shambler);
                        if (shambler != null)
                        {
                            HediffComp_DisappearsAndKills hcdak = shambler.TryGetComp<HediffComp_DisappearsAndKills>();
                            if (hcdak != null)
                            {
                                hcdak.ticksToDisappear = (int)(hcdak.ticksToDisappear*qprm.NOTED_lifespanFactor);
                            }
                        }
                    }
                }
            }
        }
    }
}
