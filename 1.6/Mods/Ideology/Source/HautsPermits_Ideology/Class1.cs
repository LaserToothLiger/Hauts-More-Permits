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
                bool tt = pawn.health.hediffSet.HasHediff(HVMPDefOf.HVMP_TactlessTongues);
                float val = Rand.Value;
                if (tt)
                {
                    val *= 0.5f;
                }
                if (val <= 0.05f)
                {
                    recipient.needs.mood.thoughts.memories.TryGainMemory(HVMPDefOf.HVMP_AnthroAnnoyance, pawn);
                    recipient.needs.mood.thoughts.memories.TryGainMemory(HVMPDefOf.HVMP_AnthroAnnoyance, pawn);
                } else if (val <= 0.3f) {
                    recipient.needs.mood.thoughts.memories.TryGainMemory(HVMPDefOf.HVMP_AnthroAnnoyance, pawn);
                    if (tt)
                    {
                        recipient.needs.mood.thoughts.memories.TryGainMemory(HVMPDefOf.HVMP_AnthroAnnoyance, pawn);
                    }
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
        public override int ItemStackCount(PermitMoreEffects pme, Pawn caller)
        {
            int result = base.ItemStackCount(pme, caller);
            if (this.faction != null)
            {
                float curSeniority = HVMP_Mod.settings.permitsScaleBySeniority ? caller.royalty.GetCurrentTitleInFaction(this.faction).def.seniority : this.def.minTitle.seniority;
                float divisor = (pme != null && pme.minPetness > 0) ? pme.minPetness : 100f;
                int seniority = Math.Max((int)(curSeniority / divisor), 1);
                result *= seniority;
            }
            return result;
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
    //quest node
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
    //quest: ethnography
    public class QuestNode_Multiply_QQ : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return !this.storeAs.GetValue(slate).NullOrEmpty();
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            slate.Set<int>(this.storeAs.GetValue(slate), (int)(this.value1.GetValue(slate) * (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.ethnog1, HVMP_Mod.settings.ethnogX) ? this.QQ_factor : 1f)), false);
        }
        public SlateRef<int> value1;
        public float QQ_factor;
        [NoTranslate]
        public SlateRef<string> storeAs;
    }
    public class QuestNode_Give_SS_TT : QuestNode
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
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.ethnog2, HVMP_Mod.settings.ethnogX))
            {
                QuestPart_Give_SS qpss = new QuestPart_Give_SS();
                qpss.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
                qpss.pawns.AddRange(this.pawns.GetValue(slate));
                qpss.hediff = this.SS_hediff;
                QuestGen.quest.AddPart(qpss);
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_SS_info_singular", this.SS_description_singular.Formatted()),
                    new Rule_String("mutator_SS_info_plural", this.SS_description_plural.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_SS_info_singular", " "), new Rule_String("mutator_SS_info_plural", " ") });
            }
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.ethnog3, HVMP_Mod.settings.ethnogX))
            {
                QuestPart_Give_SS qpss = new QuestPart_Give_SS();
                qpss.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
                qpss.pawns.AddRange(this.pawns.GetValue(slate));
                qpss.hediff = this.TT_hediff;
                QuestGen.quest.AddPart(qpss);
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_TT_info_singular", this.TT_description_singular.Formatted()),
                    new Rule_String("mutator_TT_info_plural", this.TT_description_plural.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_TT_info_singular", " "), new Rule_String("mutator_TT_info_plural", " ") });
            }
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        public SlateRef<IEnumerable<Pawn>> pawns;
        public HediffDef SS_hediff;
        [MustTranslate]
        public string SS_description_singular;
        [MustTranslate]
        public string SS_description_plural;
        public HediffDef TT_hediff;
        [MustTranslate]
        public string TT_description_singular;
        [MustTranslate]
        public string TT_description_plural;
    }
    public class QuestPart_Give_SS : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                for (int i = 0; i < this.pawns.Count; i++)
                {
                    Hediff hef = HediffMaker.MakeHediff(this.hediff, this.pawns[i]);
                    this.pawns[i].health.AddHediff(hef, null);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_Collections.Look<Pawn>(ref this.pawns, "pawns", LookMode.Reference, Array.Empty<object>());
            Scribe_Defs.Look<HediffDef>(ref this.hediff, "hediff");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.pawns.RemoveAll((Pawn x) => x == null);
            }
        }
        public override void ReplacePawnReferences(Pawn replace, Pawn with)
        {
            this.pawns.Replace(replace, with);
        }
        public string inSignal;
        public List<Pawn> pawns = new List<Pawn>();
        public HediffDef hediff;
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
    //quest: excavation
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
            bool mayhemMode = HVMP_Mod.settings.excavX;
            bool WATEH_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.excav3, mayhemMode);
            if (WATEH_on)
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_WATEH_info", this.WATEH_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_WATEH_info", " ") });
            }
            LayoutDef ld = WATEH_on ?this.WATEH_layoutDef:this.layoutDef;
            LayoutStructureSketch layoutStructureSketch = ld.Worker.GenerateStructureSketch(structureGenParams);
            if (generateTerminals)
            {
                int num2 = Mathf.FloorToInt(this.terminalsOverRoomCountCurve.Evaluate((float)layoutStructureSketch.structureLayout.Rooms.Count));
                bool CS_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.excav1, mayhemMode);
                bool ET_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.excav2, mayhemMode);
                Faction ET_faction = Find.FactionManager.RandomRaidableEnemyFaction(false, false, false, TechLevel.Undefined);
                for (int i = 0; i < num2; i++)
                {
                    Thing thing = ThingMaker.MakeThing(HVMPDefOf.HVMP_AncientTerminal, null);
                    CompExcavationMutatorHandler cemh = thing.TryGetComp<CompExcavationMutatorHandler>();
                    if (cemh != null)
                    {
                        if (CS_on)
                        {
                            cemh.CS_on = true;
                            QuestGen.AddQuestDescriptionRules(new List<Rule>
                            {
                                new Rule_String("mutator_CS_info", this.CS_description.Formatted())
                            });
                        } else {
                            QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_CS_info", " ") });
                        }
                        if (ET_on && ET_faction != null)
                        {
                            cemh.ET_points = Math.Max(points * this.ET_pointFactor,this.ET_minPoints);
                            cemh.ET_faction = ET_faction;
                            QuestGen.AddQuestDescriptionRules(new List<Rule>
                            {
                                new Rule_String("mutator_ET_info", this.ET_description.Formatted())
                            });
                        } else {
                            QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_ET_info", " ") });
                        }
                    }
                    layoutStructureSketch.thingsToSpawn.Add(thing);
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
        [MustTranslate]
        public string CS_description;
        public float ET_pointFactor;
        public float ET_minPoints;
        [MustTranslate]
        public string ET_description;
        public LayoutDef WATEH_layoutDef;
        [MustTranslate]
        public string WATEH_description;
    }
    public class CompProperties_ExcavationMutatorHandler : CompProperties_Hackable
    {
        public CompProperties_ExcavationMutatorHandler()
        {
            this.compClass = typeof(CompExcavationMutatorHandler);
        }
        public float CS_heavyChance;
        public float CS_ultraChance;
        public float CS_bonusDefenderChance;
        public float ET_chance;
    }
    public class CompExcavationMutatorHandler : CompHackable
    {
        public new CompProperties_ExcavationMutatorHandler Props
        {
            get
            {
                return (CompProperties_ExcavationMutatorHandler)this.props;
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (this.CS_on)
            {
                int defenders = Rand.Chance(this.Props.CS_bonusDefenderChance) ? 2 : 1;
                List<Pawn> pawns = new List<Pawn>();
                for (int i = defenders; i > 0; i--)
                {
                    MechWeightClassDef mwcd = (ModsConfig.BiotechActive && Rand.Chance(0.5f)) ? MechWeightClassDefOf.Light:MechWeightClassDefOf.Medium;
                    float weightClassChance = Rand.Value;
                    if (ModsConfig.BiotechActive && weightClassChance <= this.Props.CS_ultraChance)
                    {
                        mwcd = MechWeightClassDefOf.UltraHeavy;
                    } else if (weightClassChance <= this.Props.CS_ultraChance+this.Props.CS_heavyChance) {
                        mwcd = MechWeightClassDefOf.Heavy;
                    }
                    PawnKindDef pkd = DefDatabase<PawnKindDef>.AllDefsListForReading.Where((PawnKindDef pk)=>pk.RaceProps.IsMechanoid && !pk.RaceProps.IsWorkMech && pk.RaceProps.mechWeightClass == mwcd).RandomElement();
                    if (pkd != null)
                    {
                        Pawn pawn = PawnGenerator.GeneratePawn(pkd, this.parent.Faction??Faction.OfMechanoids, null);
                        GenSpawn.Spawn(pawn,this.parent.PositionHeld,this.parent.MapHeld);
                        pawns.Add(pawn);
                    }
                }
                LordMaker.MakeNewLord(Faction.OfMechanoids, new LordJob_DefendPoint(this.parent.Position), this.parent.Map, pawns);
            }
            if (this.ET_faction != null && Rand.Chance(this.Props.ET_chance))
            {
                float num = Mathf.Max(this.ET_points, this.ET_faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat, null) * 1.05f);
                IncidentParms incidentParms = new IncidentParms
                {
                    forced = true,
                    target = this.parent.MapHeld,
                    points = num,
                    faction = this.ET_faction,
                    raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn,
                    raidStrategy = RaidStrategyDefOf.ImmediateAttack
                };
                CompHackable compHackable = this.parent.TryGetComp<CompHackable>();
                string text = compHackable.hackingCompletedSignal;
                if (text.NullOrEmpty())
                {
                    text = "RaidSignal" + Find.UniqueIDsManager.GetNextSignalTagID().ToString();
                    compHackable.hackingCompletedSignal = text;
                }
                SignalAction_Incident signalAction_Incident = (SignalAction_Incident)ThingMaker.MakeThing(ThingDefOf.SignalAction_Incident, null);
                signalAction_Incident.incident = IncidentDefOf.RaidEnemy;
                signalAction_Incident.incidentParms = incidentParms;
                signalAction_Incident.signalTag = text;
                GenSpawn.Spawn(signalAction_Incident, this.parent.PositionHeld, this.parent.MapHeld, WipeMode.Vanish);
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.CS_on, "CS_on", false, false);
            Scribe_References.Look<Faction>(ref this.ET_faction, "ET_faction", false);
            Scribe_Values.Look<float>(ref this.ET_points, "ET_points", -1f, false);
        }
        public bool CS_on;
        public Faction ET_faction;
        public float ET_points;
    }
    public class ComplexThreatWorker_WATEH : ComplexThreatWorker
    {
        protected override bool CanResolveInt(ComplexResolveParams parms)
        {
            IntVec3 intVec;
            return base.CanResolveInt(parms) && ComplexUtility.TryFindRandomSpawnCell(ThingDefOf.AncientFuelNode, parms.room, parms.map, out intVec, 1, null);
        }
        protected override void ResolveInt(ComplexResolveParams parms, ref float threatPointsUsed, List<Thing> outSpawnedThings)
        {
            int nodesToPlace = Rand.RangeInclusive(3, 5);
            for (int i = nodesToPlace; i > 0; i--)
            {
                int tries = 30;
                while (tries > 0)
                {
                    tries--;
                    ComplexUtility.TryFindRandomSpawnCell(ThingDefOf.AncientFuelNode, parms.room, parms.map, out IntVec3 intVec, 1, null);
                    if (intVec.IsValid && intVec.InBounds(parms.map))
                    {
                        Thing thing = GenSpawn.Spawn(ThingDefOf.AncientFuelNode, intVec, parms.map, WipeMode.Vanish);
                        SignalAction_StartWick signalAction_StartWick = (SignalAction_StartWick)ThingMaker.MakeThing(ThingDefOf.SignalAction_StartWick, null);
                        signalAction_StartWick.thingWithWick = thing;
                        signalAction_StartWick.signalTag = parms.triggerSignal;
                        signalAction_StartWick.completedSignalTag = "CompletedStartWickAction" + Find.UniqueIDsManager.GetNextSignalTagID().ToString();
                        if (parms.delayTicks != null)
                        {
                            signalAction_StartWick.delayTicks = parms.delayTicks.Value;
                        }
                        GenSpawn.Spawn(signalAction_StartWick, parms.room.rects[0].CenterCell, parms.map, WipeMode.Vanish);
                        CompExplosive compExplosive = thing.TryGetComp<CompExplosive>();
                        float randomInRange = ComplexThreatWorker_WATEH.ExplosiveRadiusRandomRange.RandomInRange;
                        compExplosive.customExplosiveRadius = new float?(randomInRange);
                        break;
                    }
                }
            }
            threatPointsUsed = 2f;
        }
        private static readonly FloatRange ExplosiveRadiusRandomRange = new FloatRange(2f, 12f);
    }
    //quest: remnant
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
            CompStudiableRemnant cda = thing.TryGetComp<CompStudiableRemnant>();
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
                bool mayhemMode = HVMP_Mod.settings.remnantX;
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.remnant1, mayhemMode))
                {
                    Map map = slate.Get<Map>("map", null, false) ?? Find.AnyPlayerHomeMap;
                    IncidentParms incidentParms = new IncidentParms
                    {
                        target = map,
                        forced = true,
                        points = StorytellerUtility.DefaultThreatPointsNow(map),
                    };
                    cda.BJ_activationPoint = cda.reqProgress*Rand.Range(0.1f,0.9f);
                    cda.BJ_incident = HautsUtility.badEventPool.Where((IncidentDef id) => id.Worker.CanFireNow(incidentParms)).RandomElement();
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_BJ_info", this.BJ_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_BJ_info", " ") });
                }
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.remnant2, mayhemMode))
                {
                    cda.CD_on = true;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_CD_info", this.CD_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_CD_info", " ") });
                }
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.remnant3, mayhemMode))
                {
                    cda.PI_on = true;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_PI_info", this.PI_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_PI_info", " ") });
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
        [MustTranslate]
        public string BJ_description;
        [MustTranslate]
        public string CD_description;
        [MustTranslate]
        public string PI_description;
    }
    public class CompProperties_StudiableRemnant : CompProperties_StudiableQuestItem
    {
        public CompProperties_StudiableRemnant()
        {
            this.compClass = typeof(CompStudiableRemnant);
        }
        public float CD_damagePerDay;
        public int PI_maxCooldown;
        public float PI_progressLossPerDay;
    }
    public class CompStudiableRemnant : CompStudiableQuestItem
    {
        public new CompProperties_StudiableRemnant Props
        {
            get
            {
                return (CompProperties_StudiableRemnant)this.props;
            }
        }
        public override void ExtraStudyEffects(int delta, Pawn researcher, Thing brb, Thing researchBench)
        {
            base.ExtraStudyEffects(delta, researcher, brb, researchBench);
            this.PI_cooldown = this.Props.PI_maxCooldown;
            if (this.BJ_activationPoint > 0f && this.curProgress > this.BJ_activationPoint && this.BJ_incident != null)
            {
                IncidentParms incidentParms = new IncidentParms
                {
                    target = this.parent.MapHeld ?? Find.AnyPlayerHomeMap,
                    forced = true,
                    points = StorytellerUtility.DefaultThreatPointsNow(this.parent.MapHeld ?? Find.AnyPlayerHomeMap),
                };
                if (this.BJ_incident.Worker.CanFireNow(incidentParms))
                {
                    Find.Storyteller.incidentQueue.Add(this.BJ_incident, Find.TickManager.TicksGame + 60, incidentParms, 60000);
                } else {
                    Log.Error("Tried to fire " + this.BJ_incident.label + " for the Remnant quest's Bad Juju mutator, but its worker could not fire. Using a random other bad event instead...");
                    List<IncidentDef> incidents = HautsUtility.badEventPool.Where((IncidentDef id) => id.Worker.CanFireNow(incidentParms)).ToList();
                    Find.Storyteller.incidentQueue.Add(incidents.RandomElement(), Find.TickManager.TicksGame + 60, incidentParms, 60000);
                }
                this.BJ_activationPoint = -1f;
            }
        }
        public override void CompTickRare()
        {
            base.CompTickRare();
            if (this.CD_on)
            {
                if (this.parent.IsHashIntervalTick((int)(60000 / this.Props.CD_damagePerDay), 250))
                {
                    this.parent.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, 1f, 0f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true, QualityCategory.Normal, true, false));
                }
            }
            if (this.PI_on)
            {
                if (this.PI_cooldown < 0)
                {
                    this.curProgress -= Math.Min(this.curProgress, this.Props.PI_progressLossPerDay / 240f);
                } else {
                    this.PI_cooldown -= 250;
                }
            }
        }
        public override string CompInspectStringExtra()
        {
            string desc = base.CompInspectStringExtra();
            if (this.BJ_activationPoint > this.curProgress && this.BJ_incident != null)
            {
                desc += "\n" + "HVMP_Remnant_BJlabel".Translate();
            }
            if (this.CD_on)
            {
                desc += "\n" + "HVMP_Remnant_CDlabel".Translate(this.Props.CD_damagePerDay);
            }
            if (this.PI_on)
            {
                desc += "\n" + "HVMP_Remnant_PIlabel".Translate(this.Props.PI_maxCooldown.ToStringTicksToPeriod(true, true, true, true, true), this.Props.PI_progressLossPerDay);
            }
            return desc;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref this.BJ_activationPoint, "BJ_activationPoint", -1f, false);
            Scribe_Defs.Look<IncidentDef>(ref this.BJ_incident, "BJ_incident");
            Scribe_Values.Look<bool>(ref this.CD_on, "CD_on", false, false);
            Scribe_Values.Look<int>(ref this.PI_cooldown, "PI_cooldown", -1, false);
            Scribe_Values.Look<bool>(ref this.PI_on, "PI_on", false, false);
        }
        public float BJ_activationPoint;
        public IncidentDef BJ_incident;
        public bool CD_on;
        public bool PI_on;
        public int PI_cooldown;
    }
    //quest: satellite
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
            bool mayhemMode = HVMP_Mod.settings.satX;
            Thing thing;
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.sat3, mayhemMode))
            {
                thing = ThingMaker.MakeThing(this.SW_droneDef, null);
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_SW_info", this.SW_description.Formatted())
                });
            } else {
                thing = ThingMaker.MakeThing(this.nonSW_droneDef, null);
                thing.SetFaction(Faction.OfMechanoids);
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_SW_info", " ") });
            }
            slate.Set<Thing>("spacedrone", thing, false);
            QuestPart_Filter_AllThingsHacked questPart_Filter_AllThingsHacked = new QuestPart_Filter_AllThingsHacked();
            questPart_Filter_AllThingsHacked.things.Add(thing);
            questPart_Filter_AllThingsHacked.inSignal = spacedroneDestroyedSignal;
            questPart_Filter_AllThingsHacked.outSignal = QuestGen.GenerateNewSignal("QuestEndSuccess", true);
            questPart_Filter_AllThingsHacked.outSignalElse = QuestGen.GenerateNewSignal("QuestEndFailure", true);
            quest.AddPart(questPart_Filter_AllThingsHacked);
            qpdpg.drone = thing;
            qpdpg.sleepyTime = this.sleepyTime.RandomInRange;
            slate.Set<int>("timeToWake", qpdpg.sleepyTime, false);
            List<PawnKindDef> pkdList = new List<PawnKindDef>();
            foreach (Pawn p in list2)
            {
                pkdList.Add(p.kindDef);
            }
            qpdpg.dropSpot = this.dropSpot.GetValue(slate) ?? IntVec3.Invalid;
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.sat1, mayhemMode))
            {
                qpdpg.DESC_on = true;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_DESC_info", this.DESC_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_DESC_info", this.nonDESC_description.Formatted())
                });
            }
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.sat2, mayhemMode))
            {
                CompHB chb = thing.TryGetComp<CompHB>();
                if (chb != null)
                {
                    chb.HB_on = true;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_HB_info", this.HB_description.Formatted())
                    });
                }
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_HB_info", " ") });
            }
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
        public IntRange sleepyTime;
        [MustTranslate]
        public string DESC_description;
        [MustTranslate]
        public string nonDESC_description;
        [MustTranslate]
        public string HB_description;
        public ThingDef nonSW_droneDef;
        public ThingDef SW_droneDef;
        [MustTranslate]
        public string SW_description;
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
                    this.TryFindSpacedronePosition(this.mapParent.Map, out this.spawnedClusterPos);
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
                Lord lj;
                if (!this.DESC_on)
                {
                    lj = LordMaker.MakeNewLord(Faction.OfMechanoids, new LordJob_SleepThenMechanoidsDefendDrone(toWakeUpOn, Faction.OfMechanoids, 28f, this.spawnedClusterPos, false, false, this.sleepyTime * 2500), this.mapParent.Map, spawnedPawns);
                }
                else
                {
                    lj = LordMaker.MakeNewLord(Faction.OfMechanoids, new LordJob_MechanoidsDefend(toWakeUpOn, Faction.OfMechanoids, 14f, this.spawnedClusterPos, false, false), this.mapParent.Map, spawnedPawns);
                }
                if (this.drone is Building bdrone)
                {
                    lj.AddBuilding(bdrone);
                    bdrone.SetFaction(Faction.OfMechanoids);
                }
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
            }
            else
            {
                Scribe_References.Look<Thing>(ref this.drone, "drone", false);
            }
            Scribe_Values.Look<int>(ref this.sleepyTime, "sleepyTime", 2500, false);
            Scribe_Values.Look<bool>(ref this.DESC_on, "DESC_on", false, false);
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
        public bool DESC_on;
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
        protected override void OnHacked(Pawn hacker = null, bool suppressMessages = false)
        {
            base.OnHacked(hacker, suppressMessages);
            if (this.parent.Faction != null)
            {
                this.parent.SetFaction(null);
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
    public class CompProperties_HB : CompProperties
    {
        public CompProperties_HB()
        {
            this.compClass = typeof(CompHB);
        }
        public int periodicity;
        public float pointFactor;
        public int pointMinimum;
    }
    public class CompHB : ThingComp
    {
        public CompProperties_HB Props
        {
            get
            {
                return (CompProperties_HB) this.props;
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            this.HB_cooldown = this.Props.periodicity;
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.HB_on && this.parent.Spawned)
            {
                CompHackable ch = this.parent.GetComp<CompHackable>();
                if (ch != null && ch.IsHacked)
                {
                    this.HB_on = false;
                    return;
                }
                this.HB_cooldown -= delta;
                if (this.HB_cooldown <= 0)
                {
                    this.HB_cooldown = this.Props.periodicity;
                    IncidentParms incidentParms = new IncidentParms();
                    incidentParms.forced = true;
                    incidentParms.target = this.parent.MapHeld;
                    incidentParms.points = StorytellerUtility.DefaultThreatPointsNow(this.parent.MapHeld) * this.Props.pointFactor;
                    incidentParms.faction = Faction.OfMechanoids;
                    incidentParms.generateFightersOnly = true;
                    incidentParms.sendLetter = true;
                    IncidentDef incidentDef = IncidentDefOf.RaidEnemy;
                    incidentParms.points = Mathf.Max(incidentParms.points, Faction.OfMechanoids.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat, null));
                    if (incidentDef.Worker.CanFireNow(incidentParms))
                    {
                        incidentDef.Worker.TryExecute(incidentParms);
                    }
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.HB_on, "HB_on", false, false);
            Scribe_Values.Look<int>(ref this.HB_cooldown, "HB_cooldown", -1, false);
        }
        public bool HB_on;
        public int HB_cooldown;
    }
    //quest: shrine
    public class QuestNode_ArchiveShrine : QuestNode
    {
        protected override void RunInt()
        {
            if (!ModLister.CheckIdeology("Worshipped terminal"))
            {
                return;
            }
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            Map map = slate.Get<Map>("map", null, false) ?? Find.AnyPlayerHomeMap;
            QuestGenUtility.RunAdjustPointsForDistantFight();
            float num = slate.Get<float>("points", 0f, false);
            slate.Set<Faction>("playerFaction", Faction.OfPlayer, false);
            slate.Set<bool>("allowViolentQuests", Find.Storyteller.difficulty.allowViolentQuests, false);
            bool mayhemMode = HVMP_Mod.settings.shrineX;
            PlanetTile planetTile;
            this.TryFindSiteTile(out planetTile);
            FactionDef factionDef = DefDatabase<FactionDef>.AllDefsListForReading.Where((FactionDef fd)=>!fd.hidden&&fd.humanlikeFaction&&!fd.permanentEnemy&&!fd.naturalEnemy&&!fd.HasModExtension<EBranchQuests>()&&!fd.pawnGroupMakers.NullOrEmpty()&&fd.pawnGroupMakers.ContainsAny((PawnGroupMaker pgm)=>pgm.kindDef == PawnGroupKindDefOf.Combat) && fd.pawnGroupMakers.ContainsAny((PawnGroupMaker pgm) => pgm.kindDef == PawnGroupKindDefOf.Settlement)).RandomElement()??FactionDefOf.TribeCivil;
            List<FactionRelation> list = new List<FactionRelation>();
            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            {
                if (!faction.def.PermanentlyHostileTo(factionDef))
                {
                    list.Add(new FactionRelation
                    {
                        other = faction,
                        kind = FactionRelationKind.Neutral
                    });
                }
            }
            FactionGeneratorParms factionGeneratorParms = new FactionGeneratorParms(factionDef, default(IdeoGenerationParms), true);
            factionGeneratorParms.ideoGenerationParms = new IdeoGenerationParms(factionGeneratorParms.factionDef, false, null, null, null, false, false, false, false, "", null, null, false, "", false);
            Faction shrineFaction = FactionGenerator.NewGeneratedFactionWithRelations(factionGeneratorParms, list);
            shrineFaction.temporary = true;
            shrineFaction.factionHostileOnHarmByPlayer = true;
            shrineFaction.neverFlee = true;
            Find.FactionManager.Add(shrineFaction);
            quest.ReserveFaction(shrineFaction);
            string text = QuestGenUtility.HardcodedSignalWithQuestID("playerFaction.BuiltBuilding");
            string text2 = QuestGenUtility.HardcodedSignalWithQuestID("playerFaction.PlacedBlueprint");
            bool PB_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.shrine1, mayhemMode);
            bool TTWSD_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.shrine2, mayhemMode);
            bool XP_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.shrine3, mayhemMode);
            num = this.base_populationFactor *Mathf.Max(num, shrineFaction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Settlement, null));
            if (PB_on)
            {
                num *= this.PB_populationFactor;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_PB_info", this.PB_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule>{ new Rule_String("mutator_PB_info", " ") });
            }
            if (XP_on)
            {
                num *= this.XP_populationFactor;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_XP_info", this.XP_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_XP_info", this.nonXP_description.Formatted())
                });
            }
            QuestGen.AddQuestDescriptionRules(new List<Rule>
            {
                new Rule_String("mutator_XP_info_nonviolent", this.nonviolent_description.Formatted())
            });
            SitePartParams sitePartParams = new SitePartParams
            {
                points = num,
                threatPoints = num
            };
            Site site = QuestGen_Sites.GenerateSite(Gen.YieldSingle<SitePartDefWithParams>(new SitePartDefWithParams(HVMPDefOf.HVMP_Shrine, sitePartParams)), planetTile, shrineFaction, false, null, null);
            site.doorsAlwaysOpenForPlayerPawns = true;
            slate.Set<Site>("site", site, false);
            quest.SpawnWorldObject(site, null, null);
            int num2 = XP_on?30:25000;
            site.GetComponent<TimedMakeFactionHostile>().SetupTimer(num2, "WorshippedTerminalFactionBecameHostileTimed".Translate(shrineFaction.Named("FACTION")), null);
            Thing thing = site.parts[0].things.First((Thing t) => t.def == ThingDefOf.AncientTerminal_Worshipful);
            if (TTWSD_on)
            {
                CompTTWSD cttwsd = thing.TryGetComp<CompTTWSD>();
                if (cttwsd != null)
                {
                    cttwsd.TTWSD_on = true;
                }
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_TTWSD_info", this.TTWSD_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_TTWSD_info", " ") });
            }
            slate.Set<Thing>("terminal", thing, false);
            string text3 = QuestGenUtility.HardcodedSignalWithQuestID("terminal.Hacked");
            string text4 = QuestGenUtility.HardcodedSignalWithQuestID("terminal.HackingStarted");
            string text6 = QuestGenUtility.HardcodedSignalWithQuestID("site.MapGenerated");
            string text7 = QuestGenUtility.HardcodedSignalWithQuestID("shrineFaction.FactionMemberArrested");
            CompHackable compHackable = thing.TryGetComp<CompHackable>();
            compHackable.hackingStartedSignal = text4;
            compHackable.defence = (float)QuestNode_ArchiveShrine.HackDefenceRange.RandomInRange;
            quest.Message("[terminalHackedMessage]", null, true, null, null, text3);
            quest.SetFactionHidden(shrineFaction, false, null);
            if (Find.Storyteller.difficulty.allowViolentQuests)
            {
                quest.FactionRelationToPlayerChange(shrineFaction, FactionRelationKind.Hostile, false, text4);
                quest.StartRecurringRaids(site, new FloatRange?(new FloatRange(24f, 24f)), new int?(2500), text4);
                quest.BuiltNearSettlement(shrineFaction, site, delegate
                {
                    quest.FactionRelationToPlayerChange(shrineFaction, FactionRelationKind.Hostile, true, null);
                }, null, text, null, null, QuestPart.SignalListenMode.OngoingOnly);
                quest.BuiltNearSettlement(shrineFaction, site, delegate
                {
                    quest.Message("WarningBuildingCausesHostility".Translate(shrineFaction.Named("FACTION")), MessageTypeDefOf.CautionInput, false, null, null, null);
                }, null, text2, null, null, QuestPart.SignalListenMode.OngoingOnly);
                quest.FactionRelationToPlayerChange(shrineFaction, FactionRelationKind.Hostile, true, text7);
            }
            slate.Set<Map>("map", map, false);
            slate.Set<int>("timer", num2, false);
            slate.Set<Faction>("shrineFaction", shrineFaction, false);
        }
        private bool TryFindSiteTile(out PlanetTile tile)
        {
            return TileFinder.TryFindNewSiteTile(out tile, 2, 20, false, null, 0.5f, true, TileFinderMode.Near, false, false, null, null);
        }
        protected override bool TestRunInt(Slate slate)
        {
            PlanetTile planetTile;
            return this.TryFindSiteTile(out planetTile);
        }
        private static IntRange HackDefenceRange = new IntRange(10, 100);
        public float base_populationFactor;
        public float PB_populationFactor;
        [MustTranslate]
        public string PB_description;
        [MustTranslate]
        public string TTWSD_description;
        public float XP_populationFactor;
        [MustTranslate]
        public string XP_description;
        [MustTranslate]
        public string nonXP_description;
        [MustTranslate]
        public string nonviolent_description;
    }
    public class SitePartWorker_Shrine : SitePartWorker
    {
        public override void Init(Site site, SitePart sitePart)
        {
            base.Init(site, sitePart);
            sitePart.things = new ThingOwner<Thing>(sitePart);
            Thing thing = ThingMaker.MakeThing(ThingDefOf.AncientTerminal_Worshipful, null);
            sitePart.things.TryAdd(thing, true);
        }
        public override string GetArrivedLetterPart(Map map, out LetterDef preferredLetterDef, out LookTargets lookTargets)
        {
            return base.GetArrivedLetterPart(map, out preferredLetterDef, out lookTargets).Formatted(map.Parent.GetComponent<TimedMakeFactionHostile>().TicksLeft.Value.ToStringTicksToPeriod(true, false, true, true, false).Named("TIMER"));
        }
    }
    public class GenStep_MaybeTurrets : GenStep_Turrets
    {
        public override void Generate(Map map, GenStepParams parms)
        {
            if (Rand.Chance(this.chance))
            {
                base.Generate(map, parms);
            }
        }
        public float chance = 0.5f;
    }
    public class CompProperties_TTWSD : CompProperties
    {
        public CompProperties_TTWSD()
        {
            this.compClass = typeof(CompTTWSD);
        }
        public ThingDef thingToSpawn;
    }
    public class CompTTWSD : ThingComp
    {
        public CompProperties_TTWSD Props
        {
            get
            {
                return (CompProperties_TTWSD)this.props;
            }
        }
        public override void Notify_Hacked(Pawn hacker = null)
        {
            base.Notify_Hacked(hacker);
            if (this.TTWSD_on)
            {
                Thing thing = ThingMaker.MakeThing(this.Props.thingToSpawn, null);
                GenPlace.TryPlaceThing(thing, this.parent.Position, this.parent.Map, ThingPlaceMode.Near, null, null, null, 1);
                CompExplosive ce = thing.TryGetComp<CompExplosive>();
                if (ce != null)
                {
                    ce.StartWick();
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.TTWSD_on, "TTWSD_on", false, false);
        }
        public bool TTWSD_on;
    }
}
