using HarmonyLib;
using HautsF_Ideology;
using HautsFramework;
using HautsPermits;
using RimWorld;
using RimWorld.BaseGen;
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
using static System.Collections.Specialized.BitVector32;
using static UnityEngine.GraphicsBuffer;

namespace HautsPermits_Ideology
{

    [StaticConstructorOnStartup]
    public class HautsPermits_Ideology
    {
        private static readonly Type patchType = typeof(HautsPermits_Ideology);
        static HautsPermits_Ideology()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautspermits.ideology");
            harmony.Patch(AccessTools.Method(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.TryInteractWith)),
                          postfix: new HarmonyMethod(patchType, nameof(HVMP_I_TryInteractWithPostfix)));
            harmony.Patch(AccessTools.Method(typeof(GenStep_ManhunterPack), nameof(GenStep_ManhunterPack.Generate)),
                          prefix: new HarmonyMethod(patchType, nameof(HVMPManhunterPackGeneratePrefix)));
        }
        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
        public static void HVMP_I_TryInteractWithPostfix(Pawn_InteractionsTracker __instance, Pawn recipient, InteractionDef intDef)
        {
            Pawn pawn = GetInstanceField(typeof(Pawn_InteractionsTracker), __instance, "pawn") as Pawn;
            if (pawn.kindDef == HVMPDefOf.HVMP_Anthropologist && recipient.needs.mood != null && recipient.IsColonist && !recipient.IsQuestLodger() && recipient.kindDef != HVMPDefOf.HVMP_Anthropologist)
            {
                float val = Rand.Value;
                if (val <= 0.05f)
                {
                    recipient.needs.mood.thoughts.memories.TryGainMemory(HVMPDefOf.HVMP_AnthroAnnoyance, pawn);
                    recipient.needs.mood.thoughts.memories.TryGainMemory(HVMPDefOf.HVMP_AnthroAnnoyance, pawn);
                } else if (val <= 0.3f) {
                    recipient.needs.mood.thoughts.memories.TryGainMemory(HVMPDefOf.HVMP_AnthroAnnoyance, pawn);
                }
                List<Quest> questsListForReading = Find.QuestManager.QuestsListForReading;
                for (int i = 0; i < questsListForReading.Count; i++)
                {
                    if (questsListForReading[i].State == QuestState.Ongoing)
                    {
                        List<QuestPart> partsListForReading = questsListForReading[i].PartsListForReading;
                        for (int j = 0; j < partsListForReading.Count; j++)
                        {
                            if (partsListForReading[j] is QuestPart_ShuttleAnthro qpsa && qpsa.lodgers.Contains(pawn))
                            {
                                qpsa.questionsLeft--;
                                if (qpsa.questionsLeft <= 0 && qpsa.State == QuestPartState.Enabled)
                                {
                                    qpsa.CompleteOnLastQuestion();
                                }
                            }
                        }
                    }
                }
            }
        }
        public static bool HVMPManhunterPackGeneratePrefix(GenStep_ManhunterPack __instance, Map map, GenStepParams parms)
        {
            if (map.Parent is RimWorld.Planet.Site s)
            {
                foreach (SitePart sp in s.parts)
                {
                    if (sp.def.workerClass == typeof(SitePartWorker_CrashedShuttle))
                    {
                        TraverseParms traverseParams = TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly, false, false, false).WithFenceblocked(true);
                        IntVec3 intVec;
                        if (RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith((IntVec3 x) => x.IsValid, map, out intVec))
                        {
                            float num = ((parms.sitePart != null) ? parms.sitePart.parms.threatPoints : __instance.defaultPointsRange.RandomInRange);
                            PawnKindDef animalKind;
                            bool goodAnimal = true;
                            if (parms.sitePart != null && parms.sitePart.parms.animalKind != null)
                            {
                                animalKind = parms.sitePart.parms.animalKind;
                            }
                            else if (!ManhunterPackGenStepUtility.TryGetAnimalsKind(num, map.Tile, out animalKind))
                            {
                                goodAnimal = false;
                            }
                            if (goodAnimal)
                            {
                                List<Pawn> list = AggressiveAnimalIncidentUtility.GenerateAnimals(animalKind, map.Tile, num, 0);
                                for (int i = 0; i < list.Count; i++)
                                {
                                    IntVec3 intVec2 = CellFinder.RandomSpawnCellForPawnNear(intVec, map, 10);
                                    GenSpawn.Spawn(list[i], intVec2, map, Rot4.Random, WipeMode.Vanish, false, false);
                                    list[i].health.AddHediff(HediffDefOf.Scaria, null, null, null);
                                    list[i].mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent, null, false, false, false, null, false, false, false);
                                }
                            }
                        }
                        return false;
                    }
                }
            }
            return true;
        }
    }
    //permit unique mechanics
    public class RoyalTitlePermitWorker_DropIdeoBook_PTargFriendly : RoyalTitlePermitWorker_DropIdeoBookForCaller
    {
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
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_ScannerSweep : RoyalTitlePermitWorker_Targeted
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.Reveal(target.Cell, this.caller.Map);
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            bool free;
            if (HVMP_Utility.ProprietaryFillAidOption(this,pawn, faction, ref text, out free))
            {
                action = delegate
                {
                    this.BeginReveal(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            yield break;
        }
        private void BeginReveal(Pawn caller, Faction faction, Map map, bool free)
        {
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetPawns = false;
            this.caller = caller;
            this.map = map;
            this.faction = faction;
            this.free = free;
            this.targetingParameters.validator = (TargetInfo target) => (this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(caller.Position) <= this.def.royalAid.targetingRange);
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void Reveal(IntVec3 cell, Map map)
        {
            FloodFillerFog.FloodUnfog(cell, map);
            FogGrid fg = map.fogGrid;
            foreach (IntVec3 iv3 in GenRadial.RadialCellsAround(cell, 1.9f, true))
            {
                fg.Unfog(iv3);
            }
            Messages.Message("HVMP_ScannerSweep".Translate(this.faction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
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
    public class RoyalTitlePermitWorker_ScannerDeep : RoyalTitlePermitWorker_Targeted
    {
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            bool free;
            if (HVMP_Utility.ProprietaryFillAidOption(this,pawn, faction, ref text, out free))
            {
                action = delegate
                {
                    this.MakeCondition(pawn, faction, new IncidentParms(), this.free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            yield break;
        }
        protected void MakeCondition(Pawn caller, Faction faction, IncidentParms parms, bool free)
        {
            if (caller.Spawned)
            {
                Map map = caller.Map;
                IntVec3 intVec;
                if (!CellFinderLoose.TryFindRandomNotEdgeCellWith(10, (IntVec3 x) => this.CanScatterAt(x, map), map, out intVec))
                {
                    Log.Error("Could not find a center cell for deep scanning lump generation!");
                }
                ThingDef thingDef = DefDatabase<ThingDef>.AllDefs.RandomElementByWeight((ThingDef def) => def.deepCommonality);
                int num = Mathf.CeilToInt((float)thingDef.deepLumpSizeRange.RandomInRange);
                foreach (IntVec3 intVec2 in GridShapeMaker.IrregularLump(intVec, map, num, null))
                {
                    if (this.CanScatterAt(intVec2, map) && !intVec2.InNoBuildEdgeArea(map))
                    {
                        map.deepResourceGrid.SetAt(intVec2, thingDef, thingDef.deepCountPerCell);
                    }
                }
                Find.LetterStack.ReceiveLetter("LetterLabelDeepScannerFoundLump".Translate() + ": " + thingDef.LabelCap, "HVMP_ScannerDeep".Translate(thingDef.label, faction.Named("FACTION")), LetterDefOf.PositiveEvent, new LookTargets(intVec, map), null, null, null, null, 0, true);
                caller.royalty.GetPermit(this.def, faction).Notify_Used();
                if (!this.free)
                {
                    caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
                }
                HVMP_Utility.DoPTargeterCooldown(faction,caller,this);
            }
        }
        private bool CanScatterAt(IntVec3 pos, Map map)
        {
            int num = CellIndicesUtility.CellToIndex(pos, map.Size.x);
            TerrainDef terrainDef = map.terrainGrid.TerrainAt(num);
            return (terrainDef == null || !terrainDef.IsWater || terrainDef.passability != Traversability.Impassable) && terrainDef.affordances.Contains(ThingDefOf.DeepDrill.terrainAffordanceNeeded) && !map.deepResourceGrid.GetCellBool(num);
        }
    }
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_DiscoverRelic : RoyalTitlePermitWorker
    {
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            bool free;
            if (HVMP_Utility.ProprietaryFillAidOption(this,pawn, faction, ref text, out free))
            {
                action = delegate
                {
                    this.MakeRelic(pawn, faction, this.free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            string text;
            bool flag;
            if (!base.FillCaravanAidOption(pawn, faction, out text, out this.free, out flag))
            {
                yield break;
            }
            Command_Action command_Action = new Command_Action
            {
                defaultLabel = this.def.LabelCap + " (" + pawn.LabelShort + ")",
                defaultDesc = text,
                icon = RoyalTitlePermitWorker_DiscoverRelic.CommandTex,
                action = delegate
                {
                    this.MakeRelic(pawn, faction, this.free);
                }
            };
            if (faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null)
            {
                command_Action.Disable("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")));
            }
            if (flag)
            {
                command_Action.Disable("CommandCallRoyalAidNotEnoughFavor".Translate());
            }
            yield return command_Action;
            yield break;
        }
        protected virtual void MakeRelic(Pawn caller, Faction faction, bool free)
        {
            if (caller.Ideo != null)
            {
                Precept_Relic precept = (Precept_Relic)PreceptMaker.MakePrecept(PreceptDefOf.IdeoRelic);
                caller.Ideo.AddPrecept(precept, true, caller.Faction.def);
                Messages.Message("HVMP_RelicDiscovered".Translate(faction.Name, caller.Ideo.name), null, MessageTypeDefOf.NeutralEvent, true);
                caller.royalty.GetPermit(this.def, faction).Notify_Used();
                if (!free)
                {
                    caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
                }
                HVMP_Utility.DoPTargeterCooldown(faction,caller,this);
            }
        }
        protected bool free;
        private static readonly Texture2D CommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CallAid", true);
    }
    public class GameCondition_RevealingScan : GameCondition
    {
        private int TransitionDurationTicks
        {
            get
            {
                return 200;
            }
        }
        public override void Init()
        {
            base.Init();
            this.RevealAll();
            this.ticks = 60;
        }
        public override void GameConditionTick()
        {
            this.ticks--;
            if (this.ticks <= 0)
            {
                this.RevealAll();
                this.ticks = 60;
            }
        }
        public void RevealAll()
        {
            foreach (Map m in base.AffectedMaps)
            {
                foreach (Pawn p in m.mapPawns.AllPawnsSpawned)
                {
                    foreach (Hediff h in p.health.hediffSet.hediffs)
                    {
                        HediffComp_Invisibility hci = h.TryGetComp<HediffComp_Invisibility>();
                        if (hci != null)
                        {
                            hci.DisruptInvisibility();
                            HediffComp_Disappears hcd = h.TryGetComp<HediffComp_Disappears>();
                            if (hcd != null)
                            {
                                hcd.ticksToDisappear = 0;
                            }
                        }
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.ticks, "ticks", 0, false);
        }
        public int ticks;
    }
    //permit books
    public class BookOutcomeProperties_InspiringVerse : BookOutcomeProperties_PromoteIdeo
    {
        public override Type DoerClass
        {
            get
            {
                return typeof(BookOutcomeDoerInspiringVerse);
            }
        }
        public SimpleCurve inspireChancePerHour = new SimpleCurve
            {
                {new CurvePoint(0f, 0.0001f),true},
                {new CurvePoint(1f, 0.0001f),true},
                {new CurvePoint(2f, 0.0001f),true},
                {new CurvePoint(3f, 0.0001f),true},
                {new CurvePoint(4f, 0.0001f),true},
                {new CurvePoint(5f, 0.0001f),true},
                {new CurvePoint(6f, 0.0001f),true},
            };
    }
    public class BookOutcomeDoerInspiringVerse : BookOutcomeDoerPromoteIdeo
    {
        public new BookOutcomeProperties_InspiringVerse Props
        {
            get
            {
                return (BookOutcomeProperties_InspiringVerse)this.props;
            }
        }
        public float ChanceToInspirePerHour
        {
            get
            {
                return this.Props.inspireChancePerHour.Evaluate((float)this.Quality);
            }
        }
        public override void ExtraReasurreEffect(Pawn reader, float oldCertainty)
        {
            if (reader.mindState.inspirationHandler != null && !reader.Inspired && Rand.Chance(this.ChanceToInspirePerHour/10f))
            {
                InspirationDef inspiration = reader.mindState.inspirationHandler.GetRandomAvailableInspirationDef();
                if (inspiration != null)
                {
                    reader.mindState.inspirationHandler.TryStartInspiration(inspiration, "HVMP_InspiringVerseHitsRight".Translate(this.Book.Title,reader.Name.ToStringShort), true);
                }
            }
        }
        public override void ExtraEffectsStrings(ref StringBuilder stringBuilder)
        {
            if (this.ChanceToInspirePerHour > 0f)
            {
                string text3 = "HVMP_IdeoBookInspireChance".Translate((this.ChanceToInspirePerHour*100f).ToStringDecimalIfSmall());
                stringBuilder.AppendLine(" - " + text3);
            }
        }
    }
    public class BookOutcomeProperties_HediffGrantingIdeoBook : BookOutcomeProperties_PromoteIdeo
    {
        public override Type DoerClass
        {
            get
            {
                return typeof(BookOutcomeDoerHediffGrantingForIdeo);
            }
        }
        public HediffDef hediff;
        public int durationPerHour;
        public float severityPerHour;
        public int maxDuration = 60000;
        public SimpleCurve hediffGainPerHour = new SimpleCurve
            {
                {new CurvePoint(0f, 1f),true},
                {new CurvePoint(1f, 1f),true},
                {new CurvePoint(2f, 1f),true},
                {new CurvePoint(3f, 1f),true},
                {new CurvePoint(4f, 1f),true},
                {new CurvePoint(5f, 1f),true},
                {new CurvePoint(6f, 1f),true},
            };
    }
    public class BookOutcomeDoerHediffGrantingForIdeo : BookOutcomeDoerPromoteIdeo
    {
        public new BookOutcomeProperties_HediffGrantingIdeoBook Props
        {
            get
            {
                return (BookOutcomeProperties_HediffGrantingIdeoBook)this.props;
            }
        }
        public float HediffGainPerHour
        {
            get
            {
                return this.Props.hediffGainPerHour.Evaluate((float)this.Quality);
            }
        }
        public float DurationPerHour
        {
            get
            {
                return this.Props.durationPerHour * HediffGainPerHour/10;
            }
        }
        public float SeverityPerHour
        {
            get
            {
                return this.Props.severityPerHour * HediffGainPerHour/10;
            }
        }
        public override void ExtraReasurreEffect(Pawn reader, float oldCertainty)
        {
            Hediff hediff = reader.health.hediffSet.GetFirstHediffOfDef(this.Props.hediff);
            if (hediff != null)
            {
                hediff.Severity += this.SeverityPerHour;
            } else {
                hediff = HediffMaker.MakeHediff(this.Props.hediff,reader,null);
                reader.health.AddHediff(hediff);
            }
            HediffComp_Disappears hcd = hediff.TryGetComp<HediffComp_Disappears>();
            if (hcd != null)
            {
                hcd.ticksToDisappear = Math.Min((int)this.DurationPerHour+hcd.ticksToDisappear,this.Props.maxDuration);
            }
        }
        public override void ExtraEffectsStrings(ref StringBuilder stringBuilder)
        {
            if (this.Props.hediff != null)
            {
                string text3 = "HVMP_IdeoBookHediffGain".Translate((this.SeverityPerHour*10).ToStringByStyle(ToStringStyle.FloatTwo), this.Props.hediff.label, (this.DurationPerHour/250).ToStringByStyle(ToStringStyle.FloatTwo));
                stringBuilder.AppendLine(" - " + text3);
            }
        }
    }
    //quest nodes
    public class QuestNode_ArchiveIntermediary : QuestNode_Sequence
    {
        protected override void RunInt()
        {
            if (HVMP_Utility.TryFindArchiveFaction(out Faction faction))
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
                qpbgfh.goodwill = HVMP_Utility.ExpectationBasedGoodwillLoss(map, true,true, faction);
                QuestGen.quest.AddPart(qpbgfh);
                HVMP_Utility.SetSettingScalingRewardValue(slate);
            }
            base.RunInt();
        }
        protected override bool TestRunInt(Slate slate)
        {
            HVMP_Utility.SetSettingScalingRewardValue(slate);
            return HVMP_Utility.TryFindArchiveFaction(out Faction archiveFaction) && base.TestRunInt(slate);
        }
    }
    public class QuestNode_ShuttleAnthro : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return this.node == null || this.node.TestRun(slate);
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            QuestPart_ShuttleAnthro qpsa = new QuestPart_ShuttleAnthro();
            qpsa.inSignalEnable = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignalEnable.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
            qpsa.questionsLeft = this.questionsToAsk.GetValue(slate);
            qpsa.questionsInit = this.questionsToAsk.GetValue(slate);
            if (this.lodgers.GetValue(slate) != null)
            {
                qpsa.lodgers.AddRange(this.lodgers.GetValue(slate));
            }
            qpsa.expiryInfoPart = "HVMP_ShuttleAnthroArrivesIn".Translate();
            qpsa.expiryInfoPartTip = "HVMP_ShuttleAnthroArrivesOn".Translate();
            if (this.node != null)
            {
                QuestGenUtility.RunInnerNode(this.node, qpsa);
            }
            if (!this.outSignalComplete.GetValue(slate).NullOrEmpty())
            {
                qpsa.outSignalsCompleted.Add(QuestGenUtility.HardcodedSignalWithQuestID(this.outSignalComplete.GetValue(slate)));
            }
            QuestGen.quest.AddPart(qpsa);
        }
        [NoTranslate]
        public SlateRef<string> inSignalEnable;
        [NoTranslate]
        public SlateRef<string> outSignalComplete;
        public SlateRef<int> questionsToAsk;
        public SlateRef<IEnumerable<Pawn>> lodgers;
        public QuestNode node;
    }
    public class QuestPart_ShuttleAnthro : QuestPartActivable
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
        public override string ExpiryInfoPart
        {
            get
            {
                if (this.quest.Historical)
                {
                    return null;
                }
                return this.expiryInfoPart.Formatted(this.questionsLeft);
            }
        }
        public override string ExpiryInfoPartTip
        {
            get
            {
                return this.expiryInfoPartTip.Formatted(this.questionsInit);
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
                return "HVMP_ShuttleArriveAnthro".Translate(this.questionsLeft);
            }
        }
        public override string AlertExplanation
        {
            get
            {
                if (this.quest.hidden)
                {
                    return "HVMP_ShuttleArriveAnthroDescHidden".Translate(this.questionsLeft.ToString().Colorize(this.ColorString));
                }
                return "HVMP_ShuttleArriveAnthroDesc".Translate(this.quest.name, this.questionsLeft.ToString().Colorize(this.ColorString), this.lodgers.Select((Pawn p) => p.LabelShort).ToLineList("- ", false));
            }
        }
        public void CompleteOnLastQuestion()
        {
            base.Complete();
        }
        public Color ColorString
        {
            get {
                return GenColor.FromHex("87f6f6");
            }
        }
        public override string ExtraInspectString(ISelectable target)
        {
            Pawn pawn = target as Pawn;
            if (pawn != null && this.lodgers.Contains(pawn))
            {
                return "HVMP_ShuttleAnthroInspectString".Translate(this.questionsLeft);
            }
            return null;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.questionsLeft, "questionsLeft", 0, false);
            Scribe_Values.Look<int>(ref this.questionsInit, "questionsInit", 0, false);
            Scribe_Values.Look<string>(ref this.expiryInfoPart, "expiryInfoPart", null, false);
            Scribe_Values.Look<string>(ref this.expiryInfoPartTip, "expiryInfoPartTip", null, false);
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
        public int questionsLeft;
        public int questionsInit;
        public List<Pawn> lodgers = new List<Pawn>();
        public bool alert;
        public string expiryInfoPart;
        public string expiryInfoPartTip;
    }
    public class QuestNode_GenerateAncientComplex : QuestNode_Root_AncientComplex
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;
            Map map = QuestGen_Get.GetMap(false, null);
            float points = slate.Get<float>("points", 0f, false);
            string text = QuestGenUtility.HardcodedSignalWithQuestID("terminals.Destroyed");
            string text2 = QuestGen.GenerateNewSignal("TerminalHacked", true);
            string text3 = QuestGen.GenerateNewSignal("AllTerminalsHacked", true);
            LayoutStructureSketch layoutStructureSketch = this.QuestSetupComplex(quest, points);
            float num3 = (Find.Storyteller.difficulty.allowViolentQuests ? this.threatPointsOverPointsCurve.Evaluate(points) : 0f);
            SitePartParams sitePartParams = new SitePartParams
            {
                ancientLayoutStructureSketch = layoutStructureSketch,
                ancientComplexRewardMaker = ThingSetMakerDefOf.MapGen_AncientComplexRoomLoot_Default,
                threatPoints = num3
            };
            Site site = QuestGen_Sites.GenerateSite(Gen.YieldSingle<SitePartDefWithParams>(new SitePartDefWithParams(SitePartDefOf.AncientComplex, sitePartParams)), this.tile.GetValue(slate), Faction.OfAncients, false, null);
            quest.SpawnWorldObject(site, null, null);
            TimedDetectionRaids component = site.GetComponent<TimedDetectionRaids>();
            if (component != null)
            {
                component.alertRaidsArrivingIn = true;
            }
            if (this.storeAs.GetValue(slate) != null)
            {
                QuestGen.slate.Set<WorldObject>(this.storeAs.GetValue(slate), site, false);
            }
            quest.Message("[terminalHackedMessage]", null, true, null, null, text2);
            quest.Message("[allTerminalsHackedMessage]", MessageTypeDefOf.PositiveEvent, false, null, null, text3);
            QuestGen.slate.Set<string>(this.successSignal.GetValue(slate), text3, false);
            this.TryFindEnemyFaction(out Faction faction);
            if (Find.Storyteller.difficulty.allowViolentQuests && Rand.Chance(0.5f) && faction != null)
            {
                quest.RandomRaid(site, this.randomRaidPointsFactorRange * num3, faction, text3, PawnsArrivalModeDefOf.EdgeWalkIn, RaidStrategyDefOf.ImmediateAttack, null, null);
            }
            QuestPart_Filter_Hacked questPart_Filter_Hacked = new QuestPart_Filter_Hacked();
            questPart_Filter_Hacked.inSignal = text;
            questPart_Filter_Hacked.outSignalElse = QuestGen.GenerateNewSignal("FailQuestTerminalDestroyed", true);
            quest.AddPart(questPart_Filter_Hacked);
            quest.End(QuestEndOutcome.Fail, 0, null, questPart_Filter_Hacked.outSignalElse, QuestPart.SignalListenMode.OngoingOnly, true, false);
            slate.Set<List<Thing>>("terminals", layoutStructureSketch.thingsToSpawn, false);
            slate.Set<int>("terminalCount", layoutStructureSketch.thingsToSpawn.Count, false);
            slate.Set<Map>("map", map, false);
            slate.Set<Site>("site", site, false);
            quest.AddPart(new QuestPart_LookOverHere(site));
        }
        public override LayoutStructureSketch QuestSetupComplex(Quest quest, float points)
        {
            LayoutStructureSketch layoutStructureSketch = this.GenerateStructureSketch(points, true);
            layoutStructureSketch.thingDiscoveredMessage = "HVMP_AncientTerminalDiscovered".Translate();
            string text = QuestGen.GenerateNewSignal("AllTerminalsHacked", false);
            string text2 = QuestGen.GenerateNewSignal("TerminalHacked", false);
            List<string> list = new List<string>();
            for (int i = 0; i < layoutStructureSketch.thingsToSpawn.Count; i++)
            {
                Thing thing = layoutStructureSketch.thingsToSpawn[i];
                string text3 = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("terminal" + i);
                QuestUtility.AddQuestTag(thing, text3);
                string text4 = QuestGenUtility.HardcodedSignalWithQuestID(text3 + ".Hacked");
                list.Add(text4);
                thing.TryGetComp<CompHackable>().defence = (float)(Rand.Chance(0.5f) ? this.hackDefenceRange.min : this.hackDefenceRange.max);
            }
            QuestPart_PassAllActivable questPart_PassAllActivable;
            if (!quest.TryGetFirstPartOfType<QuestPart_PassAllActivable>(out questPart_PassAllActivable))
            {
                questPart_PassAllActivable = quest.AddPart<QuestPart_PassAllActivable>();
                questPart_PassAllActivable.inSignalEnable = QuestGen.slate.Get<string>("inSignal", null, false);
                questPart_PassAllActivable.expiryInfoPartKey = "TerminalsHacked";
            }
            questPart_PassAllActivable.inSignals = list;
            questPart_PassAllActivable.outSignalsCompleted.Add(text);
            questPart_PassAllActivable.outSignalAny = text2;
            return layoutStructureSketch;
        }
        protected override LayoutStructureSketch GenerateStructureSketch(float points, bool generateTerminals = true)
        {
            int num = (int)this.complexSizeOverPointsCurve.Evaluate(points);
            StructureGenParams structureGenParams = new StructureGenParams
            {
                size = new IntVec2(num, num)
            };
            LayoutStructureSketch layoutStructureSketch = this.layoutDef.Worker.GenerateStructureSketch(structureGenParams);
            if (generateTerminals)
            {
                int num2 = Mathf.FloorToInt(this.terminalsOverRoomCountCurve.Evaluate((float)layoutStructureSketch.structureLayout.Rooms.Count));
                for (int i = 0; i < num2; i++)
                {
                    layoutStructureSketch.thingsToSpawn.Add(ThingMaker.MakeThing(ThingDefOf.AncientTerminal, null));
                }
            }
            return layoutStructureSketch;
        }
        private bool TryFindEnemyFaction(out Faction enemyFaction)
        {
            enemyFaction = Find.FactionManager.RandomRaidableEnemyFaction(false, false, true, TechLevel.Undefined);
            return enemyFaction != null;
        }
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        public SlateRef<PlanetTile> tile;
        [NoTranslate]
        public SlateRef<string> storeAs;
        [NoTranslate]
        public SlateRef<string> successSignal;
        public FloatRange randomRaidPointsFactorRange;
        public IntRange hackDefenceRange;
        public SimpleCurve threatPointsOverPointsCurve;
        public SimpleCurve terminalsOverRoomCountCurve;
        public SimpleCurve complexSizeOverPointsCurve;
        public LayoutDef layoutDef;
    }
    public class QuestNode_SpawnDronePlusGuards : QuestNode
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;
            QuestPart_DronePlusGuards qpdpg = new QuestPart_DronePlusGuards();
            qpdpg.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
            qpdpg.tag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(this.tag.GetValue(slate));
            qpdpg.mapParent = slate.Get<Map>("map", null, false).Parent;
            float num = Mathf.Max(slate.Get<float>("points", 0f, false) * 0.9f, 300f);
            string spacedroneDestroyedSignal = QuestGenUtility.HardcodedSignalWithQuestID("spacedrone.Destroyed");
            string spacedroneHackedSignal = QuestGenUtility.HardcodedSignalWithQuestID("spacedrone.Hacked");
            List<Pawn> list2 = PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
            {
                groupKind = PawnGroupKindDefOf.Combat,
                tile = slate.Get<Map>("map", null, false).Tile,
                faction = Faction.OfMechanoids,
                points = num
            }, true).ToList<Pawn>();
            qpdpg.pawnsToSpawn = new List<PawnKindDef>();
            foreach (Pawn p in list2)
            {
                qpdpg.pawnsToSpawn.Add(p.kindDef);
            }
            Thing thing = ThingMaker.MakeThing(this.droneDef, null);
            slate.Set<Thing>("spacedrone", thing, false);
            QuestPart_Filter_AllThingsHacked questPart_Filter_AllThingsHacked = new QuestPart_Filter_AllThingsHacked();
            questPart_Filter_AllThingsHacked.things.Add(thing);
            questPart_Filter_AllThingsHacked.inSignal = spacedroneDestroyedSignal;
            questPart_Filter_AllThingsHacked.outSignal = QuestGen.GenerateNewSignal("QuestEndSuccess", true);
            questPart_Filter_AllThingsHacked.outSignalElse = QuestGen.GenerateNewSignal("QuestEndFailure", true);
			quest.AddPart(questPart_Filter_AllThingsHacked);
            qpdpg.drone = thing;
            qpdpg.sleepyTime = this.sleepyTime.RandomInRange;
            slate.Set<int>("timeToWake",qpdpg.sleepyTime,false);
            List<PawnKindDef> pkdList = new List<PawnKindDef>();
            foreach (Pawn p in list2)
            {
                pkdList.Add(p.kindDef);
            }
            qpdpg.dropSpot = this.dropSpot.GetValue(slate) ?? IntVec3.Invalid;
            quest.AddPart(qpdpg);
            string text = PawnUtility.PawnKindsToLineList(pkdList, "  - ", ColoredText.ThreatColor);
            if (text != "")
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("allThreats", text)
                });
            }
            QuestGen.quest.AddPart(new QuestPart_LookAtThis(thing));
        }
        protected override bool TestRunInt(Slate slate)
        {
            return Find.Storyteller.difficulty.allowViolentQuests && Faction.OfMechanoids != null && slate.Get<Map>("map", null, false) != null;
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        [NoTranslate]
        public SlateRef<string> tag;
        public SlateRef<float?> points;
        public SlateRef<IntVec3?> dropSpot;
        public ThingDef droneDef;
        public IntRange sleepyTime;
    }
    public class QuestPart_DronePlusGuards : QuestPart
    {
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo globalTargetInfo in base.QuestLookTargets)
                {
                    yield return globalTargetInfo;
                }
                if (this.spawnedClusterPos.IsValid && this.mapParent != null && this.mapParent.HasMap)
                {
                    yield return new GlobalTargetInfo(this.spawnedClusterPos, this.mapParent.Map, false);
                }
                yield break;
            }
        }
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal && this.mapParent != null && this.mapParent.HasMap)
            {
                List<TargetInfo> list = new List<TargetInfo>();
                this.spawnedClusterPos = this.dropSpot;
                if (this.spawnedClusterPos == IntVec3.Invalid)
                {
                    this.TryFindSpacedronePosition(this.mapParent.Map,out this.spawnedClusterPos);
                }
                if (this.spawnedClusterPos == IntVec3.Invalid)
                {
                    return;
                }
                List<Pawn> spawnedPawns = new List<Pawn>();
                foreach (PawnKindDef pkd in this.pawnsToSpawn)
                {
                    spawnedPawns.Add(PawnGenerator.GeneratePawn(new PawnGenerationRequest(pkd, Faction.OfMechanoids, PawnGenerationContext.NonPlayer, this.mapParent.Tile, false, false, false, true, true, 1f, false, true, true, false, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false)));
                }
                List<Thing> toWakeUpOn = new List<Thing> {
                    this.drone
                };
                toWakeUpOn.AddRange(spawnedPawns);
                LordMaker.MakeNewLord(Faction.OfMechanoids, new LordJob_SleepThenMechanoidsDefendDrone(toWakeUpOn, Faction.OfMechanoids, 28f, this.spawnedClusterPos, false, false, this.sleepyTime*2500), this.mapParent.Map, spawnedPawns);
                DropPodUtility.DropThingsNear(this.spawnedClusterPos, this.mapParent.Map, spawnedPawns.Cast<Thing>(), 110, false, false, true, true, true, null);
                list.AddRange(spawnedPawns.Select((Pawn p) => new TargetInfo(p)));
                GenSpawn.Spawn(SkyfallerMaker.MakeSkyfaller(ThingDefOf.CrashedShipPartIncoming, this.drone), this.spawnedClusterPos, this.mapParent.Map, WipeMode.Vanish);
                list.Add(new TargetInfo(this.spawnedClusterPos, this.mapParent.Map, false));
                this.spawned = true;
                Find.LetterStack.ReceiveLetter("HVMP_DroneArrivedLabel".Translate(), "HVMP_DroneArrivedText".Translate(), LetterDefOf.ThreatBig, new TargetInfo(this.spawnedClusterPos, this.mapParent.Map, false), null, this.quest, null, null, 0, true);
            }
        }
        public bool TryFindSpacedronePosition(Map map, out IntVec3 spot)
        {
            IntVec2 size = this.drone.def.size;
            CellRect cellRect = GenAdj.OccupiedRect(IntVec3.Zero, this.drone.def.defaultPlacingRot, this.drone.def.size);
            IntVec3 intVec = cellRect.CenterCell + this.drone.def.interactionCellOffset;
            cellRect = cellRect.ExpandToFit(intVec);
            return DropCellFinder.FindSafeLandingSpot(out spot, null, map, 35, 15, 25, new IntVec2?(new IntVec2(cellRect.Width, cellRect.Height)), new IntVec3?(this.drone.def.interactionCellOffset));
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<PawnKindDef>(ref this.pawnsToSpawn, "pawnsToSpawn", LookMode.Def, Array.Empty<object>());
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_Values.Look<string>(ref this.hackingCompletedSignal, "hackingCompletedSignal", null, false);
            Scribe_Values.Look<string>(ref this.tag, "tag", null, false);
            Scribe_References.Look<MapParent>(ref this.mapParent, "mapParent", false);
            Scribe_Values.Look<IntVec3>(ref this.dropSpot, "dropSpot", default(IntVec3), false);
            Scribe_Values.Look<bool>(ref this.spawned, "spawned", false, false);
            Scribe_Values.Look<IntVec3>(ref this.spawnedClusterPos, "spawnedClusterPos", default(IntVec3), false);
            if (!this.spawned && (this.drone == null || !(this.drone is Pawn)))
            {
                Scribe_Deep.Look<Thing>(ref this.drone, "drone", Array.Empty<object>());
            } else {
                Scribe_References.Look<Thing>(ref this.drone, "drone", false);
            }
            Scribe_Values.Look<int>(ref this.sleepyTime, "sleepyTime", 2500, false);
        }
        public string hackingCompletedSignal;
        public Thing drone;
        public bool spawned;
        public List<PawnKindDef> pawnsToSpawn = new List<PawnKindDef>();
        public string inSignal;
        public string tag;
        public MapParent mapParent;
        public IntVec3 dropSpot = IntVec3.Invalid;
        private IntVec3 spawnedClusterPos = IntVec3.Invalid;
        public int sleepyTime;
    }
    //quest things
    public class QuestNode_GenerateStrangeArtifact : QuestNode
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
                    slate.Set<string>(this.storeSecondStatAs.GetValue(slate), secondStat.defName, false);
                    slate.Set<string>(this.storeSecondStatLabelAs.GetValue(slate), secondStat.label, false);
                }
                QuestGen.quest.AddPart(new QuestPart_LookAtThis(thing));
            }
            slate.Set<Thing>(this.storeAs.GetValue(slate), thing, false);
        }
        [NoTranslate]
        public SlateRef<string> storeAs;
        public ThingDef thingDef;
        [NoTranslate]
        public SlateRef<string> storeSecondStatAs;
        [NoTranslate]
        public SlateRef<string> storeSecondStatLabelAs;
    }
    public class CompProperties_HackableQuestLink : CompProperties_Hackable
    {
        public CompProperties_HackableQuestLink()
        {
            this.compClass = typeof(CompHackableQuestLink);
        }
    }
    public class CompHackableQuestLink : CompHackable
    {
        public new CompProperties_HackableQuestLink Props
        {
            get
            {
                return (CompProperties_HackableQuestLink)this.props;
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            foreach (Gizmo gizmo3 in QuestUtility.GetQuestRelatedGizmos(this.parent))
            {
                yield return gizmo3;
            }
            yield break;
        }
    }
    public class LordJob_SleepThenMechanoidsDefendDrone : LordJob_MechanoidDefendBase
    {
        public override bool GuiltyOnDowned
        {
            get
            {
                return true;
            }
        }
        public LordJob_SleepThenMechanoidsDefendDrone()
        {
        }
        public LordJob_SleepThenMechanoidsDefendDrone(List<Thing> things, Faction faction, float defendRadius, IntVec3 defSpot, bool canAssaultColony, bool isMechCluster, int sleepyTime)
        {
            if (things != null)
            {
                this.things.AddRange(things);
            }
            this.faction = faction;
            this.defendRadius = defendRadius;
            this.defSpot = defSpot;
            this.canAssaultColony = canAssaultColony;
            this.isMechCluster = isMechCluster;
            this.sleepyTime = sleepyTime;
        }
        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            LordToil_Sleep lordToil_Sleep = new LordToil_Sleep();
            stateGraph.StartingToil = lordToil_Sleep;
            LordToil startingToil = stateGraph.AttachSubgraph(new LordJob_MechanoidsDefend(this.things, this.faction, this.defendRadius, this.defSpot, this.canAssaultColony, this.isMechCluster).CreateGraph()).StartingToil;
            Transition transition = new Transition(lordToil_Sleep, startingToil, false, true);
            transition.AddTrigger(new Trigger_DormancyWakeup());
            transition.AddTrigger(new Trigger_OnHumanlikeHarmAnyThing(this.things));
            transition.AddTrigger(new Trigger_OnPlayerMechHarmAnything(this.things));
            transition.AddTrigger(new Trigger_TicksPassed(this.sleepyTime));
            transition.AddPreAction(new TransitionAction_Message("MessageSleepingPawnsWokenUp".Translate(this.faction.def.pawnsPlural).CapitalizeFirst(), MessageTypeDefOf.ThreatBig, null, 1f, null));
            transition.AddPostAction(new TransitionAction_WakeAll());
            transition.AddPostAction(new TransitionAction_Custom(action: delegate
            {
                Find.SignalManager.SendSignal(new Signal("CompCanBeDormant.WakeUp", this.things.First<Thing>().Named("SUBJECT"), Faction.OfMechanoids.Named("FACTION")));
                SoundDefOf.MechanoidsWakeUp.PlayOneShot(new TargetInfo(this.defSpot, base.Map, false));
            }));
            stateGraph.AddTransition(transition, false);
            return stateGraph;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.sleepyTime, "sleepyTime", 2500, false);
        }
        public int sleepyTime;
    }
    //the quest formerly known as icarus
    public class QuestNode_Root_Blackbox : QuestNode_Sequence
    {
        protected override void RunInt()
        {
            if (HVMP_Utility.TryFindArchiveFaction(out Faction faction))
            {
                Slate slate = QuestGen.slate;
                Quest quest = QuestGen.quest;
                slate.Set<Faction>("faction", faction, false);
                slate.Set<string>("factionName", faction.Name, false);
                slate.Set<Pawn>("asker", faction.leader, false);
                Map map = HVMP_Utility.TryGetMap();
                slate.Set<Map>("map", map, false);
                PlanetTile tile = HVMP_Utility.TryGetPlanetTile();
                slate.Set<PlanetTile>("pTile", tile, false);
                QuestPart_BranchGoodwillFailureHandler qpbgfh = new QuestPart_BranchGoodwillFailureHandler();
                qpbgfh.faction = faction;
                qpbgfh.goodwill = HVMP_Utility.ExpectationBasedGoodwillLoss(map, true, true, faction);
                QuestGen.quest.AddPart(qpbgfh);
                HVMP_Utility.SetSettingScalingRewardValue(slate, 0.7f);
            }
            base.RunInt();
        }
        protected override bool TestRunInt(Slate slate)
        {
            HVMP_Utility.SetSettingScalingRewardValue(slate);
            return HVMP_Utility.TryFindArchiveFaction(out Faction archiveFaction) && base.TestRunInt(slate);
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
    }
    public class SitePartWorker_CrashedShuttle : SitePartWorker
    {
        public override void Notify_GeneratedByQuestGen(SitePart part, Slate slate, List<Rule> outExtraDescriptionRules, Dictionary<string, string> outExtraDescriptionConstants)
        {
            base.Notify_GeneratedByQuestGen(part, slate, outExtraDescriptionRules, outExtraDescriptionConstants);
            Thing shuttle = ThingMaker.MakeThing(HVMPDefOf.HVMP_ShuttleCrashed, null);
            List<Thing> list = new List<Thing> { shuttle };
            part.things = new ThingOwner<Thing>(part, false, LookMode.Deep);
            part.things.TryAddRangeOrTransfer(list, false, false);
            slate.Set<List<Thing>>("generatedItemStashThings", list, false);
            slate.Set<Thing>("shuttlething", shuttle, false);
            outExtraDescriptionRules.Add(new Rule_String("itemStashContents", GenLabel.ThingsLabel(list, "  - ")));
            outExtraDescriptionRules.Add(new Rule_String("itemStashContentsValue", GenThing.GetMarketValue(list).ToStringMoney(null)));
        }
    }
    public class GenStep_CrashedShuttle : GenStep_Scatterer
    {
        public override int SeedPart
        {
            get
            {
                return 84572345;
            }
        }
        protected override bool CanScatterAt(IntVec3 c, Map map)
        {
            if (!base.CanScatterAt(c, map))
            {
                return false;
            }
            if (!map.reachability.CanReachMapEdge(c, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false, false, false)))
            {
                return false;
            }
            CellRect rect = CellRect.CenteredOn(c, 7, 7);
            List<CellRect> list;
            if (MapGenerator.TryGetVar<List<CellRect>>("UsedRects", out list) && list.Any((CellRect x) => x.Overlaps(rect)))
            {
                return false;
            }
            foreach (IntVec3 intVec in rect)
            {
                if (!intVec.InBounds(map) || intVec.GetEdifice(map) != null)
                {
                    return false;
                }
            }
            return true;
        }
        protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int count = 1)
        {
            CellRect cellRect = CellRect.CenteredOn(loc, 7, 7).ClipInsideMap(map);
            List<CellRect> list;
            if (!MapGenerator.TryGetVar<List<CellRect>>("UsedRects", out list))
            {
                list = new List<CellRect>();
                MapGenerator.SetVar<List<CellRect>>("UsedRects", list);
            }
            if (parms.sitePart != null && parms.sitePart.things != null && parms.sitePart.things.Any)
            {
                foreach (Thing thing in parms.sitePart.things)
                {
                    GenSpawn.Spawn(thing, cellRect.RandomCell, map);
                }
            }
            ResolveParams resolveParams = default(ResolveParams);
            resolveParams.rect = cellRect;
            resolveParams.faction = map.ParentFaction;
            resolveParams.filthDef = ThingDefOf.Filth_RubbleBuilding;
            resolveParams.filthDensity = new FloatRange?(new FloatRange(0.025f, 0.05f));
            BaseGen.globalSettings.map = map;
            BaseGen.symbolStack.Push("filth", resolveParams, null);
            BaseGen.Generate();
            MapGenerator.SetVar<CellRect>("RectOfInterest", cellRect);
            list.Add(cellRect);
        }
        public ThingSetMakerDef thingSetMakerDef;
    }
    public class CompProperties_Icarus : CompProperties
    {
        public CompProperties_Icarus()
        {
            this.compClass = typeof(CompIcarus);
        }
    }
    public class CompIcarus : ThingComp
    {
        public CompProperties_Icarus Props
        {
            get
            {
                return (CompProperties_Icarus)this.props;
            }
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (mode == DestroyMode.Deconstruct && this.parent.questTags != null && this.parent.questTags.Count > 0)
            {
                QuestUtility.SendQuestTargetSignals(this.parent.questTags, "DeconstructedIcarus", this.Named("SUBJECT"), previousMap.Named("MAP"));
            }
        }
    }
}
