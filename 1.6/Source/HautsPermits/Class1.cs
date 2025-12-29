using RimWorld.Planet;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using UnityEngine;
using HarmonyLib;
using RimWorld.QuestGen;
using HautsFramework;
using Verse.AI;
using Verse.Grammar;
using RimWorld.BaseGen;
using Verse.Sound;
using Verse.AI.Group;
using System.Xml;
using static System.Collections.Specialized.BitVector32;
using System.Net.NetworkInformation;
using System.Collections;
using Verse.Noise;
using static UnityEngine.GraphicsBuffer;
using VEF;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace HautsPermits
{
    [StaticConstructorOnStartup]
    public class HautsPermits
    {
        private static readonly Type patchType = typeof(HautsPermits);
        static HautsPermits()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautspermits");
            MethodInfo methodInfo = typeof(QuestNode_GetFaction).GetMethod("IsGoodFaction", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo,
                          prefix: new HarmonyMethod(patchType, nameof(HVMPIsGoodFactionPrefix)));
            MethodInfo methodInfo1 = typeof(QuestNode_GetPawn).GetMethod("IsGoodPawn", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo1,
                          prefix: new HarmonyMethod(patchType, nameof(HVMPIsGoodPawnPrefix)));
            MethodInfo methodInfo2 = typeof(QuestNode_GetPawn).GetMethod("TryFindFactionForPawnGeneration", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo2,
                          postfix: new HarmonyMethod(patchType, nameof(HVMPTryFindFactionForPawnGenerationPostfix)));
            if (ModsConfig.IdeologyActive)
            {
                MethodInfo methodInfo4 = typeof(QuestNode_Root_Mission_AncientComplex).GetMethod("AskerFactionValid", BindingFlags.NonPublic | BindingFlags.Instance);
                harmony.Patch(methodInfo4,
                              postfix: new HarmonyMethod(patchType, nameof(HVMPAskerFactionValidPostfix)));
            }
            if (ModsConfig.BiotechActive)
            {
                MethodInfo methodInfo4 = typeof(QuestNode_Root_PollutionDump).GetMethod("FindAsker", BindingFlags.NonPublic | BindingFlags.Instance);
                harmony.Patch(methodInfo4,
                              postfix: new HarmonyMethod(patchType, nameof(HVMPFindAskerPostfix)));
            }
            if (ModsConfig.AnomalyActive)
            {
                MethodInfo methodInfo4 = typeof(QuestNode_Root_MysteriousCargo).GetMethod("FindAsker", BindingFlags.NonPublic | BindingFlags.Instance);
                harmony.Patch(methodInfo4,
                              postfix: new HarmonyMethod(patchType, nameof(HVMPFindAskerPostfix)));
            }
            MethodInfo methodInfo5 = typeof(QuestNode_Root_Mission_BanditCamp).GetMethod("GetAsker", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo5,
                          postfix: new HarmonyMethod(patchType, nameof(HVMPFindAskerPostfix)));
            harmony.Patch(AccessTools.Method(typeof(FactionDialogMaker), nameof(FactionDialogMaker.FactionDialogFor)),
                          prefix: new HarmonyMethod(patchType, nameof(HVMPFactionDialogForPrefix)));
            harmony.Patch(AccessTools.Property(typeof(Pawn_TraderTracker), nameof(Pawn_TraderTracker.CanTradeNow)).GetGetMethod(),
                           prefix: new HarmonyMethod(patchType, nameof(HVMPCanTradeNowPrefix)));
            MethodInfo methodInfo3 = typeof(TradeDeal).GetMethod("AddAllTradeables", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo3,
                          prefix: new HarmonyMethod(patchType, nameof(HVMPAddAllTradeablesPrefix)));
            if (ModsConfig.IdeologyActive)
            {
                harmony.Patch(AccessTools.Method(typeof(IncidentWorker_RaidFriendly), nameof(IncidentWorker_RaidFriendly.ResolveRaidStrategy)),
                              postfix: new HarmonyMethod(patchType, nameof(HVMPResolveRaidStrategyPostfix)));
            }
            harmony.Patch(AccessTools.Method(typeof(IncidentWorker), nameof(IncidentWorker.TryExecute)),
                           prefix: new HarmonyMethod(patchType, nameof(HVMPTryExecutePrefix)));
            harmony.Patch(AccessTools.Method(typeof(Faction), nameof(Faction.Notify_BuildingTookDamage)),
                          prefix: new HarmonyMethod(patchType, nameof(HVMPNotify_BuildingTookDamagePrefix)));
            harmony.Patch(AccessTools.Method(typeof(Faction), nameof(Faction.Notify_MemberTookDamage)),
                          prefix: new HarmonyMethod(patchType, nameof(HVMPNotify_MemberTookDamagePrefix)));
            harmony.Patch(AccessTools.Method(typeof(QuestManager), nameof(QuestManager.Add)),
                          postfix: new HarmonyMethod(patchType, nameof(HVMPQuestManager_AddPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Faction), nameof(Faction.TryAffectGoodwillWith)),
                          prefix: new HarmonyMethod(patchType, nameof(HVMPTryAffectGoodwillWithPrefix)));
            harmony.Patch(AccessTools.Method(typeof(Faction), nameof(Faction.TryAffectGoodwillWith)),
                          postfix: new HarmonyMethod(patchType, nameof(HVMPTryAffectGoodwillWithPostfix)));
            harmony.Patch(AccessTools.Method(typeof(SettlementUtility), nameof(SettlementUtility.AffectRelationsOnAttacked)),
                          postfix: new HarmonyMethod(patchType, nameof(HVMPAffectRelationsOnAttackedPostfix)));
            harmony.Patch(AccessTools.Method(typeof(SettlementDefeatUtility), nameof(SettlementDefeatUtility.CheckDefeated)),
                          prefix: new HarmonyMethod(patchType, nameof(HVMPCheckDefeatedPrefix)));
            if (ModsConfig.OdysseyActive)
            {
                harmony.Patch(AccessTools.Method(typeof(TendUtility), nameof(TendUtility.DoTend)),
                              postfix: new HarmonyMethod(patchType, nameof(HVMPDoTendPostfix)));
            }
            harmony.Patch(AccessTools.Method(typeof(WorldComponent_HautsFactionComps), nameof(WorldComponent_HautsFactionComps.ThirdTickEffects)),
                          postfix: new HarmonyMethod(patchType, nameof(HVMPThirdTickEffectsPostfix)));
            harmony.Patch(AccessTools.Property(typeof(Settlement), nameof(Settlement.CanTradeNow)).GetGetMethod(),
                           prefix: new HarmonyMethod(patchType, nameof(HVMP_Settlement_CanTradeNowPrefix)));
            if (!HVMP_Mod.settings.permitsScaleBySeniority)
            {
                foreach (RoyalTitlePermitDef rtpd in DefDatabase<RoyalTitlePermitDef>.AllDefsListForReading)
                {
                    ScalingDisabledDescription pme = rtpd.GetModExtension<ScalingDisabledDescription>();
                    if (pme != null && pme.extraString != null)
                    {
                        rtpd.description = pme.extraString;
                    }
                }
            }
            Log.Message("HVMP_Initialize".Translate().CapitalizeFirst());
        }
        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
        public static bool HVMPIsGoodFactionPrefix(Faction faction, ref bool __result)
        {
            if (faction.def.HasModExtension<EBranchQuests>())
            {
                __result = false;
                return false;
            }
            return true;
        }
        public static bool HVMPIsGoodPawnPrefix(Pawn pawn, ref bool __result)
        {
            if (pawn.Faction != null && pawn.Faction.def.HasModExtension<EBranchQuests>())
            {
                __result = false;
                return false;
            }
            return true;
        }
        public static void HVMPTryFindFactionForPawnGenerationPostfix(QuestNode_GetPawn __instance, Slate slate, ref Faction faction)
        {
            if (faction.def.HasModExtension<EBranchQuests>())
            {
                List<Faction> factions = new List<Faction>();
                foreach (Faction f in Find.FactionManager.GetFactions(false, false, false, TechLevel.Undefined, false))
                {
                    Map map;
                    if (!f.def.HasModExtension<EBranchQuests>() && (__instance.excludeFactionDefs.GetValue(slate) == null || !__instance.excludeFactionDefs.GetValue(slate).Contains(f.def)) && (!__instance.mustHaveRoyalTitleInCurrentFaction.GetValue(slate) || f.def.HasRoyalTitles) && (!__instance.mustBeNonHostileToPlayer.GetValue(slate) || !f.HostileTo(Faction.OfPlayer)) && (!slate.TryGet<Map>("map", out map, false) || !__instance.mustHaveSettlementOnLayer.GetValue(slate) || !map.Tile.Valid || Find.WorldObjects.AnyFactionSettlementOnLayer(f, map.Tile.Layer)) && ((__instance.allowPermanentEnemyFaction.GetValue(slate) ?? false) || !f.def.permanentEnemy) && f.def.techLevel >= __instance.minTechLevel.GetValue(slate) && (!__instance.factionMustBePermanent.GetValue(slate) || !f.temporary))
                    {
                        factions.Add(f);
                    }
                }
                factions.TryRandomElementByWeight(delegate (Faction x)
                {
                    if (x.HostileTo(Faction.OfPlayer))
                    {
                        float? num = __instance.hostileWeight.GetValue(slate);
                        if (num == null)
                        {
                            return 1f;
                        }
                        return num.GetValueOrDefault();
                    } else {
                        float? num = __instance.nonHostileWeight.GetValue(slate);
                        if (num == null)
                        {
                            return 1f;
                        }
                        return num.GetValueOrDefault();
                    }
                }, out faction);
            }
        }
        public static void HVMPAskerFactionValidPostfix(ref bool __result, Faction faction)
        {
            if (faction.def.HasModExtension<EBranchQuests>())
            {
                __result = false;
            }
        }
        public static void HVMPFindAskerPostfix(ref Pawn __result)
        {
            if (__result.Faction != null && __result.Faction.def.HasModExtension<EBranchQuests>())
            {
                Faction faction;
                if (Find.FactionManager.AllFactionsVisible.Where((Faction f) => f.def.humanlikeFaction && !f.IsPlayer && !f.HostileTo(Faction.OfPlayer) && f.def.techLevel > TechLevel.Neolithic && f.leader != null && !f.temporary && !f.def.HasModExtension<EBranchQuests>() && !f.Hidden).TryRandomElement(out faction))
                {
                    __result = faction.leader;
                    return;
                }
                __result = null;
            }
        }
        public static bool HVMPFactionDialogForPrefix(ref DiaNode __result, Pawn negotiator, Faction faction)
        {
            WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
            Hauts_FactionCompHolder fch = WCFC.FindCompsFor(faction);
            EBranchQuests gq = faction.def.GetModExtension<EBranchQuests>();
            if (gq != null && fch != null)
            {
                Map map = negotiator.Map;
                if (map.generatorDef.isUnderground)
                {
                    string textFail = "HVMP_TooDeepToReach".Translate(faction.NameColored);
                    __result = new DiaNode(textFail);
                    return false;
                }
                bool isCosmopolitan = HVMP_Utility.NegotiatorIsCosmopolitan(negotiator);
                string text = "HVMP_Representative".Translate(faction.NameColored);
                __result = new DiaNode(text);
                TaggedString taggedString2 = "HVMP_SolicitQuest".Translate();
                HautsFactionComp_PeriodicBranchQuests pgq = fch.TryGetComp<HautsFactionComp_PeriodicBranchQuests>();
                if (pgq != null)
                {
                    if (!pgq.AnomalyRequirementsMet(true))
                    {
                        string questCooldown = "HVMP_QuestAnomalyInactive".Translate();
                        DiaOption diaOption2 = new DiaOption(taggedString2);
                        diaOption2.Disable(questCooldown);
                        __result.options.Add(diaOption2);
                    } else if (pgq.commsQuestCooldownTicks > 0) {
                        string questCooldown = "HVMP_QuestCooldown".Translate(pgq.commsQuestCooldownTicks.TicksToDays());
                        DiaOption diaOption2 = new DiaOption(taggedString2);
                        diaOption2.Disable(questCooldown);
                        __result.options.Add(diaOption2);
                    } else {
                        DiaOption diaOption2 = new DiaOption(taggedString2);
                        diaOption2.action = delegate
                        {
                            if (gq.quests != null)
                            {
                                List<QuestScriptDef> qsds = new List<QuestScriptDef>();
                                foreach (QuestScriptDef qsd in gq.quests)
                                {
                                    Slate slate0 = new Slate();
                                    slate0.Set<Faction>("branchFaction", faction, false);
                                    slate0.Set<float>("points", HVMP_Utility.TryGetPoints(negotiator), false);
                                    Map maplike = HVMP_Utility.TryGetMap();
                                    slate0.Set<Map>("map", maplike, false);
                                    PlanetTile tile = HVMP_Utility.TryGetPlanetTile();
                                    if (qsd.CanRun(slate0, map))
                                    {
                                        qsds.Add(qsd);
                                    }
                                }
                                if (qsds.Count > 0)
                                {
                                    QuestScriptDef questDef = qsds.RandomElement();
                                    BranchQuestProps gqp = questDef.GetModExtension<BranchQuestProps>();
                                    Slate slate = new Slate();
                                    slate.Set<Faction>("branchFaction", faction, false);
                                    slate.Set<float>("points", gqp != null ? gqp.points.RandomInRange : 1000f, false);
                                    Map maplike = HVMP_Utility.TryGetMap();
                                    slate.Set<Map>("map", gqp != null ? (gqp.needsPlayerMap ? maplike : null) : null, false);
                                    PlanetTile tile = HVMP_Utility.TryGetPlanetTile();
                                    slate.Set<PlanetTile>("pTile", tile, false);
                                    Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(questDef, slate);
                                    if (!quest.hidden && quest.root.sendAvailableLetter)
                                    {
                                        QuestUtility.SendLetterQuestAvailable(quest);
                                    }
                                    foreach (Faction f in Find.FactionManager.AllFactionsListForReading)
                                    {
                                        Hauts_FactionCompHolder hfch = WCFC.FindCompsFor(f);
                                        if (hfch != null)
                                        {
                                            HautsFactionComp_PeriodicBranchQuests hpgq = hfch.TryGetComp<HautsFactionComp_PeriodicBranchQuests>();
                                            if (hpgq != null)
                                            {
                                                hpgq.commsQuestCooldownTicks = 180000;
                                            }
                                        }
                                    }
                                } else {
                                    Log.Error("HVMP_ErrorNoUsableBranchQuests".Translate(faction.NameColored));
                                }
                            } else {
                                Log.Error("HVMP_ErrorNoQuestsFoundForBranch".Translate(faction.NameColored));
                            }
                        };
                        diaOption2.link = new DiaNode("HVMP_GotQuest".Translate(faction.NameColored).CapitalizeFirst())
                        {
                            options = { new DiaOption("OK".Translate()) { linkLateBind = FactionDialogMaker.ResetToRoot(faction, negotiator) } }
                        };
                        __result.options.Add(diaOption2);
                    }
                }
                if (faction.PlayerRelationKind != FactionRelationKind.Hostile)
                {
                    TaggedString taggedString;
                    if (gq.donationTraderKind != null)
                    {
                        DonationTypeBeat sa3d = gq.donationTraderKind.GetModExtension<DonationTypeBeat>();
                        taggedString = "HVMP_Donate".Translate(sa3d != null ? sa3d.donationString : "");
                    }
                    int cdRefreshCost = isCosmopolitan ? -16 : -20;
                    TaggedString taggedString3 = "HVMP_RefreshPermitCooldowns".Translate(negotiator.Name.ToStringShort, -Faction.OfPlayer.CalculateAdjustedGoodwillChange(faction, cdRefreshCost));
                    if (gq.donationTraderKind != null)
                    {
                        if (negotiator.GetCurrentTitleIn(faction) == null)
                        {
                            string needTitle = "HVMP_NeedTitle".Translate(negotiator.Name.ToStringShort, faction.NameColored);
                            DiaOption diaOption = new DiaOption(taggedString);
                            diaOption.Disable(needTitle);
                            __result.options.Add(diaOption);
                        } else {
                            DiaOption diaOption = new DiaOption(taggedString);
                            diaOption.action = delegate
                            {
                                if (faction.leader.trader == null)
                                {
                                    faction.leader.trader = new Pawn_TraderTracker(faction.leader);
                                }
                                faction.leader.mindState.wantsToTradeWithColony = true;
                                faction.leader.trader.traderKind = gq.donationTraderKind;
                                Find.WindowStack.Add(new Dialog_Trade(negotiator, faction.leader, false));
                            };
                            __result.options.Add(diaOption);
                        }
                    }
                    if (negotiator.GetCurrentTitleIn(faction) == null)
                    {
                        string needTitle = "HVMP_NeedTitle".Translate(negotiator.Name.ToStringShort, faction.NameColored);
                        DiaOption diaOption3 = new DiaOption(taggedString3);
                        diaOption3.Disable(needTitle);
                        __result.options.Add(diaOption3);
                    } else if (faction.PlayerRelationKind != FactionRelationKind.Ally) {
                        DiaOption diaOption3 = new DiaOption(taggedString3);
                        diaOption3.Disable("MustBeAlly".Translate());
                        __result.options.Add(diaOption3);
                    } else {
                        if (negotiator.royalty == null || negotiator.royalty.PermitsFromFaction(faction).Count == 0)
                        {
                            DiaOption diaOption3 = new DiaOption(taggedString3);
                            diaOption3.Disable("HVMP_NoPermitsToRefresh".Translate());
                            __result.options.Add(diaOption3);
                        } else {
                            DiaOption diaOption3 = new DiaOption(taggedString3);
                            diaOption3.action = delegate
                            {
                                if (negotiator.royalty != null)
                                {
                                    foreach (FactionPermit fp in negotiator.royalty.PermitsFromFaction(faction))
                                    {
                                        fp.ResetCooldown();
                                    }
                                }
                                Faction.OfPlayer.TryAffectGoodwillWith(faction, cdRefreshCost, false, true, HVMPDefOf.HVMP_RefreshedPermitCDs, null);
                            };
                            diaOption3.link = new DiaNode("HVMP_RefreshedPermitCDs".Translate(faction.NameColored, negotiator.Name.ToStringShort).CapitalizeFirst())
                            {
                                options = { new DiaOption("OK".Translate()) { linkLateBind = FactionDialogMaker.ResetToRoot(faction, negotiator) } }
                            };
                            __result.options.Add(diaOption3);
                        }
                    }
                }
                DiaOption diaOptionOuties = new DiaOption("(" + "Disconnect".Translate() + ")")
                {
                    resolveTree = true
                };
                __result.options.Add(diaOptionOuties);
                return false;
            } else if (Find.World.gameConditionManager.ConditionIsActive(HVMPDefOf.HVMP_CommsBlackout)) {
                Pawn pawn;
                string text;
                if (faction.leader != null)
                {
                    pawn = faction.leader;
                    text = faction.leader.Name.ToStringFull.Colorize(ColoredText.NameColor);
                } else {
                    Log.Error(string.Format("Faction {0} has no leader.", faction));
                    pawn = negotiator;
                    text = faction.Name;
                }
                __result = new DiaNode("HVMP_FactionGreeting_CommunicationsBlackout".Translate(text).AdjustedFor(pawn, "PAWN", true));
                DiaOption diaOptionOuties = new DiaOption("(" + "Disconnect".Translate() + ")")
                {
                    resolveTree = true
                };
                __result.options.Add(diaOptionOuties);
                return false;
            }
            if (fch != null)
            {
                HautsFactionComp_PeriodicBranchQuests pgq = fch.TryGetComp<HautsFactionComp_PeriodicBranchQuests>();
                if (pgq != null)
                {
                    pgq.tmpNegotiatorForInterfactionAid = negotiator;
                    pgq.interfactionAidTick = Find.TickManager.TicksGame;
                }
            }
            return true;
        }
        public static bool HVMPCanTradeNowPrefix(ref bool __result, Pawn_TraderTracker __instance)
        {
            Pawn pawn = GetInstanceField(typeof(Pawn_TraderTracker), __instance, "pawn") as Pawn;
            if (pawn.Faction != null && pawn.Faction.leader == pawn)
            {
                EBranchQuests gq = pawn.Faction.def.GetModExtension<EBranchQuests>();
                if (gq != null)
                {
                    __result = true;
                    return false;
                }
            }
            if (Find.World.gameConditionManager.ConditionIsActive(HVMPDefOf.HVMP_CommsBlackout))
            {
                __result = false;
                return false;
            }
            return true;
        }
        public static bool HVMPAddAllTradeablesPrefix(TradeDeal __instance)
        {
            if (TradeSession.trader is Pawn pawn && pawn.Faction != null && TradeSession.trader.TraderKind.HasModExtension<DonationTypeBeat>())
            {
                EBranchQuests gq = pawn.Faction.def.GetModExtension<EBranchQuests>();
                if (gq != null)
                {
                    MethodInfo AddToTradeables = typeof(TradeDeal).GetMethod("AddToTradeables", BindingFlags.NonPublic | BindingFlags.Instance);
                    List<Thing> enumerable = TradeSession.playerNegotiator.Map.listerThings.AllThings.Where((Thing x) => x.def.category == ThingCategory.Item && TradeUtility.PlayerSellableNow(x, pawn) && !x.Position.Fogged(x.Map) && (TradeSession.playerNegotiator.Map.areaManager.Home[x.Position] || x.IsInAnyStorage())).ToList();
                    foreach (Thing thing in enumerable)
                    {
                        if (TradeUtility.PlayerSellableNow(thing, TradeSession.trader))
                        {
                            AddToTradeables.Invoke(__instance, new object[] { thing, Transactor.Colony });
                        }
                    }
                    IEnumerable<IHaulSource> enumerable2 = TradeSession.playerNegotiator.Map.listerBuildings.AllColonistBuildingsOfType<IHaulSource>();
                    foreach (IHaulSource haulSource in enumerable2)
                    {
                        Building building2 = (Building)haulSource;
                        foreach (Thing thing2 in haulSource.GetDirectlyHeldThings())
                        {
                            AddToTradeables.Invoke(__instance, new object[] { thing2, Transactor.Colony });
                        }
                    }
                    if (TradeSession.TradeCurrency == TradeCurrency.Favor)
                    {
                        List<Tradeable> tradeables = (List<Tradeable>)__instance.GetType().GetField("tradeables", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
                        tradeables.Add(new Tradeable_RoyalFavor());
                        __instance.GetType().GetField("tradeables", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, tradeables);
                    }
                    return false;
                }
            }
            return true;
        }
        public static void HVMPResolveRaidStrategyPostfix(IncidentParms parms)
        {
            if (parms.raidArrivalModeForQuickMilitaryAid)
            {
                WorldComponent_HautsFactionComps hfc = Find.World.GetComponent<WorldComponent_HautsFactionComps>();
                Hauts_FactionCompHolder fch = hfc.FindCompsFor(parms.faction);
                if (fch != null)
                {
                    HautsFactionComp_PeriodicBranchQuests pgq = fch.TryGetComp<HautsFactionComp_PeriodicBranchQuests>();
                    if (pgq != null && pgq.tmpNegotiatorForInterfactionAid != null && pgq.interfactionAidTick == Find.TickManager.TicksGame)
                    {
                        if (HVMP_Utility.NegotiatorIsCosmopolitan(pgq.tmpNegotiatorForInterfactionAid) || HVMP_Utility.FactionIsCosmopolitan(parms.faction))
                        {
                            parms.faction.lastMilitaryAidRequestTick -= 60000;
                        }
                    }
                }
            }
        }
        public static bool HVMPTryExecutePrefix(IncidentWorker __instance, IncidentParms parms)
        {
            if (__instance.def == IncidentDefOf.TraderCaravanArrival || __instance.def == IncidentDefOf.OrbitalTraderArrival)
            {
                WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
                if (wcbs != null)
                {
                    if (wcbs.tradeBlockages > 0)
                    {
                        wcbs.tradeBlockages--;
                        TaggedString letterLabel = "HVMP_NoTraderForYou".Translate();
                        TaggedString letterText = (__instance.def == IncidentDefOf.TraderCaravanArrival ? "HVMP_TraderBlockedCaravan".Translate() : "HVMP_TraderBlockedOrbital".Translate()) + "\n\n" + (wcbs.tradeBlockages == 0 ? "HVMP_TraderBlocksRemainingNone".Translate() : "HVMP_TraderBlocksRemaining".Translate(wcbs.tradeBlockages));
                        ChoiceLetter notification = LetterMaker.MakeLetter(
                        letterLabel, letterText, LetterDefOf.NegativeEvent, null, null, null, null);
                        Find.LetterStack.ReceiveLetter(notification, null);
                        return false;
                    }
                    if (Find.World.gameConditionManager.ConditionIsActive(HVMPDefOf.HVMP_CommsBlackout))
                    {
                        TaggedString letterLabel = "HVMP_NoTraderForYou".Translate();
                        TaggedString letterText = (__instance.def == IncidentDefOf.TraderCaravanArrival ? "HVMP_TraderBlockedCaravan".Translate() : "HVMP_TraderBlockedOrbital".Translate()) + "\n\n" + "HVMP_TraderBlocksCommsBlackout".Translate();
                        ChoiceLetter notification = LetterMaker.MakeLetter(
                        letterLabel, letterText, LetterDefOf.NegativeEvent, null, null, null, null);
                        Find.LetterStack.ReceiveLetter(notification, null);
                        return false;
                    }
                }
            }
            return true;
        }
        public static bool HVMPNotify_BuildingTookDamagePrefix(Building building)
        {
            if (building.def.HasModExtension<HVMP_ItsOkToHarmThis>())
            {
                return false;
            }
            return true;
        }
        public static bool HVMPNotify_MemberTookDamagePrefix(Pawn member)
        {
            if (member.kindDef.HasModExtension<HVMP_ItsOkToHarmThis>())
            {
                return false;
            }
            return true;
        }
        public static void HVMPQuestManager_AddPostfix(Quest quest)
        {
            WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
            if (wcbs != null)
            {
                for (int i = 0; i < quest.PartsListForReading.Count; i++)
                {
                    QuestPart_PaxMundi qppm = quest.GetFirstPartOfType<QuestPart_PaxMundi>();
                    if (qppm != null)
                    {
                        wcbs.qppms.Add(qppm);
                        break;
                    }
                }
            }
        }
        public static void HVMPTryAffectGoodwillWithPrefix(Faction __instance, Faction other, out int __state)
        {
            if (__instance == Faction.OfPlayerSilentFail && other != Faction.OfPlayerSilentFail)
            {
                __state = other.GoodwillWith(Faction.OfPlayerSilentFail);
            }
            else if (__instance != Faction.OfPlayerSilentFail && other == Faction.OfPlayerSilentFail)
            {
                __state = __instance.GoodwillWith(Faction.OfPlayerSilentFail);
            } else {
                __state = 0;
            }
        }
        public static void HVMPTryAffectGoodwillWithPostfix(Faction __instance, bool __result, Faction other, HistoryEventDef reason, int __state)
        {
            if (reason == null)
            {
                return;
            }
            if ((other == Faction.OfPlayerSilentFail || __instance == Faction.OfPlayerSilentFail))
            {
                Faction nonPlayerFaction = other != Faction.OfPlayerSilentFail ? other : (__instance != Faction.OfPlayerSilentFail ? __instance : null);
                if (nonPlayerFaction != null && !nonPlayerFaction.def.HasModExtension<EBranchQuests>() && !nonPlayerFaction.def.permanentEnemy && nonPlayerFaction.HasGoodwill && (reason == null || reason != HistoryEventDefOf.ReachNaturalGoodwill))
                {
                    int goodwillChange = nonPlayerFaction.GoodwillWith(Faction.OfPlayerSilentFail) - __state;
                    HVMP_Utility.PaxMundiInner(goodwillChange);
                }
            }
            if (__instance == Faction.OfPlayerSilentFail && ModsConfig.IdeologyActive)
            {
                if (reason == HistoryEventDefOf.RequestedTrader && other.lastTraderRequestTick == Find.TickManager.TicksGame)
                {
                    WorldComponent_HautsFactionComps hfc = Find.World.GetComponent<WorldComponent_HautsFactionComps>();
                    Hauts_FactionCompHolder fch = hfc.FindCompsFor(other);
                    if (fch != null)
                    {
                        HautsFactionComp_PeriodicBranchQuests pgq = fch.TryGetComp<HautsFactionComp_PeriodicBranchQuests>();
                        if (pgq != null && pgq.tmpNegotiatorForInterfactionAid != null)
                        {
                            if (HVMP_Utility.NegotiatorIsCosmopolitan(pgq.tmpNegotiatorForInterfactionAid) || HVMP_Utility.FactionIsCosmopolitan(other))
                            {
                                other.lastTraderRequestTick -= 96000;
                            }
                        }
                    }
                }
            }
        }
        public static void HVMPAffectRelationsOnAttackedPostfix(MapParent mapParent)
        {
            if (mapParent.Faction != null && mapParent.Faction.def.HasModExtension<EBranchQuests>())
            {
                foreach (Faction f in Find.FactionManager.AllFactionsVisible)
                {
                    if (f != mapParent.Faction && f.def.HasModExtension<EBranchQuests>())
                    {
                        FactionRelationKind playerRelationKind = f.PlayerRelationKind;
                        Faction.OfPlayer.TryAffectGoodwillWith(f, Faction.OfPlayer.GoodwillToMakeHostile(f), false, false, HistoryEventDefOf.AttackedSettlement, null);
                        TaggedString taggedString = "Hauts_AttackedAlliedFaction".Translate();
                        mapParent.Faction.TryAppendRelationKindChangedInfo(ref taggedString, playerRelationKind, mapParent.Faction.PlayerRelationKind, null);
                    }
                }
            }
        }
        public static bool HVMPCheckDefeatedPrefix(Settlement factionBase)
        {
            if (factionBase.Faction != null && factionBase.Faction.def.HasModExtension<EBranchQuests>())
            {
                return false;
            }
            return true;
        }
        public static void HVMPDoTendPostfix(Pawn doctor, Pawn patient, Medicine medicine)
        {
            if (medicine != null && medicine.def == HVMPDefOf.HVMP_BiofilmMedicine)
            {
                List<Hediff> remainingToTend = new List<Hediff>();
                List<Hediff> hediffs = patient.health.hediffSet.hediffs;
                for (int i = 0; i < hediffs.Count; i++)
                {
                    if (hediffs[i].TendableNow(false))
                    {
                        remainingToTend.Add(hediffs[i]);
                    }
                }
                foreach (Hediff h in remainingToTend)
                {
                    h.Tended(TendUtility.CalculateBaseTendQuality(doctor, patient, medicine.def), medicine.def.GetStatValueAbstract(StatDefOf.MedicalQualityMax, null));
                }
                Hediff hef = HediffMaker.MakeHediff(HVMPDefOf.HVMP_HostileEnvironmentFilm, patient);
                patient.health.AddHediff(hef,null);
                hef.Severity = 1f;
            }
        }
        public static void HVMPThirdTickEffectsPostfix()
        {
            List<WorldObject> wosToDestroy = new List<WorldObject>();
            foreach (WorldObject wo in Find.WorldObjects.AllWorldObjects)
            {
                if (wo.Faction != null && wo.Faction.def.HasModExtension<EBranchQuests>() && !(wo is BranchPlatform))
                {
                    wosToDestroy.Add(wo);
                }
            }
            foreach (WorldObject wo in wosToDestroy)
            {
                wo.Destroy();
            }
        }
        public static bool HVMP_Settlement_CanTradeNowPrefix(ref bool __result)
        {
            if (Find.World.gameConditionManager.ConditionIsActive(HVMPDefOf.HVMP_CommsBlackout))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
    [DefOf]
    public static class HVMPDefOf
    {
        static HVMPDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HVMPDefOf));
        }
        public static FactionDef HVMP_CommerceBranch;
        public static FactionDef HVMP_PaxBranch;
        public static FactionDef HVMP_RoverBranch;
        [MayRequireIdeology]
        public static FactionDef HVMP_ArchiveBranch;
        [MayRequireBiotech]
        public static FactionDef HVMP_EcosphereBranch;
        [MayRequireAnomaly]
        public static FactionDef HVMP_OccultBranch;

        public static GameConditionDef HVMP_CommsBlackout;
        [MayRequireIdeology]
        public static GameConditionDef HVMP_RevealingScanEffect;

        public static HediffDef HVMP_HostileEnvironmentFilm;
        public static HediffDef HVMP_TargetedPsychicSuppression;

        public static HistoryEventDef HVMP_CutTiesWithBranch;
        public static HistoryEventDef HVMP_SolicitedQuest;
        public static HistoryEventDef HVMP_RefreshedPermitCDs;
        public static HistoryEventDef HVMP_IgnoredQuest;
        public static HistoryEventDef HVMP_IngratiationAccepted;

        public static IncidentDef HVMP_RaidAres;
        [MayRequireBiotech]
        public static IncidentDef HVMP_MutantManhunterPack;
        [MayRequireAnomaly]
        public static IncidentDef HVMP_ShamblerAssault;

        public static JobDef HVMP_StudyQuestItem;
        [MayRequireBiotech]
        public static JobDef HVMP_InjectChargecellBattery;
        [MayRequireBiotech]
        public static JobDef HVMP_InjectChargecellMech;
        [MayRequireOdyssey]
        public static JobDef HVMP_InstallPTargeter;
        [MayRequireOdyssey]
        public static JobDef HVMP_InstallBSU;
        [MayRequireOdyssey]
        public static JobDef HVMP_AttachIngressor;

        [MayRequireIdeology]
        public static PawnKindDef HVMP_Anthropologist;

        [MayRequireIdeology]
        public static PreceptDef HVMP_InterfactionAidImproved;

        public static QuestScriptDef HVMP_BranchIntro;
        public static QuestScriptDef HVMP_BranchOutro;

        [MayRequireIdeology]
        public static SitePartDef HVMP_Shrine;

        public static ThingDef HVMP_DropPodOfFaction;
        public static ThingDef HVMP_DelayedPowerBeam;
        public static ThingDef HVMP_DatedAtlas;
        public static ThingDef HVMP_TunnelHiveSpawner;
        [MayRequireIdeology]
        public static ThingDef HVMP_AncientTerminal;
        [MayRequireOdyssey]
        public static ThingDef HVMP_EnterpriseSecurityCrate;
        [MayRequireOdyssey]
        public static ThingDef HVMP_BiofilmMedicine;
        public static FleckDef HVMP_BribeGlow;
        public static FleckDef HVMP_ScalpelBLAST;
        public static FleckDef HVMP_RepairGlow;
        public static FleckDef HVMP_DecryptGlow;
        public static FleckDef HVMP_QualityGlow;
        public static FleckDef HVMP_VaalOrNoBaals;

        [MayRequireIdeology]
        public static ThoughtDef HVMP_AnthroAnnoyance;

        public static WorldObjectDef HVMP_AtlasPoint;
        public static WorldObjectDef HVMP_OdysseyPoint;
        [MayRequireOdyssey]
        public static WorldObjectDef HVMP_BranchPlatform;
    }
    //world object to handle caravan-blocking
    public class WorldComponent_BranchStuff : WorldComponent
    {
        public WorldComponent_BranchStuff(World world) : base(world)
        {
            this.world = world;
        }
        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % 2500 == 0)
            {
                List<Settlement> settToRemove = new List<Settlement>();
                foreach (Settlement sett in Find.WorldObjects.Settlements)
                {
                    if (!(sett is BranchPlatform) && sett.Faction != null && sett.Faction.def.HasModExtension<EBranchQuests>())
                    {
                        settToRemove.Add(sett);
                    }
                }
                foreach (Settlement s in settToRemove)
                {
                    s.Destroy();
                }
            }
            if (this.lovecraftEventTimer > 0)
            {
                this.lovecraftEventTimer--;
            }
            if (this.newSettlementTick > 0)
            {
                this.newSettlementTick--;
            } else if (ModsConfig.OdysseyActive) {
                WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
                List<Faction> factionsToMakePlatformsFor = new List<Faction>();
                foreach (Faction f in Find.FactionManager.AllFactionsVisible)
                {
                    Hauts_FactionCompHolder fch = WCFC.FindCompsFor(f);
                    if (fch != null)
                    {
                        HautsFactionComp_PeriodicBranchQuests pbq = fch.TryGetComp<HautsFactionComp_PeriodicBranchQuests>();
                        if (pbq != null && pbq.isBranch)
                        {
                            int platforms = 0;
                            foreach (Settlement bpsh in Find.World.worldObjects.Settlements)
                            {
                                if (bpsh is BranchPlatform && bpsh.Faction != null && bpsh.Faction == f)
                                {
                                    platforms++;
                                }
                            }
                            if (platforms < HVMP_Mod.settings.maxPlatformsPerBranch)
                            {
                                factionsToMakePlatformsFor.Add(f);
                            }
                            if (f.defeated)
                            {
                                f.defeated = false;
                            }
                        }
                    }
                }
                foreach (Faction f2 in factionsToMakePlatformsFor)
                {
                    WorldObject worldObject = WorldObjectMaker.MakeWorldObject(HVMPDefOf.HVMP_BranchPlatform);
                    worldObject.SetFaction(f2);
                    worldObject.Tile = TileFinder.RandomSettlementTileFor(Find.WorldGrid.Orbit, f2, false, null);
                    INameableWorldObject nameableWorldObject = worldObject as INameableWorldObject;
                    if (nameableWorldObject != null)
                    {
                        nameableWorldObject.Name = SettlementNameGenerator.GenerateSettlementName(worldObject, null);
                    }
                    Find.WorldObjects.Add(worldObject);
                }
                this.newSettlementTick = (int)(HVMP_Mod.settings.makeNewBranchPlatformInterval*60000);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.tradeBlockages, "tradeBlockages", 0, false);
            Scribe_Values.Look<int>(ref this.lovecraftEventTimer, "lovecraftEventTimer", 0, false);
            Scribe_Values.Look<int>(ref this.newSettlementTick, "newSettlementTick", 100, false);
        }
        public int tradeBlockages;
        public int lovecraftEventTimer = 0;
        public int newSettlementTick = 100;
        public List<QuestPart_PaxMundi> qppms = new List<QuestPart_PaxMundi>();
    }
    //basis for branch properties
    public class EBranchQuests : DefModExtension
    {
        public EBranchQuests() { }
        public List<QuestScriptDef> quests;
        public TraderKindDef donationTraderKind;
        public bool tiedToAnomalyThreatFraction;
    }
    public class BranchQuestProps : DefModExtension
    {
        public BranchQuestProps() { }
        public FloatRange points;
        public bool needsPlayerMap;
    }
    public class DonationTypeBeat : DefModExtension
    {
        public DonationTypeBeat() { }
        public string donationString;
    }
    public class HautsFactionCompProperties_PeriodicBranchQuests : HautsFactionCompProperties
    {
        public HautsFactionCompProperties_PeriodicBranchQuests()
        {
            this.compClass = typeof(HautsFactionComp_PeriodicBranchQuests);
        }
        public IntRange initDelay;
    }
    public class HautsFactionComp_PeriodicBranchQuests : HautsFactionComp
    {
        public HautsFactionCompProperties_PeriodicBranchQuests Props
        {
            get
            {
                return (HautsFactionCompProperties_PeriodicBranchQuests)this.props;
            }
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
            EBranchQuests gq = this.ThisFaction.def.GetModExtension<EBranchQuests>();
            if (gq != null)
            {
                this.cooldownTicks = this.Props.initDelay.RandomInRange - Find.TickManager.TicksGame;
                this.commsQuestCooldownTicks = 0;
                this.isBranch = true;
                Pawn leader = this.ThisFaction.leader;
                if (leader != null)
                {
                    if (leader.royalty != null)
                    {
                        leader.royalty = new Pawn_RoyaltyTracker(leader);
                    }
                    if (leader.Faction.def.HasRoyalTitles)
                    {
                        leader.royalty.SetTitle(leader.Faction, leader.Faction.def.RoyalTitlesAllInSeniorityOrderForReading.Last(), false, false, false);
                    }
                }
            } else {
                this.cooldownTicks = -1;
                this.commsQuestCooldownTicks = -1;
            }
        }
        public override void CompPostTick()
        {
            base.CompPostTick();
            if (this.isBranch)
            {
                if (this.commsQuestCooldownTicks > 0)
                {
                    this.commsQuestCooldownTicks--;
                }
                if (this.cooldownTicks > 0)
                {
                    this.cooldownTicks--;
                } else {
                    this.DoCooldowns();
                    if (this.tiesEstablished)
                    {
                        if (this.ThisFaction.allowGoodwillRewards && this.ThisFaction.allowRoyalFavorRewards)
                        {
                            this.IssueQuest();
                        } else {
                            int ebgl = HVMP_Utility.ExpectationBasedGoodwillLoss(null, true, true, this.ThisFaction);
                            if (ebgl != 0)
                            {
                                Faction.OfPlayer.TryAffectGoodwillWith(this.ThisFaction, ebgl, true, true, HVMPDefOf.HVMP_IgnoredQuest, null);
                            }
                        }
                    } else if (!this.tieQuestOffered && this.AnomalyRequirementsMet()) {
                        this.IssueQuest(true);
                    }
                }
            }
        }
        public bool AnomalyRequirementsMet(bool onlyIfNotDisabled = false)
        {
            if (HVMP_Mod.settings.occultTiedToAnomalyActivityLevel)
            {
                return true;
            }
            if (ModsConfig.AnomalyActive)
            {
                AnomalyPlaystyleDef apsd = Find.Storyteller.difficulty.AnomalyPlaystyleDef;
                if (apsd != null && apsd.enableAnomalyContent)
                {
                    if (Find.Anomaly.HighestLevelReached > 0 || (onlyIfNotDisabled && !Find.Anomaly.GenerateMonolith))
                    {
                        return true;
                    }
                    EBranchQuests gq = this.ThisFaction.def.GetModExtension<EBranchQuests>();
                    if (gq != null && (!gq.tiedToAnomalyThreatFraction || Rand.Value <= Find.Anomaly.AnomalyThreatFractionNow))
                    {
                        return true;
                    }
                }
                return false;
            }
            return true;
        }
        public void DoCooldowns()
        {
            if (ModsConfig.OdysseyActive)
            {
                Faction tg = Find.FactionManager.OfTradersGuild;
                if (tg != null)
                {
                    this.ThisFaction.SetRelation(new FactionRelation
                    {
                        other = tg,
                        kind = FactionRelationKind.Ally
                    });
                }
            }
            Faction mech = Find.FactionManager.OfMechanoids;
            if (mech != null)
            {
                this.ThisFaction.SetRelation(new FactionRelation
                {
                    other = mech,
                    kind = FactionRelationKind.Hostile
                });
            }
            if (HVMP_Mod.settings.maximumChaosMode || !this.AnomalyRequirementsMet())
            {
                //this.cooldownTicks = 600;
                this.cooldownTicks = Rand.RangeInclusive(60000 * (int)HVMP_Mod.settings.minBranchQuestInterval, 60000 * (int)HVMP_Mod.settings.maxBranchQuestInterval);
            } else {
                WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
                foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
                {
                    Hauts_FactionCompHolder fch = WCFC.FindCompsFor(faction);
                    if (fch != null)
                    {
                        HautsFactionComp_PeriodicBranchQuests pgq = fch.TryGetComp<HautsFactionComp_PeriodicBranchQuests>();
                        if (pgq != null)
                        {
                            //pgq.cooldownTicks = 600;
                            pgq.cooldownTicks = Rand.RangeInclusive(60000 * (int)HVMP_Mod.settings.minBranchQuestInterval, 60000 * (int)HVMP_Mod.settings.maxBranchQuestInterval);
                        }
                    }
                }
            }
        }
        public void IssueQuest(bool intro = false)
        {
            if (Find.AnyPlayerHomeMap == null)
            {
                return;
            }
            EBranchQuests gq = this.ThisFaction.def.GetModExtension<EBranchQuests>();
            if (gq != null && this.AnomalyRequirementsMet())
            {
                if (intro)
                {
                    this.tieQuestOffered = true;
                    Slate slate = new Slate();
                    slate.Set<Faction>("faction", this.ThisFaction, false);
                    slate.Set<Thing>("asker", this.ThisFaction.leader, false);
                    Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(HVMPDefOf.HVMP_BranchIntro, slate);
                    if (!quest.hidden && quest.root.sendAvailableLetter)
                    {
                        QuestUtility.SendLetterQuestAvailable(quest);
                    }
                } else if (gq.quests != null) {
                    List<QuestScriptDef> qsds = new List<QuestScriptDef>();
                    Slate slate = new Slate();
                    slate.Set<Faction>("branchFaction", this.ThisFaction, false);
                    slate.Set<float>("points", HVMP_Utility.TryGetPoints(null), false);
                    slate.Set<Map>("map", null, false);
                    IIncidentTarget randomPlayerHomeMap = Find.RandomSurfacePlayerHomeMap;
                    if (randomPlayerHomeMap == null)
                    {
                        randomPlayerHomeMap = Find.RandomPlayerHomeMap;
                    }
                    foreach (QuestScriptDef qsd in gq.quests)
                    {
                        if (qsd.CanRun(slate, randomPlayerHomeMap ?? Find.World))
                        {

                            qsds.Add(qsd);
                        }
                    }
                    if (qsds.Count == 0)
                    {
                        Log.Error("HVMP_ErrorNoUsableBranchQuests".Translate(this.ThisFaction.NameColored));
                        return;
                    }
                    QuestScriptDef questDef = qsds.RandomElement();
                    Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(questDef, slate);
                    if (!quest.hidden && quest.root.sendAvailableLetter)
                    {
                        QuestUtility.SendLetterQuestAvailable(quest);
                    }
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<bool>(ref this.tieQuestOffered, "tieQuestOffered", false, false);
            Scribe_Values.Look<bool>(ref this.tiesEstablished, "tiesEstablished", false, false);
            Scribe_Values.Look<int>(ref this.cooldownTicks, "cooldownTicks", 0, false);
            Scribe_Values.Look<int>(ref this.commsQuestCooldownTicks, "commsQuestCooldownTicks", 0, false);
            Scribe_Values.Look<bool>(ref this.isBranch, "isBranch", false, false);
            Scribe_Values.Look<int>(ref this.interfactionAidTick, "interfactionAidTick", 0, false);
        }
        public bool tieQuestOffered;
        public bool tiesEstablished;
        public int cooldownTicks;
        public int commsQuestCooldownTicks;
        public bool isBranch;
        public Pawn tmpNegotiatorForInterfactionAid;
        public int interfactionAidTick;
    }
    public class QuestNode_GetFactionDesc : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return Find.FactionManager.AllFactionsListForReading.Any((Faction f) => f == QuestGen.slate.Get<Faction>("faction", null, false));
        }
        protected override void RunInt()
        {
            QuestGen.slate.Set<string>(this.storeAs.GetValue(QuestGen.slate), QuestGen.slate.Get<Faction>("faction", null, false).def.description, false);
        }
        [NoTranslate]
        public SlateRef<string> storeAs;
    }
    public class QuestNode_EstablishBranchTies : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            QuestPart_EstablishBranchTies qpebt = new QuestPart_EstablishBranchTies();
            qpebt.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
            qpebt.faction = slate.Get<Faction>("faction",null,false);
            QuestGen.quest.AddPart(qpebt);
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
    }
    public class QuestPart_EstablishBranchTies : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal && faction != null && !faction.defeated)
            {
                WorldComponent_HautsFactionComps wcfc = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
                if (wcfc != null)
                {
                    Hauts_FactionCompHolder fch = wcfc.FindCompsFor(this.faction);
                    if (fch != null)
                    {
                        HautsFactionComp_PeriodicBranchQuests pbq = fch.TryGetComp<HautsFactionComp_PeriodicBranchQuests>();
                        if (pbq != null)
                        {
                            pbq.tiesEstablished = true;
                            Slate slate = new Slate();
                            slate.Set<Faction>("faction", this.faction, false);
                            slate.Set<Thing>("asker", this.faction.leader, false);
                            Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(HVMPDefOf.HVMP_BranchOutro, slate);
                        }
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_References.Look<Faction>(ref this.faction, "faction", false);
        }
        public string inSignal;
        public Faction faction;
    }
    public class QuestNode_ForsakeBranchTies : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            QuestPart_ForsakeBranchTies qpfbt = new QuestPart_ForsakeBranchTies();
            qpfbt.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
            qpfbt.faction = slate.Get<Faction>("faction", null, false);
            QuestGen.quest.AddPart(qpfbt);
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
    }
    public class QuestPart_ForsakeBranchTies : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal && faction != null && !faction.defeated)
            {
                WorldComponent_HautsFactionComps wcfc = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
                if (wcfc != null)
                {
                    Hauts_FactionCompHolder fch = wcfc.FindCompsFor(this.faction);
                    if (fch != null)
                    {
                        HautsFactionComp_PeriodicBranchQuests pbq = fch.TryGetComp<HautsFactionComp_PeriodicBranchQuests>();
                        if (pbq != null)
                        {
                            pbq.tiesEstablished = false;
                            pbq.tieQuestOffered = false;
                        }
                    }
                }
                Faction.OfPlayer.TryAffectGoodwillWith(this.faction, -100, true, true, HVMPDefOf.HVMP_CutTiesWithBranch, null);
                if (Faction.OfPlayer.RelationKindWith(this.faction) != FactionRelationKind.Hostile)
                {
                    Faction.OfPlayer.TryAffectGoodwillWith(this.faction, -100, true, true, HVMPDefOf.HVMP_CutTiesWithBranch, null);
                }
                foreach (Map m in Find.Maps)
                {
                    List<Thing> toDestroy = new List<Thing>();
                    List<Pawn> toFlee = new List<Pawn>();
                    foreach (Thing t in m.spawnedThings)
                    {
                        if (t.Faction != null && t.Faction == this.faction)
                        {
                            if (t is Pawn p) {
                                p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee,null,true);
                            } else if (t.def.HasModExtension<HVMP_ItsOkToHarmThis>()) {
                                toDestroy.Add(t);
                            }
                        }
                    }
                    foreach (Thing t in toDestroy)
                    {
                        t.Destroy();
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_References.Look<Faction>(ref this.faction, "faction", false);
        }
        public string inSignal;
        public Faction faction;
    }
    //branch settlements
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
        public BranchPlatform()
        {

        }
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
                if (f.Color.r/f.Color.g >= 2f)
                {
                    fr += 0.25f;
                } else if (f.Color.r/f.Color.g >= 1.25f) {
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
    public class GenStep_BigassPawnLoot : GenStep
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
                Lord lord = LordMaker.MakeNewLord(faction, new LordJob_DefendBase(faction, cellRect.CenterCell, 25000, false), map, null);
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
                    Lord lord2 = lord;
                    PawnGroupKindDef settlement = PawnGroupKindDefOf.Settlement;
                    MapGenUtility.GeneratePawns(map, cellRect2, faction2, lord2, settlement, pawnGroupMakerParms, null, null, null, this.requiresRoof);
                    pawnGroupMakerParms.points = this.pointsPerPawnGen.RandomInRange;
                    CellRect cellRect3 = cellRect;
                    PawnGroupKindDef settlement2 = PawnGroupKindDefOf.Settlement_RangedOnly;
                    MapGenUtility.GeneratePawns(map, cellRect3, faction2, lord2, settlement2, pawnGroupMakerParms, null, null, null, this.requiresRoof);
                }
                int combMulti = (int)Math.Ceiling(HVMP_Mod.settings.platformDefenderScale);
                for (int i = 0; i < combMulti; i++)
                {
                    pawnGroupMakerParms.points = this.pointsPerPawnGen.RandomInRange;
                    CellRect cellRect2 = cellRect;
                    Faction faction2 = faction;
                    Lord lord2 = lord;
                    PawnGroupKindDef settlement = PawnGroupKindDefOf.Combat;
                    MapGenUtility.GeneratePawns(map, cellRect2, faction2, lord2, settlement, pawnGroupMakerParms, null, null, null, this.requiresRoof);
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
    public class RoomContents_HVMP_RewardVault : RoomContentsWorker
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints = null)
        {
            RoomGenUtility.FillWithPadding(ThingDefOf.AncientCryptosleepCasket, RoomContents_HVMP_RewardVault.LootRange.RandomInRange, room, map, new Rot4?(Rot4.South), null, null, 2, null, false, false, null, null);
            base.FillRoom(map, room, faction, threatPoints);
            foreach (IntVec3 iv3 in room.Cells)
            {
                List<Thing> things = iv3.GetThingList(map);
                if (!things.NullOrEmpty())
                {
                    for (int i = things.Count - 1; i >= 0; i--)
                    {
                        Thing thing = things[i];
                        if (thing.def == ThingDefOf.AncientCryptosleepCasket)
                        {
                            thing.Destroy();
                            Thing thing2 = ThingMaker.MakeThing(HVMPDefOf.HVMP_EnterpriseSecurityCrate, null);
                            thing2.SetFactionDirect(faction);
                            GenSpawn.Spawn(thing2, iv3, map, Rot4.South, WipeMode.Vanish, false, false);
                        }
                    }
                }
            }
        }
        private static readonly IntRange LootRange = new IntRange(2, 4);
    }
    public class HackRewardDef : Def
    {
        public HackRewardDef()
        {

        }
        public List<ThingDefCountClass> itemsToDrop;
        public float baseChance = 0.1f;
        public FactionDef guaranteedForFaction;
    }
    public class CompProperties_CreateRewardsForHack : CompProperties
    {
        public CompProperties_CreateRewardsForHack()
        {
            this.compClass = typeof(CompCreateRewardsForHack);
        }
        public HackRewardDef guaranteedReward;
        public IntRange bonusRewardCount;
        public SoundDef soundOnUnlock;
    }
    public class CompCreateRewardsForHack : ThingComp
    {
        public CompProperties_CreateRewardsForHack Props
        {
            get
            {
                return (CompProperties_CreateRewardsForHack) this.props;
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            this.beenOpened = false;
            if (this.parent.Faction == null && this.parent.Map != null && this.parent.Map.ParentFaction != null)
            {
                this.parent.SetFaction(this.parent.Map.ParentFaction);
            }
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (!this.beenOpened)
            {
                CompHackable ch = this.parent.TryGetComp<CompHackable>();
                if (ch != null && ch.IsHacked)
                {
                    this.beenOpened = true;
                    if (this.parent.Spawned)
                    {
                        this.Props.soundOnUnlock.PlayOneShot(new TargetInfo(this.parent.Position, this.parent.Map, false));
                        if (this.Props.guaranteedReward != null)
                        {
                            this.SpawnReward(this.Props.guaranteedReward);
                        }
                        if (this.Props.bonusRewardCount != null)
                        {
                            int bonusRewards = this.Props.bonusRewardCount.RandomInRange;
                            if (bonusRewards > 0 && this.parent.Faction != null)
                            {
                                HackRewardDef factionHrd = this.GetFactionSpecificReward();
                                if (factionHrd != null)
                                {
                                    this.SpawnReward(factionHrd);
                                    bonusRewards--;
                                }
                            }
                            if (bonusRewards > 0)
                            {
                                for (int i = 0; i < bonusRewards; i++)
                                {
                                    HackRewardDef hrdextra;
                                    if (Rand.Chance(0.35f))
                                    {
                                        hrdextra = this.GetFactionSpecificReward();
                                    } else {
                                        hrdextra = DefDatabase<HackRewardDef>.AllDefsListForReading.RandomElementByWeight((HackRewardDef hrdish) => hrdish.baseChance);
                                    }
                                    SpawnReward(hrdextra);
                                }
                            }
                        }
                    }
                }
            }
        }
        public HackRewardDef GetFactionSpecificReward()
        {
            List<HackRewardDef> factionHrds = new List<HackRewardDef>();
            foreach (HackRewardDef hrdi in DefDatabase<HackRewardDef>.AllDefsListForReading)
            {
                if (hrdi.guaranteedForFaction != null && hrdi.guaranteedForFaction == this.parent.Faction.def)
                {
                    factionHrds.Add(hrdi);
                }
            }
            if (factionHrds.Count > 0)
            {
                return factionHrds.RandomElement();
            }
            return null;
        }
        public void SpawnReward(HackRewardDef hrd)
        {
            if (hrd != null && hrd.itemsToDrop != null)
            {
                foreach (ThingDefCountClass tdcc in hrd.itemsToDrop)
                {
                    Thing thing = ThingMaker.MakeThing(tdcc.thingDef, tdcc.stuff);
                    thing.stackCount = tdcc.count;
                    if (thing.TryGetComp(out CompQuality compQuality))
                    {
                        compQuality.SetQuality(tdcc.quality, new ArtGenerationContext?(ArtGenerationContext.Outsider));
                    }
                    if (thing.TryGetComp(out CompPowerBattery compBattery))
                    {
                        compBattery.SetStoredEnergyPct(1f);
                    }
                    if (this.parent.Faction != null && thing.TryGetComp(out CompTargetEffect_InstallPTargeter cteipt))
                    {
                        cteipt.faction = this.parent.Faction;
                        cteipt.freshFromVault = true;
                    }
                    if (thing.def.Minifiable)
                    {
                        MinifiedThing minifiedThing = thing.MakeMinified();
                        GenSpawn.Spawn(minifiedThing, this.parent.Position, this.parent.Map);
                    } else {
                        GenSpawn.Spawn(thing, this.parent.Position, this.parent.Map);
                    }
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.beenOpened, "beenOpened", false, false);
        }
        public bool beenOpened;
    }
    //branch settlement rewards
    public class CompProperties_UseEffect_MultipleChoicePallet : CompProperties_UseEffect
    {
        public CompProperties_UseEffect_MultipleChoicePallet()
        {
            this.compClass = typeof(CompUseEffect_MultipleChoicePallet);
        }
        public ThingCategoryDef thingCategoryWhitelist;
        public string allowedTradeTag;
        public int maxNumOptions = 10;
        public float unitMarketValueOutputCap = -1f;
        public float unitMassOutputCap = -1f;
        public float netMarketValueOutputCap = -1f;
        public float netMassOutputCap = -1f;
        public bool onlyHasStackableItems = true;
        public bool countCantExceedStackSize = true;
        public int netCountOutputCap = 100000;
        public Tradeability requiredTradeability = Tradeability.All;
        public QualityGenerator qualityGenerator = QualityGenerator.BaseGen;
    }
    public class CompUseEffect_MultipleChoicePallet : CompUseEffect
    {
        public CompProperties_UseEffect_MultipleChoicePallet Props
        {
            get
            {
                return (CompProperties_UseEffect_MultipleChoicePallet)this.props;
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            if (this.options.NullOrEmpty())
            {
                List<ThingDefCountClass> tdccs = new List<ThingDefCountClass>();
                int mno = this.Props.maxNumOptions;
                ThingCategoryDef category = this.Props.thingCategoryWhitelist;
                if (category != null)
                {
                    foreach (ThingDef td in DefDatabase<ThingDef>.AllDefsListForReading.InRandomOrder())
                    {
                        if (td.thingCategories != null && td.thingCategories.Contains(category) && this.ValidateOption(td))
                        {
                            tdccs.Add(this.CreateTDCC(td));
                            if (mno > 0)
                            {
                                mno--;
                                if (mno <= 0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                } else if (this.Props.allowedTradeTag != null) {
                    string tts = this.Props.allowedTradeTag;
                    foreach (ThingDef td in DefDatabase<ThingDef>.AllDefsListForReading.InRandomOrder())
                    {
                        if (td.tradeTags != null && td.tradeTags.Contains(tts) && this.ValidateOption(td))
                        {
                            tdccs.Add(this.CreateTDCC(td));
                            if (mno > 0)
                            {
                                mno--;
                                if (mno <= 0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                } else if (mno > 0) {
                    List<ThingDef> usedTds = new List<ThingDef>();
                    while (mno > 0)
                    {
                        ThingDef td = DefDatabase<ThingDef>.AllDefsListForReading.Where((ThingDef thing) => !usedTds.Contains(thing) && this.ValidateOption(thing)).RandomElement();
                        usedTds.Add(td);
                        tdccs.Add(this.CreateTDCC(td));
                        mno--;
                    }
                } else {
                    this.parent.Destroy();
                }
                this.options = tdccs;
            }
        }
        public bool ValidateOption(ThingDef thing)
        {
            return thing.category == ThingCategory.Item && thing.BaseMarketValue > 0f && !thing.HasComp<CompBook>() && (this.Props.unitMarketValueOutputCap <= 0f || thing.BaseMarketValue <= this.Props.unitMarketValueOutputCap) && (this.Props.unitMassOutputCap <= 0f || thing.BaseMass <= this.Props.unitMassOutputCap) && ((this.Props.requiredTradeability == Tradeability.All && thing.tradeability != Tradeability.None) || thing.tradeability == this.Props.requiredTradeability) && (!this.Props.onlyHasStackableItems || thing.stackLimit > 1) && !thing.destroyOnDrop;
        }
        public ThingDefCountClass CreateTDCC(ThingDef td)
        {
            ThingDefCountClass tdcc = new ThingDefCountClass();
            tdcc.thingDef = td;
            float mass = td.BaseMass;
            float mv = td.BaseMarketValue;
            tdcc.count = Math.Max(1,this.Props.netCountOutputCap);
            if (this.Props.netMarketValueOutputCap > 0f)
            {
                int limit = Mathf.FloorToInt(this.Props.netMarketValueOutputCap / mv);
                if (limit < tdcc.count)
                {
                    tdcc.count = limit;
                }
            }
            if (this.Props.netMassOutputCap > 0f)
            {
                int limit = Mathf.FloorToInt(this.Props.netMassOutputCap / mass);
                if (limit < tdcc.count)
                {
                    tdcc.count = limit;
                }
            }
            if (!this.Props.countCantExceedStackSize && tdcc.count > td.stackLimit)
            {
                tdcc.count = td.stackLimit;
            }
            tdcc.count = Math.Max(1, tdcc.count);
            tdcc.quality = QualityUtility.GenerateQuality(this.Props.qualityGenerator);
            if (td.MadeFromStuff)
            {
                tdcc.stuff = GenStuff.RandomStuffByCommonalityFor(td, TechLevel.Undefined);
            }
            return tdcc;
        }
        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            if (usedBy.Spawned)
            {
                if (this.options.NullOrEmpty())
                {
                    Log.Error("there were no valid items, somehow");
                    this.parent.Destroy();
                    return;
                }
                if (!usedBy.IsPlayerControlled)
                {
                    HVMP_Utility.OpenPallet(usedBy, this.parent, this.options.RandomElement());
                }
                MultipleChoicePalletWindow window = new MultipleChoicePalletWindow(usedBy, this);
                Find.WindowStack.Add(window);
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look<ThingDefCountClass>(ref this.options, "options", LookMode.Deep, Array.Empty<object>());
        }
        public List<ThingDefCountClass> options;
    }
    public class MultipleChoicePalletWindow : Window
    {
        public MultipleChoicePalletWindow(Pawn pawn, CompUseEffect_MultipleChoicePallet palletComp)
        {
            this.pawn = pawn;
            this.options.Clear();
            this.pallet = palletComp.parent;
            this.options = palletComp.options;
        }
        public override void PreOpen()
        {
            base.PreOpen();
            this.forcePause = true;
        }
        private float Height
        {
            get
            {
                return CharacterCardUtility.PawnCardSize(this.pawn).y + Window.CloseButSize.y + 4f + this.Margin * 2f;
            }
        }
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(500f, this.Height);
            }
        }
        public override void DoWindowContents(Rect inRect)
        {
            inRect.yMax -= 4f + Window.CloseButSize.y;
            Text.Font = GameFont.Small;
            Rect viewRect = new Rect(inRect.x, inRect.y, inRect.width * 0.7f, this.scrollHeight);
            Widgets.BeginScrollView(inRect, ref this.scrollPosition, viewRect, true);
            float num = 0f;
            Widgets.Label(0f, ref num, viewRect.width, "HVMP_PalletOpeningLabel".Translate().CapitalizeFirst().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), default(TipSignal));
            num += 14f;
            Listing_Standard listing_Standard = new Listing_Standard();
            Rect rect = new Rect(0f, num, inRect.width - 30f, 99999f);
            listing_Standard.Begin(rect);
            foreach (ThingDefCountClass tdcc in this.options)
            {
                bool flag = this.chosenTDCC == tdcc;
                bool flag2 = flag;
                string descString = tdcc.count + "x ";
                if (tdcc.thingDef.MadeFromStuff)
                {
                    descString += tdcc.stuff.label + " ";
                }
                descString += tdcc.thingDef.label + " ";
                if (tdcc.thingDef.HasComp<CompQuality>())
                {
                    descString += "(" + tdcc.quality + ")";
                }
                listing_Standard.CheckboxLabeled(descString, ref flag, null);
                if (flag != flag2)
                {
                    if (flag)
                    {
                        this.chosenTDCC = tdcc;
                    }
                }
            }
            listing_Standard.End();
            num += listing_Standard.CurHeight + 10f + 4f;
            if (Event.current.type == EventType.Layout)
            {
                this.scrollHeight = Mathf.Max(num, inRect.height);
            }
            Widgets.EndScrollView();
            Rect rect2 = new Rect(0f, inRect.yMax + 4f, inRect.width, Window.CloseButSize.y);
            AcceptanceReport acceptanceReport = this.CanClose();
            if (!acceptanceReport.Accepted)
            {
                TextAnchor anchor = Text.Anchor;
                GameFont font = Text.Font;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleRight;
                Rect rect3 = rect;
                rect3.xMax = rect2.xMin - 4f;
                Widgets.Label(rect3, acceptanceReport.Reason.Colorize(ColoredText.WarningColor));
                Text.Font = font;
                Text.Anchor = anchor;
            }
            if (Widgets.ButtonText(rect2, "OK".Translate(), true, true, true, null))
            {
                if (acceptanceReport.Accepted)
                {
                    HVMP_Utility.OpenPallet(this.pawn,this.pallet,chosenTDCC);
                    this.Close(true);
                } else {
                    Messages.Message(acceptanceReport.Reason, null, MessageTypeDefOf.RejectInput, false);
                }
            }
        }
        private AcceptanceReport CanClose()
        {
            if (this.chosenTDCC == null)
            {
                return "HVMP_Choose".Translate();
            }
            return AcceptanceReport.WasAccepted;
        }
        private Pawn pawn;
        private Thing pallet;
        private ThingDefCountClass chosenTDCC = null;
        private float scrollHeight;
        private Vector2 scrollPosition;
        private List<ThingDefCountClass> options = new List<ThingDefCountClass>();
    }
    public class CompProperties_TargetEffectInstallPTargeter : CompProperties
    {
        public CompProperties_TargetEffectInstallPTargeter()
        {
            this.compClass = typeof(CompTargetEffect_InstallPTargeter);
        }
        public HediffDef hediffDef;
        public BodyPartDef bodyPart;
        public bool canUpgrade;
        public bool requiresExistingHediff;
        public SoundDef soundOnUsed;
        public float standingGainFactorIfSecondhand;
    }
    public class CompTargetEffect_InstallPTargeter : CompTargetEffect
    {
        public CompProperties_TargetEffectInstallPTargeter Props
        {
            get
            {
                return (CompProperties_TargetEffectInstallPTargeter)this.props;
            }
        }
        public override string CompInspectStringExtra()
        {
            if (this.faction != null)
            {
                return "HVMP_GiveStandingFromFaction".Translate(this.StandingToGive, this.faction.NameColored);
            }
            return base.CompInspectStringExtra();
        }
        public int StandingToGive
        {
            get {
                float result = Math.Max(1, (int)Math.Ceiling(HVMP_Mod.settings.authorizerStandingGain));
                if (!this.freshFromVault)
                {
                    result *= this.Props.standingGainFactorIfSecondhand;
                }
                return (int)result;
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            if (this.faction == null)
            {
                if (this.parent.Map != null && this.parent.Map.ParentFaction != null && this.parent.Map.ParentFaction.def.HasModExtension<EBranchQuests>())
                {
                    this.faction = this.parent.Map.ParentFaction;
                    return;
                }
                this.faction = HVMP_Utility.AssignFallbackFactionToPermitTargeter();
            }
            this.freshFromVault = false;
        }
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (!user.IsColonistPlayerControlled)
            {
                return;
            }
            Job job = JobMaker.MakeJob(HVMPDefOf.HVMP_InstallPTargeter, target, this.parent);
            job.count = 1;
            job.playerForced = true;
            user.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look<Faction>(ref this.faction, "faction", false);
            Scribe_Values.Look<bool>(ref this.freshFromVault, "freshFromVault", false, false);
        }
        public Faction faction;
        public bool freshFromVault;
    }
    public class JobDriver_InstallPTargeter : JobDriver
    {
        private Pawn TargetPawn
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
            return this.pawn.Reserve(this.TargetPawn, this.job, 1, -1, null, errorOnFailed, false) && this.pawn.Reserve(this.Item, this.job, 1, -1, null, errorOnFailed, false);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false, true, false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.A);
            Toil toil = Toils_General.WaitWith(TargetIndex.A, 240, false, true, false, TargetIndex.A, PathEndMode.Touch);
            toil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return toil;
            yield return Toils_General.Do(new Action(this.Install));
            yield break;
        }
        private void Install()
        {
            CompTargetEffect_InstallPTargeter cteipt = this.Item.TryGetComp<CompTargetEffect_InstallPTargeter>();
            if (cteipt == null || cteipt.faction == null || this.TargetPawn.royalty == null)
            {
                return;
            }
            BodyPartRecord bodyPartRecord = this.TargetPawn.RaceProps.body.GetPartsWithDef(cteipt.Props.bodyPart).FirstOrFallback(null);
            if (bodyPartRecord == null)
            {
                return;
            }
            Faction f = cteipt.faction;
            if (f == null)
            {
                f = HVMP_Utility.AssignFallbackFactionToPermitTargeter();
            }
            Hediff firstHediffOfDef = this.TargetPawn.health.hediffSet.GetFirstHediffOfDef(cteipt.Props.hediffDef, false);
            if (firstHediffOfDef == null && !cteipt.Props.requiresExistingHediff)
            {
                Hediff newHediff = HediffMaker.MakeHediff(cteipt.Props.hediffDef,this.TargetPawn,bodyPartRecord);
                this.TargetPawn.health.AddHediff(newHediff);
                if (newHediff is Hediff_PTargeter ptarg)
                {
                    ptarg.faction = f;
                }
                this.TargetPawn.royalty.GainFavor(cteipt.faction,cteipt.StandingToGive);
            } else if (cteipt.Props.canUpgrade && firstHediffOfDef is Hediff_PTargeter ptargf && f == ptargf.faction) {
                this.TargetPawn.royalty.GainFavor(f, cteipt.StandingToGive);
            } else {
                return;
            }
            if (this.TargetPawn.Map == Find.CurrentMap && cteipt.Props.soundOnUsed != null)
            {
                cteipt.Props.soundOnUsed.PlayOneShot(SoundInfo.InMap(this.TargetPawn, MaintenanceType.None));
            }
            this.Item.SplitOff(1).Destroy(DestroyMode.Vanish);
        }
    }
    public class Hediff_PTargeter : Hediff
    {
        public override string Label
        { 
            get {
                string label = this.faction != null ? this.faction.Name + " " : "";
                label += base.Label;
                if (this.Severity < 1f)
                {
                    if (this.cooldownTicks < 2500)
                    {
                        label += "(" + this.cooldownTicks.ToStringSecondsFromTicks("F0") + ")";
                    }
                    label += "(" + this.cooldownTicks.ToStringTicksToPeriod(true, true, true, true, false) + ")";
                }
                return label;
            }
        }
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.cooldownTicks > 0)
            {
                this.Severity = 0.001f;
                this.cooldownTicks -= delta;
            } else {
                this.Severity = 1.001f;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Faction>(ref this.faction, "faction", false);
            Scribe_Values.Look<int>(ref this.cooldownTicks, "cooldownTicks", 0, false);
        }
        public Faction faction;
        public int cooldownTicks;
    }
    public class CompProperties_TargetEffectInstallBSU : CompProperties
    {
        public CompProperties_TargetEffectInstallBSU()
        {
            this.compClass = typeof(CompTargetEffect_InstallBSU);
        }
        public HediffDef hediffDef;
        public BodyPartDef bodyPart;
        public SoundDef soundOnUsed;
    }
    public class CompTargetEffect_InstallBSU : CompTargetEffect
    {
        public CompProperties_TargetEffectInstallBSU Props
        {
            get
            {
                return (CompProperties_TargetEffectInstallBSU)this.props;
            }
        }
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (!user.IsColonistPlayerControlled)
            {
                return;
            }
            Job job = JobMaker.MakeJob(HVMPDefOf.HVMP_InstallBSU, target, this.parent);
            job.count = 1;
            job.playerForced = true;
            user.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
        }
    }
    public class JobDriver_InstallBSU : JobDriver
    {
        private Pawn TargetPawn
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
            return this.pawn.Reserve(this.TargetPawn, this.job, 1, -1, null, errorOnFailed, false) && this.pawn.Reserve(this.Item, this.job, 1, -1, null, errorOnFailed, false);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false, true, false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.A);
            Toil toil = Toils_General.WaitWith(TargetIndex.A, 60, false, true, false, TargetIndex.A, PathEndMode.Touch);
            toil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return toil;
            yield return Toils_General.Do(new Action(this.Install));
            yield break;
        }
        private void Install()
        {
            CompTargetEffect_InstallBSU ibsu = this.Item.TryGetComp<CompTargetEffect_InstallBSU>();
            if (ibsu == null)
            {
                return;
            }
            BodyPartRecord bodyPartRecord = this.TargetPawn.RaceProps.body.GetPartsWithDef(ibsu.Props.bodyPart).FirstOrFallback(null);
            if (bodyPartRecord == null)
            {
                return;
            }
            Hediff firstHediffOfDef = this.TargetPawn.health.hediffSet.GetFirstHediffOfDef(ibsu.Props.hediffDef, false);
            if (firstHediffOfDef == null)
            {
                this.TargetPawn.health.AddHediff(ibsu.Props.hediffDef, bodyPartRecord, null, null);
                if (this.TargetPawn.Map == Find.CurrentMap && ibsu.Props.soundOnUsed != null)
                {
                    ibsu.Props.soundOnUsed.PlayOneShot(SoundInfo.InMap(this.TargetPawn, MaintenanceType.None));
                }
                this.Item.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
        }
    }
    public class CompProperties_GatepunchAbility : CompProperties_AbilityEffect
    {
        public CompProperties_GatepunchAbility()
        {
            this.compClass = typeof(CompAbilityEffect_GatepunchAbility);
        }
        public int chargeCost;
        public HediffDef gatepuncherHediff;
    }
    public class CompAbilityEffect_GatepunchAbility : CompAbilityEffect
    {
        public new CompProperties_GatepunchAbility Props
        {
            get
            {
                return (CompProperties_GatepunchAbility)this.props;
            }
        }
        public Hediff Gatepuncher
        {
            get
            {
                if (this.parent.pawn != null)
                {
                    return this.parent.pawn.health.hediffSet.GetFirstHediffOfDef(this.Props.gatepuncherHediff);
                }
                return null;
            }
        }
        public override bool CanCast
        {
            get
            {
                Hediff h = this.Gatepuncher;
                return h != null && h.Severity >= this.Props.chargeCost;
            }
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Hediff h = this.Gatepuncher;
            if (h != null)
            {
                h.Severity -= this.Props.chargeCost;
            }
        }
    }
    public class CompTargetable_BuildingOrItem : CompTargetable
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
            return new TargetingParameters
            {
                canTargetPawns = false,
                canTargetBuildings = true,
                canTargetItems = true,
                mapObjectTargetsMustBeAutoAttackable = false
            };
        }
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (target.Thing != null)
            {
                bool goAhead = false;
                CompHackable ch = target.Thing.TryGetComp<CompHackable>();
                if (ch != null && !ch.IsHacked)
                {
                    goAhead = true;
                }
                if (!goAhead)
                {
                    if (target.Thing is Building && target.Thing.def.useHitPoints)
                    {
                        goAhead = true;
                    }
                }
                if (!goAhead)
                {
                    return false;
                }
            }
            return base.ValidateTarget(target, showMessages);
        }
        public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
        {
            if (this.ValidateTarget(targetChosenByPlayer))
            {
                yield return targetChosenByPlayer;
            }
            yield break;
        }
    }
    public class CompProperties_TargetEffectIngression : CompProperties
    {
        public CompProperties_TargetEffectIngression()
        {
            this.compClass = typeof(CompTargetEffect_Ingression);
        }
        public DamageDef damageType;
        public int damageAmount;
        public float hackingPower;
    }
    public class CompTargetEffect_Ingression : CompTargetEffect
    {
        public CompProperties_TargetEffectIngression Props
        {
            get
            {
                return (CompProperties_TargetEffectIngression)this.props;
            }
        }
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (!user.IsColonistPlayerControlled)
            {
                return;
            }
            Job job = JobMaker.MakeJob(HVMPDefOf.HVMP_AttachIngressor, target, this.parent);
            job.count = 1;
            job.playerForced = true;
            user.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
        }
    }
    public class JobDriver_ApplyIngressor : JobDriver
    {
        private Thing TargetToIngress
        {
            get
            {
                return this.job.GetTarget(TargetIndex.A).Thing;
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
            return this.pawn.Reserve(this.TargetToIngress, this.job, 1, -1, null, errorOnFailed, false) && this.pawn.Reserve(this.Item, this.job, 1, -1, null, errorOnFailed, false);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false, true, false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.A);
            Toil toil = Toils_General.WaitWith(TargetIndex.A, 300, false, true, false, TargetIndex.A, PathEndMode.Touch);
            toil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return toil;
            yield return Toils_General.Do(new Action(this.Ingress));
            yield break;
        }
        private void Ingress()
        {
            CompTargetEffect_Ingression ctei = this.Item.TryGetComp<CompTargetEffect_Ingression>();
            if (ctei == null)
            {
                return;
            }
            CompHackable ch = this.TargetToIngress.TryGetComp<CompHackable>();
            if (ch != null && !ch.IsHacked)
            {
                ch.Hack(ctei.Props.hackingPower,null);
            } else if (this.TargetToIngress is Building && this.TargetToIngress.def.useHitPoints) {
                this.TargetToIngress.TakeDamage(new DamageInfo(ctei.Props.damageType,ctei.Props.damageAmount));
            }
            this.Item.SplitOff(1).Destroy(DestroyMode.Vanish);
        }
    }
    public class Hediff_Ptuning : HediffWithComps
    {
        public override string LabelBase
        {
            get
            {
                HediffComp_PTuner hcpt = this.TryGetComp<HediffComp_PTuner>();
                if (hcpt != null && !hcpt.isActive)
                {
                    return hcpt.Props.permanentLabel;
                }
                return base.LabelBase;
            }
        }
    }
    public class HediffCompProperties_PTuner : HediffCompProperties
    {
        public HediffCompProperties_PTuner()
        {
            this.compClass = typeof(HediffComp_PTuner);
        }
        public float sensPerHour;
        public float MTBhoursToGoodEvent;
        public string permanentLabel;
        public float postEventRecoveryPerHour;
    }
    public class HediffComp_PTuner : HediffComp
    {
        public HediffCompProperties_PTuner Props
        {
            get
            {
                return (HediffCompProperties_PTuner)this.props;
            }
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
            this.isActive = true;
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(60, delta))
            {
                if (this.isActive)
                {
                    if (this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon)
                    {
                        this.parent.Severity += this.Props.sensPerHour * 0.024f;
                        if (Rand.MTBEventOccurs(this.Props.MTBhoursToGoodEvent, 2500f, 60))
                        {
                            HautsUtility.MakeGoodEvent(this.Pawn);
                            Messages.Message("HVMP_ProbabilityTunerActivated".Translate().CapitalizeFirst().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve(), this.Pawn, MessageTypeDefOf.PositiveEvent, true);
                            this.isActive = false;
                        }
                    } else {
                        this.isActive = false;
                    }
                } else {
                    this.parent.Severity -= this.Props.postEventRecoveryPerHour * 0.024f;
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<bool>(ref this.isActive, "isActive", false, false);
        }
        public bool isActive = true;
    }
    //permit mechanics: workers
    public class ScalingDisabledDescription : DefModExtension
    {
        public ScalingDisabledDescription() { }
        public string extraString;
    }
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_DropFactionThing : RoyalTitlePermitWorker_Targeted
    {
        public override void DrawHighlight(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(this.caller.Position, this.def.royalAid.targetingRange, Color.white, null);
            if (this.def.royalAid.radius > 0f)
            {
                GenDraw.DrawRadiusRing(target.Cell, this.def.royalAid.radius, Color.white, null);
            }
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.CallResources(target.Cell);
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
            if (HVMP_Utility.ProprietaryFillAidOption(this,pawn, faction, ref text, out free))
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
            this.targetingParameters.validator = (TargetInfo target) => (this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(caller.Position) <= this.def.royalAid.targetingRange) && !target.Cell.Fogged(map) && DropCellFinder.CanPhysicallyDropInto(target.Cell, map, true, true) && target.Cell.GetTerrain(map).affordances.Contains(DefDatabase<TerrainAffordanceDef>.GetNamed("Light"));
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
                    List<Thing> dummyThingForCompat = new List<Thing>();
                    Thing thing = ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel,null);
                    thing.stackCount = 1;
                    dummyThingForCompat.Add(thing);
                    if (dummyThingForCompat.Any())
                    {
                        ActiveTransporterInfo activeDropPodInfo = new ActiveTransporterInfo();
                        activeDropPodInfo.innerContainer.TryAddRangeOrTransfer(dummyThingForCompat, true, false);
                        ActiveTransporter activeDropPod = (ActiveTransporter)ThingMaker.MakeThing(((faction != null) ? faction.def.dropPodActive : null) ?? ThingDefOf.ActiveDropPod, null);
                        activeDropPod.Contents = activeDropPodInfo;
                        dp.innerContainer.TryAdd(activeDropPod);
                        dp.thing = tdcc.thingDef;
                        dp.faction = this.faction;
                        GenSpawn.Spawn(dp, intVec, this.map, WipeMode.Vanish);
                    }
                }
            }
            Messages.Message("MessagePermitTransportDrop".Translate(this.faction.Named("FACTION")), new LookTargets(cell, this.map), MessageTypeDefOf.NeutralEvent, true);
            this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
            }
            HVMP_Utility.DoPTargeterCooldown(this.faction, caller, this);
        }
        private Faction faction;
    }
    public class CompProperties_FactionColoredTeamJimmy : CompProperties_FactionColored
    {
        public CompProperties_FactionColoredTeamJimmy()
        {
            this.compClass = typeof(CompFactionColored_TeamJimmy);
        }
    }
    public class CompFactionColored_TeamJimmy : CompFactionColored
    {
        public new CompProperties_FactionColoredTeamJimmy Props
        {
            get
            {
                return (CompProperties_FactionColoredTeamJimmy)this.props;
            }
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.parent.IsHashIntervalTick(5) && this.parent.Spawned)
            {
                if (!this.jimmiedYet)
                {
                    if (this.team == null)
                    {
                        this.team = this.parent.Faction;
                        this.parent.SetFaction(Faction.OfPlayerSilentFail);
                    } else {
                        this.jimmiedYet = true;
                        this.parent.SetFaction(this.team);
                    }
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look<Faction>(ref this.team, "team", false);
            Scribe_Values.Look<bool>(ref this.jimmiedYet, "jimmiedYet", false, false);
        }
        public Faction team;
        public bool jimmiedYet;
    }
    public class DropPodIncomingOfFaction : DropPodIncoming
    {
        protected override void SpawnThings()
        {
            if (placeDirect)
            {
                GenSpawn.Spawn(thing,base.Position,base.Map).SetFactionDirect(this.faction);
            } else {
                ThingDef thingDef = GenStuff.RandomStuffFor(thing);
                Thing t = ThingMaker.MakeThing(thing, thingDef);
                GenPlace.TryPlaceThing(t, base.Position, base.Map, ThingPlaceMode.Near, null, null, null, 1);
                t.SetFactionDirect(this.faction);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<ThingDef>(ref this.thing, "thing");
            Scribe_References.Look<Faction>(ref this.faction, "faction", false);
            Scribe_Values.Look<bool>(ref this.placeDirect, "placeDirect", false, false);
        }
        public ThingDef thing;
        public Faction faction;
        public bool placeDirect;
    }
    public class HVMP_ItsOkToHarmThis : DefModExtension
    {
        public HVMP_ItsOkToHarmThis() { }
    }
    public class RoyalTitlePermitWorker_Investment : RoyalTitlePermitWorker_MultiplyItemStack
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
    public class RoyalTitlePermitWorker_DROC_PTargFriendly : RoyalTitlePermitWorker_DropResourcesOfCategory
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
    public class RoyalTitlePermitWorker_DROCSeniorityScaling_PTargFriendly : RoyalTitlePermitWorker_DROC_PTargFriendly
    {
        public override int ItemStackCount(PermitMoreEffects pme, Pawn caller)
        {
            int result = base.ItemStackCount(pme, caller);
            if (this.faction != null)
            {
                float curSeniority = HVMP_Mod.settings.permitsScaleBySeniority ? caller.royalty.GetCurrentTitleInFaction(this.faction).def.seniority : this.def.minTitle.seniority;
                float divisor = pme.phenomenonCount > 0 ? pme.phenomenonCount : 100f;
                int seniority = Math.Max((int)(curSeniority / divisor), 1);
                result *= seniority;
            }
            return result;
        }
    }
    public class RoyalTitlePermitWorker_GenerateQuest_PTargFriendly : RoyalTitlePermitWorker_GenerateQuest
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
    public class RoyalTitlePermitWorker_GenerateQuestSeniorityScaling_PTargFriendly : RoyalTitlePermitWorker_GenerateQuest_PTargFriendly
    {
        public override int NumQuestsToGenerate(PermitMoreEffects pme, Pawn caller, Faction faction)
        {
            int result = base.NumQuestsToGenerate(pme, caller, faction);
            if (faction != null)
            {
                float curSeniority = HVMP_Mod.settings.permitsScaleBySeniority ? caller.royalty.GetCurrentTitleInFaction(faction).def.seniority : this.def.minTitle.seniority;
                int seniority = Math.Max((int)(curSeniority / 100f), 1);
                result += seniority;
            }
            return result;
        }
    }
    public class RoyalTitlePermitWorker_GiveHediffs_PTargFriendly : RoyalTitlePermitWorker_GiveHediffs
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
    public class RoyalTitlePermitWorker_DropResourcesStuff_PTargFriendly : RoyalTitlePermitWorker_DropResourcesStuff
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
    public class RoyalTitlePermitWorker_DropResourcesStuffSeniorityScaling_PTargFriendly : RoyalTitlePermitWorker_DropResourcesStuff_PTargFriendly
    {
        public override int ItemStackCount(ThingDefCountClass tdcc, PermitMoreEffects pme, Pawn caller)
        {
            int result = base.ItemStackCount(tdcc, pme, caller);
            if (this.faction != null)
            {
                float curSeniority = HVMP_Mod.settings.permitsScaleBySeniority ? caller.royalty.GetCurrentTitleInFaction(this.faction).def.seniority : this.def.minTitle.seniority;
                float divisor = (pme != null && pme.phenomenonCount > 0) ? pme.phenomenonCount : 100f;
                int seniority = Math.Max((int)(curSeniority / divisor), 1);
                result *= seniority;
            }
            return result;
        }
    }
    public class RoyalTitlePermitWorker_CauseCondition_PTargFriendly : RoyalTitlePermitWorker_CauseCondition
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
    public class RoyalTitlePermitWorker_DropBook_PTargFriendly : RoyalTitlePermitWorker_DropBook
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
    public class RoyalTitlePermitWorker_DropBookSeniorityScaling_PTargFriendly : RoyalTitlePermitWorker_DropBook_PTargFriendly
    {
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
    public class RoyalTitlePermitWorker_DropPawns_PTargFriendly : RoyalTitlePermitWorker_DropPawns
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
    public class RoyalTitlePermitWorker_CallShuttlePTargFriendly : RoyalTitlePermitWorker_CallShuttle
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.CallShuttle(target.Cell);
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            MapGeneratorDef generatorDef = map.generatorDef;
            if (generatorDef != null && generatorDef.isUnderground)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn,faction) == null)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            string text = this.def.LabelCap + ": ";
            Action action = null;
            bool free;
            if (HVMP_Utility.ProprietaryFillAidOption(this,pawn, faction, ref text, out free))
            {
                action = delegate
                {
                    this.BeginCallShuttle(pawn, pawn.MapHeld, faction, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            string text;
            if (!base.FillCaravanAidOption(pawn, faction, out text, out this.free, out bool flag))
            {
                yield break;
            }
            Command_Action command_Action = new Command_Action
            {
                defaultLabel = this.def.LabelCap + " (" + pawn.LabelShort + ")",
                defaultDesc = text,
                icon = RoyalTitlePermitWorker_CallShuttlePTargFriendly.CommandTex,
                action = delegate
                {
                    this.CallShuttleToCaravan(pawn, faction, this.free);
                }
            };
            if (pawn.MapHeld != null && pawn.MapHeld.generatorDef.isUnderground)
            {
                command_Action.Disable("CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")));
            }
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
        private void BeginCallShuttle(Pawn caller, Map map, Faction faction, bool free)
        {
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetSelf = false;
            this.targetingParameters.canTargetPawns = false;
            this.targetingParameters.canTargetFires = false;
            this.targetingParameters.canTargetBuildings = true;
            this.targetingParameters.canTargetItems = true;
            this.caller = caller;
            this.map = map;
            this.calledFaction = faction;
            this.free = free;
            float rangeActual = base.RangeClamped;
            this.targetingParameters.validator = (TargetInfo target) => rangeActual <= 0f || target.Cell.DistanceTo(caller.Position) <= rangeActual;
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void CallShuttle(IntVec3 landingCell)
        {
            if (this.caller.Spawned)
            {
                Thing thing = ThingMaker.MakeThing(ThingDefOf.Shuttle, null);
                CompShuttle compShuttle = thing.TryGetComp<CompShuttle>();
                compShuttle.permitShuttle = true;
                compShuttle.acceptChildren = true;
                TransportShip transportShip = TransportShipMaker.MakeTransportShip(TransportShipDefOf.Ship_Shuttle, null, thing);
                transportShip.ArriveAt(landingCell, this.map.Parent);
                transportShip.AddJobs(new ShipJobDef[]
                {
                    ShipJobDefOf.WaitForever,
                    ShipJobDefOf.Unload_Destination,
                    ShipJobDefOf.FlyAway
                });
                this.caller.royalty.GetPermit(this.def, this.calledFaction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(this.calledFaction, this.def.royalAid.favorCost);
                }
                HVMP_Utility.DoPTargeterCooldown(this.calledFaction, this.caller, this);
            }
        }
        private void CallShuttleToCaravan(Pawn caller, Faction faction, bool free)
        {
            MethodInfo CSTS = typeof(RoyalTitlePermitWorker_CallShuttle).GetMethod("CallShuttleToCaravan", BindingFlags.NonPublic | BindingFlags.Instance);
            CSTS.Invoke(this, new object[] { caller, faction, free});
            HVMP_Utility.DoPTargeterCooldown(faction, caller, this);
        }
        private Faction calledFaction;
        private static readonly Texture2D CommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CallShuttle", true);
    }
    public class RoyalTitlePermitWorker_CallLaborersPTargFriendly : RoyalTitlePermitWorker_CallLaborers
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.CallLaborers(target.Cell);
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            string text;
            if (this.AidDisabled_NewTemp(map, pawn, faction, out text, true))
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + text, null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text2 = this.def.LabelCap + " (" + "CommandCallLaborersNumLaborers".Translate(this.def.royalAid.pawnCount) + "): ";
            bool free;
            if (base.FillAidOption(pawn, faction, ref text2, out free))
            {
                action = delegate
                {
                    this.BeginCallLaborers(pawn, map, faction, free);
                };
            }
            yield return new FloatMenuOption(text2, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        protected override bool AidDisabled_NewTemp(Map map, Pawn pawn, Faction faction, out string reason, bool temperatureMatters = true)
        {
            if (map.generatorDef.isUnderground)
            {
                reason = "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION"));
                return true;
            }
            if (this.def.layerBlacklist.Contains(pawn.MapHeld.Tile.LayerDef))
            {
                reason = "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION"));
                return true;
            }
            if (temperatureMatters && !this.TemperatureIsAcceptable(map, faction))
            {
                reason = "BadTemperature".Translate();
                return true;
            }
            reason = null;
            return false;
        }
        private void BeginCallLaborers(Pawn pawn, Map map, Faction faction, bool free)
        {
            if (faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null)
            {
                return;
            }
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetSelf = false;
            this.targetingParameters.canTargetPawns = false;
            this.targetingParameters.canTargetFires = false;
            this.targetingParameters.canTargetBuildings = true;
            this.targetingParameters.canTargetItems = true;
            this.caller = pawn;
            this.map = map;
            this.calledFaction = faction;
            this.free = free;
            float rangeActual = base.RangeClamped;
            this.targetingParameters.validator = (TargetInfo target) => rangeActual <= 0f || target.Cell.DistanceTo(this.caller.Position) <= rangeActual;
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void CallLaborers(IntVec3 landingCell)
        {
            QuestScriptDef permit_CallLaborers = QuestScriptDefOf.Permit_CallLaborers;
            Slate slate = new Slate();
            slate.Set<Map>("map", this.map, false);
            slate.Set<int>("laborersCount", this.def.royalAid.pawnCount, false);
            slate.Set<Faction>("permitFaction", this.calledFaction, false);
            slate.Set<PawnKindDef>("laborersPawnKind", this.def.royalAid.pawnKindDef, false);
            slate.Set<float>("laborersDurationDays", this.def.royalAid.aidDurationDays, false);
            slate.Set<IntVec3>("landingCell", landingCell, false);
            QuestUtility.GenerateQuestAndMakeAvailable(permit_CallLaborers, slate);
            this.caller.royalty.GetPermit(this.def, this.calledFaction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(this.calledFaction, this.def.royalAid.favorCost);
            }
            HVMP_Utility.DoPTargeterCooldown(this.calledFaction, caller, this);
        }
        private Faction calledFaction;
    }
    public class RoyalTitlePermitWorker_OrbitalStrikePTargFriendly : RoyalTitlePermitWorker_OrbitalStrike
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.CallBombardment(target.Cell);
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            string text;
            if (this.AidDisabled_NewTemp(map, pawn, faction, out text, false))
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + text, null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            string text2 = this.def.LabelCap + ": ";
            Action action = null;
            bool free;
            if (base.FillAidOption(pawn, faction, ref text2, out free))
            {
                action = delegate
                {
                    this.BeginCallBombardment(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text2, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        protected override bool AidDisabled_NewTemp(Map map, Pawn pawn, Faction faction, out string reason, bool temperatureMatters = true)
        {
            if (map.generatorDef.isUnderground)
            {
                reason = "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION"));
                return true;
            }
            if (this.def.layerBlacklist.Contains(pawn.MapHeld.Tile.LayerDef))
            {
                reason = "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION"));
                return true;
            }
            if (temperatureMatters && !this.TemperatureIsAcceptable(map, faction))
            {
                reason = "BadTemperature".Translate();
                return true;
            }
            reason = null;
            return false;
        }
        private void BeginCallBombardment(Pawn caller, Faction faction, Map map, bool free)
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
            float rangeActual = base.RangeClamped;
            this.targetingParameters.validator = delegate (TargetInfo target)
            {
                if (rangeActual > 0f && target.Cell.DistanceTo(caller.Position) > rangeActual)
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
        private void CallBombardment(IntVec3 targetCell)
        {
            Bombardment bombardment = (Bombardment)GenSpawn.Spawn(ThingDefOf.Bombardment, targetCell, this.map, WipeMode.Vanish);
            bombardment.impactAreaRadius = this.def.royalAid.radius;
            bombardment.explosionRadiusRange = this.def.royalAid.explosionRadiusRange;
            bombardment.bombIntervalTicks = this.def.royalAid.intervalTicks;
            bombardment.randomFireRadius = 1;
            bombardment.explosionCount = this.def.royalAid.explosionCount;
            bombardment.warmupTicks = this.def.royalAid.warmupTicks;
            bombardment.instigator = this.caller;
            SoundDefOf.OrbitalStrike_Ordered.PlayOneShotOnCamera(null);
            this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
            }
            HVMP_Utility.DoPTargeterCooldown(this.faction, this.caller, this);
        }
        private Faction faction;
    }
    public class RoyalTitlePermitWorker_Retreat : RoyalTitlePermitWorker_TargetPawn
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return (pawn.HostileTo(this.CasterPawn.Faction) || pawn.HostileTo(this.CasterPawn)) && !pawn.InMentalState && pawn.Awake() && !pawn.DeadOrDowned && ((pawn.Faction != null && pawn.Faction.def.humanlikeFaction) || pawn.RaceProps.Humanlike);
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return HVMP_Utility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
            base.AffectPawnInner(pme, pawn, faction);
            if (!pawn.InMentalState && pawn.Awake() && !pawn.DeadOrDowned)
            {
                HVMP_Utility.ThrowBribeGlow(pawn.Position.ToVector3() + new Vector3(0.5f, 0f, 0.5f), this.map, 1.5f);
                pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, null, false, false, false, null, false, false, false);
            }
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            HVMP_Utility.DoPTargeterCooldown(faction, caller, this);
        }
    }
    public class RoyalTitlePermitWorker_Recruit : RoyalTitlePermitWorker_TargetPawn
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return pawn.IsPrisonerOfColony && !pawn.InMentalState && pawn.Awake() && !pawn.DeadOrDowned && pawn.guest.resistance >= float.Epsilon;
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return HVMP_Utility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
            base.AffectPawnInner(pme, pawn, faction);
            if (!pawn.InMentalState && pawn.Awake() && !pawn.DeadOrDowned && pawn.guest != null && pawn.guest.resistance >= float.Epsilon)
            {
                HVMP_Utility.ThrowBribeGlow(pawn.Position.ToVector3() + new Vector3(0.5f, 0f, 0.5f), this.map, 1.5f);
                pawn.guest.resistance += pme.extraNumber.RandomInRange;
            }
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            HVMP_Utility.DoPTargeterCooldown(faction, caller, this);
        }
    }
    public class RoyalTitlePermitWorker_Ingratiate : RoyalTitlePermitWorker_TargetPawn
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return pawn.Faction != null && this.CasterPawn.Faction != null && pawn.Faction != this.CasterPawn.Faction && !pawn.Faction.def.HasModExtension<EBranchQuests>() && !pawn.Faction.def.PermanentlyHostileTo(this.CasterPawn.Faction.def) && !pawn.InMentalState && pawn.Awake() && !pawn.DeadOrDowned && (pawn.Faction.def.humanlikeFaction || pawn.RaceProps.Humanlike);
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return HVMP_Utility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
            base.AffectPawnInner(pme, pawn, faction);
            if (!pawn.InMentalState && pawn.Awake() && !pawn.DeadOrDowned && pawn.Faction != null)
            {
                HVMP_Utility.ThrowBribeGlow(pawn.Position.ToVector3() + new Vector3(0.5f, 0f, 0.5f), this.map, 1.5f);
                this.CasterPawn.Faction.TryAffectGoodwillWith(pawn.Faction, (int)pme.extraNumber.RandomInRange, true, true, HVMPDefOf.HVMP_IngratiationAccepted, null);
            }
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            HVMP_Utility.DoPTargeterCooldown(faction, caller, this);
        }
    }
    public class RoyalTitlePermitWorker_OrbitalScalpel : RoyalTitlePermitWorker_Targeted
    {
        public AcceptanceReport IsValidThing(LocalTargetInfo lti)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                TaggedString error = pme.invalidTargetMessage.Translate();
                if (!lti.IsValid)
                {
                    return new AcceptanceReport(error);
                } else {
                    foreach (Thing t in lti.Cell.GetThingList(this.caller.Map))
                    {
                        if ((t is Pawn p && !p.IsPsychologicallyInvisible()) || (t is Building && t.def.useHitPoints && t.def.building.canBeDamagedByAttacks))
                        {
                            return AcceptanceReport.WasAccepted;
                        }
                    }
                }
                return new AcceptanceReport(error);
            }
            return new AcceptanceReport("Hauts_PMEMisconfig".Translate());
        }
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (target.IsValid && !HautsUtility.CanBeHitByAirToSurface(target.Cell, this.caller.Map, false))
            {
                if (showMessages)
                {
                    Messages.Message(this.def.LabelCap + ": " + "AbilityCannotHitTarget".Translate(), MessageTypeDefOf.RejectInput, true);
                }
                return false;
            }
            if (!base.CanHitTarget(target))
            {
                if (target.IsValid && showMessages)
                {
                    Messages.Message(this.def.LabelCap + ": " + "AbilityCannotHitTarget".Translate(), MessageTypeDefOf.RejectInput, true);
                }
                return false;
            }
            AcceptanceReport acceptanceReport = this.IsValidThing(target);
            if (!acceptanceReport.Accepted)
            {
                Messages.Message(acceptanceReport.Reason, new LookTargets(target.Cell, this.map), MessageTypeDefOf.RejectInput, false);
            }
            return acceptanceReport.Accepted;
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                bool checkBuildings = true;
                foreach (Thing t in target.Cell.GetThingList(this.caller.Map))
                {
                    if ((t is Pawn p && !p.IsPsychologicallyInvisible()))
                    {
                        this.InstantiateEffect(p, pme);
                        checkBuildings = false;
                        break;
                    }
                }
                if (checkBuildings)
                {
                    foreach (Thing t in target.Cell.GetThingList(this.caller.Map))
                    {
                        if (t is Building && t.def.useHitPoints && t.def.building.canBeDamagedByAttacks)
                        {
                            this.InstantiateEffect(t, pme);
                            break;
                        }
                    }
                }
            }
        }
        public void InstantiateEffect(Thing thing, PermitMoreEffects pme)
        {
            if (!pme.hediffs.NullOrEmpty())
            {
                Hediff h = HediffMaker.MakeHediff(pme.hediffs.First(),this.caller,null);
                HediffComp_BoomHeadshot hcbh = h.TryGetComp<HediffComp_BoomHeadshot>();
                if (hcbh != null)
                {
                    hcbh.victim = thing;
                }
                this.caller.health.AddHediff(h);
            }
            HVMP_Utility.ThrowScalpelScope(thing,thing.Map,1f);
            this.caller.royalty.GetPermit(this.def, this.calledFaction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(this.calledFaction, this.def.royalAid.favorCost);
            }
            HVMP_Utility.DoPTargeterCooldown(this.calledFaction, this.caller, this);
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
            if (HVMP_Utility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free))
            {
                action = delegate
                {
                    this.BeginAffectPawn(pawn, map, faction, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null;
        }
        private void BeginAffectPawn(Pawn pawn, Map map, Faction faction, bool free)
        {
            if (this.IsFactionHostileToPlayer(faction, pawn))
            {
                return;
            }
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetSelf = false;
            this.targetingParameters.canTargetPawns = false;
            this.targetingParameters.canTargetFires = false;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetItems = true;
            this.targetingParameters.validator = (TargetInfo target) => this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(this.caller.Position) <= this.def.royalAid.targetingRange;
            this.caller = pawn;
            this.map = map;
            this.calledFaction = faction;
            this.free = free;
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        public Faction CalledFaction
        {
            get
            {
                return this.CalledFaction;
            }
        }
        private Faction calledFaction;
    }
    public class ScalpelTracker : HediffWithComps
    {
        public override bool TryMergeWith(Hediff other)
        {
            return false;
        }
    }
    public class HediffCompProperties_BoomHeadshot : HediffCompProperties
    {
        public HediffCompProperties_BoomHeadshot()
        {
            this.compClass = typeof(HediffComp_BoomHeadshot);
        }
        public DamageDef damageType;
        public float damageAmount;
        public float armorPen;
        public SoundDef soundDef;
    }
    public class HediffComp_BoomHeadshot : HediffComp
    {
        public HediffCompProperties_BoomHeadshot Props
        {
            get
            {
                return (HediffCompProperties_BoomHeadshot)this.props;
            }
        }
        public override void CompPostPostRemoved()
        {
            Thing victim = this.victim;
            if (!victim.DestroyedOrNull() && victim.Spawned)
            {
                this.Props.soundDef.PlayOneShot(new TargetInfo(victim.Position, victim.Map, false));
                if (HautsUtility.CanBeHitByAirToSurface(victim.Position, victim.Map, false))
                {
                    RoofDef roof = victim.Map.roofGrid.RoofAt(victim.Position);
                    if (roof != null && !roof.isThickRoof)
                    {
                        victim.Map.roofGrid.SetRoof(victim.Position,null);
                    }
                    this.victim.TakeDamage(new DamageInfo(DamageDefOf.Bomb, this.Props.damageAmount, this.Props.armorPen, instigator: this.Pawn));
                }
                //FleckMaker.ThrowSmoke(victim.DrawPos, victim.Map, 10f);
            }
            base.CompPostPostRemoved();
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look<Thing>(ref this.victim, "victim", false);
        }
        public Thing victim;
    }
    public class HediffCompProperties_CaravanHediffAura : HediffCompProperties_AuraHediff
    {
        public HediffCompProperties_CaravanHediffAura()
        {
            this.compClass = typeof(HediffComp_CaravanHediffAura);
        }
    }
    public class HediffComp_CaravanHediffAura : HediffComp_AuraHediff
    {
        public new HediffCompProperties_CaravanHediffAura Props
        {
            get
            {
                return (HediffCompProperties_CaravanHediffAura)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            Pawn pawn = this.parent.pawn;
            Caravan caravan = pawn.GetCaravan();
            if (caravan != null)
            {
                this.AffectSelf();
                this.AffectPawns(pawn, caravan.pawns.InnerListForReading, true);
            }
        }
    }
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_EMI : RoyalTitlePermitWorker_CauseCondition
    {
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return HVMP_Utility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null;
        }
        protected override void MakeCondition(Pawn caller, Faction faction, IncidentParms parms, bool free)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            base.MakeCondition(caller, faction, parms, free);
            if (caller.MapHeld != null && pme != null && pme.extraNumber != null)
            {
                foreach (Building b in caller.MapHeld.listerBuildings.allBuildingsNonColonist)
                {
                    if (b.Faction == null || b.Faction.RelationKindWith(Faction.OfPlayerSilentFail) == FactionRelationKind.Hostile)
                    {
                        CompStunnable stunComp = b.GetComp<CompStunnable>();
                        if (stunComp != null)
                        {
                            stunComp.StunHandler.StunFor((int)pme.extraNumber.RandomInRange, null, false);
                        }
                    }
                }
            }
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            HVMP_Utility.DoPTargeterCooldown(faction, caller, this);
        }
    }
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_Infestation : RoyalTitlePermitWorker_Targeted
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.BaitInfestation(target.Cell);
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
                    this.BeginInfestation(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            yield break;
        }
        private void BeginInfestation(Pawn caller, Faction faction, Map map, bool free)
        {
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetPawns = false;
            this.caller = caller;
            this.map = map;
            this.faction = faction;
            this.free = free;
            this.targetingParameters.validator = (TargetInfo target) => (this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(caller.Position) <= this.def.royalAid.targetingRange) && !target.Cell.Fogged(map) && target.Cell.GetRegion(map, RegionType.Set_Passable) != null && target.Cell.GetTemperature(map) >= -17f;
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void BaitInfestation(IntVec3 cell)
        {
            IncidentParms incidentParms = new IncidentParms();
            incidentParms.target = this.map;
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                incidentParms.points = pme.incidentPoints.RandomInRange;
            }
            else
            {
                incidentParms.points = 1750;
            }
            incidentParms.infestationLocOverride = cell;
            incidentParms.forced = true;
            IncidentDefOf.Infestation.Worker.TryExecute(incidentParms);
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
    public class RoyalTitlePermitWorker_ManhunterPulse : RoyalTitlePermitWorker_Targeted
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
        protected virtual void MakeCondition(Pawn caller, Faction faction, IncidentParms parms, bool free)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null && caller.MapHeld != null)
            {
                foreach (Pawn p in caller.MapHeld.mapPawns.AllPawnsSpawned)
                {
                    if (p.IsAnimal && p.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon && (p.Faction == null || p.Faction != Faction.OfPlayerSilentFail) && !p.IsQuestLodger() && !p.Dead)
                    {
                        if (!p.Awake())
                        {
                            RestUtility.WakeUp(p, true);
                        }
                        p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, null, false, false, false, null, false, false, false);
                    }
                }
                Messages.Message(pme.onUseMessage.Translate(faction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
                if (pme.screenShake && caller.MapHeld == Find.CurrentMap)
                {
                    Find.CameraDriver.shaker.DoShake(1f);
                }
                if (pme.soundDef != null)
                {
                    pme.soundDef.PlayOneShot(new TargetInfo(caller.PositionHeld, caller.MapHeld, false));
                }
                caller.royalty.GetPermit(this.def, faction).Notify_Used();
                if (!free)
                {
                    caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
                }
                HVMP_Utility.DoPTargeterCooldown(faction,caller,this);
            }
        }
    }
    public class RoyalTitlePermitWorker_OrbitalBeam : RoyalTitlePermitWorker_Targeted
    {
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.CanHitTarget(target))
            {
                if (target.IsValid && showMessages)
                {
                    Messages.Message(this.def.LabelCap + ": " + "AbilityCannotHitTarget".Translate(), MessageTypeDefOf.RejectInput, true);
                }
                return false;
            }
            return true;
        }
        public override void DrawHighlight(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(this.caller.Position, this.def.royalAid.targetingRange, Color.white, null);
            GenDraw.DrawRadiusRing(target.Cell, this.def.royalAid.radius + this.def.royalAid.explosionRadiusRange.max, Color.white, null);
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.CallBombardment(target.Cell);
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
            if (HVMP_Utility.ProprietaryFillAidOption(this,pawn, faction, ref text, out free))
            {
                action = delegate
                {
                    this.BeginCallBombardment(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        private void BeginCallBombardment(Pawn caller, Faction faction, Map map, bool free)
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
        private void CallBombardment(IntVec3 targetCell)
        {
            DelayedPowerBeam dpb = (DelayedPowerBeam)GenSpawn.Spawn(HVMPDefOf.HVMP_DelayedPowerBeam,targetCell,this.map,WipeMode.Vanish);
            dpb.duration = this.def.royalAid.explosionCount;
            dpb.instigator = this.caller;
            SoundDefOf.OrbitalStrike_Ordered.PlayOneShotOnCamera(null);
            this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
            }
            HVMP_Utility.DoPTargeterCooldown(this.faction,this.caller,this);
        }
        private Faction faction;
    }
    public class DelayedPowerBeam : ThingWithComps
    {
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.Comps_PostDraw();
        }
        protected override void Tick()
        {
            base.Tick();
            if (this.warmupTicks > 0)
            {
                this.warmupTicks--;
                if (this.warmupTicks == 60)
                {
                    this.angle = DelayedPowerBeam.AngleRange.RandomInRange;
                    base.GetComp<CompOrbitalBeam>().StartAnimation(this.duration, 10, this.angle);
                }
                if (this.warmupTicks == 0)
                {
                    PowerBeam powerBeam = (PowerBeam)GenSpawn.Spawn(ThingDefOf.PowerBeam, this.Position, this.Map, WipeMode.Vanish);
                    powerBeam.duration = this.duration;
                    powerBeam.instigator = this.instigator;
                    powerBeam.weaponDef = null;
                    if (!powerBeam.Spawned)
                    {
                        Log.Error("Called StartStrike() on unspawned thing.");
                        return;
                    }
                    powerBeam.StartStrike();
                    SoundDefOf.OrbitalStrike_Ordered.PlayOneShotOnCamera(null);
                    this.Destroy();
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.warmupTicks, "warmupTicks", 0, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 600, false);
            Scribe_References.Look<Thing>(ref this.instigator, "instigator", false);
            Scribe_Values.Look<float>(ref this.angle, "angle", 0f, false);
        }
        public int warmupTicks = 120;
        public int duration;
        public Thing instigator;
        private float angle;
        private static readonly FloatRange AngleRange = new FloatRange(-12f, 12f);
    }
    public class RoyalTitlePermitWorker_MechCluster : RoyalTitlePermitWorker_Targeted
    {
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.CanHitTarget(target))
            {
                if (target.IsValid && showMessages)
                {
                    Messages.Message(this.def.LabelCap + ": " + "AbilityCannotHitTarget".Translate(), MessageTypeDefOf.RejectInput, true);
                }
                return false;
            }
            return true;
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.CallCluster(target.Cell);
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
                    this.BeginCallCluster(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        private void BeginCallCluster(Pawn caller, Faction faction, Map map, bool free)
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
        private void CallCluster(IntVec3 targetCell)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            float points = (pme != null && pme.incidentPoints.max > 0) ? pme.incidentPoints.RandomInRange : StorytellerUtility.DefaultThreatPointsNow(this.map);
            MechClusterSketch mechClusterSketch = MechClusterGenerator.GenerateClusterSketch(points, this.map, true, false);
            MechClusterUtility.SpawnCluster(targetCell, this.map, mechClusterSketch, true, false, null);
            this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
            }
            HVMP_Utility.DoPTargeterCooldown(this.faction, this.caller, this);
        }
        private Faction faction;
    }
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_CallRaid : RoyalTitlePermitWorker_GenerateQuest
    {
        public override bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
        {
            return !f.IsPlayer && !f.defeated && !f.temporary && (desperate || (map != null && f.def.allowedArrivalTemperatureRange.Includes(map.mapTemperature.OutdoorTemp) && f.def.allowedArrivalTemperatureRange.Includes(map.mapTemperature.SeasonalTemp))) && !f.Hidden && f.HostileTo(Faction.OfPlayer);
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (!this.CandidateFactions(map, false).Any<Faction>())
            {
                yield return new FloatMenuOption("HVMP_NoFactionCanSendRaids".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            if (HVMP_Utility.ProprietaryFillAidOption(this,pawn, faction, ref text, out bool free))
            {
                action = delegate
                {
                    this.GiveQuest(pawn, faction, new IncidentParms(), this.free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            HVMP_Utility.DoPTargeterCooldown(faction, caller, this);
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            yield break;
        }
    }
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_Peacemaking : RoyalTitlePermitWorker_Targeted
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
            if (HVMP_Utility.ProprietaryFillAidOption(this,pawn, faction, ref text, out bool free))
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
        protected virtual void MakeCondition(Pawn caller, Faction faction, IncidentParms parms, bool free)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null && caller.MapHeld != null)
            {
                foreach (Pawn p in caller.MapHeld.mapPawns.AllPawnsSpawned)
                {
                    if ((p.HostileTo(caller.Faction) || p.HostileTo(caller)) && !p.InMentalState && p.Awake() && !p.DeadOrDowned && ((p.Faction != null && p.Faction.def.humanlikeFaction) || p.RaceProps.Humanlike) && !p.IsPrisoner)
                    {
                        HVMP_Utility.ThrowBribeGlow(p.Position.ToVector3() + new Vector3(0.5f, 0f, 0.5f), caller.MapHeld, 1.5f);
                        p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, null, false, false, false, null, false, false, false);
                    }
                }
                Messages.Message(pme.onUseMessage.Translate(faction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
                if (pme.screenShake && caller.MapHeld == Find.CurrentMap)
                {
                    Find.CameraDriver.shaker.DoShake(1f);
                }
                if (pme.soundDef != null)
                {
                    pme.soundDef.PlayOneShot(new TargetInfo(caller.PositionHeld, caller.MapHeld, false));
                }
                caller.royalty.GetPermit(this.def, faction).Notify_Used();
                if (!free)
                {
                    caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
                }
                HVMP_Utility.DoPTargeterCooldown(faction,caller,this);
            }
        }
    }

    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_CallTrader : RoyalTitlePermitWorker_GenerateQuest
    {
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
            if (wcbs != null && wcbs.tradeBlockages > 0)
            {
                yield return new FloatMenuOption("HVMP_CommandCallRoyalAidTradersBlocked".Translate(wcbs.tradeBlockages, faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            if (HVMP_Utility.ProprietaryFillAidOption(this,pawn, faction, ref text, out bool free))
            {
                action = delegate
                {
                    this.GiveQuest(pawn, faction, new IncidentParms(), this.free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            HVMP_Utility.DoPTargeterCooldown(faction, caller, this);
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            string text;
            bool flag;
            if (!base.FillCaravanAidOption(pawn, faction, out text, out this.free, out flag))
            {
                yield break;
            }
            WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
            if (wcbs != null && wcbs.tradeBlockages > 0)
            {
                yield break;
            }
            Command_Action command_Action = new Command_Action
            {
                defaultLabel = this.def.LabelCap + " (" + pawn.LabelShort + ")",
                defaultDesc = text,
                icon = RoyalTitlePermitWorker_CallTrader.CommandTex,
                action = delegate
                {
                    this.GiveQuest(pawn, faction, new IncidentParms(), this.free);
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
        private static readonly Texture2D CommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CallAid", true);
    }

    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_CallTraderCaravan : RoyalTitlePermitWorker_GenerateQuest
    {
        public override bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
        {
            return !f.IsPlayer && !f.defeated && !f.temporary && (desperate || (map != null && f.def.allowedArrivalTemperatureRange.Includes(map.mapTemperature.OutdoorTemp) && f.def.allowedArrivalTemperatureRange.Includes(map.mapTemperature.SeasonalTemp))) && !f.Hidden && !f.HostileTo(Faction.OfPlayer) && f.def.pawnGroupMakers != null && f.def.pawnGroupMakers.Any((PawnGroupMaker x) => x.kindDef == PawnGroupKindDefOf.Trader) && !NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, f) && f.def.caravanTraderKinds.Count != 0 && f.def.caravanTraderKinds.Any((TraderKindDef t) => t.requestable && this.TraderKindCommonality(t, map, f) > 0f);
        }
        public float TraderKindCommonality(TraderKindDef traderKind, Map map, Faction faction)
        {
            if (traderKind.faction != null && faction.def != traderKind.faction)
            {
                return 0f;
            }
            if (ModsConfig.IdeologyActive && faction.ideos != null && traderKind.category == "Slaver")
            {
                using (IEnumerator<Ideo> enumerator = faction.ideos.AllIdeos.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (!enumerator.Current.IdeoApprovesOfSlavery())
                        {
                            return 0f;
                        }
                    }
                }
            }
            if (traderKind.permitRequiredForTrading != null && !map.mapPawns.FreeColonists.Any((Pawn p) => p.royalty != null && p.royalty.HasPermit(traderKind.permitRequiredForTrading, faction)))
            {
                return 0f;
            }
            return traderKind.CalculatedCommonality;
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
            if (wcbs != null && wcbs.tradeBlockages > 0)
            {
                yield return new FloatMenuOption("HVMP_CommandCallRoyalAidTradersBlocked".Translate(wcbs.tradeBlockages, faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (!this.CandidateFactions(map, false).Any<Faction>())
            {
                yield return new FloatMenuOption("HVMP_NoFactionCanSendTraders".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            if (HVMP_Utility.ProprietaryFillAidOption(this,pawn, faction, ref text, out bool free))
            {
                action = delegate
                {
                    this.GiveQuest(pawn, faction, new IncidentParms(), this.free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            HVMP_Utility.DoPTargeterCooldown(faction, caller, this);
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            yield break;
        }
    }
    public class RoyalTitlePermitWorker_AlterItemQuality : RoyalTitlePermitWorker_Targeted
    {
        public AcceptanceReport IsValidThing(LocalTargetInfo lti)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                TaggedString error = pme.invalidTargetMessage.Translate();
                if (!lti.IsValid)
                {
                    return new AcceptanceReport(error);
                } else {
                    if (pme.extraNumber != null)
                    {
                        foreach (Thing t in lti.Cell.GetThingList(this.caller.Map))
                        {
                            if (t.def.category == ThingCategory.Item && t.TryGetQuality(out QualityCategory qc) && t.def.thingCategories != null && (float)qc >= pme.extraNumber.min && (float)qc <= pme.extraNumber.max && (pme.thingCategories == null || t.def.thingCategories.ContainsAny((ThingCategoryDef tcd) => pme.thingCategories.Contains(tcd))) && (pme.forbiddenThingCategories == null || !t.def.thingCategories.ContainsAny((ThingCategoryDef tcd) => pme.forbiddenThingCategories.Contains(tcd))))
                            {
                                return AcceptanceReport.WasAccepted;
                            }
                        }
                    }
                }
                return new AcceptanceReport(error);
            }
            return new AcceptanceReport("Hauts_PMEMisconfig".Translate());
        }
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.CanHitTarget(target))
            {
                if (target.IsValid && showMessages)
                {
                    Messages.Message(this.def.LabelCap + ": " + "AbilityCannotHitTarget".Translate(), MessageTypeDefOf.RejectInput, true);
                }
                return false;
            }
            AcceptanceReport acceptanceReport = this.IsValidThing(target);
            if (!acceptanceReport.Accepted)
            {
                Messages.Message(acceptanceReport.Reason, new LookTargets(target.Cell, this.map), MessageTypeDefOf.RejectInput, false);
            }
            return acceptanceReport.Accepted;
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null && pme.extraNumber != null)
            {
                foreach (Thing t in target.Cell.GetThingList(this.caller.Map))
                {
                    if (t.def.category == ThingCategory.Item && t.TryGetQuality(out QualityCategory qc) && (float)qc >= pme.extraNumber.min && (float)qc <= pme.extraNumber.max && (pme.forbiddenThingCategories == null || !t.def.thingCategories.ContainsAny((ThingCategoryDef tcd) => pme.forbiddenThingCategories.Contains(tcd))))
                    {
                        this.ImproveQuality(t, this.calledFaction);
                        break;
                    }
                }
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
            if (HVMP_Utility.ProprietaryFillAidOption(this,pawn, faction, ref text, out bool free))
            {
                action = delegate
                {
                    this.BeginImproveQuality(pawn, map, faction, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        private void BeginImproveQuality(Pawn pawn, Map map, Faction faction, bool free)
        {
            if (faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null)
            {
                return;
            }
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetSelf = false;
            this.targetingParameters.canTargetPawns = false;
            this.targetingParameters.canTargetFires = false;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetItems = true;
            this.targetingParameters.validator = (TargetInfo target) => this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(this.caller.Position) <= this.def.royalAid.targetingRange;
            this.caller = pawn;
            this.map = map;
            this.calledFaction = faction;
            this.free = free;
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void ImproveQuality(Thing thing, Faction faction)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                if (Rand.Chance(pme.gambaFactorRange.RandomInRange))
                {
                    HVMP_Utility.ThrowQualityDestroyGlow(thing.PositionHeld.ToVector3(), this.map, 1f);
                    thing.Destroy();
                } else {
                    HVMP_Utility.ThrowQualityUpgradeGlow(thing.PositionHeld.ToVector3(), this.map, 1f);
                    MinifiedThing minifiedThing = thing as MinifiedThing;
                    CompQuality coq = ((minifiedThing != null) ? minifiedThing.InnerThing.TryGetComp<CompQuality>() : thing.TryGetComp<CompQuality>());
                    if (coq != null)
                    {
                        coq.SetQuality(coq.Quality + 1, new ArtGenerationContext?(ArtGenerationContext.Outsider));
                    }
                }
                Messages.Message(pme.onUseMessage.Translate(faction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
                this.caller.royalty.GetPermit(this.def, this.calledFaction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(this.calledFaction, this.def.royalAid.favorCost);
                }
                HVMP_Utility.DoPTargeterCooldown(this.calledFaction,this.caller,this);
            }
        }
        private Faction calledFaction;
    }
    public class RoyalTitlePermitWorker_RestoreItemHP : RoyalTitlePermitWorker_Targeted
    {
        public AcceptanceReport IsValidThing(LocalTargetInfo lti)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                TaggedString error = pme.invalidTargetMessage.Translate();
                if (!lti.IsValid)
                {
                    return new AcceptanceReport(error);
                } else {
                    if (pme.extraNumber != null)
                    {
                        foreach (Thing t in lti.Cell.GetThingList(this.caller.Map))
                        {
                            if (t.def.useHitPoints && (t.HitPoints < t.MaxHitPoints || this.OtherQualifiers(t)) && (t is Building || (t.def.thingCategories != null && (pme.thingCategories == null || t.def.thingCategories.ContainsAny((ThingCategoryDef tcd) => pme.thingCategories.Contains(tcd))) && (pme.forbiddenThingCategories == null || !t.def.thingCategories.ContainsAny((ThingCategoryDef tcd) => pme.forbiddenThingCategories.Contains(tcd))))))
                            {
                                return AcceptanceReport.WasAccepted;
                            }
                        }
                    }
                }
                return new AcceptanceReport(error);
            }
            return new AcceptanceReport("Hauts_PMEMisconfig".Translate());
        }
        public virtual bool OtherQualifiers(Thing t)
        {
            return true;
        }
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.CanHitTarget(target))
            {
                if (target.IsValid && showMessages)
                {
                    Messages.Message(this.def.LabelCap + ": " + "AbilityCannotHitTarget".Translate(), MessageTypeDefOf.RejectInput, true);
                }
                return false;
            }
            AcceptanceReport acceptanceReport = this.IsValidThing(target);
            if (!acceptanceReport.Accepted)
            {
                Messages.Message(acceptanceReport.Reason, new LookTargets(target.Cell, this.map), MessageTypeDefOf.RejectInput, false);
            }
            return acceptanceReport.Accepted;
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null && pme.extraNumber != null)
            {
                HVMP_Utility.ThrowRepairGlow(target.Cell.ToVector3(), this.map, 1.5f);
                Thing toHeal = null;
                float worstMissingHp = 0;
                foreach (Thing t in target.Cell.GetThingList(this.caller.Map))
                {
                    if (t.def.useHitPoints && (t.HitPoints < t.MaxHitPoints || this.OtherQualifiers(t)) && (t is Building || (t.def.thingCategories != null && (pme.thingCategories == null || t.def.thingCategories.ContainsAny((ThingCategoryDef tcd) => pme.thingCategories.Contains(tcd))) && (pme.forbiddenThingCategories == null || !t.def.thingCategories.ContainsAny((ThingCategoryDef tcd) => pme.forbiddenThingCategories.Contains(tcd))))))
                    {
                        if (t.MaxHitPoints - t.HitPoints > worstMissingHp)
                        {
                            worstMissingHp = t.MaxHitPoints - t.HitPoints;
                            toHeal = t;
                        }
                    }
                }
                if (toHeal != null)
                {
                    this.Heal(toHeal, this.calledFaction);
                }
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
            if (HVMP_Utility.ProprietaryFillAidOption(this,pawn, faction, ref text, out bool free))
            {
                action = delegate
                {
                    this.BeginHeal(pawn, map, faction, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        private void BeginHeal(Pawn pawn, Map map, Faction faction, bool free)
        {
            if (faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null)
            {
                return;
            }
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetSelf = false;
            this.targetingParameters.canTargetPawns = false;
            this.targetingParameters.canTargetFires = false;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetItems = true;
            this.targetingParameters.validator = (TargetInfo target) => this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(this.caller.Position) <= this.def.royalAid.targetingRange;
            this.caller = pawn;
            this.map = map;
            this.calledFaction = faction;
            this.free = free;
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void Heal(Thing thing, Faction faction)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null && pme.extraNumber != null)
            {
                thing.HitPoints += Math.Min((int)Math.Ceiling(pme.extraNumber.RandomInRange),thing.MaxHitPoints-thing.HitPoints);
                this.OtherEffects(thing);
                Messages.Message(pme.onUseMessage.Translate(faction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
                this.caller.royalty.GetPermit(this.def, this.calledFaction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(this.calledFaction, this.def.royalAid.favorCost);
                }
                HVMP_Utility.DoPTargeterCooldown(this.calledFaction,this.caller,this);
            }
        }
        public virtual void OtherEffects(Thing thing)
        {

        }
        private Faction calledFaction;
    }
    public class RoyalTitlePermitWorker_RestoreItemHP_Perfect : RoyalTitlePermitWorker_RestoreItemHP
    {
        public override bool OtherQualifiers(Thing t)
        {
            return (t is Apparel a && a.WornByCorpse) || t.IsBrokenDown();
        }
        public override void OtherEffects(Thing thing)
        {
            if (thing is Apparel a)
            {
                a.WornByCorpse = false;
            }
            CompBreakdownable cbd = thing.TryGetComp<CompBreakdownable>();
            if (cbd != null && cbd.BrokenDown)
            {
                cbd.Notify_Repaired();
            }
        }
    }
    public class RoyalTitlePermitWorker_RestoreItemHP_AOE : RoyalTitlePermitWorker_Targeted
    {
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.CanHitTarget(target))
            {
                if (target.IsValid && showMessages)
                {
                    Messages.Message(this.def.LabelCap + ": " + "AbilityCannotHitTarget".Translate(), MessageTypeDefOf.RejectInput, true);
                }
                return false;
            }
            return true;
        }
        public override void DrawHighlight(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(this.caller.Position, this.def.royalAid.targetingRange, Color.white, null);
            GenDraw.DrawRadiusRing(target.Cell, this.def.royalAid.radius, Color.white, null);
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.DoAOE(target.Cell);
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
            if (HVMP_Utility.ProprietaryFillAidOption(this,pawn, faction, ref text, out free))
            {
                action = delegate
                {
                    this.BeginAOE(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        private void BeginAOE(Pawn caller, Faction faction, Map map, bool free)
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
        private void DoAOE(IntVec3 targetCell)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                List<IntVec3> iv3s = new List<IntVec3>();
                foreach (Building building in GenRadial.RadialDistinctThingsAround(targetCell, this.map, this.def.royalAid.radius, true).OfType<Building>().Distinct<Building>())
                {
                    if (building.Faction == null || this.caller.Faction == null || this.caller.Faction == building.Faction || !this.caller.Faction.HostileTo(building.Faction))
                    {
                        if (building.def.useHitPoints)
                        {
                            if (!iv3s.Contains(building.Position))
                            {
                                HVMP_Utility.ThrowRepairGlow(building.Position.ToVector3(), this.map, 1f);
                            }
                            building.HitPoints += Math.Min((int)Math.Ceiling(pme.extraNumber.RandomInRange), building.MaxHitPoints - building.HitPoints);
                        }
                        CompBreakdownable cbd = building.TryGetComp<CompBreakdownable>();
                        if (cbd != null && cbd.BrokenDown)
                        {
                            if (!iv3s.Contains(building.Position))
                            {
                                HVMP_Utility.ThrowRepairGlow(building.Position.ToVector3(), this.map, 1f);
                            }
                            cbd.Notify_Repaired();
                        }
                    }
                }
                if (pme.screenShake && this.map == Find.CurrentMap)
                {
                    Find.CameraDriver.shaker.DoShake(1f);
                }
                if (pme.soundDef != null)
                {
                    pme.soundDef.PlayOneShot(new TargetInfo(targetCell, this.map, false));
                }
                Messages.Message(pme.onUseMessage.Translate(this.faction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
                this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
                }
                HVMP_Utility.DoPTargeterCooldown(this.faction,this.caller,this);
            }
        }
        private Faction faction;
    }
    public class RoyalTitlePermitWorker_DecryptBiocoding : RoyalTitlePermitWorker_Targeted
    {
        public AcceptanceReport IsValidThing(LocalTargetInfo lti)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                TaggedString error = pme.invalidTargetMessage.Translate();
                if (!lti.IsValid)
                {
                    return new AcceptanceReport(error);
                } else {
                    foreach (Thing t in lti.Cell.GetThingList(this.caller.Map))
                    {
                        CompBiocodable comp = t.TryGetComp<CompBiocodable>();
                        if (comp != null && comp.Biocoded)
                        {
                            return AcceptanceReport.WasAccepted;
                        }
                    }
                }
                return new AcceptanceReport(error);
            }
            return new AcceptanceReport("Hauts_PMEMisconfig".Translate());
        }
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.CanHitTarget(target))
            {
                if (target.IsValid && showMessages)
                {
                    Messages.Message(this.def.LabelCap + ": " + "AbilityCannotHitTarget".Translate(), MessageTypeDefOf.RejectInput, true);
                }
                return false;
            }
            AcceptanceReport acceptanceReport = this.IsValidThing(target);
            if (!acceptanceReport.Accepted)
            {
                Messages.Message(acceptanceReport.Reason, new LookTargets(target.Cell, this.map), MessageTypeDefOf.RejectInput, false);
            }
            return acceptanceReport.Accepted;
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                HVMP_Utility.ThrowDecryptionGlow(target.Cell.ToVector3(), this.map, 1f);
                foreach (Thing t in target.Cell.GetThingList(this.caller.Map))
                {
                    CompBiocodable comp = t.TryGetComp<CompBiocodable>();
                    if (comp != null && comp.Biocoded)
                    {
                        comp.UnCode();
                        Messages.Message(pme.onUseMessage.Translate(this.calledFaction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
                        this.caller.royalty.GetPermit(this.def, this.calledFaction).Notify_Used();
                        if (!this.free)
                        {
                            this.caller.royalty.TryRemoveFavor(this.calledFaction, this.def.royalAid.favorCost);
                        }
                        HVMP_Utility.DoPTargeterCooldown(this.calledFaction,this.caller,this);
                        break;
                    }
                }
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
            if (HVMP_Utility.ProprietaryFillAidOption(this,pawn, faction, ref text, out bool free))
            {
                action = delegate
                {
                    this.BeginHeal(pawn, map, faction, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        private void BeginHeal(Pawn pawn, Map map, Faction faction, bool free)
        {
            if (faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null)
            {
                return;
            }
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetSelf = false;
            this.targetingParameters.canTargetPawns = false;
            this.targetingParameters.canTargetFires = false;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetItems = true;
            this.targetingParameters.validator = (TargetInfo target) => this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(this.caller.Position) <= this.def.royalAid.targetingRange;
            this.caller = pawn;
            this.map = map;
            this.calledFaction = faction;
            this.free = free;
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private Faction calledFaction;
    }
    public class HediffCompProperties_TMD : HediffCompProperties
    {
        public HediffCompProperties_TMD()
        {
            this.compClass = typeof(HediffComp_TMD);
        }
        public HediffDef convertedFrom;
    }
    public class HediffComp_TMD : HediffComp
    {
        public HediffCompProperties_TMD Props
        {
            get
            {
                return (HediffCompProperties_TMD)this.props;
            }
        }
    }
    public class RoyalTitlePermitWorker_TMD : RoyalTitlePermitWorker_TargetPawn
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                foreach (HediffDef h in pme.hediffs)
                {
                    HediffCompProperties_TMD compTMD = null;
                    if (h.comps != null)
                    {
                        foreach (HediffCompProperties hcp in h.comps)
                        {
                            if (hcp is HediffCompProperties_TMD hcptmd)
                            {
                                compTMD = hcptmd;
                            }
                        }
                    }
                    if (compTMD != null)
                    {
                        foreach (Hediff ph in pawn.health.hediffSet.hediffs)
                        {
                            if (ph.def == compTMD.convertedFrom)
                            {
                                this.toReplace = ph;
                                this.toGive = h;
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return HVMP_Utility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
            base.AffectPawnInner(pme, pawn, faction);
            if (this.toReplace != null && this.toGive != null)
            {
                HediffComp_Disappears hcd = this.toReplace.TryGetComp<HediffComp_Disappears>();
                int ticksRemaining = hcd != null ? hcd.ticksToDisappear : 900000;
                Hediff hediff = HediffMaker.MakeHediff(this.toGive, pawn);
                pawn.health.AddHediff(hediff);
                hcd = hediff.TryGetComp<HediffComp_Disappears>();
                if (hcd != null)
                {
                    hcd.ticksToDisappear = ticksRemaining / 2;
                }
                pawn.health.RemoveHediff(this.toReplace);

            }
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            HVMP_Utility.DoPTargeterCooldown(faction, caller, this);
        }
        private Hediff toReplace;
        private HediffDef toGive;
    }
    public class RoyalTitlePermitWorker_DupeAnimal : RoyalTitlePermitWorker_TargetPawn
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            if (pawn.IsAnimal && !pawn.RaceProps.Dryad)
            {
                PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
                if (pme != null && (pme.bodySizeCapRange == null || pme.bodySizeCapRange.Includes(pawn.RaceProps.baseBodySize)))
                {
                    return true;
                }
            }
            return false;
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return HVMP_Utility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
            base.AffectPawnInner(pme, pawn, faction);
            List<Pawn> list = new List<Pawn>();
            Pawn p = PawnGenerator.GeneratePawn(pawn.kindDef, pme.startsTamed ? this.caller.Faction : null);
            if (pme.hediffs != null)
            {
                foreach (HediffDef hd in pme.hediffs)
                {
                    p.health.AddHediff(hd);
                }
            }
            list.Add(p);
            if (list.Any<Pawn>())
            {
                ActiveTransporterInfo activeTransporterInfo = new ActiveTransporterInfo();
                activeTransporterInfo.innerContainer.TryAddRangeOrTransfer(list, true, false);
                DropPodUtility.MakeDropPodAt(pawn.Position, this.map, activeTransporterInfo, null);
                Messages.Message("MessagePermitTransportDrop".Translate(faction.Named("FACTION")), new LookTargets(pawn.Position, this.map), MessageTypeDefOf.NeutralEvent, true);
                this.caller.royalty.GetPermit(this.def, faction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
                }
                this.DoOtherEffect(this.caller, faction);
            }
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            HVMP_Utility.DoPTargeterCooldown(faction, caller, this);
        }
    }
    public class RoyalTitlePermitWorker_PollutionScoop : RoyalTitlePermitWorker_Targeted
    {
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.CanHitTarget(target))
            {
                if (target.IsValid && showMessages)
                {
                    Messages.Message(this.def.LabelCap + ": " + "AbilityCannotHitTarget".Translate(), MessageTypeDefOf.RejectInput, true);
                }
                return false;
            }
            return true;
        }
        public override void DrawHighlight(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(this.caller.Position, this.def.royalAid.targetingRange, Color.white, null);
            GenDraw.DrawRadiusRing(target.Cell, this.def.royalAid.radius, Color.white, null);
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.ScoopPollution(target.Cell);
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
            if (HVMP_Utility.ProprietaryFillAidOption(this,pawn, faction, ref text, out bool free))
            {
                action = delegate
                {
                    this.BeginScoop(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        private void BeginScoop(Pawn caller, Faction faction, Map map, bool free)
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
        private void ScoopPollution(IntVec3 targetCell)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                List<Thing> thingsToDestroy = new List<Thing>();
                foreach (Thing thing in GenRadial.RadialDistinctThingsAround(targetCell, this.map, 6, true))
                {
                    if (thing.def == ThingDefOf.Wastepack || (!thing.def.thingCategories.NullOrEmpty() && !pme.thingCategories.NullOrEmpty() && thing.def.thingCategories.ContainsAny((ThingCategoryDef tcd) => pme.thingCategories.Contains(tcd))))
                    {
                        thingsToDestroy.Add(thing);
                    }
                }
                for (int i = thingsToDestroy.Count - 1; i >= 0; i--)
                {
                    thingsToDestroy[i].Destroy();
                }
                int cells = GenRadial.NumCellsInRadius(this.def.royalAid.radius);
                for (int i = 0; i < cells; i++)
                {
                    IntVec3 c = targetCell + GenRadial.RadialPattern[i];
                    if (c.InBounds(this.map) && c.IsValid)
                    {
                        if (c.CanUnpollute(this.map))
                        {
                            this.map.pollutionGrid.SetPolluted(c, false, false);
                        }
                    }
                }
                if (pme.screenShake && this.map == Find.CurrentMap)
                {
                    Find.CameraDriver.shaker.DoShake(1f);
                }
                if (pme.soundDef != null)
                {
                    pme.soundDef.PlayOneShot(new TargetInfo(targetCell, this.map, false));
                }
                GenExplosion.DoExplosion(targetCell, this.map, this.def.royalAid.radius*0.67f, DamageDefOf.Smoke, null, -1, -1f, null, null, null, null, null, 0f, 1, new GasType?(GasType.BlindSmoke), null, 255, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f, null, null);
                this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
                }
                HVMP_Utility.DoPTargeterCooldown(this.faction,this.caller,this);
            }
        }
        private Faction faction;
    }
    public class RoyalTitlePermitWorker_SoilEnrichment : RoyalTitlePermitWorker_Targeted
    {
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.CanHitTarget(target))
            {
                if (target.IsValid && showMessages)
                {
                    Messages.Message(this.def.LabelCap + ": " + "AbilityCannotHitTarget".Translate(), MessageTypeDefOf.RejectInput, true);
                }
                return false;
            }
            return true;
        }
        public override void DrawHighlight(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(this.caller.Position, this.def.royalAid.targetingRange, Color.white, null);
            GenDraw.DrawRadiusRing(target.Cell, this.def.royalAid.radius, Color.white, null);
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.ScoopPollution(target.Cell);
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
            if (HVMP_Utility.ProprietaryFillAidOption(this,pawn, faction, ref text, out bool free))
            {
                action = delegate
                {
                    this.BeginScoop(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        private void BeginScoop(Pawn caller, Faction faction, Map map, bool free)
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
        private void ScoopPollution(IntVec3 targetCell)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                int cells = GenRadial.NumCellsInRadius(this.def.royalAid.radius);
                for (int i = 0; i < cells; i++)
                {
                    IntVec3 c = targetCell + GenRadial.RadialPattern[i];
                    if (c.InBounds(this.map) && c.IsValid)
                    {
                        TerrainDef td = this.map.terrainGrid.TerrainAt(c);
                        if (c.CanUnpollute(this.map))
                        {
                            this.map.pollutionGrid.SetPolluted(c, false, false);
                            if (Rand.Chance(0.25f))
                            {
                                GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.Wastepack, null), targetCell, this.map, ThingPlaceMode.Near);
                            }
                        }
                        if ((td.fertility > 0f || td.categoryType == TerrainDef.TerrainCategoryType.Sand || td.categoryType == TerrainDef.TerrainCategoryType.Soil) && !td.IsFloor && !td.affordances.Contains(TerrainAffordanceDefOf.SmoothableStone) && !td.IsRiver && !td.IsWater)
                        {
                            List<TerrainDef> tdList = HautsUtility.FertilityTerrainDefs(this.map);
                            IOrderedEnumerable<TerrainDef> source = from e in tdList.FindAll((TerrainDef e) => (double)e.fertility > (double)td.fertility && !td.IsWater && !td.IsRiver)
                                                                    orderby e.fertility
                                                                    select e;
                            if (source.Count<TerrainDef>() != 0)
                            {
                                TerrainDef newTerr = source.First<TerrainDef>();
                                this.map.terrainGrid.SetTerrain(c, newTerr);
                            }
                        }
                    }
                }
                if (pme.screenShake && this.map == Find.CurrentMap)
                {
                    Find.CameraDriver.shaker.DoShake(1f);
                }
                if (pme.soundDef != null)
                {
                    pme.soundDef.PlayOneShot(new TargetInfo(targetCell, this.map, false));
                }
                GenExplosion.DoExplosion(targetCell, this.map, this.def.royalAid.radius * 0.67f, DamageDefOf.Smoke, null, -1, -1f, null, null, null, null, null, 0f, 1, new GasType?(GasType.BlindSmoke), null, 255, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f, null, null);
                this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
                }
                HVMP_Utility.DoPTargeterCooldown(this.faction,this.caller,this);
            }
        }
        private Faction faction;
    }
    public class CompProperties_Chargecell : CompProperties_Targetable
    {
        public CompProperties_Chargecell()
        {
            this.compClass = typeof(CompTargetable_Chargecell);
        }
    }
    public class CompTargetable_Chargecell : CompTargetable
    {
        public new CompProperties_Chargecell Props
        {
            get
            {
                return (CompProperties_Chargecell)this.props;
            }
        }
        protected override bool PlayerChoosesTarget
        {
            get
            {
                return true;
            }
        }
        protected override TargetingParameters GetTargetingParameters()
        {
            return new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = true,
                canTargetItems = false,
                canTargetCorpses = false,
                mapObjectTargetsMustBeAutoAttackable = false
            };
        }
        public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
        {
            yield return targetChosenByPlayer;
            yield break;
        }
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            return ((target.Thing is Building b && b.HasComp<CompPowerBattery>()) || (target.Thing is Pawn p && p.needs.energy != null)) && base.ValidateTarget(target.Thing, showMessages);
        }
    }
    public class CompProperties_TargetEffectChargecell : CompProperties
    {
        public CompProperties_TargetEffectChargecell()
        {
            this.compClass = typeof(CompTargetEffect_Chargecell);
        }
        public float batteryWatts;
        public float energyForOneBodySizeMech;
        public SoundDef sound;
    }
    public class CompTargetEffect_Chargecell : CompTargetEffect
    {
        public CompProperties_TargetEffectChargecell Props
        {
            get
            {
                return (CompProperties_TargetEffectChargecell)this.props;
            }
        }
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (!user.IsColonistPlayerControlled)
            {
                return;
            }
            if (target is Building)
            {
                Job job = JobMaker.MakeJob(HVMPDefOf.HVMP_InjectChargecellBattery, target, this.parent);
                job.count = 1;
                job.playerForced = true;
                user.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
            }
            if (target is Pawn)
            {
                Job job = JobMaker.MakeJob(HVMPDefOf.HVMP_InjectChargecellMech, target, this.parent);
                job.count = 1;
                job.playerForced = true;
                user.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
            }
        }
    }
    public class JobDriver_ChargecellBattery : JobDriver
    {
        private Building Battery
        {
            get
            {
                return (Building)this.job.GetTarget(TargetIndex.A).Thing;
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
            return this.pawn.Reserve(this.Battery, this.job, 1, -1, null, errorOnFailed, false) && this.pawn.Reserve(this.Item, this.job, 1, -1, null, errorOnFailed, false);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false, true, false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.A);
            Toil toil = Toils_General.Wait(10, TargetIndex.None);
            toil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            toil.tickIntervalAction = delegate(int delta)
            {
                Pawn actor = toil.actor;
                toil.handlingFacing = true;
                toil.tickAction = delegate
                {
                    actor.rotationTracker.FaceTarget(this.job.GetTarget(TargetIndex.A));
                };
                CompUsable compUsable = this.Item.TryGetComp<CompUsable>();
                if (compUsable != null && this.warmupMote == null && compUsable.Props.warmupMote != null)
                {
                    this.warmupMote = MoteMaker.MakeAttachedOverlay(this.Battery, compUsable.Props.warmupMote, Vector3.zero, 1f, -1f);
                }
                Mote mote = this.warmupMote;
                if (mote == null)
                {
                    return;
                }
                mote.Maintain();
            };
            yield return toil;
            yield return Toils_General.Do(new Action(this.ChargeBattery));
            yield break;
        }
        private void ChargeBattery()
        {
            CompPowerBattery cpb = this.Battery.GetComp<CompPowerBattery>();
            CompTargetEffect_Chargecell ctecc = this.Item.TryGetComp<CompTargetEffect_Chargecell>();
            if (cpb != null && ctecc != null)
            {
                cpb.AddEnergy(ctecc.Props.batteryWatts);
                if (this.Battery.Spawned)
                {
                    ctecc.Props.sound.PlayOneShot(new TargetInfo(this.Battery.Position, this.Battery.Map, false));
                }
            }
            this.Item.SplitOff(1).Destroy(DestroyMode.Vanish);
        }
        private Mote warmupMote;
    }
    public class JobDriver_ChargecellMech : JobDriver
    {
        protected Pawn Mech
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
            this.Mech.ClearAllReservations(true);
            return this.pawn.Reserve(this.Mech, this.job, 1, -1, null, errorOnFailed, false) && this.pawn.Reserve(this.Item, this.job, 1, -1, null, errorOnFailed, false);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnAggroMentalStateAndHostile(TargetIndex.A);
            this.FailOn(delegate
            {
                if (this.Mech.needs.energy == null)
                {
                    return true;
                }
                return false;
            });
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false, true, false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_General.Do(new Action(this.ChargeMech));
            yield break;
        }
        private void ChargeMech()
        {
            CompTargetEffect_Chargecell ctecc = this.Item.TryGetComp<CompTargetEffect_Chargecell>();
            if (this.Mech.needs.energy != null && ctecc != null)
            {
                this.Mech.needs.energy.CurLevel += ctecc.Props.energyForOneBodySizeMech/this.Mech.BodySize;
                if (this.Mech.Spawned)
                {
                    ctecc.Props.sound.PlayOneShot(new TargetInfo(this.Mech.Position, this.Mech.Map, false));
                }
            }
            this.Item.SplitOff(1).Destroy(DestroyMode.Vanish);
        }
        private const TargetIndex TakeeIndex = TargetIndex.A;
        private const TargetIndex BedIndex = TargetIndex.B;
    }
    public class RoyalTitlePermitWorker_MakeXenogermOfEndo : RoyalTitlePermitWorker_TargetPawn
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return pawn.genes != null && pawn.genes.Endogenes.Count > 0 && !pawn.Map.generatorDef.isUnderground && DropCellFinder.CanPhysicallyDropInto(pawn.Position, pawn.Map, true, true) && !pawn.health.hediffSet.HasHediff(HediffDefOf.XenogermReplicating);
        }
        public override void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
            base.AffectPawnInner(pme, pawn, faction);
            Xenogerm xg = this.MakeXenogerm(pawn);
            List<Thing> list = new List<Thing> {
                xg
            };
            ActiveTransporterInfo activeDropPodInfo = new ActiveTransporterInfo();
            activeDropPodInfo.innerContainer.TryAddRangeOrTransfer(list, true, false);
            DropPodUtility.MakeDropPodAt(pawn.Position, pawn.Map, activeDropPodInfo, null);
            Messages.Message("MessagePermitTransportDrop".Translate(faction.Named("FACTION")), new LookTargets(pawn.Position, pawn.Map), MessageTypeDefOf.NeutralEvent, true);
            this.caller.royalty.GetPermit(this.def, faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
            }
        }
        public Xenogerm MakeXenogerm(Pawn target)
        {
            Xenogerm xenogerm = (Xenogerm)ThingMaker.MakeThing(ThingDefOf.Xenogerm, null);
            List<Genepack> noGenepacks = new List<Genepack>();
            string name = "HVB_ProgenoidXenogerm".Translate(target.Name.ToStringShort);
            xenogerm.Initialize(noGenepacks, (target.genes.XenotypeLabel != null) ? target.genes.XenotypeLabel.Trim() : name, (target.genes.iconDef != null) ? target.genes.iconDef : XenotypeIconDefOf.Basic);
            foreach (Gene g in target.genes.Endogenes)
            {
                if (g.def.biostatArc <= 0)
                {
                    xenogerm.GeneSet.AddGene(g.def);
                }
            }
            Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.XenogermReplicating, target, null);
            target.health.AddHediff(hediff, null, null, null);
            return xenogerm;
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return HVMP_Utility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            HVMP_Utility.DoPTargeterCooldown(faction, caller, this);
        }
    }
    public class RoyalTitlePermitWorker_MakeXenogermOfXeno : RoyalTitlePermitWorker_TargetPawn
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return pawn.genes != null && pawn.genes.Xenogenes.Count > 0 && !pawn.Map.generatorDef.isUnderground && DropCellFinder.CanPhysicallyDropInto(pawn.Position, pawn.Map, true, true) && !pawn.health.hediffSet.HasHediff(HediffDefOf.XenogermReplicating);
        }
        public override void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
            base.AffectPawnInner(pme, pawn, faction);
            Xenogerm xg = this.MakeXenogerm(pawn);
            List<Thing> list = new List<Thing> {
                xg
            };
            ActiveTransporterInfo activeDropPodInfo = new ActiveTransporterInfo();
            activeDropPodInfo.innerContainer.TryAddRangeOrTransfer(list, true, false);
            DropPodUtility.MakeDropPodAt(pawn.Position, pawn.Map, activeDropPodInfo, null);
            Messages.Message("MessagePermitTransportDrop".Translate(faction.Named("FACTION")), new LookTargets(pawn.Position, pawn.Map), MessageTypeDefOf.NeutralEvent, true);
            this.caller.royalty.GetPermit(this.def, faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
            }
        }
        public Xenogerm MakeXenogerm(Pawn target)
        {
            Xenogerm xenogerm = (Xenogerm)ThingMaker.MakeThing(ThingDefOf.Xenogerm, null);
            List<Genepack> noGenepacks = new List<Genepack>();
            string name = "HVB_ProgenoidXenogerm".Translate(target.Name.ToStringShort);
            xenogerm.Initialize(noGenepacks, (target.genes.XenotypeLabel != null) ? target.genes.XenotypeLabel.Trim() : name, (target.genes.iconDef != null) ? target.genes.iconDef : XenotypeIconDefOf.Basic);
            foreach (Gene g in target.genes.Xenogenes)
            {
                if (g.def.biostatArc <= 0)
                {
                    xenogerm.GeneSet.AddGene(g.def);
                }
            }
            Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.XenogermReplicating, target, null);
            target.health.AddHediff(hediff, null, null, null);
            return xenogerm;
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return HVMP_Utility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            HVMP_Utility.DoPTargeterCooldown(faction, caller, this);
        }
    }
    public class CompProperties_BlightBlast : CompProperties
    {
        public CompProperties_BlightBlast()
        {
            this.compClass = typeof(CompBlightBlast);
        }
        public float radius;
        public int periodicity;
        public float damageToPlant;
        public EffecterDef effecter;
        public SoundDef sound;
    }
    public class CompBlightBlast : ThingComp
    {
        public CompProperties_BlightBlast Props
        {
            get
            {
                return (CompProperties_BlightBlast)this.props;
            }
        }
        public override void PostDrawExtraSelectionOverlays()
        {
            GenDraw.DrawRadiusRing(this.parent.Position, this.Props.radius);
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.parent.Spawned)
            {
                this.ticksToNextBlast -= delta;
                if (this.ticksToNextBlast <= 0)
                {
                    if (this.Props.effecter != null)
                    {
                        this.Props.effecter.SpawnMaintained(this.parent.PositionHeld, this.parent.MapHeld, 1f);
                    }
                    bool anyBlightKilled = false;
                    foreach (Blight blight in GenRadial.RadialDistinctThingsAround(this.parent.PositionHeld, this.parent.MapHeld, this.Props.radius, true).OfType<Blight>().Distinct<Blight>())
                    {
                        anyBlightKilled = true;
                        Plant plant = blight.Plant;
                        blight.Destroy();
                        if (plant != null)
                        {
                            plant.TakeDamage(new DamageInfo(DamageDefOf.Rotting, this.Props.damageToPlant, 0f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true, QualityCategory.Normal, true));
                        }
                    }
                    if (anyBlightKilled)
                    {
                        if (this.Props.sound != null)
                        {
                            this.Props.sound.PlayOneShot(new TargetInfo(this.parent.PositionHeld, this.parent.MapHeld, false));
                        }
                    }
                    this.ticksToNextBlast = this.Props.periodicity;
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.ticksToNextBlast, "ticksToNextBlast", 0, false);
        }
        public int ticksToNextBlast = 0;
    }
    public class CompProperties_FishGenerator : CompProperties
    {
        public CompProperties_FishGenerator()
        {
            this.compClass = typeof(CompFishGenerator);
        }
        public int periodicity;
        public int fishPerTrigger;
    }
    public class CompFishGenerator : ThingComp
    {
        public CompProperties_FishGenerator Props
        {
            get
            {
                return (CompProperties_FishGenerator)this.props;
            }
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.parent.Spawned)
            {
                this.ticksToNextBlast -= delta;
                if (this.ticksToNextBlast <= 0)
                {
                    if (ModsConfig.OdysseyActive)
                    {
                        WaterBody wb = this.parent.Position.GetWaterBody(this.parent.Map);
                        if (wb != null)
                        {
                            wb.Population += this.Props.fishPerTrigger;
                        }
                    }
                    this.ticksToNextBlast = this.Props.periodicity;
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.ticksToNextBlast, "ticksToNextBlast", 0, false);
        }
        public int ticksToNextBlast = 0;
    }
    public class CompProperties_PlantGrow : CompProperties
    {
        public CompProperties_PlantGrow()
        {
            this.compClass = typeof(CompPlantGrow);
        }
        public float radius;
        public int periodicity;
        public float growthAmount;
    }
    public class CompPlantGrow : ThingComp
    {
        public CompProperties_PlantGrow Props
        {
            get
            {
                return (CompProperties_PlantGrow)this.props;
            }
        }
        public override void PostDrawExtraSelectionOverlays()
        {
            GenDraw.DrawRadiusRing(this.parent.Position, this.Props.radius);
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.parent.Spawned)
            {
                this.ticksToNextBlast -= delta;
                if (this.ticksToNextBlast <= 0)
                {
                    foreach (Plant plant in GenRadial.RadialDistinctThingsAround(this.parent.Position, this.parent.Map, this.Props.radius, true).OfType<Plant>().Distinct<Plant>())
                    {
                        if (!plant.Blighted && plant.LifeStage > PlantLifeStage.Sowing)
                        {
                            plant.Growth += (this.Props.growthAmount * this.Props.periodicity * plant.GrowthRate) / (60000f * plant.def.plant.growDays);
                            plant.DirtyMapMesh(plant.Map);
                        }
                    }
                    this.ticksToNextBlast = this.Props.periodicity;
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.ticksToNextBlast, "ticksToNextBlast", 0, false);
        }
        public int ticksToNextBlast = 0;
    }
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_DropGenePack : RoyalTitlePermitWorker_Targeted
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.CallResources(target.Cell);
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
            if (HVMP_Utility.ProprietaryFillAidOption(this,pawn, faction, ref text, out bool free))
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
                icon = RoyalTitlePermitWorker_DropGenePack.CommandTex,
                action = delegate
                {
                    Caravan caravan = pawn.GetCaravan();
                    float num = caravan.MassUsage;
                    List<ThingDefCountClass> itemsToDrop = this.def.royalAid.itemsToDrop;
                    for (int i = 0; i < itemsToDrop.Count; i++)
                    {
                        num += itemsToDrop[i].thingDef.BaseMass * (float)itemsToDrop[i].count;
                    }
                    if (num > caravan.MassCapacity)
                    {
                        WindowStack windowStack = Find.WindowStack;
                        TaggedString taggedString = "DropResourcesOverweightConfirm".Translate();
                        Action action= delegate
                        {
                            this.CallResourcesToCaravan(pawn, faction, this.free);
                        };
                        windowStack.Add(Dialog_MessageBox.CreateConfirmation(taggedString, action, true, null, WindowLayer.Dialog));
                        return;
                    }
                    this.CallResourcesToCaravan(pawn, faction, this.free);
                }
            };
            if (pawn.MapHeld != null && pawn.MapHeld.generatorDef.isUnderground)
            {
                command_Action.Disable("CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")));
            }
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
            List<Thing> list = new List<Thing>();
            for (int i = 0; i < this.def.royalAid.itemsToDrop.Count; i++)
            {
                for (int k = 0; k < this.def.royalAid.itemsToDrop[i].count; k++)
                {
                    Thing thing = ThingMaker.MakeThing(this.def.royalAid.itemsToDrop[i].thingDef, null);
                    if (thing is Genepack gp)
                    {
                        if (gp.GeneSet.ArchitesTotal > 0)
                        {
                            int toReplace = 0;
                            for (int j = gp.GeneSet.GenesListForReading.Count - 1; j >= 0; j--)
                            {
                                if (gp.GeneSet.GenesListForReading[j].biostatArc > 0)
                                {
                                    gp.GeneSet.Debug_RemoveGene(gp.GeneSet.GenesListForReading[j]);
                                    toReplace++;
                                }
                            }
                            while (toReplace > 0)
                            {
                                gp.GeneSet.AddGene(DefDatabase<GeneDef>.AllDefsListForReading.Where((GeneDef gd) => gd.canGenerateInGeneSet && (gd.prerequisite == null || gp.GeneSet.GenesListForReading.Contains(gd.prerequisite)) && gd.biostatArc <= 0).RandomElement());
                                toReplace--;
                            }
                        }
                    }
                    list.Add(thing);
                }
            }
            if (list.Any<Thing>())
            {
                ActiveTransporterInfo activeDropPodInfo = new ActiveTransporterInfo();
                activeDropPodInfo.innerContainer.TryAddRangeOrTransfer(list, true, false);
                DropPodUtility.MakeDropPodAt(cell, this.map, activeDropPodInfo, null);
                Messages.Message("MessagePermitTransportDrop".Translate(this.faction.Named("FACTION")), new LookTargets(cell, this.map), MessageTypeDefOf.NeutralEvent, true);
                this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
                }
                HVMP_Utility.DoPTargeterCooldown(this.faction,this.caller,this);
            }
        }
        private void CallResourcesToCaravan(Pawn caller, Faction faction, bool free)
        {
            Caravan caravan = caller.GetCaravan();
            for (int i = 0; i < this.def.royalAid.itemsToDrop.Count; i++)
            {
                Thing thing = ThingMaker.MakeThing(this.def.royalAid.itemsToDrop[i].thingDef, null);
                thing.stackCount = this.def.royalAid.itemsToDrop[i].count;
                CaravanInventoryUtility.GiveThing(caravan, thing);
            }
            Messages.Message("MessagePermitTransportDropCaravan".Translate(faction.Named("FACTION"), caller.Named("PAWN")), caravan, MessageTypeDefOf.NeutralEvent, true);
            caller.royalty.GetPermit(this.def, faction).Notify_Used();
            if (!free)
            {
                caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
            }
            HVMP_Utility.DoPTargeterCooldown(faction,caller,this);
        }
        private Faction faction;
        private static readonly Texture2D CommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CallAid", true);
    }
    public class HediffCompProperties_GetOffMyMap : HediffCompProperties
    {
        public HediffCompProperties_GetOffMyMap()
        {
            this.compClass = typeof(HediffComp_GetOffMyMap);
        }
        public int minDuration;
        public int periodicity;
    }
    public class HediffComp_GetOffMyMap : HediffComp
    {
        public HediffCompProperties_GetOffMyMap Props
        {
            get
            {
                return (HediffCompProperties_GetOffMyMap)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            this.faction = this.Pawn.Faction;
            this.timer = this.Props.minDuration;
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            this.timer -= delta;
            if (this.timer <= 0)
            {
                this.timer = this.Props.periodicity;
                this.JustLeaveAlready();
            }
        }
        public void JustLeaveAlready()
        {
            if (!this.Pawn.Spawned)
            {
                this.parent.Severity = -1f;
                return;
            }
            if (this.Pawn.Faction != null && this.faction != null)
            {
                if (this.Pawn.Faction == Faction.OfPlayerSilentFail)
                {
                    this.parent.Severity = -1f;
                    return;
                }
                if (this.Pawn.Faction == this.faction && this.Pawn.Map.CanEverExit && (this.Pawn.Map.ParentFaction == null || this.Pawn.Map.ParentFaction == this.faction))
                {
                    if (this.Pawn.jobs.curJob != null)
                    {
                        this.Pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
                    }
                    Lord lord2 = this.Pawn.GetLord();
                    bool mustMakeNewLord = true;
                    if (lord2 != null)
                    {
                        if (lord2.LordJob is LordJob_ExitMapBest)
                        {
                            mustMakeNewLord = false;
                        } else {
                            lord2.Notify_PawnLost(this.Pawn, PawnLostCondition.Undefined);
                        }
                    }
                    if (mustMakeNewLord)
                    {
                        List<Pawn> pawn = new List<Pawn>
                        {
                            this.Pawn
                        };
                        Lord lord = LordMaker.MakeNewLord(this.faction, new LordJob_ExitMapBest(LocomotionUrgency.Jog, false, true), this.Pawn.Map, pawn);
                    }
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.timer, "timer", 2500, false);
            Scribe_References.Look<Faction>(ref this.faction, "faction", false);
        }
        public int timer;
        public Faction faction;
    }
    //branchquest mechanics: basic sets
    public class QuestNode_CommerceIntermediary : QuestNode_Sequence
    {
        protected override void RunInt()
        {
            if (HVMP_Utility.TryFindCommerceFaction(out Faction commerceFaction))
            {
                Slate slate = QuestGen.slate;
                slate.Set<Thing>("asker", commerceFaction.leader, false);
                slate.Set<Faction>("faction", commerceFaction, false);
                Map map = HVMP_Utility.TryGetMap();
                slate.Set<Map>("map", map, false);
                PlanetTile tile = HVMP_Utility.TryGetPlanetTile();
                slate.Set<PlanetTile>("pTile", tile, false);
                QuestPart_BranchGoodwillFailureHandler qpbgfh = new QuestPart_BranchGoodwillFailureHandler();
                qpbgfh.faction = commerceFaction;
                QuestGen.quest.AddPart(qpbgfh);
                HVMP_Utility.SetSettingScalingRewardValue(slate);
            }
            base.RunInt();
        }
        protected override bool TestRunInt(Slate slate)
        {
            HVMP_Utility.SetSettingScalingRewardValue(slate);
            return HVMP_Utility.TryFindCommerceFaction(out Faction commerceFaction) && base.TestRunInt(slate);
        }
    }
    public class QuestNode_PaxIntermediary : QuestNode_Sequence
    {
        protected override void RunInt()
        {
            if (HVMP_Utility.TryFindPaxFaction(out Faction paxFaction))
            {
                Slate slate = QuestGen.slate;
                slate.Set<Thing>("asker", paxFaction.leader, false);
                QuestGen.slate.Set<Faction>("faction", paxFaction, false);
                Map map = HVMP_Utility.TryGetMap();
                slate.Set<Map>("map", map, false);
                PlanetTile tile = HVMP_Utility.TryGetPlanetTile();
                slate.Set<PlanetTile>("pTile", tile, false);
                QuestPart_BranchGoodwillFailureHandler qpbgfh = new QuestPart_BranchGoodwillFailureHandler();
                qpbgfh.faction = paxFaction;
                QuestGen.quest.AddPart(qpbgfh);
                HVMP_Utility.SetSettingScalingRewardValue(slate);
            }
            base.RunInt();
        }
        protected override bool TestRunInt(Slate slate)
        {
            HVMP_Utility.SetSettingScalingRewardValue(slate);
            return HVMP_Utility.TryFindPaxFaction(out Faction paxFaction) && base.TestRunInt(slate);
        }
    }
    public class QuestNode_RoverIntermediary : QuestNode_Sequence
    {
        protected override void RunInt()
        {
            if (HVMP_Utility.TryFindRoverFaction(out Faction roverFaction))
            {
                Slate slate = QuestGen.slate;
                slate.Set<Thing>("asker", roverFaction.leader, false);
                QuestGen.slate.Set<Faction>("faction", roverFaction, false);
                Map map = HVMP_Utility.TryGetMap();
                slate.Set<Map>("map", map, false);
                PlanetTile tile = HVMP_Utility.TryGetPlanetTile();
                slate.Set<PlanetTile>("pTile", tile, false);
                QuestPart_BranchGoodwillFailureHandler qpbgfh = new QuestPart_BranchGoodwillFailureHandler();
                qpbgfh.faction = roverFaction;
                QuestGen.quest.AddPart(qpbgfh);
                HVMP_Utility.SetSettingScalingRewardValue(slate);
            }
            base.RunInt();
        }
        protected override bool TestRunInt(Slate slate)
        {
            HVMP_Utility.SetSettingScalingRewardValue(slate);
            return HVMP_Utility.TryFindRoverFaction(out Faction roverFaction) && base.TestRunInt(slate);
        }
    }
    public class QuestNode_GiveRewardsBranch : QuestNode
    {
        protected override void RunInt()
        {
            this.parms.giverFaction = this.faction.GetValue(QuestGen.slate);
            this.parms.allowGoodwill = true;
            this.parms.allowRoyalFavor = true;
            this.parms.thingRewardDisallowed = true;
            QuestGen.quest.GiveRewards(this.parms, this.inSignal.GetValue(QuestGen.slate), this.customLetterLabel.GetValue(QuestGen.slate), this.LetterText(), null, null, false, delegate
            {
                QuestNode questNode = this.nodeIfChosenPawnSignalUsed;
                if (questNode == null)
                {
                    return;
                }
                questNode.Run();
            }, this.variants.GetValue(QuestGen.slate), false, this.parms.giverFaction.leader);
        }
        public virtual string LetterText()
        {
            return this.customLetterText.GetValue(QuestGen.slate);
        }
        protected override bool TestRunInt(Slate slate)
        {
            return this.nodeIfChosenPawnSignalUsed == null || this.nodeIfChosenPawnSignalUsed.TestRun(slate);
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        public QuestNode nodeIfChosenPawnSignalUsed;
        public RewardsGeneratorParams parms;
        public SlateRef<string> customLetterLabel;
        public SlateRef<string> customLetterText;
        public SlateRef<Faction> faction;
        public SlateRef<int?> variants;
    }
    public class QuestNode_MapIsSurface : QuestNode
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Map map = QuestGen.slate.Get<Map>("map", null, false);
            if (map.Tile.Layer.IsRootSurface)
            {
                if (this.node != null)
                {
                    this.node.Run();
                    return;
                }
            } else if (this.elseNode != null) {
                this.elseNode.Run();
            }
        }
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        public QuestNode node;
        public QuestNode elseNode;
    }
    public class QuestNode_GiveHostileEnvironmentFilm : QuestNode
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
            QuestPart_GiveHostileEnvironmentFilm qpghef = new QuestPart_GiveHostileEnvironmentFilm();
            qpghef.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
            qpghef.pawns.AddRange(this.pawns.GetValue(slate));
            QuestGen.quest.AddPart(qpghef);
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        public SlateRef<IEnumerable<Pawn>> pawns;
    }
    public class QuestPart_GiveHostileEnvironmentFilm : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                for (int i = 0; i < this.pawns.Count; i++)
                {
                    Hediff hef = HediffMaker.MakeHediff(HVMPDefOf.HVMP_HostileEnvironmentFilm, this.pawns[i]);
                    this.pawns[i].health.AddHediff(hef,null);
                    hef.Severity = 1f;
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
        }
        public override void ReplacePawnReferences(Pawn replace, Pawn with)
        {
            this.pawns.Replace(replace, with);
        }
        public string inSignal;
        public List<Pawn> pawns = new List<Pawn>();
    }
    public class QuestPart_BranchGoodwillFailureHandler : QuestPart
    {
        public override void Notify_PreCleanup()
        {
            base.Notify_PreCleanup();
            int num = HVMP_Utility.ExpectationBasedGoodwillLoss(null, true, true, this.faction);
            if (this.quest.State == QuestState.EndedOfferExpired)
            {
                Faction.OfPlayer.TryAffectGoodwillWith(this.faction, num, true, true, HVMPDefOf.HVMP_IgnoredQuest, null);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Faction>(ref this.faction, "faction", false);
        }
        public Faction faction;
    }
    public class QuestNode_HospitalityPawnType : QuestNode
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            PawnKindDef pawnKindDef;
            if (pawnKinds.TryRandomElement(out pawnKindDef))
            {
                slate.Set<PawnKindDef>(this.storePawnKindAs.GetValue(slate), pawnKindDef, false);
            }
        }
        protected override bool TestRunInt(Slate slate)
        {
            return !this.pawnKinds.NullOrEmpty();
        }
        [NoTranslate]
        public SlateRef<string> storePawnKindAs;
        public List<PawnKindDef> pawnKinds;
    }
    public class QuestNode_SetRewardValue_BranchSettingScaling : QuestNode_Sequence
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            HVMP_Utility.SetSettingScalingRewardValue(slate, rewardFactor);
            base.RunInt();
        }
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        public float rewardFactor = 1f;
    }
    public class QuestNode_EndBranch : QuestNode_End
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Map map = slate.Get<Map>("map");
            if (this.faction != null)
            {
                QuestPart_BranchGoodwillChange qpbgc = new QuestPart_BranchGoodwillChange();
                qpbgc.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
                qpbgc.faction = Find.FactionManager.FirstFactionOfDef(this.faction);
                qpbgc.historyEvent = this.goodwillChangeReason.GetValue(slate);
                slate.Set<string>("goodwillPenalty", "HVMP_GoodwillLoss".Translate(), false);
                QuestGen.quest.AddPart(qpbgc);
            }
            QuestPart_QuestEnd questPart_QuestEnd = new QuestPart_QuestEnd();
            questPart_QuestEnd.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
            questPart_QuestEnd.outcome = new QuestEndOutcome?(this.outcome.GetValue(slate));
            questPart_QuestEnd.signalListenMode = this.signalListenMode.GetValue(slate) ?? QuestPart.SignalListenMode.OngoingOnly;
            questPart_QuestEnd.sendLetter = this.sendStandardLetter.GetValue(slate) ?? false;
            QuestGen.quest.AddPart(questPart_QuestEnd);
        }
        public FactionDef faction;
    }
    public class QuestNode_EndAndDestroySite : QuestNode_End
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            QuestPart_DestroySite qp = new QuestPart_DestroySite();
            qp.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
            qp.worldObjects = this.siteToDestroy.GetValue(slate);
            Map map = slate.Get<Map>("map");
            if (this.faction != null)
            {
                QuestPart_BranchGoodwillChange qpbgc = new QuestPart_BranchGoodwillChange();
                qpbgc.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
                qpbgc.faction = Find.FactionManager.FirstFactionOfDef(this.faction);
                qpbgc.historyEvent = this.goodwillChangeReason.GetValue(slate);
                slate.Set<string>("goodwillPenalty", "dependent on current HEMP settings", false);
                QuestGen.quest.AddPart(qpbgc);
            }
            qp.outcome = new QuestEndOutcome?(this.outcome.GetValue(slate));
            qp.signalListenMode = this.signalListenMode.GetValue(slate) ?? QuestPart.SignalListenMode.OngoingOnly;
            qp.sendLetter = this.sendStandardLetter.GetValue(slate) ?? false;
            QuestGen.quest.AddPart(qp);
        }
        public SlateRef<List<WorldObject>> siteToDestroy;
        public FactionDef faction;
    }
    [StaticConstructorOnStartup]
    public class QuestPart_BranchGoodwillChange : QuestPart
    {
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo globalTargetInfo in base.QuestLookTargets)
                {
                    yield return globalTargetInfo;
                }
                yield return this.lookTarget;
                yield break;
            }
        }
        public override IEnumerable<Faction> InvolvedFactions
        {
            get
            {
                foreach (Faction faction in base.InvolvedFactions)
                {
                    yield return faction;
                }
                if (this.faction != null)
                {
                    yield return this.faction;
                }
                yield break;
            }
        }
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal && this.faction != null && this.faction != Faction.OfPlayer)
            {
                if (this.lookTarget.IsValid)
                {
                    GlobalTargetInfo globalTargetInfo = this.lookTarget;
                } else if (this.getLookTargetFromSignal) {
                    if (SignalArgsUtility.TryGetLookTargets(signal.args, "SUBJECT", out LookTargets lookTargets))
                    {
                        lookTargets.TryGetPrimaryTarget();
                    } else {
                        GlobalTargetInfo invalid = GlobalTargetInfo.Invalid;
                    }
                } else {
                    GlobalTargetInfo invalid2 = GlobalTargetInfo.Invalid;
                }
                FactionRelationKind playerRelationKind = this.faction.PlayerRelationKind;
                int num = HVMP_Utility.ExpectationBasedGoodwillLoss(null, true, false, this.faction);
                if (this.ensureMakesHostile)
                {
                    num = Mathf.Min(num, Faction.OfPlayer.GoodwillToMakeHostile(this.faction));
                }
                Faction.OfPlayer.TryAffectGoodwillWith(this.faction, num, this.canSendMessage, this.canSendHostilityLetter, (num >= 0) ? (this.historyEvent ?? HistoryEventDefOf.QuestGoodwillReward) : this.historyEvent, null);
                TaggedString taggedString = "";
                this.faction.TryAppendRelationKindChangedInfo(ref taggedString, playerRelationKind, this.faction.PlayerRelationKind, null);
                if (!taggedString.NullOrEmpty())
                {
                    taggedString = "\n\n" + taggedString;
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<HistoryEventDef>(ref this.historyEvent, "historyEvent");
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_References.Look<Faction>(ref this.faction, "faction", false);
            Scribe_Values.Look<bool>(ref this.canSendMessage, "canSendMessage", true, false);
            Scribe_Values.Look<bool>(ref this.canSendHostilityLetter, "canSendHostilityLetter", true, false);
            Scribe_Values.Look<bool>(ref this.getLookTargetFromSignal, "getLookTargetFromSignal", true, false);
            Scribe_TargetInfo.Look(ref this.lookTarget, "lookTarget");
            Scribe_Values.Look<bool>(ref this.ensureMakesHostile, "ensureMakesHostile", false, false);
        }
        public override void AssignDebugData()
        {
            base.AssignDebugData();
            this.inSignal = "DebugSignal" + Rand.Int.ToString();
            this.faction = Find.FactionManager.RandomNonHostileFaction(false, false, false, TechLevel.Undefined);
        }
        public HistoryEventDef historyEvent;
        public string inSignal;
        public Faction faction;
        public bool canSendMessage = true;
        public bool canSendHostilityLetter = true;
        public bool getLookTargetFromSignal = true;
        public GlobalTargetInfo lookTarget;
        public bool ensureMakesHostile;
    }
    [StaticConstructorOnStartup]
    public class QuestPart_DestroySite : QuestPart_QuestEnd
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag == this.inSignal && this.worldObjects != null)
            {
                QuestEndOutcome questEndOutcome;
                if (this.outcome != null)
                {
                    questEndOutcome = this.outcome.Value;
                }
                else if (!signal.args.TryGetArg<QuestEndOutcome>("OUTCOME", out questEndOutcome))
                {
                    questEndOutcome = QuestEndOutcome.Unknown;
                }
                this.quest.End(questEndOutcome, this.sendLetter, this.playSound);
                foreach (WorldObject wo in this.worldObjects)
                {
                    if (!wo.Destroyed)
                    {
                        wo.Destroy();
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<WorldObject>(ref this.worldObjects, "worldObjects", LookMode.Reference, Array.Empty<object>());
        }
        public List<WorldObject> worldObjects;
    }
    public class QuestPart_LookAtThis : QuestPart
    {
        public QuestPart_LookAtThis() { }
        public QuestPart_LookAtThis(Thing thing)
        {
            this.thing = thing;
        }
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo globalTargetInfo in base.QuestLookTargets)
                {
                    yield return globalTargetInfo;
                }
                if (this.thing != null)
                {
                    yield return this.thing;
                }
                yield break;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Thing>(ref this.thing, "thing", false);
        }
        public override void Cleanup()
        {
            base.Cleanup();
            this.thing = null;
        }
        private Thing thing;
    }
    public class QuestNode_LookOverHere : QuestNode
    {
        protected override void RunInt()
        {
            if (this.worldObject != null)
            {
                QuestGen.quest.AddPart(new QuestPart_LookOverHere(worldObject));
            }
            if (this.worldObjects.GetValue(QuestGen.slate) != null)
            {
                foreach (WorldObject wobj in this.worldObjects.GetValue(QuestGen.slate))
                {
                    QuestGen.quest.AddPart(new QuestPart_LookOverHere(wobj));
                }
            }
        }
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        private WorldObject worldObject;
        public SlateRef<IEnumerable<WorldObject>> worldObjects;
    }
    public class QuestPart_LookOverHere : QuestPart
    {
        public QuestPart_LookOverHere() { }
        public QuestPart_LookOverHere(WorldObject wo)
        {
            this.wo = wo;
        }
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo globalTargetInfo in base.QuestLookTargets)
                {
                    yield return globalTargetInfo;
                }
                if (this.wo != null)
                {
                    yield return this.wo;
                }
                yield break;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<WorldObject>(ref this.wo, "wo", false);
        }
        public override void Cleanup()
        {
            base.Cleanup();
            this.wo = null;
        }
        private WorldObject wo;
    }
    public class StatPart_DirectlyAdjustedByGameCondition_World : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (this.ShouldWork(req))
            {
                val += this.offset;
                val *= this.factor;
            }
        }
        public override string ExplanationPart(StatRequest req)
        {
            if (this.ShouldWork(req))
            {
                string explanation = "";
                if (this.offset != 0)
                {
                    explanation += this.requiredCondition.LabelCap + ": +" + this.offset.ToStringPercent() + "\n";
                }
                if (this.offset != 1)
                {
                    explanation += this.requiredCondition.LabelCap + ": x" + this.factor.ToStringPercent();
                }
                return explanation;
            }
            return null;
        }
        public bool ShouldWork(StatRequest req)
        {
            return req.HasThing && req.Thing is Pawn p && Find.World.gameConditionManager.ConditionIsActive(this.requiredCondition) && (this.worksOnMechs || p.RaceProps.IsFlesh) && (!ModsConfig.AnomalyActive || ((this.worksOnMutants || !p.IsMutant) && (this.worksOnEntities || !p.IsEntity)));
        }
        public float offset;
        public float factor = 1f;
        public bool worksOnMechs = true;
        public bool worksOnMutants = true;
        public bool worksOnEntities = true;
        public GameConditionDef requiredCondition;
    }
    public class HediffCompProperties_ReliantOnGameCondition : HediffCompProperties
    {
        public HediffCompProperties_ReliantOnGameCondition()
        {
            this.compClass = typeof(HediffComp_ReliantOnGameCondition);
        }
        public GameConditionDef gameCondition;
        public bool dontAffectAnomalies;
        public bool dontAffectMechs;
    }
    public class HediffComp_ReliantOnGameCondition : HediffComp
    {
        public HediffCompProperties_ReliantOnGameCondition Props
        {
            get
            {
                return (HediffCompProperties_ReliantOnGameCondition)this.props;
            }
        }
        public override bool CompShouldRemove
        {
            get
            {
                if (this.Props.dontAffectMechs && !this.Pawn.RaceProps.IsFlesh)
                {
                    return true;
                }
                if (ModsConfig.AnomalyActive && this.Props.dontAffectAnomalies && (this.Pawn.IsMutant || this.Pawn.IsEntity))
                {
                    return true;
                }
                if (base.Pawn.SpawnedOrAnyParentSpawned)
                {
                    if (this.Pawn.MapHeld.gameConditionManager.ConditionIsActive(this.Props.gameCondition))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
    //branchquest mechanics: studiable items
    public class CompProperties_StudiableQuestItem : CompProperties_Interactable
    {
        public CompProperties_StudiableQuestItem()
        {
            this.compClass = typeof(CompStudiableQuestItem);
        }
        public FloatRange totalRequiredProgressHours;
        public List<SkillDef> possibleExtraSkillDefs;
        public List<SkillDef> requiredSkillDefs;
        public bool extraStat;
        public List<StatDef> possibleExtraStatDefsLikely;
        public List<StatDef> possibleExtraStatDefsProbable;
        public List<StatDef> possibleExtraStatDefsUnlikely;
        public List<StatDef> requiredStatDefs;
        public bool canStudyInPlace;
        public string progressInspectStringSkills;
        public string progressInspectStringStats;
        public bool mustBeOnList;
        public string notOnListstring;
    }
    public class CompStudiableQuestItem : CompInteractable
    {
        public new CompProperties_StudiableQuestItem Props
        {
            get
            {
                return (CompProperties_StudiableQuestItem)this.props;
            }
        }
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            Building_ResearchBench building_ResearchBench = null;
            if (base.ValidateTarget(target, false) && (this.Props.canStudyInPlace || StudyUtility.TryFindResearchBench(target.Pawn, out building_ResearchBench)))
            {
                target.Pawn.jobs.TryTakeOrderedJob(this.DoStudyJob(building_ResearchBench), new JobTag?(JobTag.Misc), false);
            }
        }
        public Job DoStudyJob(Thing brb)
        {
            ThingWithComps parent = this.parent;
            CompForbiddable compForbiddable = ((parent != null) ? parent.TryGetComp<CompForbiddable>() : null);
            if (compForbiddable != null)
            {
                compForbiddable.Forbidden = false;
            }
            return JobMaker.MakeJob(HVMPDefOf.HVMP_StudyQuestItem, this.parent, brb, (brb != null) ? brb.Position : IntVec3.Invalid);
        }
        public void Study(int delta, Pawn researcher, Thing brb, Thing researchBench)
        {
            if (researcher.skills != null)
            {
                float toAdd = 0f;
                foreach (SkillDef sd in this.relevantSkills)
                {
                    toAdd += researcher.skills.GetSkill(sd).Level / 4f;
                }
                foreach (StatDef sd in this.relevantStats)
                {
                    toAdd += researcher.GetStatValue(sd) + 1f - sd.defaultBaseValue;
                }
                if (researchBench != null)
                {
                    toAdd *= researchBench.GetStatValue(StatDefOf.ResearchSpeedFactor);
                }
                this.curProgress += (toAdd * (float)delta / 2500f);
                if (this.curProgress >= this.RequiredProgress)
                {
                    this.curProgress = this.RequiredProgress;
                }
                this.ExtraStudyEffects(delta,researcher,brb,researchBench);
            }
        }
        public virtual void ExtraStudyEffects(int delta, Pawn researcher, Thing brb, Thing researchBench)
        {

        }
        public override AcceptanceReport CanInteract(Pawn activateBy = null, bool checkOptionalItems = true)
        {
            AcceptanceReport acceptanceReport = base.CanInteract(activateBy, checkOptionalItems);
            if (!acceptanceReport.Accepted)
            {
                return acceptanceReport;
            }
            if (activateBy != null && StatDefOf.ResearchSpeed.Worker.IsDisabledFor(activateBy))
            {
                return "Incapable".Translate();
            }
            Building_ResearchBench building_ResearchBench;
            if (activateBy != null)
            {
                if (!this.Props.canStudyInPlace && !StudyUtility.TryFindResearchBench(activateBy, out building_ResearchBench))
                {
                    return "NoResearchBench".Translate();
                }
                if (!this.Props.canStudyInPlace && !this.parent.MapHeld.listerBuildings.ColonistsHaveResearchBench())
                {
                    return "NoResearchBench".Translate();
                }
                if (!this.PawnOnList(activateBy))
                {
                    return this.Props.notOnListstring.Translate();
                }
                if (this.PawnHasAnyUsableSkill(activateBy))
                {
                    return true;
                }
                return "HVMP_WrongSkillsToStudy".Translate();
            }
            return true;
        }
        public bool PawnOnList(Pawn pawn)
        {
            return !this.Props.mustBeOnList || this.pawns.Contains(pawn);
        }
        public bool PawnHasAnyUsableSkill(Pawn pawn)
        {
            if (pawn.skills != null)
            {
                foreach (SkillDef sd in this.relevantSkills)
                {
                    if (!pawn.skills.GetSkill(sd).TotallyDisabled)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public void OnAnalyzed(Pawn pawn)
        {
            if (!this.parent.questTags.NullOrEmpty())
            {
                QuestUtility.SendQuestTargetSignals(this.parent.questTags, "StudiableItemFinished", this.Named("SUBJECT"));
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            this.relevantSkills = new List<SkillDef>
            {
                SkillDefOf.Intellectual
            };
            this.reqProgress = this.Props.totalRequiredProgressHours.RandomInRange;
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (!this.parent.questTags.NullOrEmpty())
            {
                QuestUtility.SendQuestTargetSignals(this.parent.questTags, "StudiableItemDestroyed", this.Named("SUBJECT"), previousMap.Named("MAP"));
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Finish analysis",
                    action = delegate
                    {
                        this.OnAnalyzed(Find.CurrentMap.mapPawns.FreeColonistsSpawned.First<Pawn>());
                    }
                };
            }
            foreach (Gizmo gizmo3 in QuestUtility.GetQuestRelatedGizmos(this.parent))
            {
                yield return gizmo3;
            }
            yield break;
        }
        public override string CompInspectStringExtra()
        {
            string text = "HVMP_CCProgress".Translate(this.curProgress.ToStringByStyle(ToStringStyle.FloatOne), this.RequiredProgress.ToStringByStyle(ToStringStyle.FloatOne));
            if (!this.relevantSkills.NullOrEmpty())
            {
                text += "\n" + this.Props.progressInspectStringSkills.Translate();
                for (int i = 0; i < this.relevantSkills.Count; i++)
                {
                    text += this.relevantSkills[i].LabelCap;
                    if (i < this.relevantSkills.Count - 1)
                    {
                        text += ", ";
                    }
                }
            }
            if (!this.relevantStats.NullOrEmpty())
            {
                text += "\n" + this.Props.progressInspectStringStats.Translate();
                for (int i = 0; i < this.relevantStats.Count; i++)
                {
                    text += this.relevantStats[i].LabelCap;
                    if (i < this.relevantStats.Count - 1)
                    {
                        text += ", ";
                    }
                }
            }
            return text;
        }
        public virtual float RequiredProgress
        {
            get
            {
                return this.reqProgress * this.challengeRating;
            }
        }
        public bool Completed
        {
            get
            {
                return this.curProgress >= this.RequiredProgress;
            }
        }
        public float ProgressPercent
        {
            get
            {
                return this.curProgress / this.RequiredProgress;
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref this.curProgress, "curProgress", 0f, false);
            Scribe_Values.Look<float>(ref this.reqProgress, "reqProgress", 1f, false);
            Scribe_Values.Look<int>(ref this.challengeRating, "challengeRating", 1, false);
            Scribe_Collections.Look<SkillDef>(ref this.relevantSkills, "relevantSkills", LookMode.Undefined, LookMode.Undefined);
            Scribe_Collections.Look<StatDef>(ref this.relevantStats, "relevantStats", LookMode.Undefined, LookMode.Undefined);
            Scribe_Collections.Look<Pawn>(ref this.pawns, "pawns", LookMode.Reference, Array.Empty<object>());
        }
        public float curProgress;
        public float reqProgress;
        public int challengeRating = 1;
        public List<SkillDef> relevantSkills = new List<SkillDef>();
        public List<StatDef> relevantStats = new List<StatDef>();
        public List<Pawn> pawns = new List<Pawn>();
    }
    public class JobDriver_StudyQuestItem : JobDriver_StudyItem
    {
        public CompStudiableQuestItem StudiableComp
        {
            get
            {
                return base.ThingToStudy.TryGetComp<CompStudiableQuestItem>();
            }
        }
        protected override IEnumerable<Toil> GetStudyToils()
        {
            Toil study = ToilMaker.MakeToil("GetStudyToils");
            study.tickIntervalAction = delegate(int delta)
            {
                Pawn actor = study.actor;
                study.handlingFacing = true;
                study.tickIntervalAction = delegate
                {
                    actor.rotationTracker.FaceTarget(this.job.GetTarget(TargetIndex.A));
                    this.StudiableComp.Study(delta, actor, this.TargetThingB, this.job.GetTarget(TargetIndex.A).Thing ?? null);
                    if (!this.StudiableComp.relevantSkills.NullOrEmpty())
                    {
                        foreach (SkillDef sd in this.StudiableComp.relevantSkills)
                        {
                            actor.skills.Learn(sd, 0.1f*(float)delta, false, false);
                        }
                    }
                    actor.GainComfortFromCellIfPossible(delta, true);
                    if (this.StudiableComp.Completed)
                    {
                        this.StudiableComp.OnAnalyzed(actor);
                        actor.jobs.curDriver.ReadyForNextToil();
                    }
                };
            };
            study.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            study.WithProgressBar(TargetIndex.A, () => this.StudiableComp.ProgressPercent, false, -0.5f, false);
            study.defaultCompleteMode = ToilCompleteMode.Delay;
            study.defaultDuration = 2500;
            study.activeSkill = () => SkillDefOf.Intellectual;
            yield return study;
            yield break;
        }
    }
    public class WorkGiver_StudyQuestItem : WorkGiver_Scanner
    {
        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Some;
        }
        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.ResearchBench);
            }
        }
        public override bool Prioritized
        {
            get
            {
                return true;
            }
        }
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (!pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.ResearchBench).NullOrEmpty())
            {
                foreach (Thing t in pawn.Map.listerThings.AllThings)
                {
                    if (this.def.fixedBillGiverDefs.Contains(t.def))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            if (pawn.CanReserve(thing, 1, -1, null, forced) && (!thing.def.hasInteractionCell || pawn.CanReserveSittableOrSpot(thing.InteractionCell, forced)))
            {
                foreach (Thing t in pawn.Map.listerThings.AllThings)
                {
                    if (this.def.fixedBillGiverDefs.Contains(t.def) && pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Deadly) && !t.IsForbidden(pawn) && !t.Fogged())
                    {
                        CompStudiableQuestItem cda = t.TryGetComp<CompStudiableQuestItem>();
                        if (cda != null && cda.PawnOnList(pawn) && cda.PawnHasAnyUsableSkill(pawn))
                        {
                            return cda.DoStudyJob(thing);
                        }
                    }
                }
            }
            return null;
        }
    }
    //branchquest mechanics: other
    public class GameCondition_MutationDummy : GameCondition
    {

    }
    //commerce branchquest: fortification
    public class QuestNode_Root_Fortification : QuestNode_Sequence
    {
        protected override void RunInt()
        {
            if (HVMP_Utility.TryFindCommerceFaction(out Faction faction))
            {
                Slate slate = QuestGen.slate;
                Quest quest = QuestGen.quest;
                slate.Set<Faction>("faction", faction, false);
                slate.Set<Pawn>("asker", faction.leader, false);
                bool TTOT_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.fort2, HVMP_Mod.settings.fortX);
                slate.Set<bool>("punishmentOnDestroy", TTOT_on || Rand.Chance(0.5f), false);
                if (TTOT_on)
                {
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_TTOT_info", this.TTOT_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_TTOT_info", " ") });
                }
                slate.Set<bool>(this.storeTTOT.GetValue(slate), TTOT_on, false);
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
            return HVMP_Utility.TryFindCommerceFaction(out Faction commerceFaction) && base.TestRunInt(slate);
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        [NoTranslate]
        public SlateRef<string> storeTTOT;
        [MustTranslate]
        public string TTOT_description;
    }
    public class QuestNode_Fortification_TTOT : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            this.SetVars(slate);
            return true;
        }
        protected override void RunInt()
        {
            this.SetVars(QuestGen.slate);
        }
        private void SetVars(Slate slate)
        {
            slate.Set<int>(this.nameTime.GetValue(slate), (int)(this.valueTime.RandomInRange * this.valueTimeUnits * (this.TTOT_enabled.GetValue(slate) ? this.timeFactorTTOT : 1f)), false);
            slate.Set<int>(this.nameGoodwill.GetValue(slate), (int)(this.valueGoodwill * (this.TTOT_enabled.GetValue(slate) ? this.goodwillFactorTTOT : 1f)), false);
        }
        [NoTranslate]
        public SlateRef<string> nameTime;
        public IntRange valueTime;
        public int valueTimeUnits;
        public float timeFactorTTOT;
        [NoTranslate]
        public SlateRef<string> nameGoodwill;
        public int valueGoodwill;
        public float goodwillFactorTTOT;
        public SlateRef<bool> TTOT_enabled;
    }
    public class QuestNode_Multiply_RM : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return !this.storeAs.GetValue(slate).NullOrEmpty();
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            slate.Set<double>(this.storeAs.GetValue(slate), this.value1.GetValue(slate) * this.value2.GetValue(slate) * (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.fort1, HVMP_Mod.settings.fortX) ? this.RM_factor : 1d), false);
        }
        public SlateRef<double> value1;
        public SlateRef<double> value2;
        public double RM_factor;
        [NoTranslate]
        public SlateRef<string> storeAs;
    }
    public class QuestNode_Fortification_UTTN : QuestNode
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
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.fort3, HVMP_Mod.settings.fortX))
            {
                largestSize = (int)Math.Max(largestSize * this.UTTN_factor, largestSize + this.UTTN_minBonus);
                slate.Set<float>(this.UTTN_scalarSaveAs.GetValue(slate), this.UTTN_scalarIfOn, false);
            } else {
                slate.Set<float>(this.UTTN_scalarSaveAs.GetValue(slate), this.UTTN_scalarIfOff, false);
            }
            slate.Set<int>(this.storeAs.GetValue(slate), largestSize, false);
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
            return Mathf.Min(new int[] { cellRect.Width, cellRect.Height, value });
        }
        private bool IsClear(IntVec3 c, Map map)
        {
            if (!c.GetAffordances(map).Contains(TerrainAffordanceDefOf.Heavy))
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
        public int UTTN_minBonus;
        public float UTTN_factor = 1f;
        [NoTranslate]
        public SlateRef<string> UTTN_scalarSaveAs;
        public float UTTN_scalarIfOff;
        public float UTTN_scalarIfOn;
    }
    //commerce branchquest: intervention
    public class QuestNode_InterventionInner : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return Find.Storyteller.difficulty.allowViolentQuests && slate.Exists("map", false) && Faction.OfInsects != null;
        }
        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            Map map = slate.Get<Map>("map", null, false);
            float num = slate.Get<float>("points", 0f, false);
            Faction faction = Faction.OfInsects;
            QuestPart_Incident questPart_Incident = new QuestPart_Incident();
            questPart_Incident.debugLabel = "raid";
            questPart_Incident.incident = this.incidentDef;
            this.ImplementQuestMutators(slate, quest);
            IncidentParms incidentParms;
            incidentParms = this.GenerateIncidentParms(map, num, faction, slate, quest, questPart_Incident);
            questPart_Incident.SetIncidentParmsAndRemoveTarget(incidentParms);
            questPart_Incident.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
            quest.AddPart(questPart_Incident);
        }
        private IncidentParms GenerateIncidentParms(Map map, float points, Faction faction, Slate slate, Quest quest, QuestPart_Incident questPart)
        {
            IncidentParms incidentParms = new IncidentParms();
            incidentParms.forced = true;
            incidentParms.target = map;
            incidentParms.points = slate.Get<float>("points", 0f, false);
            incidentParms.faction = faction;
            incidentParms.pawnGroupMakerSeed = new int?(Rand.Int);
            incidentParms.inSignalEnd = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignalLeave.GetValue(slate));
            incidentParms.questTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(this.tag.GetValue(slate));
            incidentParms.quest = quest;
            incidentParms.canTimeoutOrFlee = false;
            return incidentParms;
        }
        public void ImplementQuestMutators(Slate slate, Quest quest)
        {
            QuestPart_OtherTwoInterventionMutators qpa3 = new QuestPart_OtherTwoInterventionMutators();
            bool mayhemMode = HVMP_Mod.settings.intervX;
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.interv2, mayhemMode))
            {
                qpa3.II_hediff = this.II_hediff;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_II_info", this.II_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_II_info", " ") });
            }
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.interv3, mayhemMode))
            {
                qpa3.JB_hediff = this.JB_hediff;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_JB_info", this.JB_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_JB_info", " ") });
            }
            quest.AddPart(qpa3);
        }
        [NoTranslate]
        public SlateRef<string> inSignal;
        [NoTranslate]
        public SlateRef<string> inSignalLeave;
        [NoTranslate]
        public SlateRef<string> tag;
        public IncidentDef incidentDef;
        public HediffDef II_hediff;
        [MustTranslate]
        public string II_description;
        public HediffDef JB_hediff;
        [MustTranslate]
        public string JB_description;
    }
    public class QuestNode_Intervention_BUB : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return this.BUB_hediff != null && slate.Exists("map", false);
        }
        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.interv1, HVMP_Mod.settings.intervX))
            {
                QuestPart_Intervention_BUB qpBUB = new QuestPart_Intervention_BUB();
                qpBUB.BUB_hediff = this.BUB_hediff;
                qpBUB.map = QuestGen.slate.Get<Map>("map", null, false);
                qpBUB.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(quest.InitiateSignal);
                quest.AddPart(qpBUB);
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_BUB_info", this.BUB_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_BUB_info", " ") });
            }
        }
        public HediffDef BUB_hediff;
        [MustTranslate]
        public string BUB_description;
    }
    public class QuestPart_Intervention_BUB : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                if (this.BUB_hediff != null)
                {
                    foreach (Pawn p in this.map.mapPawns.AllPawnsSpawned)
                    {
                        if (!p.RaceProps.Insect)
                        {
                            p.health.AddHediff(this.BUB_hediff);
                            if (TameUtility.CanTame(p) && !p.InMentalState)
                            {
                                p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, null, false, true, false, null, false, false, false);
                            }
                        }
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<HediffDef>(ref this.BUB_hediff, "BUB_hediff");
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_References.Look<Map>(ref this.map, "map", false);
        }
        public HediffDef BUB_hediff;
        public string inSignal;
        public Map map;
    }
    public class HediffCompProperties_SeverityPerDay_BUB : HediffCompProperties_SeverityPerDay
    {
        public HediffCompProperties_SeverityPerDay_BUB()
        {
            this.compClass = typeof(HediffComp_SeverityPerDay_BUB);
        }
        public StatDef adjustingStat;
    }
    public class HediffComp_SeverityPerDay_BUB : HediffComp_SeverityPerDay
    {
        private HediffCompProperties_SeverityPerDay_BUB Props
        {
            get
            {
                return (HediffCompProperties_SeverityPerDay_BUB)this.props;
            }
        }
        public override float SeverityChangePerDay()
        {
            return base.SeverityChangePerDay()*(1f+this.Pawn.GetStatValue(this.Props.adjustingStat));
        }
    }
    public class QuestPart_OtherTwoInterventionMutators : QuestPart
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<HediffDef>(ref this.II_hediff, "II_hediff");
            Scribe_Defs.Look<HediffDef>(ref this.JB_hediff, "JB_hediff");
        }
        public HediffDef II_hediff;
        public HediffDef JB_hediff;
    }
    public class IncidentWorker_InterventionInfestation : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
            {
                return false;
            }
            if (Faction.OfInsects == null)
            {
                return false;
            }
            Map map = (Map)parms.target;
            return true;
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 intVec;
            Func<IntVec3, bool> validator = delegate (IntVec3 x)
            {
                if (!x.Standable(map) || x.Fogged(map))
                {
                    return false;
                }
                return true;
            };
            IntVec3 loc;
            Faction hostFaction = map.ParentFaction ?? Faction.OfPlayer;
            IEnumerable<Thing> enumerable = map.mapPawns.FreeHumanlikesSpawnedOfFaction(hostFaction).Cast<Thing>();
            if (hostFaction == Faction.OfPlayer)
            {
                enumerable = enumerable.Concat(map.listerBuildings.allBuildingsColonist.Cast<Thing>());
            } else {
                enumerable = enumerable.Concat(from x in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
                                               where x.Faction == hostFaction
                                               select x);
            }
            int num = 0;
            float num2 = 65f;
            for (; ; )
            {
                intVec = CellFinder.RandomCell(map);
                num++;
                if (!intVec.Fogged(map) && intVec.Standable(map))
                {
                    if (num > 300)
                    {
                        break;
                    }
                    num2 -= 0.2f;
                    bool flag = false;
                    foreach (Thing thing in enumerable)
                    {
                        if ((float)(intVec - thing.Position).LengthHorizontalSquared < num2 * num2)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag && map.reachability.CanReachFactionBase(intVec, hostFaction))
                    {
                        loc = intVec;
                    }
                }
            }
            loc = intVec;
            TunnelHiveSpawner_Intervention tunnelHiveSpawner = (TunnelHiveSpawner_Intervention)ThingMaker.MakeThing(HVMPDefOf.HVMP_TunnelHiveSpawner, null);
            tunnelHiveSpawner.spawnHive = false;
            tunnelHiveSpawner.quest = parms.quest;
            tunnelHiveSpawner.insectsPoints = parms.points * Rand.Range(0.3f, 0.6f);
            tunnelHiveSpawner.spawnedByInfestationThingComp = true;
            GenSpawn.Spawn(tunnelHiveSpawner, loc, map, WipeMode.FullRefund);
            base.SendStandardLetter(parms, new TargetInfo(tunnelHiveSpawner.Position, map, false), Array.Empty<NamedArgument>());
            return true;
        }
    }
    public class TunnelHiveSpawner_Intervention : TunnelHiveSpawner
    {
        protected override void Spawn(Map map, IntVec3 loc)
        {
            if (this.spawnHive)
            {
                HiveUtility.SpawnHive(loc, map, WipeMode.FullRefund, false, true, true, false, true, true, false).questTags = this.questTags;
            }
            if (this.insectsPoints > 0f)
            {
                this.insectsPoints = Mathf.Max(this.insectsPoints, Hive.spawnablePawnKinds.Min((PawnKindDef x) => x.combatPower));
                float pointsLeft = this.insectsPoints;
                List<Pawn> list = new List<Pawn>();
                int num = 0;
                HediffDef iiHediff = null, jbHediff = null;
                QuestPart_OtherTwoInterventionMutators qpa3 = this.quest.GetFirstPartOfType<QuestPart_OtherTwoInterventionMutators>();
                if (qpa3 != null)
                {
                    iiHediff = qpa3.II_hediff;
                    jbHediff = qpa3.JB_hediff;
                }
                while (pointsLeft > 0f)
                {
                    num++;
                    if (num > 1000)
                    {
                        Log.Error("Too many iterations.");
                        break;
                    }
                    IEnumerable<PawnKindDef> spawnablePawnKinds = Hive.spawnablePawnKinds;
                    if (!spawnablePawnKinds.Where((PawnKindDef x) => x.combatPower <= pointsLeft).TryRandomElement(out PawnKindDef pawnKindDef))
                    {
                        break;
                    }
                    Pawn pawn = PawnGenerator.GeneratePawn(pawnKindDef, Faction.OfInsects, null);
                    GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(loc, map, 2, null), map, WipeMode.Vanish);
                    pawn.mindState.spawnedByInfestationThingComp = this.spawnedByInfestationThingComp;
                    if (iiHediff != null)
                    {
                        pawn.health.AddHediff(iiHediff);
                    }
                    if (jbHediff != null)
                    {
                        pawn.health.AddHediff(jbHediff);
                    }
                    list.Add(pawn);
                    pointsLeft -= pawnKindDef.combatPower;
                    if (ModsConfig.BiotechActive)
                    {
                        PollutionUtility.Notify_TunnelHiveSpawnedInsect(pawn);
                    }
                }
                if (list.Any<Pawn>())
                {
                    LordMaker.MakeNewLord(Faction.OfInsects, new LordJob_AssaultColony(Faction.OfInsects, true, false, false, false, true, false, false), map, list);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Quest>(ref this.quest, "quest", false);
        }
        public Quest quest;
    }
    public class Hediff_ScariaVector : Hediff
    {
        public override void Notify_PawnDamagedThing(Thing thing, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            if (result.hediffs.NullOrEmpty<Hediff>())
            {
                return;
            }
            foreach (Hediff hediff in result.hediffs)
            {
                HediffComp_Infecter hediffComp_Infecter = hediff.TryGetComp<HediffComp_Infecter>();
                if (hediffComp_Infecter != null)
                {
                    hediffComp_Infecter.fromScaria = true;
                }
            }
        }
    }
    //commerce branchquest: mastermind
    public class QuestNode_GiveRewardsMastermind : QuestNode_GiveRewardsBranch
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;
            WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
            if (wcbs != null)
            {
                slate.Set<int>(this.tradeBlocksRemaining.GetValue(slate), wcbs.tradeBlockages, false);
                QuestPart_TradeBlocker qptb = new QuestPart_TradeBlocker();
                qptb.map = QuestGen.slate.Get<Map>("map", null, false);
                qptb.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(quest.InitiateSignal);
                quest.AddPart(qptb);
                bool mayhemMode = HVMP_Mod.settings.mmX;
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.mm1, mayhemMode))
                {
                    qptb.CB_gcd = this.CB_conditionDef;
                    qptb.CB_duration = this.CB_duration.RandomInRange;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_CB_info", this.CB_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_CB_info", " ") });
                }
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.mm2, mayhemMode))
                {
                    qptb.IIP_pointFactor = this.IIP_pointFactor;
                    qptb.doIIP = true;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_IIP_info", this.IIP_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_IIP_info", " ") });
                }
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.mm3, mayhemMode))
                {
                    qptb.doUU = true;
                    qptb.UU_allyMax = this.UU_goodwillPenaltyAlly.max;
                    qptb.UU_allyMin = this.UU_goodwillPenaltyAlly.min;
                    qptb.UU_neutMax = this.UU_goodwillPenaltyNeutral.max;
                    qptb.UU_neutMin = this.UU_goodwillPenaltyNeutral.min;
                    qptb.UU_hostMax = this.UU_goodwillPenaltyHostile.max;
                    qptb.UU_hostMin = this.UU_goodwillPenaltyHostile.min;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_UU_info", this.UU_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_UU_info", " ") });
                }
            }
            base.RunInt();
        }
        public override string LetterText()
        {
            WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
            if (wcbs != null)
            {
                return base.LetterText().Translate(wcbs.tradeBlockages);
            }
            return base.LetterText();
        }
        [NoTranslate]
        public SlateRef<string> tradeBlocksRemaining;
        public GameConditionDef CB_conditionDef;
        public IntRange CB_duration;
        [MustTranslate]
        public string CB_description;
        public float IIP_pointFactor;
        [MustTranslate]
        public string IIP_description;
        public IntRange UU_goodwillPenaltyAlly;
        public IntRange UU_goodwillPenaltyNeutral;
        public IntRange UU_goodwillPenaltyHostile;
        [MustTranslate]
        public string UU_description;
    }
    public class QuestPart_TradeBlocker : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
                if (wcbs != null)
                {
                    wcbs.tradeBlockages++;
                }
                Faction p = Faction.OfPlayerSilentFail;
                if (p != null)
                {
                    if (this.CB_gcd != null)
                    {
                        GameCondition gameCondition = GameConditionMaker.MakeCondition(this.CB_gcd, -1);
                        gameCondition.Duration = this.CB_duration;
                        Find.World.GameConditionManager.RegisterCondition(gameCondition);
                    }
                    if (this.doIIP)
                    {
                        WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
                        if (WCFC != null)
                        {
                            float spyPoints = 0f;
                            Map m = this.map??Find.AnyPlayerHomeMap;
                            if (m != null)
                            {
                                spyPoints = StorytellerUtility.DefaultThreatPointsNow(m);
                            }
                            spyPoints *= this.IIP_pointFactor;
                            foreach (Faction f in Find.FactionManager.AllFactionsListForReading)
                            {
                                if (f != p && f.RelationKindWith(p) == FactionRelationKind.Hostile)
                                {
                                    Hauts_FactionCompHolder fch = WCFC.FindCompsFor(f);
                                    if (fch != null)
                                    {
                                        HautsFactionComp_SpyPoints spc = fch.TryGetComp<HautsFactionComp_SpyPoints>();
                                        if (spc != null)
                                        {
                                            spc.spyPoints += (int)spyPoints;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (this.doUU)
                    {
                        IntRange allyRange = new IntRange(this.UU_allyMin, this.UU_allyMax);
                        IntRange neutRange = new IntRange(this.UU_neutMin, this.UU_neutMax);
                        IntRange hostRange = new IntRange(this.UU_hostMin, this.UU_hostMax);
                        foreach (Faction f in Find.FactionManager.AllFactionsListForReading)
                        {
                            if (f != p)
                            {
                                EBranchQuests gq = f.def.GetModExtension<EBranchQuests>();
                                if (gq == null)
                                {
                                    switch (f.RelationKindWith(p))
                                    {
                                        case FactionRelationKind.Ally:
                                            f.TryAffectGoodwillWith(p, allyRange.RandomInRange, false, true);
                                            break;
                                        case FactionRelationKind.Hostile:
                                            f.TryAffectGoodwillWith(p, hostRange.RandomInRange, false, true);
                                            break;
                                        default:
                                            f.TryAffectGoodwillWith(p, neutRange.RandomInRange, false, true);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_Defs.Look<GameConditionDef>(ref this.CB_gcd, "CB_gcd");
            Scribe_Values.Look<int>(ref this.CB_duration, "CB_duration", 0, false);
            Scribe_Values.Look<bool>(ref this.doIIP, "doCB", false, false);
            Scribe_Values.Look<bool>(ref this.doUU, "doCB", false, false);
            Scribe_Values.Look<float>(ref this.IIP_pointFactor, "IIP_pointFactor", 0.5f, false);
            Scribe_Values.Look<int>(ref this.UU_allyMax, "UU_allyMax", 0, false);
            Scribe_Values.Look<int>(ref this.UU_allyMin, "UU_allyMin", 0, false);
            Scribe_Values.Look<int>(ref this.UU_neutMax, "UU_neutMax", 0, false);
            Scribe_Values.Look<int>(ref this.UU_neutMin, "UU_neutMin", 0, false);
            Scribe_Values.Look<int>(ref this.UU_hostMax, "UU_hostMax", 0, false);
            Scribe_Values.Look<int>(ref this.UU_hostMin, "UU_hostMin", 0, false);
            Scribe_References.Look<Map>(ref this.map, "map", false);
        }
        public string inSignal;
        public GameConditionDef CB_gcd;
        public int CB_duration;
        public float IIP_pointFactor;
        public bool doIIP;
        public bool doUU;
        public int UU_allyMax;
        public int UU_allyMin;
        public int UU_neutMax;
        public int UU_neutMin;
        public int UU_hostMax;
        public int UU_hostMin;
        public Map map;
    }
    //commerce branchquest: research
    public class QuestNode_GenerateChainComputer : QuestNode
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
            CompChainComputer ccc = thing.TryGetComp<CompChainComputer>();
            if (ccc != null)
            {
                ccc.challengeRating = challengeRating;
                bool mayhemMode = HVMP_Mod.settings.researchX;
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.research1, mayhemMode))
                {
                    ccc.needsPsyConnection = true;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_BD_info", this.BD_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_BD_info", " ") });
                }
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.research2, mayhemMode))
                {
                    CompHeatPusher_OI chpoi = thing.TryGetComp<CompHeatPusher_OI>();
                    if (chpoi != null)
                    {
                        chpoi.enabled = true;
                    }
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_OI_info", this.OI_description.Formatted())
                    });
                } else {
                    CompHeatPusher_OI chpoi = thing.TryGetComp<CompHeatPusher_OI>();
                    if (chpoi != null)
                    {
                        chpoi.enabled = false;
                    }
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_OI_info", " ") });
                }
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.research3, mayhemMode))
                {
                    ccc.captchaThreshold = ccc.Props.PVYAH_captchaAtPctProgress.RandomInRange*ccc.RequiredProgress;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_PVYAH_info", this.PVYAH_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_PVYAH_info", " ") });
                }
            }
            string srpa;
            switch (challengeRating)
            {
                case 1:
                    srpa = "HVMP_InterventionCR1";
                    break;
                case 2:
                    srpa = "HVMP_InterventionCR2";
                    break;
                case 3:
                    srpa = "HVMP_InterventionCR3";
                    break;
                default:
                    srpa = "HVMP_InterventionCR1";
                    break;
            }
            slate.Set<string>(this.storeReqProgressAs.GetValue(slate), srpa.Translate(), false);
            slate.Set<Thing>(this.storeAs.GetValue(slate), thing, false);
            QuestGen.quest.AddPart(new QuestPart_LookAtThis(thing));
        }
        [NoTranslate]
        public SlateRef<string> storeAs;
        public ThingDef thingDef;
        [NoTranslate]
        public SlateRef<string> storeReqProgressAs;
        [MustTranslate]
        public string BD_description;
        [MustTranslate]
        public string OI_description;
        [MustTranslate]
        public string PVYAH_description;
    }
    public class CompProperties_ChainComputer : CompProperties_Hackable
    {
        public CompProperties_ChainComputer()
        {
            this.compClass = typeof(CompChainComputer);
        }
        public float maxProgress = 100f;
		public float baseProgressPerTick;
		public float powerInterval = 1f;
		public float maxExternalPower = 1f;
		public float bonusProgressPerInterval;
        [NoTranslate]
        public string texUp = "UI/Commands/TempRaise";
        [NoTranslate]
        public string labelUp = "HVMP_CCompToggleUpLabel";
        [NoTranslate]
        public string tooltipUp = "HVMP_CCompToggleUpTooltip";
        [NoTranslate]
        public string texDown = "UI/Commands/TempLower";
        [NoTranslate]
        public string labelDown = "HVMP_CCompToggleDownLabel";
        [NoTranslate]
        public string tooltipDown = "HVMP_CCompToggleDownTooltip";
        public float raidMTBdays;
        public int minTimeBetweenRaids;
        public float raidPointFactor;
        public HediffDef BD_hediff;
        public TargetingParameters BD_targetingParameters;
        [NoTranslate]
        public string texBD = "UI/Abilities/Focus";
        [NoTranslate]
        public string labelBD = "HVMP_CComp_BDLabel";
        [NoTranslate]
        public string tooltipBD = "HVMP_CComp_BDTooltip";
        public FloatRange PVYAH_captchaAtPctProgress;
        public string PVYAH_messageLabel;
        public string PVYAH_messageText;
        public ColorInt PVYAH_mustHackColor;
    }
    public class CompChainComputer : CompHackable, ITargetingSource
    {
        public new CompProperties_ChainComputer Props
        {
            get
            {
                return (CompProperties_ChainComputer)this.props;
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (this.captchaThreshold > 0f)
            {
                foreach (Gizmo gizmo in base.CompGetGizmosExtra())
                {
                    yield return gizmo;
                }
            }
            yield return new Command_Action
            {
                icon = this.TexUp,
                defaultLabel = this.Props.labelUp.Translate(),
                defaultDesc = this.Props.tooltipUp.Translate(),
                action = delegate
                {
                    this.curPowerConsumption = Math.Min(this.curPowerConsumption + this.Props.powerInterval, this.Props.maxExternalPower);
                }
            };
            yield return new Command_Action
            {
                icon = this.TexDown,
                defaultLabel = this.Props.labelDown.Translate(),
                defaultDesc = this.Props.tooltipDown.Translate(),
                action = delegate
                {
                    this.curPowerConsumption = Math.Max(this.curPowerConsumption - this.Props.powerInterval, 0f);
                }
            };
            if (this.needsPsyConnection && this.linkedPawn == null)
            {
                yield return new Command_Action
                {
                    icon = this.UIIcon,
                    defaultLabel = this.Props.labelBD.Translate(),
                    defaultDesc = this.Props.tooltipBD.Translate(),
                    action = delegate
                    {
                        Find.Targeter.BeginTargeting(this, null, false, null, null, true);
                    }
                };
            }
            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Finish computation",
                    action = delegate
                    {
                        this.FinishComputation();
                    }
                };
            }
            foreach (Gizmo gizmo3 in QuestUtility.GetQuestRelatedGizmos(this.parent))
            {
                yield return gizmo3;
            }
            yield break;
        }
        public bool CanHitTarget(LocalTargetInfo target)
        {
            return this.ValidateTarget(target, false);
        }
        public bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!target.IsValid || target.Pawn == null)
            {
                return false;
            }
            Pawn pawn = target.Pawn;
            AcceptanceReport acceptanceReport = this.CanInteract(pawn, true);
            if (!acceptanceReport.Accepted)
            {
                if (showMessages && !acceptanceReport.Reason.NullOrEmpty())
                {
                    Messages.Message("HVMP_CannotBD".Translate() + ": " + acceptanceReport.Reason.CapitalizeFirst(), pawn, MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            return true;
        }
        public AcceptanceReport CanInteract(Pawn activateBy = null, bool checkOptionalItems = true)
        {
            if (!this.needsPsyConnection || this.linkedPawn != null)
            {
                return "AlreadyActive".Translate();
            }
            if (activateBy != null)
            {
                if (activateBy.Dead)
                {
                    return "PawnIsDead".Translate(activateBy);
                }
                if (activateBy.GetStatValue(StatDefOf.PsychicSensitivity) <= float.Epsilon || !activateBy.IsColonistPlayerControlled)
                {
                    return "Incapable".Translate().CapitalizeFirst();
                }
                if (activateBy.Downed)
                {
                    return "MessageRitualPawnDowned".Translate(activateBy);
                }
            }
            return true;
        }
        public void DrawHighlight(LocalTargetInfo target)
        {
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }
        public void OrderForceTarget(LocalTargetInfo target)
        {
            if (this.ValidateTarget(target, false) && target.Pawn != null)
            {
                this.PsychoConnectPawn(target.Pawn);
            }
        }
        public void PsychoConnectPawn(Pawn p)
        {
            Hediff h = HediffMaker.MakeHediff(this.Props.BD_hediff, p);
            p.health.AddHediff(h);
            HediffComp_BrainDrain hcbd = h.TryGetComp<HediffComp_BrainDrain>();
            if (hcbd != null)
            {
                hcbd.other = this.parent;
            }
            this.linkedPawn = p;
        }
        public void OnGUI(LocalTargetInfo target)
        {
            string text = "ChooseWhoShouldActivate".Translate();
            Widgets.MouseAttachedLabel(text, 0f, 0f, null);
            if (this.ValidateTarget(target, false) && this.Props.BD_targetingParameters.CanTarget(target.Pawn, this))
            {
                GenUI.DrawMouseAttachment(this.UIIcon);
                return;
            }
            GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
        }
        public bool CasterIsPawn
        {
            get
            {
                return true;
            }
        }
        public bool IsMeleeAttack
        {
            get
            {
                return false;
            }
        }
        public bool Targetable
        {
            get
            {
                return true;
            }
        }
        public bool MultiSelect
        {
            get
            {
                return false;
            }
        }
        public bool HidePawnTooltips
        {
            get
            {
                return false;
            }
        }
        public Thing Caster
        {
            get
            {
                return this.parent;
            }
        }
        public Pawn CasterPawn
        {
            get
            {
                return null;
            }
        }
        public Verb GetVerb
        {
            get
            {
                return null;
            }
        }
        public TargetingParameters targetParams
        {
            get
            {
                return this.Props.BD_targetingParameters;
            }
        }
        public ITargetingSource DestinationSelector
        {
            get
            {
                return null;
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            this.raidCD = 0;
        }
        public AcceptanceReport CanMakeProgress()
        {
            if (this.needsPsyConnection && this.linkedPawn == null)
            {
                return "HVMP_RequiresBD".Translate();
            }
            if (this.canBeHackedNow)
            {
                return "HVMP_RequiresPVYAH".Translate();
            }
            return true;
        }
        protected override void OnHacked(Pawn hacker = null, bool suppressMessages = false)
        {
            base.OnHacked(hacker, suppressMessages);
            CompGlower cg = this.parent.GetComp<CompGlower>();
            if (cg != null)
            {
                cg.GlowColor = cg.Props.glowColor;
            }
            this.canBeHackedNow = false;
        }
        protected override void OnLockedOut(Pawn hacker = null)
        {
            base.OnLockedOut(hacker);
            if (this.parent.Spawned)
            {
                if (Rand.Chance(0.5f))
                {
                    this.ResetStaticData();
                    CompChainComputer.Option option = CompChainComputer.options.RandomElement();
                    GameCondition gameCondition = GameConditionMaker.MakeCondition(option.gameCondition, (int)(option.durationDaysRange.RandomInRange * 60000f));
                    gameCondition.forceDisplayAsDuration = true;
                    gameCondition.Permanent = false;
                    List<Rule> listRule = new List<Rule>();
                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                    gameCondition.RandomizeSettings(1000f, this.parent.MapHeld, listRule, dictionary);
                    this.parent.MapHeld.gameConditionManager.RegisterCondition(gameCondition);
                    Find.LetterStack.ReceiveLetter(gameCondition.LabelCap, gameCondition.LetterText, gameCondition.def.letterDef, LookTargets.Invalid, null, gameCondition.quest, null, null, 0, true);
                } else {
                    MechClusterSketch mechClusterSketch = MechClusterGenerator.GenerateClusterSketch(StorytellerUtility.DefaultThreatPointsNow(this.parent.Map), this.parent.MapHeld, true, false);
                    IntVec3 intVec = MechClusterUtility.FindClusterPosition(this.parent.MapHeld, mechClusterSketch, 100, 0.5f);
                    if (!intVec.IsValid)
                    {
                        return;
                    }
                    IEnumerable<Thing> enumerable = from t in MechClusterUtility.SpawnCluster(intVec, this.parent.MapHeld, mechClusterSketch, true, true, null)
                                                    where t.def != ThingDefOf.Wall && t.def != ThingDefOf.Barricade
                                                    select t;
                    Find.LetterStack.ReceiveLetter("LetterLabelMechClusterArrived".Translate(), "LetterMechClusterArrived".Translate(), LetterDefOf.ThreatBig, new TargetInfo(intVec, this.parent.MapHeld, false), null, null, null, null, 0, true);
                }
            }
        }
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (this.PowerTrader != null)
            {
                this.PowerTrader.PowerOutput = -this.curPowerConsumption;
            }
            if (!this.parent.questTags.NullOrEmpty())
            {
                AcceptanceReport acceptanceReport = this.CanMakeProgress();
                if (acceptanceReport)
                {
                    this.curProgress += this.Props.baseProgressPerTick * (float)delta;
                    if (!this.IsHacked && this.captchaThreshold > 0f && this.curProgress >= this.captchaThreshold)
                    {
                        Find.LetterStack.ReceiveLetter(this.Props.PVYAH_messageLabel, this.Props.PVYAH_messageText, LetterDefOf.NeutralEvent, this.parent);
                        CompGlower cg = this.parent.GetComp<CompGlower>();
                        if (cg != null)
                        {
                            cg.GlowColor = this.Props.PVYAH_mustHackColor;
                        }
                        this.canBeHackedNow = true;
                        return;
                    }
                    if (this.PowerTrader.PowerOn)
                    {
                        float chanceMod = this.curPowerConsumption / this.Props.maxExternalPower;
                        this.curProgress += this.Props.bonusProgressPerInterval * (float)delta * (this.curPowerConsumption / this.Props.powerInterval);
                        if (this.raidCD <= 0 && this.parent.IsHashIntervalTick(2500, delta) && this.parent.Spawned && Rand.Chance(chanceMod) && Rand.MTBEventOccurs(this.Props.raidMTBdays, 60000f, 2500f))
                        {
                            this.raidCD = this.Props.minTimeBetweenRaids;
                            IncidentParms incidentParms = new IncidentParms
                            {
                                target = this.parent.Map,
                                points = StorytellerUtility.DefaultThreatPointsNow(this.parent.Map) * this.Props.raidPointFactor,
                                raidStrategy = RaidStrategyDefOf.ImmediateAttack
                            };
                            IncidentDefOf.RaidEnemy.Worker.TryExecute(incidentParms);
                        }
                    }
                    if (this.raidCD > 0)
                    {
                        this.raidCD -= delta;
                    }
                    if (this.curProgress >= this.RequiredProgress)
                    {
                        this.FinishComputation();
                    }
                }
                if (this.parent.Spawned && !this.IsHacked)
                {
                    if (this.captchaThreshold <= 0f)
                    {
                        this.Hack(this.defence, null, true);
                    } else if (!this.canBeHackedNow) {
                        this.Hack(-this.defence, null, true);
                    }
                }
            }
        }
        public void FinishComputation()
        {
            this.curProgress = this.RequiredProgress;
            if (this.parent.SpawnedOrAnyParentSpawned)
            {
                GenExplosion.DoExplosion(this.parent.PositionHeld, this.parent.MapHeld, 2.4f, DamageDefOf.Smoke, this.parent, -1, -1f, this.parent.def.building.destroySound ?? null);
            }
            QuestUtility.SendQuestTargetSignals(this.parent.questTags, "FinishedChainComp", this.Named("SUBJECT"));
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (!this.parent.questTags.NullOrEmpty())
            {
                QuestUtility.SendQuestTargetSignals(this.parent.questTags, "DestroyedChainComp", this.Named("SUBJECT"), previousMap.Named("MAP"));
            }
        }
        public override string CompInspectStringExtra()
        {
            string desc = "HVMP_CCProgress".Translate(this.curProgress.ToStringByStyle(ToStringStyle.FloatOne),this.RequiredProgress.ToStringByStyle(ToStringStyle.FloatOne));
            AcceptanceReport acceptanceReport = this.CanMakeProgress();
            if (!acceptanceReport.Reason.NullOrEmpty())
            {
                desc += "\n" + "HVMP_CCpaused".Translate() + ": " + acceptanceReport.Reason.CapitalizeFirst();
            }
            if (this.canBeHackedNow)
            {
                desc += "\n" + base.CompInspectStringExtra();
            }
            return desc;
        }
        private CompPowerTrader PowerTrader
        {
            get
            {
                CompPowerTrader cpt = this.parent.TryGetComp<CompPowerTrader>();
                return cpt;
            }
        }
        private Texture2D TexUp
        {
            get
            {
                if (this.texUp == null)
                {
                    this.texUp = ContentFinder<Texture2D>.Get(this.Props.texUp, true);
                }
                return this.texUp;
            }
        }
        private Texture2D TexDown
        {
            get
            {
                if (this.texDown == null)
                {
                    this.texDown = ContentFinder<Texture2D>.Get(this.Props.texDown, true);
                }
                return this.texDown;
            }
        }
        public Texture2D UIIcon
        {
            get
            {
                if (this.texBD == null)
                {
                    this.texBD = ContentFinder<Texture2D>.Get(this.Props.texBD, true);
                }
                return this.texBD;
            }
        }
        public float RequiredProgress
        {
            get
            {
                return this.Props.maxProgress * this.challengeRating;
            }
        }
        public void ResetStaticData()
        {
            CompChainComputer.options = new List<CompChainComputer.Option>
            {
                new CompChainComputer.Option(GameConditionDefOf.VolcanicWinter, new FloatRange(10f, 20f)),
                new CompChainComputer.Option(GameConditionDefOf.WeatherController, new FloatRange(5f, 20f)),
                new CompChainComputer.Option(GameConditionDefOf.HeatWave, new FloatRange(4f, 8f)),
                new CompChainComputer.Option(GameConditionDefOf.ColdSnap, new FloatRange(4f, 8f)),
                new CompChainComputer.Option(GameConditionDefOf.ToxicFallout, new FloatRange(5f, 20f)),
                new CompChainComputer.Option(GameConditionDefOf.PsychicSuppression, new FloatRange(4f, 8f)),
                new CompChainComputer.Option(GameConditionDefOf.EMIField, new FloatRange(4f, 8f)),
                new CompChainComputer.Option(GameConditionDefOf.PsychicDrone, new FloatRange(4f, 8f))
            };
        }
        private struct Option
        {
            public Option(GameConditionDef gameCondition, FloatRange durationDaysRange)
            {
                this.gameCondition = gameCondition;
                this.durationDaysRange = durationDaysRange;
            }
            public GameConditionDef gameCondition;
            public FloatRange durationDaysRange;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref this.curPowerConsumption, "curPowerConsumption", 0f, false);
            Scribe_Values.Look<float>(ref this.curProgress, "curProgress", 0f, false);
            Scribe_Values.Look<int>(ref this.challengeRating, "challengeRating", 0, false);
            Scribe_Values.Look<int>(ref this.raidCD, "raidCD", 180000, false);
            Scribe_Values.Look<bool>(ref this.needsPsyConnection, "needsPsyConnection", false, false);
            Scribe_References.Look<Pawn>(ref this.linkedPawn, "linkedPawn", false);
            Scribe_Values.Look<float>(ref this.captchaThreshold, "captchaThreshold", -1f, false);
            Scribe_Values.Look<bool>(ref this.canBeHackedNow, "canBeHackedNow", false, false);
        }
        private Texture2D texUp;
        private Texture2D texDown;
        private Texture2D texBD;
        public float curPowerConsumption;
        public float curProgress;
        public int challengeRating;
        public int raidCD;
        public bool needsPsyConnection;
        public Pawn linkedPawn;
        public float captchaThreshold;
        public bool canBeHackedNow;
        private static List<CompChainComputer.Option> options;
    }
    public class CompProperties_HeatPusher_OI : CompProperties_HeatPusher
    {
        public CompProperties_HeatPusher_OI()
        {
            this.compClass = typeof(CompHeatPusher_OI);
        }
        public float OI_heatRatePerHour;
        public float OI_heatLimitPerHour;
        public float OI_heatRateMax;
        public float OI_heatLimitMax;
        public FloatRange OI_hoursUntilHaywireRisk;
        public float OI_haywireChancePerHour_CompBreak;
        public float OI_haywireChancePerHour_Explosion;
        public float OI_haywireExplosionRadius;
        public IntRange OI_haywireExplosionDamage;
        public float OI_haywireChancePerHour_EMI;
        public IntRange OI_EMIduration;
        public int OI_haywireCooldownTicks;
        public string OI_haywireMessageLabel;
        public string OI_haywireMessageText;
        public EffecterDef OI_effecterDef;
    }
    public class CompHeatPusher_OI : CompHeatPusher
    {
        public new CompProperties_HeatPusher_OI Props
        {
            get
            {
                return (CompProperties_HeatPusher_OI)this.props;
            }
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            this.enabled = false;
            this.OI_haywireThreshold = (int)(this.Props.OI_hoursUntilHaywireRisk.RandomInRange*2500);
        }
        public override bool ShouldPushHeatNow {
            get {
                if (this.enabled)
                {
                    float ambientTemperature = this.parent.AmbientTemperature;
                    return this.enabled && ambientTemperature < this.MaxTemperature;
                }
                return false;
            }
        }
        public float EnergyPerPush
        {
            get
            {
                return Math.Min(this.Props.OI_heatRateMax, this.Props.heatPerSecond + (this.Props.OI_heatRatePerHour * this.ticksActive / 2500));
            }
        }
        public float MaxTemperature
        {
            get
            {
                return Math.Min(this.Props.OI_heatLimitMax, this.Props.heatPushMaxTemperature + (this.Props.OI_heatLimitPerHour * this.ticksActive / 2500));
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (this.enabled && this.parent.SpawnedOrAnyParentSpawned)
            {
                this.ticksActive++;
                if (this.OI_haywireCooldown > 0)
                {
                    this.OI_haywireCooldown--;
                }
                if (this.parent.IsHashIntervalTick(60) && this.ShouldPushHeatNow)
                {
                    GenTemperature.PushHeat(this.parent.PositionHeld, this.parent.MapHeld, this.EnergyPerPush);
                }
                if (this.OI_goneHaywire)
                {
                    if (this.effecter == null)
                    {
                        this.effecter = this.Props.OI_effecterDef.Spawn();
                        this.effecter.Trigger(this.parent, this.parent);
                    }
                    if (this.effecter != null)
                    {
                        this.effecter.EffectTick(this.parent, this.parent);
                    }
                }
                if (this.ticksActive >= this.OI_haywireThreshold)
                {
                    if (!this.OI_goneHaywire)
                    {
                        Find.LetterStack.ReceiveLetter(this.Props.OI_haywireMessageLabel, this.Props.OI_haywireMessageText, LetterDefOf.NegativeEvent, this.parent);
                        this.OI_goneHaywire = true;
                    }
                    if (this.OI_goneHaywire && this.OI_haywireCooldown <= 0 && this.parent.IsHashIntervalTick(250))
                    {
                        if (Rand.Chance(this.Props.OI_haywireChancePerHour_CompBreak * 0.1f))
                        {
                            CompPower power = this.parent.TryGetComp<CompPower>();
                            if (power != null)
                            {
                                PowerNet pn = power.PowerNet;
                                if (pn != null)
                                {
                                    List<CompPower> connectors = pn.connectors.InRandomOrder().ToList();
                                    if (!connectors.NullOrEmpty())
                                    {
                                        foreach (CompPower p2 in connectors)
                                        {
                                            CompBreakdownable cbd = p2.parent.GetComp<CompBreakdownable>();
                                            if (cbd != null && !cbd.BrokenDown)
                                            {
                                                GenExplosion.DoExplosion(this.parent.Position, this.parent.Map, this.Props.OI_haywireExplosionRadius, DamageDefOf.Smoke, this.parent);
                                                cbd.DoBreakdown();
                                                this.OI_haywireCooldown = this.Props.OI_haywireCooldownTicks;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (Rand.Chance(this.Props.OI_haywireChancePerHour_Explosion * 0.1f))
                        {
                            GenExplosion.DoExplosion(this.parent.Position, this.parent.Map, this.Props.OI_haywireExplosionRadius, DamageDefOf.Flame, this.parent, this.Props.OI_haywireExplosionDamage.RandomInRange);
                            Find.TickManager.slower.SignalForceNormalSpeed();
                            this.OI_haywireCooldown = this.Props.OI_haywireCooldownTicks;
                        }
                        if (Rand.Chance(this.Props.OI_haywireChancePerHour_EMI * 0.1f))
                        {
                            GenExplosion.DoExplosion(this.parent.Position, this.parent.Map, this.Props.OI_haywireExplosionRadius, DamageDefOf.EMP, this.parent);
                            GameCondition gameCondition = GameConditionMaker.MakeCondition(GameConditionDefOf.EMIField, this.Props.OI_EMIduration.RandomInRange);
                            gameCondition.forceDisplayAsDuration = true;
                            gameCondition.Permanent = false;
                            List<Rule> listRule = new List<Rule>();
                            Dictionary<string, string> dictionary = new Dictionary<string, string>();
                            this.parent.Map.gameConditionManager.RegisterCondition(gameCondition);
                            Find.LetterStack.ReceiveLetter(gameCondition.LabelCap, gameCondition.LetterText, gameCondition.def.letterDef, LookTargets.Invalid, null, null, null, null, 0, true);
                            this.OI_haywireCooldown = this.Props.OI_haywireCooldownTicks;
                        }
                    }
                }
            }
        }
        public override void CompTickRare()
        {
            base.CompTickRare();
            if (this.parent.SpawnedOrAnyParentSpawned)
            {
                if (this.ShouldPushHeatNow)
                {
                    GenTemperature.PushHeat(this.parent.PositionHeld, this.parent.MapHeld, this.EnergyPerPush * 4.1666665f);
                }
                Log.Error("meow?");
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.OI_goneHaywire, "OI_goneHaywire", false, false);
            Scribe_Values.Look<int>(ref this.OI_haywireThreshold, "OI_haywireThreshold", 3000000, false);
            Scribe_Values.Look<int>(ref this.OI_haywireCooldown, "OI_haywireCooldown", 0, false);
            Scribe_Values.Look<int>(ref this.ticksActive, "ticksActive", 0, false);
        }
        public bool OI_goneHaywire;
        public int OI_haywireThreshold;
        public int OI_haywireCooldown;
        public int ticksActive;
        public Effecter effecter;
    }
    public class HediffCompProperties_BrainDrain : HediffCompProperties_Link
    {
        public HediffCompProperties_BrainDrain()
        {
            this.compClass = typeof(HediffComp_BrainDrain);
        }
        public FloatRange BD_progressLossOnDeath;
    }
    public class HediffComp_BrainDrain : HediffComp_Link
    {
        public new HediffCompProperties_BrainDrain Props
        {
            get
            {
                return (HediffCompProperties_BrainDrain)this.props;
            }
        }
        public override void CompPostPostRemoved()
        {
            if (this.other != null)
            {
                CompChainComputer ccc = this.other.TryGetComp<CompChainComputer>();
                if (ccc != null)
                {
                    ccc.linkedPawn = null;
                    if (ccc.curProgress <= ccc.RequiredProgress)
                    {
                        ccc.curProgress -= this.Props.BD_progressLossOnDeath.RandomInRange;
                        if (ccc.curProgress <= 0f)
                        {
                            ccc.curProgress = 0f;
                        }
                    }
                }
            }
            base.CompPostPostRemoved();
        }
    }
    //commerce branchquest: transportation
    public class QuestNode_NRNSN : QuestNode
    {
        public bool NRNSN_Enabled()
        {
            return HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.transport2, HVMP_Mod.settings.transportX);
        }
        protected override bool TestRunInt(Slate slate)
        {
            bool goNRNSN = this.NRNSN_Enabled();
            this.SetVars(slate, goNRNSN);
            if (goNRNSN)
            {
                this.SetVars2(slate);
            } else {
                slate.Set<object>(this.NRNSN_timeName.GetValue(slate), 0, false);
            }
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            bool goNRNSN = this.NRNSN_Enabled();
            this.SetVars(slate, goNRNSN);
            if (goNRNSN)
            {
                this.SetVars2(slate);
            } else {
                slate.Set<object>(this.NRNSN_timeName.GetValue(slate), 0, false);
            }
        }
        private void SetVars(Slate slate, bool goNRNSN)
        {
            object obj = (goNRNSN ? valueIfNRNSN : value).GetValue(slate);
            if (this.convertTo.GetValue(slate) != null)
            {
                obj = ConvertHelper.Convert(obj, this.convertTo.GetValue(slate));
            }
            slate.Set<object>(this.name.GetValue(slate), obj, false);
        }
        private void SetVars2(Slate slate)
        {
            object obj = this.NRNSN_timeValue.GetValue(slate);
            if (this.convertTo.GetValue(slate) != null)
            {
                obj = ConvertHelper.Convert(obj, this.convertTo.GetValue(slate));
            }
            slate.Set<object>(this.NRNSN_timeName.GetValue(slate), obj, false);
        }
        [NoTranslate]
        public SlateRef<string> name;
        public SlateRef<object> value;
        public SlateRef<object> valueIfNRNSN;
        public SlateRef<Type> convertTo;
        [NoTranslate]
        public SlateRef<string> NRNSN_timeName;
        public SlateRef<object> NRNSN_timeValue;
    }
    public class QuestNode_PUKIS_TGTL : QuestNode
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            WorldObject worldObject = WorldObjectMaker.MakeWorldObject(this.def.GetValue(slate));
            worldObject.Tile = this.tile.GetValue(slate);
            if (this.faction.GetValue(slate) != null)
            {
                worldObject.SetFaction(this.faction.GetValue(slate));
            }
            if (worldObject is WorldObject_DeadDrop wodd)
            {
                bool mayhemMode = HVMP_Mod.settings.transportX;
                wodd.TryGetComponent<DeadDropComp>(out DeadDropComp ddc);
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.transport1, mayhemMode))
                {
                    Map map = QuestGen.slate.Get<Map>("map", null, false) ?? Find.AnyPlayerHomeMap;
                    wodd.PUKIS_points = (int)(this.PUKIS_pointsFactor * StorytellerUtility.DefaultThreatPointsNow(map));
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_PUKIS_info", this.PUKIS_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_PUKIS_info", " ") });
                }
                if (ddc != null)
                {
                    QuestPart_SetupDeadDrop questPart_InitiateTradeRequest = new QuestPart_SetupDeadDrop
                    {
                        settlement = wodd,
                        requestedThingDef = this.requestedThingDef.GetValue(slate),
                        requestedCount = this.requestedThingCount.GetValue(slate),
                        requestDuration = this.duration.GetValue(slate),
                        keepAfterQuestEnds = false,
                        inSignal = slate.Get<string>("inSignal", null, false)
                    };
                    QuestGen.quest.AddPart(questPart_InitiateTradeRequest);
                    if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.transport3, mayhemMode))
                    {
                        if (ddc != null)
                        {
                            ddc.requestedMinQuality = this.TGTL_qualityChances.RandomElementByWeight((KeyValuePair<QualityCategory, float> kvp) => kvp.Value).Key;
                            if (ddc.requestedMinQuality < QualityCategory.Normal)
                            {
                                ddc.requestedMinQuality = QualityCategory.Normal;
                            }
                        }
                    }
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("requestedQualityLabel", ddc.requestedMinQuality.GetLabel())
                    });
                }
            }
            if (this.storeAs.GetValue(slate) != null)
            {
                QuestGen.slate.Set<WorldObject>(this.storeAs.GetValue(slate), worldObject, false);
            }
        }
        protected override bool TestRunInt(Slate slate)
        {
            return this.requestedThingCount.GetValue(slate) > 0 && this.requestedThingDef.GetValue(slate) != null;
        }
        public SlateRef<WorldObjectDef> def;
        public SlateRef<PlanetTile> tile;
        public SlateRef<Faction> faction;
        [NoTranslate]
        public SlateRef<string> storeAs;
        public SlateRef<ThingDef> requestedThingDef;
        public SlateRef<int> requestedThingCount;
        public SlateRef<int> duration;
        public float PUKIS_pointsFactor;
        [MustTranslate]
        public string PUKIS_description;
        public Dictionary<QualityCategory, float> TGTL_qualityChances;
    }
    public class QuestPart_SetupDeadDrop : QuestPart
    {
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo globalTargetInfo in base.QuestLookTargets)
                {
                    yield return globalTargetInfo;
                }
                if (this.settlement != null)
                {
                    yield return this.settlement;
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
                yield return new Dialog_InfoCard.Hyperlink(this.requestedThingDef, -1);
                yield break;
            }
        }
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                DeadDropComp component = this.settlement.GetComponent<DeadDropComp>();
                if (component != null)
                {
                    component.requestThingDef = this.requestedThingDef;
                    component.requestCount = this.requestedCount;
                    component.expiration = Find.TickManager.TicksGame + this.requestDuration;
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_References.Look<WorldObject_DeadDrop>(ref this.settlement, "settlement", false);
            Scribe_Defs.Look<ThingDef>(ref this.requestedThingDef, "requestedThingDef");
            Scribe_Values.Look<int>(ref this.requestedCount, "requestedCount", 0, false);
            Scribe_Values.Look<int>(ref this.requestDuration, "requestDuration", 0, false);
            Scribe_Values.Look<bool>(ref this.keepAfterQuestEnds, "keepAfterQuestEnds", false, false);
        }
        public string inSignal;
        public WorldObject_DeadDrop settlement;
        public ThingDef requestedThingDef;
        public int requestedCount;
        public int requestDuration;
        public bool keepAfterQuestEnds;
    }
    public class WorldObject_DeadDrop : WorldObject
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
            if (this.PUKIS_points > 0)
            {
                this.Ambush(caravan);
            }
        }
        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (this.PUKIS_points > 0 && this.IsHashIntervalTick(250, delta) && Rand.Chance(0.05f))
            {
                foreach (Caravan c in Find.WorldObjects.Caravans.InRandomOrder())
                {
                    if (c.Tile != null && this.Tile != null && Find.WorldGrid.TraversalDistanceBetween(c.Tile, this.Tile, true) <= 6f)
                    {
                        this.Ambush(c);
                    }
                }
            }
        }
        public void Ambush(Caravan target)
        {
            HVMP_Utility.DoAmbush(target, this.PUKIS_points);
            this.PUKIS_points = -1;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.PUKIS_points, "PUKIS_points", -1, false);
        }
        private Material cachedMat;
        public int PUKIS_points;
    }
    public class WorldObjectCompProperties_DeadDrop : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_DeadDrop()
        {
            this.compClass = typeof(DeadDropComp);
        }
    }
    [StaticConstructorOnStartup]
    public class DeadDropComp : WorldObjectComp
    {
        public bool ActiveRequest
        {
            get
            {
                return this.expiration > Find.TickManager.TicksGame;
            }
        }
        public override string CompInspectStringExtra()
        {
            if (this.ActiveRequest)
            {
                return "CaravanRequestInfo".Translate(this.RequestedThingLabel(this.requestThingDef, this.requestCount).CapitalizeFirst(), (this.expiration - Find.TickManager.TicksGame).ToStringTicksToDays("F1"), (this.requestThingDef.GetStatValueAbstract(StatDefOf.MarketValue, null) * (float)this.requestCount).ToStringMoney(null));
            }
            return null;
        }
        public string RequestedThingLabel(ThingDef def, int count)
        {
            string text = GenLabel.ThingLabel(def, null, count);
            if (def.HasComp(typeof(CompQuality)))
            {
                text += " (" + "HVMP_QualOrBetter".Translate(this.requestedMinQuality.GetLabel().CapitalizeFirst()) + ")";
            }
            if (def.IsApparel)
            {
                text += " (" + "NotTainted".Translate() + ")";
            }
            return text;
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
        {
            if (this.ActiveRequest && this.DDVisitedNow(caravan) == this.parent)
            {
                yield return this.FulfillRequestCommand(caravan);
            }
            yield break;
        }
        public WorldObject_DeadDrop DDVisitedNow(Caravan caravan)
        {
            if (!caravan.Spawned || caravan.pather.Moving)
            {
                return null;
            }
            List<WorldObject> sites = Find.WorldObjects.ObjectsAt(caravan.Tile).ToList();
            foreach (WorldObject wo in sites)
            {
                if (wo == this.parent && this.parent is WorldObject_DeadDrop wodd)
                {
                    return wodd;
                }
            }
            return null;
        }
        private Command FulfillRequestCommand(Caravan caravan)
        {
            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "CommandFulfillTradeOffer".Translate();
            command_Action.defaultDesc = "CommandFulfillTradeOfferDesc".Translate();
            command_Action.icon = DeadDropComp.TradeCommandTex;
            command_Action.action = delegate
            {
                if (!this.ActiveRequest)
                {
                    Log.Error("Attempted to fulfill an unavailable request");
                    return;
                }
                if (!CaravanInventoryUtility.HasThings(caravan, this.requestThingDef, this.requestCount, new Func<Thing, bool>(this.PlayerCanGive)))
                {
                    Messages.Message("CommandFulfillTradeOfferFailInsufficient".Translate(this.RequestedThingLabel(this.requestThingDef, this.requestCount)), MessageTypeDefOf.RejectInput, false);
                    return;
                }
                WindowStack windowStack = Find.WindowStack;
                TaggedString taggedString = "CommandFulfillTradeOfferConfirm".Translate(GenLabel.ThingLabel(this.requestThingDef, null, this.requestCount));
                Action action = delegate
                {
                    this.Fulfill(caravan);
                };
                windowStack.Add(Dialog_MessageBox.CreateConfirmation(taggedString, action, false, null, WindowLayer.Dialog));
            };
            if (!CaravanInventoryUtility.HasThings(caravan, this.requestThingDef, this.requestCount, new Func<Thing, bool>(this.PlayerCanGive)))
            {
                command_Action.Disable("CommandFulfillTradeOfferFailInsufficient".Translate(this.RequestedThingLabel(this.requestThingDef, this.requestCount)));
            }
            return command_Action;
        }
        private void Fulfill(Caravan caravan)
        {
            int remaining = this.requestCount;
            List<Thing> list = CaravanInventoryUtility.TakeThings(caravan, delegate (Thing thing)
            {
                if (this.requestThingDef != thing.def)
                {
                    return 0;
                }
                if (!this.PlayerCanGive(thing))
                {
                    return 0;
                }
                int num = Mathf.Min(remaining, thing.stackCount);
                remaining -= num;
                return num;
            });
            for (int i = 0; i < list.Count; i++)
            {
                list[i].Destroy(DestroyMode.Vanish);
            }
            QuestUtility.SendQuestTargetSignals(this.parent.questTags, "TradeRequestFulfilled", this.parent.Named("SUBJECT"), caravan.Named("CARAVAN"));
            if (this.parent is WorldObject_DeadDrop wodd)
            {
                wodd.Notify_CaravanArrived(caravan);
            }
            this.parent.Destroy();
        }
        private bool PlayerCanGive(Thing thing)
        {
            if (thing.GetRotStage() != RotStage.Fresh)
            {
                return false;
            }
            Apparel apparel = thing as Apparel;
            if (apparel != null && apparel.WornByCorpse)
            {
                return false;
            }
            CompQuality compQuality = thing.TryGetComp<CompQuality>();
            return compQuality == null || compQuality.Quality >= this.requestedMinQuality;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look<ThingDef>(ref this.requestThingDef, "requestThingDef");
            Scribe_Values.Look<int>(ref this.requestCount, "requestCount", 0, false);
            Scribe_Values.Look<QualityCategory>(ref this.requestedMinQuality, "requestedMinQuality", QualityCategory.Normal, false);
            Scribe_Values.Look<int>(ref this.expiration, "expiration", 0, false);
            BackCompatibility.PostExposeData(this);
        }
        public ThingDef requestThingDef;
        public QualityCategory requestedMinQuality = QualityCategory.Normal;
        public int requestCount;
        public int expiration = -1;
        public string outSignalFulfilled;
        private static readonly Texture2D TradeCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/FulfillTradeRequest", true);
    }
    //pax branchquest: pax caelum
    public class QuestNode_AllThreeCaelumMutators : QuestNode_GameCondition
    {
        protected override void RunInt()
        {
            base.RunInt();
            bool mayhemMode = HVMP_Mod.settings.caelumX;
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.caelum1, mayhemMode))
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_MK_info", this.MK_description.Formatted())
                });
                float num = slate.Get<float>("points", 0f, false);
                GameCondition gameCondition2 = GameConditionMaker.MakeCondition(this.MK_condition, this.duration.GetValue(slate));
                QuestPart_GameCondition questPart_GameCondition2 = new QuestPart_GameCondition();
                questPart_GameCondition2.gameCondition = gameCondition2;
                List<Rule> list2 = new List<Rule>();
                Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
                if (this.targetWorld.GetValue(slate))
                {
                    questPart_GameCondition2.targetWorld = true;
                    gameCondition2.RandomizeSettings(num, null, list2, dictionary2);
                } else {
                    Map map = HVMP_Utility.GetMap_QuestNodeGameCondition(slate);
                    questPart_GameCondition2.mapParent = map.Parent;
                    gameCondition2.RandomizeSettings(num, map, list2, dictionary2);
                }
                questPart_GameCondition2.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false);
                questPart_GameCondition2.sendStandardLetter = false;
                quest.AddPart(questPart_GameCondition2);
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_MK_info", " ") });
            }
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.caelum2, mayhemMode))
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_SO_info", this.SO_description.Formatted())
                });
                QuestPart_SensoryOverload qpSO = new QuestPart_SensoryOverload();
                qpSO.SO_hediff = this.SO_hediff;
                qpSO.SO_stunDuration = this.SO_stunDuration;
                qpSO.map = slate.Get<Map>("map", null, false);
                qpSO.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(quest.InitiateSignal);
                quest.AddPart(qpSO);
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_SO_info", " ") });
            }
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.caelum3, mayhemMode))
            {
                float num = QuestGen.slate.Get<float>("points", 0f, false);
                GameCondition gameCondition = GameConditionMaker.MakeCondition(this.TE_gameCondition.GetValue(slate), (int)(this.TE_duration.GetValue(slate)*this.TE_durationPct.RandomInRange));
                QuestPart_GameCondition questPart_GameCondition = new QuestPart_GameCondition
                {
                    gameCondition = gameCondition,
                    sendStandardLetter = false
                };
                List<Rule> list = new List<Rule>();
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                if (this.targetWorld.GetValue(slate))
                {
                    questPart_GameCondition.targetWorld = true;
                    gameCondition.RandomizeSettings(num, null, list, dictionary);
                } else {
                    Map map = HVMP_Utility.GetMap_QuestNodeGameCondition(QuestGen.slate);
                    questPart_GameCondition.mapParent = map.Parent;
                    gameCondition.RandomizeSettings(num, map, list, dictionary);
                }
                questPart_GameCondition.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
                QuestGen.quest.AddPart(questPart_GameCondition);
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_TE_info", this.TE_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_TE_info", " ") });
            }
        }
        [MustTranslate]
        public string TE_description;
        public FloatRange TE_durationPct;
        public SlateRef<GameConditionDef> TE_gameCondition;
        public SlateRef<int> TE_duration;
        public GameConditionDef MK_condition;
        [MustTranslate]
        public string MK_description;
        public HediffDef SO_hediff;
        public int SO_stunDuration;
        [MustTranslate]
        public string SO_description;
    }
    public class GameCondition_MoodKiller : GameCondition
    {

    }
    public class ThoughtWorker_MoodKiller : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            Map mapHeld = p.MapHeld;
            if (mapHeld != null)
            {
                GameCondition_MoodKiller mk = mapHeld.gameConditionManager.GetActiveCondition<GameCondition_MoodKiller>();
                if (mk != null)
                {
                    return true;
                }
            }
            return false;
        }
    }
    public class QuestPart_SensoryOverload : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag == this.inSignal)
            {
                if (this.SO_hediff != null)
                {
                    SoundDefOf.ShipTakeoff.PlayOneShotOnCamera(null);
                    foreach (Pawn p in this.map.mapPawns.AllPawnsSpawned)
                    {
                        if (p.RaceProps.IsFlesh && (!ModsConfig.AnomalyActive || !(p.RaceProps.IsAnomalyEntity && !p.IsMutant)))
                        {
                            p.health.AddHediff(this.SO_hediff);
                            p.stances.stunner.StunFor(this.SO_stunDuration,null);
                        }
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<HediffDef>(ref this.SO_hediff, "SO_hediff");
            Scribe_Values.Look<int>(ref this.SO_stunDuration, "SO_stunDuration", 180, false);
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_References.Look<Map>(ref this.map, "map", false);
        }
        public HediffDef SO_hediff;
        public int SO_stunDuration;
        public string inSignal;
        public Map map;
    }
    //pax branchquest: pax machina
    public class QuestNode_RS_THTF : QuestNode
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            QuestPart_MechCluster questPart_MechCluster = new QuestPart_MechCluster();
            questPart_MechCluster.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
            questPart_MechCluster.tag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(this.tag.GetValue(slate));
            questPart_MechCluster.mapParent = slate.Get<Map>("map", null, false).Parent;
            questPart_MechCluster.sketch = this.GenerateSketch(slate);
            questPart_MechCluster.dropSpot = this.dropSpot.GetValue(slate) ?? IntVec3.Invalid;
            QuestGen.quest.AddPart(questPart_MechCluster);
            string text = "";
            if (questPart_MechCluster.sketch.pawns != null)
            {
                text += PawnUtility.PawnKindsToLineList(questPart_MechCluster.sketch.pawns.Select((MechClusterSketch.Mech m) => m.kindDef), "  - ", ColoredText.ThreatColor);
            }
            string[] array = (from t in questPart_MechCluster.sketch.buildingsSketch.Things
                              where GenHostility.IsDefMechClusterThreat(t.def)
                              group t by t.def.label).Select(delegate (IGrouping<string, SketchThing> grp)
                              {
                                  int num = grp.Count<SketchThing>();
                                  return num.ToString() + " " + ((num > 1) ? Find.ActiveLanguageWorker.Pluralize(grp.Key, num) : grp.Key);
                              }).ToArray<string>();
            if (array.Any<string>())
            {
                if (text != "")
                {
                    text += "\n";
                }
                text += array.ToLineList(ColoredText.ThreatColor, "  - ");
            }
            if (text != "")
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("allThreats", text)
                });
            }
        }
        public MechClusterSketch GenerateSketch(Slate slate)
        {
            float points = this.points.GetValue(slate) ?? slate.Get<float>("points", 0f, false);
            bool mayhemMode = HVMP_Mod.settings.machinaX;
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.machina3, mayhemMode))
            {
                points *= this.THTF_multi;
            }
            bool shouldBeAsleep = true;
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.machina2, mayhemMode))
            {
                shouldBeAsleep = false;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_RS_info", this.RS_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_RS_info", this.nonRS_description.Formatted()) });
            }
            return MechClusterGenerator.GenerateClusterSketch(points, slate.Get<Map>("map", null, false), shouldBeAsleep, false);
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
        [MustTranslate]
        public string RS_description;
        [MustTranslate]
        public string nonRS_description;
        public float THTF_multi;
    }
    public class QuestNode_MachinaOTC : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return Faction.OfMechanoids != null;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.machina1,HVMP_Mod.settings.machinaX))
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_OTC_info", this.OTC_description.Formatted())
                });
                if (this.node != null)
                {
                    slate.Set<Faction>(this.storeAs.GetValue(slate), Faction.OfMechanoids, false);
                    this.node.Run();
                }
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_OTC_info", " ") });
            }
        }
        [NoTranslate]
        public SlateRef<string> storeAs;
        public QuestNode node;
        [MustTranslate]
        public string OTC_description;
    }
    //pax branchquest: pax mundi
    public class QuestNode_PaxMundi : QuestNode
    {
        protected override void RunInt()
        {
            if (HVMP_Utility.TryFindPaxFaction(out Faction paxFaction))
            {
                Slate slate = QuestGen.slate;
                float points = slate.Get<float>("points", 0f, false);
                IntRange goodwillAmount;
                switch (QuestGen.quest.challengeRating)
                {
                    case 3:
                        goodwillAmount = this.goodwillThreeStarRange;
                        break;
                    case 2:
                        goodwillAmount = this.goodwillTwoStarRange;
                        break;
                    case 1:
                        goodwillAmount = this.goodwillOneStarRange;
                        break;
                    default:
                        goodwillAmount = this.goodwillTwoStarRange;
                        break;
                }
                slate.Set<bool>("noGoodwillableFactions", !this.TryFindFaction(out Faction faction), false);
                slate.Set<int>("goodwillAmount", Rand.Range(goodwillAmount.min, goodwillAmount.max), false);
                this.DoWork(QuestGen.slate, delegate (QuestNode n)
                {
                    n.Run();
                    return true;
                });
            }
        }
        private bool DoWork(Slate slate, Func<QuestNode, bool> func)
        {
            if (slate.Get<bool>("noGoodwillableFactions", false, false))
            {
                if (this.noGoodwillableFactionsNode != null)
                {
                    return func(this.noGoodwillableFactionsNode);
                }
            }
            else if (this.elseNode != null)
            {
                return func(this.elseNode);
            }
            return true;
        }
        private bool TryFindFaction(out Faction faction)
        {
            return (from x in Find.FactionManager.GetFactions(false, false, false, TechLevel.Undefined, false)
                    where this.IsGoodFaction(x)
                    select x).TryRandomElement(out faction);
        }
        private bool IsGoodFaction(Faction faction)
        {
            if (faction.def.HasModExtension<EBranchQuests>() || faction.IsPlayer || !faction.HasGoodwill || faction.def.PermanentlyHostileTo(Faction.OfPlayer.def) || faction.GoodwillWith(Faction.OfPlayer) >= 100)
            {
                return false;
            }
            return true;
        }
        protected override bool TestRunInt(Slate slate)
        {
            return HVMP_Utility.TryFindPaxFaction(out Faction paxFaction);
        }
        public IntRange goodwillThreeStarRange;
        public IntRange goodwillTwoStarRange;
        public IntRange goodwillOneStarRange;
        public QuestNode noGoodwillableFactionsNode;
        public QuestNode elseNode;
    }
    public class QuestNode_PaxMundiTracker : QuestNode
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;
            QuestPart_PaxMundi qppm;
            if (!quest.TryGetFirstPartOfType<QuestPart_PaxMundi>(out qppm))
            {
                qppm = quest.AddPart<QuestPart_PaxMundi>();
                qppm.goodwillChangesInt = 0;
                qppm.denominator = slate.Get<int>("goodwillAmount", 0, false);
                qppm.inSignalEnable = slate.Get<string>("inSignal", null, false);
            }
            bool mayhemMode = HVMP_Mod.settings.mundiX;
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.mundi1, mayhemMode))
            {
                qppm.MTEOP_pointFactor = this.MTEOP_pointFactor;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_MTEOP_info", this.MTEOP_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_MTEOP_info", " ") });
            }
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.mundi3, mayhemMode))
            {
                qppm.TWOS_on = true;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_TWOS_info", this.TWOS_description.Formatted())
                    });
            }
            else
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_TWOS_info", " ") });
            }
        }
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        public float MTEOP_pointFactor;
        [MustTranslate]
        public string MTEOP_description;
        [MustTranslate]
        public string TWOS_description;
    }
    public class QuestPart_PaxMundi : QuestPartActivable
    {
        public override string ExpiryInfoPart
        {
            get
            {
                return "HVMP_GoodwillCurried".Translate(this.goodwillChangesInt, this.denominator);
            }
        }
        public override void QuestPartTick()
        {
            base.QuestPartTick();
            if (this.MTEOP_pointFactor > 0f)
            {
                Faction p = Faction.OfPlayerSilentFail;
                WorldComponent_HautsFactionComps WCFC = (WorldComponent_HautsFactionComps)Find.World.GetComponent(typeof(WorldComponent_HautsFactionComps));
                if (WCFC != null && p != null)
                {
                    float spyPoints = 0f;
                    Map m = Find.AnyPlayerHomeMap;
                    spyPoints = m != null ? StorytellerUtility.DefaultThreatPointsNow(m) : StorytellerUtility.DefaultThreatPointsNow(Find.World);
                    spyPoints *= this.MTEOP_pointFactor;
                    foreach (Faction f in Find.FactionManager.AllFactionsListForReading)
                    {
                        if (f != p && f.def.PermanentlyHostileTo(p.def))
                        {
                            Hauts_FactionCompHolder fch = WCFC.FindCompsFor(f);
                            if (fch != null)
                            {
                                HautsFactionComp_SpyPoints spc = fch.TryGetComp<HautsFactionComp_SpyPoints>();
                                if (spc != null)
                                {
                                    spc.spyPoints += (int)spyPoints;
                                }
                            }
                        }
                    }
                }
                this.MTEOP_pointFactor = 0f;
            }
            if (this.goodwillChangesInt >= this.denominator)
            {
                base.Complete();
            }
            if (this.quest.State == QuestState.Ongoing)
            {
                WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
                if (wcbs != null && wcbs.qppms != null && !wcbs.qppms.Contains(this))
                {
                    wcbs.qppms.Add(this);
                }
            }
        }
        public string OutSignalCompletedPO
        {
            get
            {
                return string.Concat(new object[]
                {
                    "Quest",
                    this.quest.id,
                    ".desiredGoodwillReached"
                });
            }
        }
        protected override void Complete(SignalArgs signalArgs)
        {
            base.Complete(signalArgs);
            Find.SignalManager.SendSignal(new Signal(this.OutSignalCompletedPO, signalArgs, false));
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.goodwillChangesInt, "goodwillChangesInt", 0, false);
            Scribe_Values.Look<int>(ref this.denominator, "denominator", 0, false);
            Scribe_Values.Look<float>(ref this.MTEOP_pointFactor, "MTEOP_pointFactor", 0f, false);
            Scribe_Values.Look<bool>(ref this.TWOS_on, "TWOS_on", false, false);
        }
        public int goodwillChangesInt;
        public int denominator;
        public float MTEOP_pointFactor;
        public bool TWOS_on;
    }
    public class QuestNode_MundiPIOL : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.mundi2, HVMP_Mod.settings.mundiX))
            {
                if (this.node != null)
                {
                    this.node.Run();
                }
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_PIOL_info", this.PIOL_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_PIOL_info", " ") });
            }
        }
        [NoTranslate]
        public SlateRef<string> storeAs;
        public QuestNode node;
        [MustTranslate]
        public string PIOL_description;
    }
    //pax branchquest: pax proximum
    public class QuestNode_PaxProximum : QuestNode
    {
        private static QuestGen_Pawns.GetPawnParms CivilianPawnParams
        {
            get
            {
                return new QuestGen_Pawns.GetPawnParms
                {
                    mustBeOfFaction = QuestGen.slate.Get<Faction>("faction", null, false),
                    canGeneratePawn = true,
                    mustBeWorldPawn = true
                };
            }
        }
        protected override void RunInt()
        {
            if (!ModLister.CheckRoyalty("Shuttle crash rescue"))
            {
                return;
            }
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            Map map = QuestGen_Get.GetMap(false, null, false);
            Faction faction = QuestGen.slate.Get<Faction>("faction", null, false);
            this.TryFindEnemyFaction(out Faction enemyFaction, faction);
            float questPoints = Math.Min(slate.Get<float>("points", 500f, false),500f);
            slate.Set<Map>("map", map, false);
            slate.Set<int>("rescueDelay", 20000, false);
            slate.Set<int>("leaveDelay", 30000, false);
            slate.Set<int>("rescueShuttleAfterRaidDelay", 10000, false);
            string text = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("civilian");
            int num = Mathf.FloorToInt(this.maxCiviliansByPointsCurve.Evaluate(questPoints));
            Thing crashedShuttle = ThingMaker.MakeThing(this.shuttleDef, null);
            this.TryFindShuttleCrashPosition(map, faction, crashedShuttle.def.size, new IntVec3?(crashedShuttle.def.interactionCellOffset), out IntVec3 shuttleCrashPosition);
            List<Pawn> civilians = new List<Pawn>();
            List<Pawn> list = new List<Pawn>();
            for (int i = 0; i < Rand.Range(1, num) - 1; i++)
            {
                Pawn pawn = quest.GetPawn(QuestNode_PaxProximum.CivilianPawnParams);
                QuestUtility.AddQuestTag(ref pawn.questTags, text);
                pawn.health.AddHediff(HVMPDefOf.HVMP_HostileEnvironmentFilm,null);
                civilians.Add(pawn);
                list.Add(pawn);
            }
            Pawn asker = quest.GetPawn(new QuestGen_Pawns.GetPawnParms
            {
                mustBeOfFaction = faction,
                canGeneratePawn = true,
                mustBeWorldPawn = true,
                seniorityRange = new FloatRange(0f),
                mustHaveRoyalTitleInCurrentFaction = false
            });
            asker.health.AddHediff(HVMPDefOf.HVMP_HostileEnvironmentFilm, null);
            civilians.Add(asker);
            List<Pawn> soldiers = new List<Pawn>();
            bool mayhemMode = HVMP_Mod.settings.proxX;
            int soldierCount = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.prox1,mayhemMode) ? 0 : Rand.Range(1, Mathf.FloorToInt(this.maxSoldiersByPointsCurve.Evaluate(questPoints)));
            for (int j = 0; j < soldierCount; j++)
            {
                Pawn pawn2 = quest.GetPawn(new QuestGen_Pawns.GetPawnParms
                {
                    mustBeOfFaction = faction,
                    canGeneratePawn = true,
                    mustBeOfKind = this.soldierDefs.RandomElement(),
                    mustBeWorldPawn = true,
                    mustBeCapableOfViolence = true
                });
                pawn2.health.AddHediff(HVMPDefOf.HVMP_HostileEnvironmentFilm, null);
                soldiers.Add(pawn2);
            }
            List<Pawn> allPassengers = new List<Pawn>();
            allPassengers.AddRange(soldiers);
            allPassengers.AddRange(civilians);
            quest.BiocodeWeapons(allPassengers, null);
            Thing rescueShuttle = QuestGen_Shuttle.GenerateShuttle(Faction.OfEmpire, allPassengers, null, false, false, false, -1, false, false, false, false, null, null, -1, null, false, true, false, false, false);
            string text2 = QuestGenUtility.HardcodedSignalWithQuestID("soldiers.Rescued");
            quest.RemoveFromRequiredPawnsOnRescue(rescueShuttle, soldiers, text2);
            quest.Delay(120, delegate
            {
                quest.Letter(LetterDefOf.NegativeEvent, null, null, null, null, false, QuestPart.SignalListenMode.OngoingOnly, Gen.YieldSingle<Thing>(crashedShuttle), false, "LetterTextShuttleCrashed".Translate(), null, "LetterLabelShuttleCrashed".Translate(), null, null);
                quest.SpawnSkyfaller(map, ThingDefOf.ShuttleCrashing, Gen.YieldSingle<Thing>(crashedShuttle), Faction.OfPlayer, new IntVec3?(shuttleCrashPosition), null, false, false, null, null);
                IEnumerable<Thing> allPassengers2 = allPassengers;
                IntVec3? intVec = new IntVec3?(shuttleCrashPosition);
                quest.DropPods(map.Parent, allPassengers2, null, null, null, null, new bool?(false), false, false, false, null, null, QuestPart.SignalListenMode.OngoingOnly, intVec, true, false, false, false, null);
                quest.DefendPoint(map.Parent, asker, shuttleCrashPosition, soldiers, faction, null, null, new float?((float)12), false, false);
                IntVec3 intVec2 = shuttleCrashPosition + IntVec3.South;
                quest.WaitForEscort(map.Parent, civilians, faction, intVec2, null, false);
                string text17 = QuestGenUtility.HardcodedSignalWithQuestID("rescueShuttle.Spawned");
                quest.ExitOnShuttle(map.Parent, allPassengers, faction, rescueShuttle, text17, false);
                Quest quest3 = quest;
                int num5 = 20000;
                IEnumerable<Pawn> civilians2 = civilians;
                Action action = (delegate
                {
                    quest.Letter(LetterDefOf.NeutralEvent, null, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, Gen.YieldSingle<Thing>(rescueShuttle), false, "[rescueShuttleArrivedLetterText]", null, "[rescueShuttleArrivedLetterLabel]", null, null);
                    TransportShip transportShip = quest.GenerateTransportShip(TransportShipDefOf.Ship_Shuttle, null, rescueShuttle, null).transportShip;
                    quest.SendTransportShipAwayOnCleanup(transportShip, false, TransportShipDropMode.NonRequired);
                    IntVec3 intVec3;
                    DropCellFinder.TryFindDropSpotNear(shuttleCrashPosition, map, out intVec3, false, false, false, new IntVec2?(ThingDefOf.Shuttle.Size + new IntVec2(2, 2)), false);
                    quest.AddShipJob_Arrive(transportShip, map.Parent, null, new IntVec3?(intVec3), ShipJobStartMode.Instant, faction, null);
                    quest.AddShipJob_WaitTime(transportShip, 30000, true, allPassengers.Cast<Thing>().ToList<Thing>(), null);
                    quest.ShuttleLeaveDelay(rescueShuttle, 30000, null, null, null, null);
                    quest.AddShipJob_FlyAway(transportShip, null, null, TransportShipDropMode.None, null);
                });
                quest3.ShuttleDelay(num5, civilians2, action, null, null, true);
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.prox3, mayhemMode) && !this.ROTSJ_incidents.NullOrEmpty())
                {
                    IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
                    incidentParms.forced = true;
                    if (incidentParms.points < 500)
                    {
                        incidentParms.points = 500;
                    }
                    List<IncidentDef> ids = this.ROTSJ_incidents.Where((IncidentDef incident)=>incident.Worker.CanFireNow(incidentParms)).ToList();
                    if (ids.Count > 0)
                    {
                        QuestPart_Proximim_ROTSJ qpROTSJ = new QuestPart_Proximim_ROTSJ();
                        qpROTSJ.ROTSJ_incident = ids.RandomElement();
                        qpROTSJ.map = QuestGen.slate.Get<Map>("map", null, false);
                        qpROTSJ.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(quest.InitiateSignal);
                        quest.AddPart(qpROTSJ);
                        QuestGen.AddQuestDescriptionRules(new List<Rule>
                        {
                            new Rule_String("mutator_ROTSJ_info", this.ROTSJ_description.Formatted())
                        });
                    }
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_ROTSJ_info", " ") });
                }
                this.TryFindRaidWalkInPosition(map, shuttleCrashPosition, out IntVec3 walkIntSpot);
                float soldiersTotalCombatPower = 0f;
                for (int k = 0; k < soldiers.Count; k++)
                {
                    soldiersTotalCombatPower += soldiers[k].kindDef.combatPower;
                }
                int raidDelay = this.HOTH_delay_off.RandomInRange;
                float raidScalar = this.HOTH_raidScalar_off;
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.prox2, mayhemMode))
                {
                    raidScalar = this.HOTH_raidScalar_on;
                    raidDelay = this.HOTH_delay_on.RandomInRange;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_HOTH_info", this.HOTH_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_HOTH_info", " ") });
                }
                quest.Delay(raidDelay, delegate
                {
                    List<Pawn> list2 = PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
                    {
                        faction = enemyFaction,
                        groupKind = PawnGroupKindDefOf.Combat,
                        points = Math.Max(questPoints * raidScalar,enemyFaction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat,null)*1.05f),
                        generateFightersOnly = true,
                        tile = map.Tile
                    }, true).ToList<Pawn>();
                    for (int l = 0; l < list2.Count; l++)
                    {
                        Find.WorldPawns.PassToWorld(list2[l], PawnDiscardDecideMode.Decide);
                        QuestGen.AddToGeneratedPawns(list2[l]);
                    }
                    QuestPart_PawnsArrive questPart_PawnsArrive = new QuestPart_PawnsArrive();
                    questPart_PawnsArrive.pawns.AddRange(list2);
                    questPart_PawnsArrive.arrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                    questPart_PawnsArrive.joinPlayer = false;
                    questPart_PawnsArrive.mapParent = map.Parent;
                    questPart_PawnsArrive.spawnNear = walkIntSpot;
                    questPart_PawnsArrive.inSignal = QuestGen.slate.Get<string>("inSignal", null, false);
                    questPart_PawnsArrive.sendStandardLetter = false;
                    quest.AddPart(questPart_PawnsArrive);
                    quest.AssaultThings(map.Parent, list2, enemyFaction, allPassengers, null, null, true);
                    quest.Letter(LetterDefOf.ThreatBig, null, null, enemyFaction, null, false, QuestPart.SignalListenMode.OngoingOnly, list2, false, "[raidArrivedLetterText]", null, "[raidArrivedLetterLabel]", null, null);
                }, null, null, null, false, null, null, false, null, null, "RaidDelay", false, QuestPart.SignalListenMode.OngoingOnly, false);
            }, null, null, null, false, null, null, false, null, null, null, false, QuestPart.SignalListenMode.OngoingOnly, false);
            string text3 = QuestGenUtility.HardcodedSignalWithQuestID("rescueShuttle.SentSatisfied");
            string text4 = QuestGenUtility.HardcodedSignalWithQuestID("rescueShuttle.SentUnsatisfied");
            string[] array = new string[] { text3, text4 };
            string text5 = QuestGenUtility.HardcodedSignalWithQuestID("rescueShuttle.Destroyed");
            string text6 = QuestGenUtility.HardcodedSignalWithQuestID("rescueShuttle.LeftBehind");
            string text7 = QuestGenUtility.HardcodedSignalWithQuestID("asker.Destroyed");
            string text8 = QuestGenUtility.HardcodedSignalWithQuestID("civilian.Destroyed");
            string text9 = QuestGenUtility.HardcodedSignalWithQuestID("map.MapRemoved");
            quest.GoodwillChangeShuttleSentThings(faction, list, -5, null, array, text5, HistoryEventDefOf.ShuttleGuardsMissedShuttle, true, false, QuestPart.SignalListenMode.Always);
            quest.GoodwillChangeShuttleSentThings(faction, Gen.YieldSingle<Pawn>(asker), -10, null, array, text5, HistoryEventDefOf.ShuttleCommanderMissedShuttle, true, false, QuestPart.SignalListenMode.Always);
            quest.Leave(allPassengers, "", false, true, null, false);
            quest.Letter(LetterDefOf.PositiveEvent, text3, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, null, false, "[questCompletedSuccessLetterText]", null, "[questCompletedSuccessLetterLabel]", null, null);
            string questSuccess = QuestGen.GenerateNewSignal("QuestSuccess", true);
            quest.SignalPass(null, text3, questSuccess);
            quest.AnyOnTransporter(allPassengers, rescueShuttle, delegate
            {
                Quest quest5 = quest;
                IEnumerable<Pawn> enumerable = Gen.YieldSingle<Pawn>(asker);
                Thing rescueShuttle2 = rescueShuttle;
                Action action2= (delegate
                {
                    quest.Letter(LetterDefOf.PositiveEvent, null, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, null, false, "[questCompletedCiviliansLostSuccessLetterText]", null, "[questCompletedCiviliansLostSuccessLetterLabel]", null, null);
                    quest.SignalPass(null, null, questSuccess);
                });
                Action action3= (delegate
                {
                    quest.Letter(LetterDefOf.NegativeEvent, null, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, null, false, "[askerLostLetterText]", null, "[askerLostLetterLabel]", null, null);
                    quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, false, false);
                });
                quest5.AnyOnTransporter(enumerable, rescueShuttle2, action2, action3, null, null, null, QuestPart.SignalListenMode.OngoingOnly);
            }, delegate {
                quest.Letter(LetterDefOf.NegativeEvent, null, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, null, false, "[allLostLetterText]", null, "[allLostLetterLabel]", null, null);
                quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, false, false);
            }, text4, null, null, QuestPart.SignalListenMode.OngoingOnly);
            quest.Letter(LetterDefOf.NegativeEvent, text7, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, Gen.YieldSingle<Pawn>(asker), false, "[askerDiedLetterText]", null, "[askerDiedLetterLabel]", null, null);
            quest.End(QuestEndOutcome.Fail, 0, null, text7, QuestPart.SignalListenMode.OngoingOnly, false, false);
            quest.Letter(LetterDefOf.NegativeEvent, text8, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, civilians, false, "[civilianDiedLetterText]", null, "[civilianDiedLetterLabel]", null, null);
            quest.End(QuestEndOutcome.Fail, 0, null, text8, QuestPart.SignalListenMode.OngoingOnly, false, false);
            quest.Letter(LetterDefOf.NegativeEvent, text5, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, Gen.YieldSingle<Thing>(rescueShuttle), false, "[shuttleDestroyedLetterText]", null, "[shuttleDestroyedLetterLabel]", null, null);
            quest.End(QuestEndOutcome.Fail, 0, null, text5, QuestPart.SignalListenMode.OngoingOnly, false, false);
            quest.Letter(LetterDefOf.NegativeEvent, text6, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, Gen.YieldSingle<Thing>(rescueShuttle), false, "[shuttleLeftBehindLetterText]", null, "[shuttleLeftBehindLetterLabel]", null, null);
            quest.End(QuestEndOutcome.Fail, 0, null, text6, QuestPart.SignalListenMode.OngoingOnly, false, false);
            quest.End(QuestEndOutcome.Fail, 0, null, QuestGenUtility.HardcodedSignalWithQuestID("asker.LeftMap"), QuestPart.SignalListenMode.OngoingOnly, true, false);
            string text10 = QuestGenUtility.HardcodedSignalWithQuestID("askerFaction.BecameHostileToPlayer");
            quest.End(QuestEndOutcome.Fail, 0, null, text10, QuestPart.SignalListenMode.OngoingOnly, true, false);
            quest.End(QuestEndOutcome.InvalidPreAcceptance, 0, null, text10, QuestPart.SignalListenMode.NotYetAcceptedOnly, false, false);
            quest.Letter(LetterDefOf.NegativeEvent, text9, null, faction, null, false, QuestPart.SignalListenMode.OngoingOnly, Gen.YieldSingle<Pawn>(asker), false, "[mapRemovedLetterText]", null, "[mapRemovedLetterLabel]", null, null);
            quest.End(QuestEndOutcome.Fail, 0, null, text9, QuestPart.SignalListenMode.OngoingOnly, false, false);
            slate.Set<Pawn>("asker", asker, false);
            slate.Set<Faction>("askerFaction", faction, false);
            slate.Set<Faction>("enemyFaction", enemyFaction, false);
            slate.Set<List<Pawn>>("soldiers", soldiers, false);
            slate.Set<List<Pawn>>("civilians", civilians, false);
            slate.Set<int>("civilianCountMinusOne", civilians.Count - 1, false);
            slate.Set<Thing>("rescueShuttle", rescueShuttle, false);
        }
        private bool TryFindEnemyFaction(out Faction enemyFaction, Faction faction)
        {
            return Find.FactionManager.AllFactionsVisible.Where((Faction f) => f.HostileTo(faction) && f.HostileTo(Faction.OfPlayer)).TryRandomElement(out enemyFaction);
        }
        private bool TryFindShuttleCrashPosition(Map map, Faction faction, IntVec2 size, IntVec3? interactionCell, out IntVec3 spot)
        {
            return DropCellFinder.FindSafeLandingSpot(out spot, faction, map, 35, 15, 25, new IntVec2?(size), interactionCell);
        }
        private bool TryFindRaidWalkInPosition(Map map, IntVec3 shuttleCrashSpot, out IntVec3 spawnSpot)
        {
            Predicate<IntVec3> predicate = (IntVec3 p) => (map.TileInfo.AllowRoofedEdgeWalkIn || !map.roofGrid.Roofed(p)) && p.Walkable(map) && map.reachability.CanReach(p, shuttleCrashSpot, PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Some);
            if (RCellFinder.TryFindEdgeCellFromPositionAvoidingColony(shuttleCrashSpot, map, predicate, out spawnSpot))
            {
                return true;
            }
            if (CellFinder.TryFindRandomEdgeCellWith(predicate, map, CellFinder.EdgeRoadChance_Hostile, out spawnSpot))
            {
                return true;
            }
            spawnSpot = IntVec3.Invalid;
            return false;
        }
        protected override bool TestRunInt(Slate slate)
        {
            if (!Find.Storyteller.difficulty.allowViolentQuests)
            {
                return false;
            }
            Faction faction = QuestGen.slate.Get<Faction>("faction", null, false);
            if (faction == null)
            {
                HVMP_Utility.TryFindPaxFaction(out faction);
            }
            if (faction == null)
            {
                return false;
            }
            Pawn pawn;
            if (!QuestGen_Pawns.GetPawnTest(QuestNode_PaxProximum.CivilianPawnParams, out pawn))
            {
                return false;
            }
            Faction enemyFaction;
            if (!this.TryFindEnemyFaction(out enemyFaction, faction))
            {
                return false;
            }
            Map map = QuestGen_Get.GetMap(false, null, true);
            return map != null && this.TryFindShuttleCrashPosition(map, faction, ThingDefOf.ShuttleCrashed.size, new IntVec3?(ThingDefOf.ShuttleCrashed.interactionCellOffset), out IntVec3 intVec) && this.TryFindRaidWalkInPosition(map, intVec, out IntVec3 intVec2);
        }
        public FactionDef faction;
        public SimpleCurve maxCiviliansByPointsCurve;
        public SimpleCurve maxSoldiersByPointsCurve;
        public List<PawnKindDef> soldierDefs;
        public ThingDef shuttleDef;
        public float HOTH_raidScalar_on;
        public float HOTH_raidScalar_off;
        public IntRange HOTH_delay_on;
        public IntRange HOTH_delay_off;
        [MustTranslate]
        public string HOTH_description;
        public List<IncidentDef> ROTSJ_incidents;
        [MustTranslate]
        public string ROTSJ_description;
    }
    public class QuestPart_Proximim_ROTSJ : QuestPart
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == this.inSignal)
            {
                if (this.ROTSJ_incident != null)
                {
                    IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
                    incidentParms.forced = true;
                    if (incidentParms.points < 500)
                    {
                        incidentParms.points = 500;
                    }
                    Find.Storyteller.incidentQueue.Add(this.ROTSJ_incident, Find.TickManager.TicksGame + 60, incidentParms, 0);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<IncidentDef>(ref this.ROTSJ_incident, "ROTSJ_incident");
            Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
            Scribe_References.Look<Map>(ref this.map, "map", false);
        }
        public IncidentDef ROTSJ_incident;
        public string inSignal;
        public Map map;
    }
    //pax branchquest: pax vox
    public class QuestNode_GenerateVox : QuestNode
    {
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            WorldObject worldObject = WorldObjectMaker.MakeWorldObject(this.def.GetValue(slate));
            worldObject.Tile = this.tile.GetValue(slate);
            if (this.faction.GetValue(slate) != null)
            {
                worldObject.SetFaction(this.faction.GetValue(slate));
            }
            if (worldObject is WorldObject_PaxTalks wopt)
            {
                bool mayhemMode = HVMP_Mod.settings.voxX;
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.vox1, mayhemMode))
                {
                    wopt.AAA_difficultyFactor = this.AAA_difficultyFactor;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_AAA_info", this.AAA_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_AAA_info", " ") });
                }
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.vox2, mayhemMode))
                {
                    wopt.ST_difficultyFactor = this.ST_difficultyFactor;
                    wopt.ST_giftCount = this.ST_giftCountRange.RandomInRange;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("moneyAmount", wopt.ST_giftCount.ToString())
                    });
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_ST_info", this.ST_description.Formatted())
                    });
                }
                else
                {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_ST_info", " ") });
                }
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.vox3, mayhemMode))
                {
                    if (Rand.Chance(this.TTFTIO_chance))
                    {
                        Map map = QuestGen.slate.Get<Map>("map", null, false) ?? Find.AnyPlayerHomeMap;
                        wopt.TTFTIO_points = (int)(this.TTFTIO_pointsFactor * StorytellerUtility.DefaultThreatPointsNow(map));
                    }
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_TTFTIO_info", this.TTFTIO_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_TTFTIO_info", " ") });
                }
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
        public float AAA_difficultyFactor;
        [MustTranslate]
        public string AAA_description;
        public float ST_difficultyFactor;
        public IntRange ST_giftCountRange;
        [MustTranslate]
        public string ST_description;
        public float TTFTIO_chance;
        public float TTFTIO_pointsFactor;
        [MustTranslate]
        public string TTFTIO_description;
    }
    public class WorldObject_PaxTalks : WorldObject
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
                    }
                    else
                    {
                        color = Color.white;
                    }
                    this.cachedMat = MaterialPool.MatFrom(this.def.texture, ShaderDatabase.WorldOverlayTransparentLit, color, WorldMaterials.WorldObjectRenderQueue);
                }
                return this.cachedMat;
            }
        }
        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            if (this.TTFTIO_points > 0)
            {
                text += "HVMP_VoxSiteLabel_TTFTIO".Translate();
            }
            if (this.AAA_difficultyFactor > 1f)
            {
                text += "HVMP_VoxSiteLabel_AAA".Translate();
            }
            if (this.ST_giftCount > 0)
            {
                text += "HVMP_VoxSiteLabel_ST".Translate(this.ST_giftCount);
            }
            return text;
        }
        public void Notify_CaravanArrived(Caravan caravan)
        {
            if (this.TTFTIO_points <= 0f || Rand.Chance(0.5f))
            {
                Pawn pawn = BestCaravanPawnUtility.FindBestDiplomat(caravan);
                if (pawn == null)
                {
                    Messages.Message("MessagePeaceTalksNoDiplomat".Translate(), caravan, MessageTypeDefOf.NegativeEvent, false);
                    return;
                }
                float badOutcomeWeightFactor = this.GetBadOutcomeWeightFactor(pawn, caravan);
                if (this.ST_giftCount > 0)
                {
                    int silverCount = 0;
                    List<Thing> list = CaravanInventoryUtility.AllInventoryItems(caravan);
                    for (int i = 0; i < list.Count; i++)
                    {
                        Thing thing = list[i];
                        if (thing.def == ThingDefOf.Silver)
                        {
                            silverCount += thing.stackCount;
                        }
                    }
                    badOutcomeWeightFactor *= Math.Max(1f,((this.ST_difficultyFactor-1f)*(this.ST_giftCount-silverCount)/this.ST_giftCount)+1f);
                    int silverToTake = this.ST_giftCount;
                    List<Thing> list2 = CaravanInventoryUtility.TakeThings(caravan, delegate (Thing thing)
                    {
                        if (ThingDefOf.Silver != thing.def)
                        {
                            return 0;
                        }
                        int numS = Mathf.Min(silverToTake, thing.stackCount);
                        silverToTake -= numS;
                        return numS;
                    });
                    for (int i = 0; i < list2.Count; i++)
                    {
                        list2[i].Destroy(DestroyMode.Vanish);
                    }
                }
                float num = 1f / badOutcomeWeightFactor;
                WorldObject_PaxTalks.tmpPossibleOutcomes.Clear();
                WorldObject_PaxTalks.tmpPossibleOutcomes.Add(new Pair<Action, float>(delegate
                {
                    this.Outcome_Backfire(caravan);
                }, 0.15f * badOutcomeWeightFactor));
                WorldObject_PaxTalks.tmpPossibleOutcomes.Add(new Pair<Action, float>(delegate
                {
                    this.Outcome_TalksFlounder(caravan);
                }, 0.15f));
                WorldObject_PaxTalks.tmpPossibleOutcomes.Add(new Pair<Action, float>(delegate
                {
                    this.Outcome_Success(caravan);
                }, 0.55f * num));
                WorldObject_PaxTalks.tmpPossibleOutcomes.RandomElementByWeight((Pair<Action, float> x) => x.Second).First();
                pawn.skills.Learn(SkillDefOf.Social, 6000f, true, false);
            } else {
                HVMP_Utility.DoAmbush(caravan, this.TTFTIO_points);
                QuestUtility.SendQuestTargetSignals(this.questTags, "TTFTIO_Ambush", this.Named("SUBJECT"));
            }
            this.Destroy();
        }
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(caravan))
            {
                yield return floatMenuOption;
            }
            foreach (FloatMenuOption floatMenuOption2 in CaravanArrivalAction_VisitPaxTalks.GetFloatMenuOptions(caravan, this))
            {
                yield return floatMenuOption2;
            }
            yield break;
        }
        private void Outcome_Backfire(Caravan caravan)
        {
            Find.LetterStack.ReceiveLetter("LetterLabelPeaceTalks_Backfire".Translate(), this.GetLetterText("HVMP_PaxTalksBackfire".Translate(), caravan), LetterDefOf.NegativeEvent, caravan, base.Faction, null, null, null, 0, true);
            QuestUtility.SendQuestTargetSignals(this.questTags, "Failed", this.Named("SUBJECT"));
        }
        private void Outcome_TalksFlounder(Caravan caravan)
        {
            Find.LetterStack.ReceiveLetter("LetterLabelPeaceTalks_TalksFlounder".Translate(), this.GetLetterText("HVMP_PaxTalksFlounder".Translate(), caravan), LetterDefOf.NeutralEvent, caravan, base.Faction, null, null, null, 0, true);
            QuestUtility.SendQuestTargetSignals(this.questTags, "Failed", this.Named("SUBJECT"));
        }
        private void Outcome_Success(Caravan caravan)
        {
            Find.LetterStack.ReceiveLetter("LetterLabelPeaceTalks_Success".Translate(), this.GetLetterText("HVMP_PaxTalksSuccess".Translate(), caravan), LetterDefOf.PositiveEvent, caravan, base.Faction, null, null, null, 0, true);
            QuestUtility.SendQuestTargetSignals(this.questTags, "Resolved", this.Named("SUBJECT"));
        }
        private string GetLetterText(string baseText, Caravan caravan)
        {
            TaggedString taggedString = baseText;
            Pawn pawn = BestCaravanPawnUtility.FindBestDiplomat(caravan);
            if (pawn != null)
            {
                taggedString += "\n\n" + "PeaceTalksSocialXPGain".Translate(pawn.LabelShort, 6000f.ToString("F0"), pawn.Named("PAWN"));
            }
            return taggedString;
        }
        private float GetBadOutcomeWeightFactor(Pawn diplomat, Caravan caravan)
        {
            StatDef sd = StatDefOf.NegotiationAbility;
            PaxTalksComp ptc = this.GetComponent<PaxTalksComp>();
            if (ptc != null)
            {
                sd = ptc.negotiationStat;
            }
            float statValue = diplomat.GetStatValue(sd, true, -1);
            float num = 0f;
            if (ModsConfig.IdeologyActive)
            {
                bool flag = false;
                using (List<Pawn>.Enumerator enumerator = caravan.pawns.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current == caravan.Faction.leader)
                        {
                            flag = true;
                            break;
                        }
                    }
                }
                num = (flag ? (-0.05f) : 0.05f);
            }
            return WorldObject_PaxTalks.GetBadOutcomeWeightFactor(statValue) * (1f + num)*this.AAA_Difficulty;
        }
        private float AAA_Difficulty
        {
            get
            {
                float totalGoodwillFactions = 0f, totalAlliedFactions = 0f;
                Faction p = Faction.OfPlayer;
                if (p != null)
                {
                    foreach (Faction f in Find.FactionManager.AllFactionsVisible)
                    {
                        if (!f.def.HasModExtension<EBranchQuests>() && f != p && f.HasGoodwill && !f.def.PermanentlyHostileTo(p.def))
                        {
                            totalGoodwillFactions += 1f;
                            if (f.RelationKindWith(p) == FactionRelationKind.Ally)
                            {
                                totalAlliedFactions += 1f;
                            }
                        }
                    }
                }
                float result = this.AAA_difficultyFactor;
                if (totalGoodwillFactions <= 0f)
                {
                    return result;
                }
                return Math.Max(1f,((result-1f)*(totalGoodwillFactions-totalAlliedFactions)/totalGoodwillFactions)+1f);
            }
        }
        private static float GetBadOutcomeWeightFactor(float negotationAbility)
        {
            return WorldObject_PaxTalks.BadOutcomeChanceFactorByNegotiationAbility.Evaluate(negotationAbility);
        }
        private Material cachedMat;
        private static readonly SimpleCurve BadOutcomeChanceFactorByNegotiationAbility = new SimpleCurve
        {
            {
                new CurvePoint(0f, 4f),
                true
            },
            {
                new CurvePoint(1f, 1f),
                true
            },
            {
                new CurvePoint(1.5f, 0.4f),
                true
            },
            {
                new CurvePoint(5f, 0.01f),
                true
            }
        };
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.AAA_difficultyFactor, "AAA_difficultyFactor", 1f, false);
            Scribe_Values.Look<int>(ref this.ST_giftCount, "ST_giftCount", -1, false);
            Scribe_Values.Look<float>(ref this.ST_difficultyFactor, "ST_difficultyFactor", 1f, false);
            Scribe_Values.Look<int>(ref this.TTFTIO_points, "TTFTIO_points", -1, false);
        }
        private static List<Pair<Action, float>> tmpPossibleOutcomes = new List<Pair<Action, float>>();
        public float AAA_difficultyFactor = 1f;
        public int ST_giftCount = -1;
        public float ST_difficultyFactor = 1f;
        public int TTFTIO_points;
    }
    public class WorldObjectCompProperties_PaxTalks : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_PaxTalks ()
        {
            this.compClass = typeof(PaxTalksComp);
        }
        public StatDef negotiationStat;
    }
    public class PaxTalksComp : WorldObjectComp
    {
        public override void Initialize(WorldObjectCompProperties props)
        {
            base.Initialize(props);
            if (this.props is WorldObjectCompProperties_PaxTalks woccpt)
            {
                this.negotiationStat = woccpt.negotiationStat;
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look<StatDef>(ref this.negotiationStat, "negotiationStat");
        }
        public StatDef negotiationStat;
    }
    public class CaravanArrivalAction_VisitPaxTalks : CaravanArrivalAction
    {
        public override string Label
        {
            get
            {
                return "VisitPeaceTalks".Translate(this.paxTalks.Label);
            }
        }
        public override string ReportString
        {
            get
            {
                return "CaravanVisiting".Translate(this.paxTalks.Label);
            }
        }
        public CaravanArrivalAction_VisitPaxTalks()
        {
        }
        public CaravanArrivalAction_VisitPaxTalks(WorldObject_PaxTalks paxTalks)
        {
            this.paxTalks = paxTalks;
        }
        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(caravan, destinationTile);
            if (!floatMenuAcceptanceReport)
            {
                return floatMenuAcceptanceReport;
            }
            if (this.paxTalks != null && this.paxTalks.Tile != destinationTile)
            {
                return false;
            }
            return CaravanArrivalAction_VisitPaxTalks.CanVisit(caravan, this.paxTalks);
        }
        public override void Arrived(Caravan caravan)
        {
            this.paxTalks.Notify_CaravanArrived(caravan);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<WorldObject_PaxTalks>(ref this.paxTalks, "paxTalks", false);
        }
        public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, WorldObject_PaxTalks paxTalks)
        {
            return paxTalks != null && paxTalks.Spawned;
        }
        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, WorldObject_PaxTalks paxTalks)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions<CaravanArrivalAction_VisitPaxTalks>(() => CaravanArrivalAction_VisitPaxTalks.CanVisit(caravan, paxTalks), () => new CaravanArrivalAction_VisitPaxTalks(paxTalks), "VisitPeaceTalks".Translate(paxTalks.Label), caravan, paxTalks.Tile, paxTalks, null);
        }
        private WorldObject_PaxTalks paxTalks;
    }
    //rover branchquest: atlas
    public class Book_Atlas : ThingWithComps
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<WorldObject>(ref this.wo, "wo", false);
        }
        public WorldObject wo;
    }
    public class CompProperties_StudiableAtlas : CompProperties_StudiableQuestItem
    {
        public CompProperties_StudiableAtlas()
        {
            this.compClass = typeof(CompStudiableAtlas);
        }
        public float DTS_boredomPerHour;
        public ThoughtDef DTS_thoughtToGive;
        public int DTS_thoughtGiverCooldown;
    }
    public class CompStudiableAtlas : CompStudiableQuestItem
    {
        public new CompProperties_StudiableAtlas Props
        {
            get
            {
                return (CompProperties_StudiableAtlas)this.props;
            }
        }
        public override void ExtraStudyEffects(int delta, Pawn researcher, Thing brb, Thing researchBench)
        {
            if (this.DTS_on)
            {
                if (this.Props.DTS_thoughtToGive != null && researcher.needs.mood != null && researcher.IsHashIntervalTick(this.Props.DTS_thoughtGiverCooldown,delta))
                {
                    researcher.needs.mood.thoughts.memories.TryGainMemory(this.Props.DTS_thoughtToGive);
                }
                Need_Joy nj = researcher.needs.joy;
                if (nj != null)
                {
                    nj.CurLevel = nj.CurLevel - (this.Props.DTS_boredomPerHour*delta/2500);
                }
            }
        }
        public override float RequiredProgress => base.RequiredProgress * this.ID_multi;
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.DTS_on, "DTS_on", false, false);
            Scribe_Values.Look<float>(ref this.ID_multi, "ID_multi", 1f, false);
        }
        public bool DTS_on;
        public float ID_multi = 1f;
    }
    public class WorldObject_AtlasPoint : WorldObject
    {
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach (FloatMenuOption o in base.GetFloatMenuOptions(caravan))
            {
                yield return o;
            }
            foreach (FloatMenuOption f in CaravanArrivalAction_VisitAtlasPoint.GetFloatMenuOptions(caravan, this))
            {
                yield return f;
            }
            yield break;
        }
        public void Notify_CaravanArrived(Caravan caravan)
        {
            CompStudiableQuestItem cda = this.book.TryGetComp<CompStudiableQuestItem>();
            if (cda != null)
            {
                foreach (Pawn p in caravan.pawns)
                {
                    if (!cda.pawns.Contains(p))
                    {
                        cda.pawns.Add(p);
                    }
                }
            }
            if (this.TPBU_on)
            {
                Slate slate = new Slate();
                Map map = Find.AnyPlayerHomeMap;
                slate.Set<float>("points", StorytellerUtility.DefaultThreatPointsNow(map), false);
                slate.Set<Map>("map", map, false);
                Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(DefDatabase<QuestScriptDef>.GetNamed("ProblemCauser"), slate);
                if (!quest.hidden && quest.root.sendAvailableLetter)
                {
                    QuestUtility.SendLetterQuestAvailable(quest);
                }
                if (Rand.Chance(0.8f))
                {
                    this.TPBU_on = false;
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Book_Atlas>(ref this.book, "book", false);
            Scribe_Values.Look<bool>(ref this.TPBU_on, "TPBU_on", false, false);
        }
        public Book_Atlas book;
        public bool TPBU_on;
    }
    public class CaravanArrivalAction_VisitAtlasPoint : CaravanArrivalAction
    {
        public override string Label
        {
            get
            {
                return "VisitPeaceTalks".Translate(this.atlasPoint.Label);
            }
        }
        public override string ReportString
        {
            get
            {
                return "CaravanVisiting".Translate(this.atlasPoint.Label);
            }
        }
        public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, WorldObject_AtlasPoint atlasPoint)
        {
            return atlasPoint != null && atlasPoint.Spawned;
        }
        public CaravanArrivalAction_VisitAtlasPoint()
        {
        }
        public CaravanArrivalAction_VisitAtlasPoint(WorldObject_AtlasPoint atlasPoint)
        {
            this.atlasPoint = atlasPoint;
        }
        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(caravan, destinationTile);
            if (floatMenuAcceptanceReport)
            {
                if (this.atlasPoint != null && this.atlasPoint.Tile != destinationTile)
                {
                    floatMenuAcceptanceReport = false;
                }
                else
                {
                    floatMenuAcceptanceReport = CaravanArrivalAction_VisitAtlasPoint.CanVisit(caravan, this.atlasPoint);
                }
            }
            return floatMenuAcceptanceReport;
        }
        public override void Arrived(Caravan caravan)
        {
            this.atlasPoint.Notify_CaravanArrived(caravan);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<WorldObject_AtlasPoint>(ref this.atlasPoint, "atlasPoint", false);
        }
        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, WorldObject_AtlasPoint atlasPoint)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions<CaravanArrivalAction_VisitAtlasPoint>(() => CaravanArrivalAction_VisitAtlasPoint.CanVisit(caravan, atlasPoint), () => new CaravanArrivalAction_VisitAtlasPoint(atlasPoint), "VisitPeaceTalks".Translate(atlasPoint.Label), caravan, atlasPoint.Tile, atlasPoint, null);
        }
        private WorldObject_AtlasPoint atlasPoint;
    }
    public class QuestNode_Root_Atlas : QuestNode_Sequence
    {
        protected override void RunInt()
        {
            PlanetTile tile = HVMP_Utility.TryGetPlanetTile();
            if (this.TryFindSiteTile(tile, out PlanetTile num) && HVMP_Utility.TryFindRoverFaction(out Faction roverFaction))
            {
                Slate slate = QuestGen.slate;
                Quest quest = QuestGen.quest;
                Map map = HVMP_Utility.TryGetMap();
                bool mayhemMode = HVMP_Mod.settings.atlasX;
                this.TryFindSiteTile(tile, out PlanetTile num2);
                string text = QuestGenUtility.HardcodedSignalWithQuestID("worldObject.Destroyed");
                WorldObject_AtlasPoint worldObject_AtlasPoint = (WorldObject_AtlasPoint)WorldObjectMaker.MakeWorldObject(HVMPDefOf.HVMP_AtlasPoint);
                worldObject_AtlasPoint.Tile = num;
                worldObject_AtlasPoint.SetFaction(roverFaction);
                worldObject_AtlasPoint.book = (Book_Atlas)ThingMaker.MakeThing(HVMPDefOf.HVMP_DatedAtlas);
                worldObject_AtlasPoint.book.wo = worldObject_AtlasPoint;
                if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.atlas3, mayhemMode))
                {
                    worldObject_AtlasPoint.TPBU_on = true;
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_TPBU_info", this.TPBU_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_TPBU_info", " ") });
                }
                CompQuality cq = worldObject_AtlasPoint.book.TryGetComp<CompQuality>();
                if (cq != null)
                {
                    cq.SetQuality(QualityUtility.GenerateQualityTraderItem(), new ArtGenerationContext?(ArtGenerationContext.Outsider));
                }
                quest.SpawnWorldObject(worldObject_AtlasPoint, null, null);
                quest.End(QuestEndOutcome.Unknown, 0, null, text, QuestPart.SignalListenMode.OngoingOnly, false, false);
                slate.Set<Map>("map", map, false);
                QuestPart_BranchGoodwillFailureHandler qpbgfh = new QuestPart_BranchGoodwillFailureHandler();
                qpbgfh.faction = roverFaction;
                QuestGen.quest.AddPart(qpbgfh);
                List<WorldObject> wos = new List<WorldObject>
                {
                    worldObject_AtlasPoint
                };
                int challengeRating = QuestGen.quest.challengeRating;
                CompStudiableQuestItem cda = worldObject_AtlasPoint.book.TryGetComp<CompStudiableQuestItem>();
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
                        slate.Set<string>(this.storeExtraSkillAs.GetValue(slate), extraSkill.label, false);
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
                    if (cda is CompStudiableAtlas atlasComp)
                    {
                        if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.atlas1, mayhemMode))
                        {
                            atlasComp.DTS_on = true;
                            QuestGen.AddQuestDescriptionRules(new List<Rule>
                            {
                                new Rule_String("mutator_DTS_info", this.DTS_description.Formatted())
                            });
                        } else {
                            QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_DTS_info", " ") });
                        }
                        if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.atlas2, mayhemMode))
                        {
                            atlasComp.ID_multi = this.ID_multi;
                        }
                    } else {
                        QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_DTS_info", " ") });
                    }
                }
                slate.Set<List<WorldObject>>("worldObject", wos, false);
                HVMP_Utility.SetSettingScalingRewardValue(slate);
                QuestGenUtility.AddToOrMakeList(slate, "thingsToDrop", worldObject_AtlasPoint.book);
                QuestGen.slate.Set<Thing>(this.storeBookAs.GetValue(slate), worldObject_AtlasPoint.book, false);
                QuestGen.slate.Set<Faction>("faction", roverFaction, false);
                QuestUtility.AddQuestTag(ref worldObject_AtlasPoint.book.questTags, this.storeBookAs.GetValue(slate));
                quest.AddPart(new QuestPart_LookAtThis(worldObject_AtlasPoint.book));
            }
            base.RunInt();
        }
        private bool TryFindSiteTile(PlanetTile pTile, out PlanetTile tile)
        {
            return TileFinder.TryFindNewSiteTile(out tile, pTile, 2, 20, false, DefDatabase<LandmarkDef>.AllDefsListForReading, 0.5f, true, TileFinderMode.Near, true, false, null, null);
        }
        protected override bool TestRunInt(Slate slate)
        {
            HVMP_Utility.SetSettingScalingRewardValue(slate);
            PlanetTile tile = HVMP_Utility.TryGetPlanetTile();
            Map map = HVMP_Utility.TryGetMap();
            slate.Set<Map>("map", map, false);
            return this.TryFindSiteTile(tile, out PlanetTile num) && HVMP_Utility.TryFindRoverFaction(out Faction roverFaction) && base.TestRunInt(slate);
        }
        [NoTranslate]
        public SlateRef<string> storeBookAs;
        [NoTranslate]
        public SlateRef<string> storeExtraSkillAs;
        [NoTranslate]
        public SlateRef<string> inSignal;
        [MustTranslate]
        public string DTS_description;
        public float ID_multi;
        [MustTranslate]
        public string TPBU_description;
    }
    //rover branchquest: ares
    public class QuestNode_AllThreeAresMutators : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return Find.Storyteller.difficulty.allowViolentQuests && slate.Exists("map", false) && slate.Exists("enemyFaction", false);
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Map map = QuestGen.slate.Get<Map>("map", null, false);
            float num = QuestGen.slate.Get<float>("points", 0f, false);
            Faction faction = QuestGen.slate.Get<Faction>("enemyFaction", null, false);
            QuestPart_Incident questPart_Incident = new QuestPart_Incident();
            questPart_Incident.debugLabel = "raid";
            questPart_Incident.incident = HVMPDefOf.HVMP_RaidAres;
            int num2 = 0;
            IncidentParms incidentParms;
            PawnGroupMakerParms defaultPawnGroupMakerParms;
            IEnumerable<PawnKindDef> enumerable;
            do
            {
                incidentParms = this.GenerateIncidentParms(map, num, faction, slate, questPart_Incident);
                defaultPawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, incidentParms, true);
                defaultPawnGroupMakerParms.points = IncidentWorker_Raid.AdjustedRaidPoints(defaultPawnGroupMakerParms.points, incidentParms.raidArrivalMode, incidentParms.raidStrategy, defaultPawnGroupMakerParms.faction, PawnGroupKindDefOf.Combat, incidentParms.target, null);
                enumerable = PawnGroupMakerUtility.GeneratePawnKindsExample(defaultPawnGroupMakerParms);
                num2++;
            }
            while (!enumerable.Any<PawnKindDef>() && num2 < 50);
            if (!enumerable.Any<PawnKindDef>())
            {
                string[] array = new string[6];
                array[0] = "No pawnkinds example for ";
                array[1] = QuestGen.quest.root.defName;
                array[2] = " parms=";
                int num3 = 3;
                PawnGroupMakerParms pawnGroupMakerParms = defaultPawnGroupMakerParms;
                array[num3] = ((pawnGroupMakerParms != null) ? pawnGroupMakerParms.ToString() : null);
                array[4] = " iterations=";
                array[5] = num2.ToString();
                Log.Error(string.Concat(array));
            }
            IncidentWorker_RaidAres iwrf = (IncidentWorker_RaidAres)questPart_Incident.incident.Worker;
            this.ImplementQuestMutators(slate, faction);
            questPart_Incident.SetIncidentParmsAndRemoveTarget(incidentParms);
            questPart_Incident.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal", null, false);
            QuestGen.quest.AddPart(questPart_Incident);
            QuestGen.AddQuestDescriptionRules(new List<Rule>
            {
                new Rule_String("raidPawnKinds", PawnUtility.PawnKindsToLineList(enumerable, "  - ", ColoredText.ThreatColor)),
                new Rule_String("raidArrivalModeInfo", incidentParms.raidArrivalMode.textWillArrive.Formatted(faction))
            });
        }
        private IncidentParms GenerateIncidentParms(Map map, float points, Faction faction, Slate slate, QuestPart_Incident questPart)
        {
            IncidentParms incidentParms = new IncidentParms
            {
                forced = true,
                target = map,
                points = Mathf.Max(points, faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat, null)),
                faction = faction,
                pawnGroupMakerSeed = new int?(Rand.Int),
                inSignalEnd = QuestGenUtility.HardcodedSignalWithQuestID(this.inSignalLeave.GetValue(slate)),
                questTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(this.tag.GetValue(slate)),
                canTimeoutOrFlee = (map.CanEverExit && (this.canTimeoutOrFlee.GetValue(slate) ?? true))
            };
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
            IncidentWorker_RaidAres iwrf = (IncidentWorker_RaidAres)questPart.incident.Worker;
            iwrf.ResolveRaidStrategy(incidentParms, PawnGroupKindDefOf.Combat);
            iwrf.ResolveRaidArriveMode(incidentParms);
            iwrf.ResolveRaidAgeRestriction(incidentParms);
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
        public void ImplementQuestMutators(Slate slate, Faction faction)
        {
            QuestPart_AllThreeAresMutators qpa3 = new QuestPart_AllThreeAresMutators();
            bool mayhemMode = HVMP_Mod.settings.aresX;
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.ares1, mayhemMode))
            {
                if (Rand.Chance(this.AW_conditionChance))
                {
                    qpa3.AW_condition = this.AW_conditions.RandomElement();
                    qpa3.AW_conditionTicks = (int)(this.AW_conditionHours.RandomInRange * 2500);
                }
                else
                {
                    qpa3.AW_bonusPoints = (float)this.AW_bonusPoints.GetValue(slate);
                    qpa3.AW_pawnRosterOtherwise = this.AW_pawnRosterOtherwise.RandomElement();
                }
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_AW_info", this.AW_description.Formatted(faction))
                });
            }
            else
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_AW_info", " ") });
            }
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.ares2, mayhemMode))
            {
                qpa3.DRTNT_hediff = this.DRTNT_hediff;
                qpa3.DRTNT_spyChance = this.DRTNT_spyChance;
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_DRTNT_info", this.DRTNT_description.Formatted(faction))
                    });
            }
            else
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_DRTNT_info", " ") });
            }
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.ares3, mayhemMode))
            {
                qpa3.TG_hediffChances = this.TG_hediffChances;
                qpa3.TG_bonusPoints = (float)this.TG_bonusPoints.GetValue(slate);
                qpa3.TG_pawnRoster = this.TG_pawnRoster.RandomElement();
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_TG_info", this.TG_description.Formatted(faction))
                    });
            }
            else
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_TG_info", " ") });
            }
            QuestGen.quest.AddPart(qpa3);
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
        public float AW_conditionChance;
        public List<GameConditionDef> AW_conditions;
        public FloatRange AW_conditionHours;
        public List<PawnGroupMaker> AW_pawnRosterOtherwise;
        public SlateRef<float?> AW_bonusPoints;
        [MustTranslate]
        public string AW_description;
        public float DRTNT_spyChance;
        public HediffDef DRTNT_hediff;
        [MustTranslate]
        public string DRTNT_description;
        public Dictionary<HediffDef, float> TG_hediffChances;
        public List<PawnGroupMaker> TG_pawnRoster;
        public SlateRef<float?> TG_bonusPoints;
        [MustTranslate]
        public string TG_description;
    }
    public class QuestPart_AllThreeAresMutators : QuestPart
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<GameConditionDef>(ref this.AW_condition, "AW_condition");
            Scribe_Values.Look<int>(ref this.AW_conditionTicks, "AW_conditionTicks", 0, false);
            Scribe_Values.Look<PawnGroupMaker>(ref this.AW_pawnRosterOtherwise, "AW_pawnRosterOtherwise", null, false);
            Scribe_Values.Look<float>(ref this.AW_bonusPoints, "AW_bonusPoints", 0f, false);
            Scribe_Values.Look<float>(ref this.DRTNT_spyChance, "DRTNT_spyChance", 0f, false);
            Scribe_Defs.Look<HediffDef>(ref this.DRTNT_hediff, "DRTNT_hediff");
            Scribe_Collections.Look<HediffDef, float>(ref this.TG_hediffChances, "TG_hediffChances", LookMode.Def, LookMode.Value, ref this.tmpHediffs, ref this.tmpChances);
            Scribe_Values.Look<PawnGroupMaker>(ref this.TG_pawnRoster, "TG_pawnRoster", null, false);
            Scribe_Values.Look<float>(ref this.TG_bonusPoints, "TG_bonusPoints", 0f, false);
        }
        public GameConditionDef AW_condition;
        public int AW_conditionTicks;
        public PawnGroupMaker AW_pawnRosterOtherwise;
        public float AW_bonusPoints;
        public float DRTNT_spyChance;
        public HediffDef DRTNT_hediff;
        public Dictionary<HediffDef, float> TG_hediffChances;
        public PawnGroupMaker TG_pawnRoster;
        public float TG_bonusPoints;
        private List<HediffDef> tmpHediffs;
        private List<float> tmpChances;
    }
    public class IncidentWorker_RaidAres : IncidentWorker_RaidEnemy
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            List<Pawn> list;
            if (!this.TryGenerateRaidInfo(parms, out list, false))
            {
                return false;
            }
            List<Pawn> listMut = new List<Pawn>();
            PawnGroupMakerParms pgmp = new PawnGroupMakerParms();
            pgmp.tile = parms.target.Tile;
            pgmp.faction = parms.faction;
            pgmp.traderKind = parms.traderKind;
            pgmp.generateFightersOnly = parms.generateFightersOnly;
            pgmp.raidStrategy = parms.raidStrategy;
            pgmp.forceOneDowned = parms.raidForceOneDowned;
            pgmp.seed = parms.pawnGroupMakerSeed;
            pgmp.ideo = parms.pawnIdeo;
            pgmp.raidAgeRestriction = parms.raidAgeRestriction;
            QuestPart_AllThreeAresMutators qpa3 = parms.quest.GetFirstPartOfType<QuestPart_AllThreeAresMutators>();
            if (qpa3 != null)
            {
                if (qpa3.AW_pawnRosterOtherwise != null)
                {
                    pgmp.groupKind = qpa3.AW_pawnRosterOtherwise.kindDef;
                    if (!pgmp.faction.def.pawnGroupMakers.ContainsAny((PawnGroupMaker pgm) => pgm.kindDef == pgmp.groupKind))
                    {
                        pgmp.faction.def.pawnGroupMakers.Add(qpa3.AW_pawnRosterOtherwise);
                    }
                    float minPoints = 200f;
                    foreach (PawnGenOption pgo in qpa3.AW_pawnRosterOtherwise.options)
                    {
                        float combatPower = pgo.kind.combatPower;
                        if (combatPower > minPoints)
                        {
                            minPoints = combatPower;
                        }
                    }
                    pgmp.points = Math.Max(qpa3.AW_bonusPoints, minPoints * 4f);
                    foreach (Pawn pawn in qpa3.AW_pawnRosterOtherwise.GeneratePawns(pgmp, true))
                    {
                        listMut.Add(pawn);
                    }
                }
                if (qpa3.TG_pawnRoster != null)
                {
                    pgmp.groupKind = qpa3.TG_pawnRoster.kindDef;
                    if (!pgmp.faction.def.pawnGroupMakers.ContainsAny((PawnGroupMaker pgm) => pgm.kindDef == pgmp.groupKind))
                    {
                        pgmp.faction.def.pawnGroupMakers.Add(qpa3.AW_pawnRosterOtherwise);
                    }
                    float minPoints = 200f;
                    foreach (PawnGenOption pgo in qpa3.TG_pawnRoster.options)
                    {
                        float combatPower = pgo.kind.combatPower;
                        if (combatPower > minPoints)
                        {
                            minPoints = combatPower;
                        }
                    }
                    pgmp.points = Math.Max(qpa3.TG_bonusPoints, minPoints * 4f);
                    foreach (Pawn pawn in qpa3.TG_pawnRoster.GeneratePawns(pgmp, true))
                    {
                        pawn.health.AddHediff(HVMPDefOf.HVMP_HostileEnvironmentFilm);
                        listMut.Add(pawn);
                    }
                }
                if (!listMut.NullOrEmpty())
                {
                    parms.raidArrivalMode.Worker.Arrive(listMut, parms);
                }
                if (qpa3.AW_condition != null)
                {
                    GameCondition gameCondition = GameConditionMaker.MakeCondition(qpa3.AW_condition, qpa3.AW_conditionTicks);
                    gameCondition.forceDisplayAsDuration = true;
                    gameCondition.Permanent = false;
                    gameCondition.quest = parms.quest;
                    Map map = (Map)parms.target;
                    if (map == null)
                    {
                        MapParent mapParent = gameCondition.quest.TryFindNewSuitableMapParentForRetarget();
                        map = ((mapParent != null) ? mapParent.Map : null) ?? Find.AnyPlayerHomeMap;
                    }
                    if (map != null)
                    {
                        List<Rule> listRule = new List<Rule>();
                        Dictionary<string, string> dictionary = new Dictionary<string, string>();
                        gameCondition.RandomizeSettings(parms.points, map, listRule, dictionary);
                        map.gameConditionManager.RegisterCondition(gameCondition);
                    }
                    Find.LetterStack.ReceiveLetter(gameCondition.LabelCap, gameCondition.LetterText, gameCondition.def.letterDef, LookTargets.Invalid, null, gameCondition.quest, null, null, 0, true);
                }
            }
            TaggedString taggedString = this.GetLetterLabel(parms);
            TaggedString taggedString2 = this.GetLetterText(parms, list);
            PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(list, ref taggedString, ref taggedString2, this.GetRelatedPawnsInfoLetterText(parms), true, true);
            List<TargetInfo> list2 = new List<TargetInfo>();
            if (parms.pawnGroups != null)
            {
                List<List<Pawn>> list3 = IncidentParmsUtility.SplitIntoGroups(list, parms.pawnGroups);
                if (!listMut.NullOrEmpty())
                {
                    list3.Add(listMut);
                }
                List<Pawn> list4 = list3.MaxBy((List<Pawn> x) => x.Count);
                if (list4.Any<Pawn>())
                {
                    list2.Add(list4[0]);
                }
                for (int i = 0; i < list3.Count; i++)
                {
                    if (list3[i] != list4 && list3[i].Any<Pawn>())
                    {
                        list2.Add(list3[i][0]);
                    }
                }
            } else if (list.Any<Pawn>()) {
                foreach (Pawn pawn in list)
                {
                    list2.Add(pawn);
                }
            }
            base.SendStandardLetter(taggedString, taggedString2, this.GetLetterDef(), parms, list2, Array.Empty<NamedArgument>());
            if (parms.controllerPawn == null || parms.controllerPawn.Faction != Faction.OfPlayer)
            {
                if (!listMut.NullOrEmpty())
                {
                    list.AddRange(listMut);
                }
                parms.raidStrategy.Worker.MakeLords(parms, list);
            }
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.EquippingWeapons, OpportunityType.Critical);
            if (!PlayerKnowledgeDatabase.IsComplete(ConceptDefOf.ShieldBelts))
            {
                for (int j = 0; j < list.Count; j++)
                {
                    Pawn pawn2 = list[j];
                    if (pawn2.apparel != null)
                    {
                        if (pawn2.apparel.WornApparel.Any((Apparel ap) => ap.def == ThingDefOf.Apparel_ShieldBelt))
                        {
                            LessonAutoActivator.TeachOpportunity(ConceptDefOf.ShieldBelts, OpportunityType.Critical);
                            break;
                        }
                    }
                }
            }
            if (DebugSettings.logRaidInfo)
            {
                Log.Message(string.Format("Raid: {0} ({1}) {2} {3} c={4} p={5}", new object[]
                {
                    parms.faction.Name,
                    parms.faction.def.defName,
                    parms.raidArrivalMode.defName,
                    parms.raidStrategy.defName,
                    parms.spawnCenter,
                    parms.points
                }));
            }
            return true;
        }
        protected override void PostProcessSpawnedPawns(IncidentParms parms, List<Pawn> pawns)
        {
            base.PostProcessSpawnedPawns(parms, pawns);
            QuestPart_AllThreeAresMutators qpa3 = parms.quest.GetFirstPartOfType<QuestPart_AllThreeAresMutators>();
            if (qpa3 != null && qpa3.DRTNT_hediff != null)
            {
                int minSpyCount = 1;
                foreach (Pawn p in pawns.InRandomOrder())
                {
                    if (p.RaceProps.intelligence == Intelligence.Humanlike && (minSpyCount > 0 || Rand.Chance(qpa3.DRTNT_spyChance)))
                    {
                        p.health.AddHediff(qpa3.DRTNT_hediff);
                        minSpyCount--;
                    }
                    if (!qpa3.TG_hediffChances.NullOrEmpty())
                    {
                        foreach (KeyValuePair<HediffDef, float> kvp in qpa3.TG_hediffChances)
                        {
                            if (Rand.Chance(kvp.Value))
                            {
                                p.health.AddHediff(kvp.Key);
                            }
                        }
                    }
                }
            }
        }
    }
    public class HVMP_GameCondition_TargetedPsychicSuppression : GameCondition
    {
        public override string LetterText
        {
            get
            {
                return base.LetterText.Formatted(this.gender.GetLabel(false).ToLower());
            }
        }
        public override string Description
        {
            get
            {
                return base.Description.Formatted(this.gender.GetLabel(false).ToLower());
            }
        }
        public override void Init()
        {
            base.Init();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<Gender>(ref this.gender, "gender", Gender.None, false);
        }
        public static void CheckPawn(Pawn pawn, Gender targetGender)
        {
            if (pawn.RaceProps.Humanlike && pawn.gender == targetGender && (pawn.Faction == null || pawn.Faction == Faction.OfPlayer || !pawn.Faction.HostileTo(Faction.OfPlayer)) && !pawn.health.hediffSet.HasHediff(HVMPDefOf.HVMP_TargetedPsychicSuppression, false))
            {
                pawn.health.AddHediff(HVMPDefOf.HVMP_TargetedPsychicSuppression, null, null, null);
            }
        }
        public override void GameConditionTick()
        {
            foreach (Map map in base.AffectedMaps)
            {
                List<Pawn> allPawns = map.mapPawns.AllPawns;
                for (int i = 0; i < allPawns.Count; i++)
                {
                    HVMP_GameCondition_TargetedPsychicSuppression.CheckPawn(allPawns[i], this.gender);
                }
            }
        }
        public override void RandomizeSettings(float points, Map map, List<Rule> outExtraDescriptionRules, Dictionary<string, string> outExtraDescriptionConstants)
        {
            base.RandomizeSettings(points, map, outExtraDescriptionRules, outExtraDescriptionConstants);
            if (map.mapPawns.FreeColonistsCount > 0)
            {
                this.gender = map.mapPawns.FreeColonists.RandomElement<Pawn>().gender;
            }
            else
            {
                this.gender = Rand.Element<Gender>(Gender.Male, Gender.Female);
            }
            outExtraDescriptionRules.Add(new Rule_String("psychicSuppressorGender", this.gender.GetLabel(false)));
        }
        public Gender gender;
    }
    public class HediffComp_TargetedPsychicSuppression : HediffComp
    {
        public override bool CompShouldRemove
        {
            get
            {
                if (base.Pawn.SpawnedOrAnyParentSpawned)
                {
                    HVMP_GameCondition_TargetedPsychicSuppression activeCondition = base.Pawn.MapHeld.gameConditionManager.GetActiveCondition<HVMP_GameCondition_TargetedPsychicSuppression>();
                    if (activeCondition != null && base.Pawn.gender == activeCondition.gender)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
    //rover branchquest: laelaps
    public class QuestNode_GenerateShuttle_EMMM : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        protected override void RunInt()
        {
            if (!ModLister.CheckRoyaltyOrIdeology("Shuttle"))
            {
                return;
            }
            Slate slate = QuestGen.slate;
            Thing thing = ThingMaker.MakeThing(ThingDefOf.Shuttle, null);
            if (this.owningFaction.GetValue(slate) != null)
            {
                thing.SetFaction(this.owningFaction.GetValue(slate), null);
            }
            CompShuttle compShuttle = thing.TryGetComp<CompShuttle>();
            if (this.requiredPawns.GetValue(slate) != null)
            {
                compShuttle.requiredPawns.AddRange(this.requiredPawns.GetValue(slate));
            }
            if (this.requiredItems.GetValue(slate) != null)
            {
                compShuttle.requiredItems.AddRange(this.requiredItems.GetValue(slate));
            }
            compShuttle.acceptColonists = this.acceptColonists.GetValue(slate);
            compShuttle.acceptChildren = this.acceptChildren.GetValue(slate) ?? true;
            compShuttle.onlyAcceptColonists = this.onlyAcceptColonists.GetValue(slate);
            compShuttle.onlyAcceptHealthy = this.onlyAcceptHealthy.GetValue(slate);
            compShuttle.requiredColonistCount = this.requireColonistCount.GetValue(slate);
            compShuttle.permitShuttle = this.permitShuttle.GetValue(slate);
            compShuttle.minAge = this.minAge.GetValue(slate).GetValueOrDefault();
            bool EMMO_colonistFound = false;
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.laelaps1, HVMP_Mod.settings.laelapsX))
            {
                Map map = slate.Get<Map>("map", null, false);
                if (map != null)
                {
                    Pawn colonist = map.mapPawns.FreeColonists.Where((Pawn p) =>compShuttle.IsAllowed(p)).RandomElement();
                    if (colonist != null)
                    {
                        compShuttle.requiredPawns.Add(colonist);
                        EMMO_colonistFound = true;
                        slate.Set<Thing>("EMMO_target", colonist, false);
                        QuestGen.AddQuestDescriptionRules(new List<Rule>
                        {
                            new Rule_String("mutator_EMMO_info", this.EMMO_description.Formatted())
                        });
                    }
                }
            }
            if (!EMMO_colonistFound)
            {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_EMMO_info", " ") });
            }
            if (this.overrideMass.TryGetValue(slate, out float num) && num > 0f)
            {
                compShuttle.Transporter.massCapacityOverride = num;
            }
            QuestGen.slate.Set<Thing>(this.storeAs.GetValue(slate), thing, false);
        }
        [NoTranslate]
        public SlateRef<string> storeAs;
        public SlateRef<IEnumerable<Pawn>> requiredPawns;
        public SlateRef<IEnumerable<ThingDefCount>> requiredItems;
        public SlateRef<int> requireColonistCount;
        public SlateRef<bool> acceptColonists;
        public SlateRef<bool?> acceptChildren;
        public SlateRef<bool> onlyAcceptColonists;
        public SlateRef<bool> onlyAcceptHealthy;
        public SlateRef<Faction> owningFaction;
        public SlateRef<bool> permitShuttle;
        public SlateRef<float> overrideMass;
        public SlateRef<float?> minAge;
        [MustTranslate]
        public string EMMO_description;
    }
    public class QuestNode_SSTSS : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        protected override void RunInt()
        {
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.laelaps2,HVMP_Mod.settings.laelapsX))
            {
                if (this.node != null)
                {
                    this.node.Run();
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                        {
                            new Rule_String("mutator_SSTSS_info", this.SSTSS_description.Formatted())
                        });
                    return;
                }
            } else {
                if (this.elseNode != null)
                {
                    this.elseNode.Run();
                }
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_SSTSS_info", " ") });
            }
        }
        public QuestNode node;
        public QuestNode elseNode;
        [MustTranslate]
        public string SSTSS_description;
    }
    public class QuestNode_Multiply_TLWR : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return !this.storeAs.GetValue(slate).NullOrEmpty();
        }
        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            slate.Set<int>(this.storeAs.GetValue(slate), (int)(this.value1.GetValue(slate) * (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.laelaps3, HVMP_Mod.settings.laelapsX) ? this.TLWR_factor : 1f)), false);
        }
        public SlateRef<int> value1;
        public float TLWR_factor;
        [NoTranslate]
        public SlateRef<string> storeAs;
    }
    //rover branchquest: odyssey
    public class QuestNode_Root_Odyssey : QuestNode_Sequence
    {
        protected override void RunInt()
        {
            if (HVMP_Utility.TryFindRoverFaction(out Faction roverFaction))
            {
                Slate slate = QuestGen.slate;
                Quest quest = QuestGen.quest;
                PlanetTile tile = HVMP_Utility.TryGetPlanetTile();
                Map map = HVMP_Utility.TryGetMap();
                int numSites = this.normal_siteCount.RandomInRange;
                int timeout = (numSites - 1) * this.ticksPerSite;
                int maxDist = 20;
                bool SG_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.odyssey3, HVMP_Mod.settings.odysseyX);
                int SG_minSkillLevel = this.SG_levelBase - (this.SG_levelReductionPerExtraSite*(numSites-this.normal_siteCount.min));
                SkillDef SG_sd = DefDatabase<SkillDef>.GetRandom();
                if (SG_on)
                {
                    slate.Set<int>("SG_levelDesc", SG_minSkillLevel, false);
                    slate.Set<string>("SG_nameDesc", SG_sd.label.CapitalizeFirst(), false);
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_SG_info", this.SG_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_SG_info", " ") });
                }
                bool LI_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.odyssey1, HVMP_Mod.settings.odysseyX);
                if (LI_on)
                {
                    numSites += this.LI_extraSites;
                    timeout += this.LI_extraDays;
                    maxDist = this.LI_maxDist;
                }
                bool RE_on = HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.odyssey2, HVMP_Mod.settings.odysseyX);
                int RE_ambushLimit = this.RE_maxAmbushes;
                if (RE_on)
                {
                    QuestGen.AddQuestDescriptionRules(new List<Rule>
                    {
                        new Rule_String("mutator_RE_info", this.RE_description.Formatted())
                    });
                } else {
                    QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_RE_info", " ") });
                }
                List<WorldObject> wos = new List<WorldObject>();
                for (int i = 0; i < numSites; i++)
                {
                    if (this.TryFindSiteTile(tile, out PlanetTile num, maxDist))
                    {
                        WorldObject_OdysseyPoint wo = (WorldObject_OdysseyPoint)WorldObjectMaker.MakeWorldObject(HVMPDefOf.HVMP_OdysseyPoint);
                        wo.Tile = num;
                        wo.SetFaction(roverFaction);
                        //wo.GetComponent<TimeoutComp>().StartTimeout((numSites - 1) * this.ticksPerSite);
                        quest.SpawnWorldObject(wo, null, null);
                        QuestUtility.AddQuestTag(ref wo.questTags, this.storeSitesAs.GetValue(slate));
                        wo.linkedQuest = quest;
                        if (RE_on && RE_ambushLimit > 0 && Rand.Chance(this.RE_chance))
                        {
                            wo.RE_points = (int)(this.RE_pointsFactor * StorytellerUtility.DefaultThreatPointsNow(map));
                            RE_ambushLimit--;
                        }
                        if (SG_on && SG_sd != null)
                        {
                            wo.SG_level = SG_minSkillLevel;
                            wo.SG_skill = SG_sd;
                        }
                        quest.WorldObjectTimeout(wo, timeout, null, null, false, null, true);
                        wos.Add(wo);
                    }
                }
                slate.Set<List<WorldObject>>("worldObject", wos, false);
                slate.Set<Map>("map", map, false);
                slate.Set<int>("numSites", numSites, false);
                slate.Set<int>("timeoutDays", timeout / 60000, false);
                slate.Set<int>("timeout", timeout, false);
                slate.Set<Faction>("faction", roverFaction, false);
                HVMP_Utility.SetSettingScalingRewardValue(slate);
            }
            base.RunInt();
        }
        private bool TryFindSiteTile(PlanetTile pTile, out PlanetTile tile, int maxDist)
        {
            return TileFinder.TryFindNewSiteTile(out tile, pTile, 2, maxDist, false, DefDatabase<LandmarkDef>.AllDefsListForReading, 0.5f, true, TileFinderMode.Near, true, false, null, null);
        }
        protected override bool TestRunInt(Slate slate)
        {
            HVMP_Utility.SetSettingScalingRewardValue(slate);
            Map map = HVMP_Utility.TryGetMap();
            PlanetTile pTile = HVMP_Utility.TryGetPlanetTile();
            return this.TryFindSiteTile(pTile, out PlanetTile num, 20) && HVMP_Utility.TryFindRoverFaction(out Faction roverFaction) && base.TestRunInt(slate);
        }
        [NoTranslate]
        public SlateRef<string> storeSitesAs;
        [NoTranslate]
        public SlateRef<string> inSignal;
        public int ticksPerSite;
        public IntRange normal_siteCount;
        public int normal_maxDist;
        public int LI_extraSites;
        public int LI_extraDays;
        public int LI_maxDist;
        public float RE_chance;
        public int RE_maxAmbushes;
        public float RE_pointsFactor;
        [MustTranslate]
        public string RE_description;
        public int SG_levelBase;
        public int SG_levelReductionPerExtraSite;
        [MustTranslate]
        public string SG_description;
        [NoTranslate]
        public SlateRef<string> SG_storeLevelAs;
        [NoTranslate]
        public SlateRef<string> SG_storeSkillAs;
    }
    public class WorldObject_OdysseyPoint : WorldObject
    {
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach (FloatMenuOption o in base.GetFloatMenuOptions(caravan))
            {
                yield return o;
            }
            foreach (FloatMenuOption f in CaravanArrivalAction_VisitOdysseyPoint.GetFloatMenuOptions(caravan, this))
            {
                yield return f;
            }
            yield break;
        }
        public string OutSignalCompleted
        {
            get
            {
                return string.Concat(new object[]
                {
                    "Quest",
                    this.linkedQuest.id,
                    ".allSites.Finished"
                });
            }
        }
        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            if (this.SG_skill != null)
            {
                text += "HVMP_OdysseySiteLabel_SG".Translate(this.SG_level,this.SG_skill.label.CapitalizeFirst());
            }
            return text;
        }
        public void Notify_CaravanArrived(Caravan caravan)
        {
            if (this.SG_skill != null)
            {
                bool skillLevelMet = false;
                foreach (Pawn p in caravan.pawns)
                {
                    if (p.skills != null)
                    {
                        SkillRecord sr = p.skills.GetSkill(this.SG_skill);
                        if (sr != null && !sr.TotallyDisabled && sr.Level >= this.SG_level)
                        {
                            skillLevelMet = true;
                            break;
                        }
                    }
                }
                if (!skillLevelMet)
                {
                    return;
                }
            }
            this.Destroy();
            bool endQuest = true;
            if (this.RE_points > 0)
            {
                this.Ambush(caravan);
            }
            foreach (WorldObject wo in Find.WorldObjects.AllWorldObjects)
            {
                if (wo != this && wo is WorldObject_OdysseyPoint woop && woop.linkedQuest == this.linkedQuest)
                {
                    endQuest = false;
                    break;
                }
            }
            foreach (QuestPart qp in this.linkedQuest.PartsListForReading)
            {
                if (qp is QuestPart_DestroySite qpds && qpds.worldObjects.Contains(this))
                {
                    qpds.worldObjects.Remove(this);
                }
            }
            if (endQuest)
            {
                SignalArgs signalargs = default(SignalArgs);
                Find.SignalManager.SendSignal(new Signal(this.OutSignalCompleted, signalargs, false));
            }
        }
        public void Ambush(Caravan target)
        {
            HVMP_Utility.DoAmbush(target, this.RE_points);
            this.RE_points = -1;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Quest>(ref this.linkedQuest, "linkedQuest", false);
            Scribe_Values.Look<int>(ref this.RE_points, "RE_points", -1, false);
            Scribe_Values.Look<int>(ref this.SG_level, "SG_level", 10, false);
            Scribe_Defs.Look<SkillDef>(ref this.SG_skill, "SG_skill");
        }
        public Quest linkedQuest;
        public int RE_points;
        public int SG_level;
        public SkillDef SG_skill;
    }
    public class CaravanArrivalAction_VisitOdysseyPoint : CaravanArrivalAction
    {
        public override string Label
        {
            get
            {
                return "VisitPeaceTalks".Translate(this.odysseyPoint.Label);
            }
        }
        public override string ReportString
        {
            get
            {
                return "CaravanVisiting".Translate(this.odysseyPoint.Label);
            }
        }
        public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, WorldObject_OdysseyPoint odysseyPoint)
        {
            return odysseyPoint != null && odysseyPoint.Spawned;
        }
        public CaravanArrivalAction_VisitOdysseyPoint()
        {
        }
        public CaravanArrivalAction_VisitOdysseyPoint(WorldObject_OdysseyPoint odysseyPoint)
        {
            this.odysseyPoint = odysseyPoint;
        }
        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(caravan, destinationTile);
            if (floatMenuAcceptanceReport)
            {
                if (this.odysseyPoint != null && this.odysseyPoint.Tile != destinationTile)
                {
                    floatMenuAcceptanceReport = false;
                }
                else
                {
                    floatMenuAcceptanceReport = CaravanArrivalAction_VisitOdysseyPoint.CanVisit(caravan, this.odysseyPoint);
                }
            }
            return floatMenuAcceptanceReport;
        }
        public override void Arrived(Caravan caravan)
        {
            this.odysseyPoint.Notify_CaravanArrived(caravan);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<WorldObject_OdysseyPoint>(ref this.odysseyPoint, "odysseyPoint", false);
        }
        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, WorldObject_OdysseyPoint odysseyPoint)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions<CaravanArrivalAction_VisitOdysseyPoint>(() => CaravanArrivalAction_VisitOdysseyPoint.CanVisit(caravan, odysseyPoint), () => new CaravanArrivalAction_VisitOdysseyPoint(odysseyPoint), "VisitPeaceTalks".Translate(odysseyPoint.Label), caravan, odysseyPoint.Tile, odysseyPoint, null);
        }
        private WorldObject_OdysseyPoint odysseyPoint;
    }
    //rover branchquest: theseus
    public class QuestNode_Theseus : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            HVMP_Utility.SetSettingScalingRewardValue(slate);
            QuestGenUtility.TestRunAdjustPointsForDistantFight(slate);
            this.ResolveParameters(slate, out int num, out int num2, out Map map);
            return num != -1 && HVMP_Utility.TryFindRoverFaction(out Faction roverFaction) && this.TryGetSiteFaction(out Faction faction);
        }
        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            string text = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("BanditCamp");
            QuestGenUtility.RunAdjustPointsForDistantFight();
            int num = slate.Get<int>("points", 0, false);
            if (num <= 0)
            {
                num = Rand.RangeInclusive(200, 2000);
            }
            this.ResolveParameters(slate, out int num2, out int num3, out Map map);
            this.TryFindSiteTile(out PlanetTile num4, false);
            HVMP_Utility.TryFindRoverFaction(out Faction roverFaction);
            slate.Set<Faction>("askerFaction", roverFaction, false);
            slate.Set<int>("requiredPawnCount", num2, false);
            slate.Set<Map>("map", map, false);
            QuestPart_BranchGoodwillFailureHandler qpbgfh = new QuestPart_BranchGoodwillFailureHandler();
            qpbgfh.faction = roverFaction;
            QuestGen.quest.AddPart(qpbgfh);
            RimWorld.Planet.Site site = this.GenerateSite(roverFaction.leader, (float)num, num2, num3, num4);
            string text2 = QuestGenUtility.HardcodedSignalWithQuestID("askerFaction.BecameHostileToPlayer");
            string text3 = QuestGenUtility.QuestTagSignal(text, "AllEnemiesDefeated");
            string signalSentSatisfied = QuestGenUtility.HardcodedSignalWithQuestID("shuttle.SentSatisfied");
            string text4 = QuestGenUtility.QuestTagSignal(text, "MapRemoved");
            string signalChosenPawn = QuestGen.GenerateNewSignal("ChosenPawnSignal", true);
            this.parms.giverFaction = roverFaction;
            this.parms.allowGoodwill = true;
            this.parms.allowRoyalFavor = true;
            this.parms.thingRewardDisallowed = true;
            slate.Set<Thing>("asker", roverFaction.leader, false);
            HVMP_Utility.SetSettingScalingRewardValue(slate);
            QuestGen.slate.Set<Faction>("faction", roverFaction, false);
            quest.GiveRewards(new RewardsGeneratorParams
            {
                allowGoodwill = true,
                allowRoyalFavor = true,
                giverFaction = roverFaction,
                thingRewardDisallowed = true,
                rewardValue = slate.Get<float>("rewardValue", 200f, false),
                chosenPawnSignal = signalChosenPawn
            }, text3, null, null, null, null, null, delegate
            {
                Quest quest2 = quest;
                LetterDef choosePawn = LetterDefOf.ChoosePawn;
                string text8 = null;
                string royalFavorLabel = roverFaction.def.royalFavorLabel;
                string text9 = "LetterTextHonorAward_BanditCamp".Translate(roverFaction.def.royalFavorLabel);
                quest2.Letter(choosePawn, text8, signalChosenPawn, null, null, false, QuestPart.SignalListenMode.OngoingOnly, null, false, text9, null, royalFavorLabel, null, signalSentSatisfied);
            }, null, true, roverFaction.leader, false, false, null);
            Thing shuttle = QuestGen_Shuttle.GenerateShuttle(null, null, null, true, true, false, num2, true, true, false, true, site, map.Parent, num2, null, false, false, false, false, true);
            slate.Set<Thing>("shuttle", shuttle, false);
            QuestUtility.AddQuestTag(ref shuttle.questTags, text);
            quest.SpawnWorldObject(site, null, null);
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.theseus2, HVMP_Mod.settings.theseusX))
            {
                Mutator_GG_SD component = site.GetComponent<Mutator_GG_SD>();
                if (component != null)
                {
                    component.GG_on = true;
                }
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_GG_info", this.GG_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_GG_info", " ") });
            }
            TransportShip transportShip = quest.GenerateTransportShip(TransportShipDefOf.Ship_Shuttle, null, shuttle, null).transportShip;
            slate.Set<TransportShip>("transportShip", transportShip, false);
            QuestUtility.AddQuestTag(ref transportShip.questTags, text);
            quest.SendTransportShipAwayOnCleanup(transportShip, true, TransportShipDropMode.None);
            quest.AddShipJob_Arrive(transportShip, map.Parent, null, null, ShipJobStartMode.Instant, Faction.OfEmpire, null);
            quest.AddShipJob_WaitSendable(transportShip, site, true, false, null);
            quest.AddShipJob(transportShip, ShipJobDefOf.Unload, ShipJobStartMode.Queue, null);
            quest.AddShipJob_WaitSendable(transportShip, map.Parent, true, false, null);
            quest.AddShipJob(transportShip, ShipJobDefOf.Unload, ShipJobStartMode.Queue, null);
            quest.AddShipJob_FlyAway(transportShip, -1, null, TransportShipDropMode.None, null);
            quest.TendPawns(null, shuttle, signalSentSatisfied);
            quest.RequiredShuttleThings(shuttle, site, QuestGenUtility.HardcodedSignalWithQuestID("transportShip.FlewAway"), true, -1);
            quest.ShuttleLeaveDelay(shuttle, 60000, null, Gen.YieldSingle<string>(signalSentSatisfied), null, delegate
            {
                quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, true, false);
            });
            string text5 = QuestGenUtility.HardcodedSignalWithQuestID("shuttle.Killed");
            quest.FactionGoodwillChange(roverFaction, HVMP_Utility.ExpectationBasedGoodwillLoss(map, true, false, roverFaction), text5, true, true, true, HistoryEventDefOf.ShuttleDestroyed, QuestPart.SignalListenMode.OngoingOnly, true);
            quest.End(QuestEndOutcome.Fail, 0, null, text5, QuestPart.SignalListenMode.OngoingOnly, true, false);
            quest.SignalPass(delegate
            {
                quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, true, false);
            }, text2, null);
            quest.FeedPawns(null, shuttle, signalSentSatisfied);
            QuestUtility.AddQuestTag(ref site.questTags, text);
            slate.Set<RimWorld.Planet.Site>("site", site, false);
            quest.SignalPassActivable(delegate
            {
                quest.Message("MessageMissionGetBackToShuttle".Translate(site.Faction.Named("FACTION")), MessageTypeDefOf.PositiveEvent, false, null, new LookTargets(shuttle), null);
                quest.Notify_PlayerRaidedSomeone(null, site, null);
            }, signalSentSatisfied, text3, null, null, null, false);
            quest.SignalPassAllSequence(delegate
            {
                quest.End(QuestEndOutcome.Success, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, true, false);
            }, new List<string> { signalSentSatisfied, text3, text4 }, null);
            Quest quest3 = quest;
            Action action = delegate
            {
                quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, true, false);
            };
            string text6 = null;
            string text7 = text3;
            quest3.SignalPassActivable(action, text6, text4, null, null, text7, false);
            int num5 = (int)(this.timeLimitDays.RandomInRange * 60000f);
            slate.Set<int>("timeoutTicks", num5, false);
            if (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.theseus3,HVMP_Mod.settings.theseusX))
            {
                Mutator_GG_SD component = site.GetComponent<Mutator_GG_SD>();
                if (component != null)
                {
                    component.SD_on = true;
                }
                QuestGen.AddQuestDescriptionRules(new List<Rule>
                {
                    new Rule_String("mutator_SD_info", this.SD_description.Formatted())
                });
            } else {
                QuestGen.AddQuestDescriptionRules(new List<Rule> { new Rule_String("mutator_SD_info", " ") });
            }
            quest.WorldObjectTimeout(site, num5, null, null, false, null, true);
            List<Rule> list = new List<Rule>();
            list.AddRange(GrammarUtility.RulesForWorldObject("site", site, true));
            QuestGen.AddQuestDescriptionRules(list);
        }
        protected bool TryFindSiteTile(out PlanetTile tile, bool exitOnFirstTileFound = false)
        {
            return TileFinder.TryFindNewSiteTile(out tile, 80, 85, false, null, 0.5f, true, TileFinderMode.Near, exitOnFirstTileFound, this.canBeSpace, null, null);
        }
        private void ResolveParameters(Slate slate, out int requiredPawnCount, out int population, out Map colonyMap)
        {
            try
            {
                foreach (Map map in Find.Maps)
                {
                    if (map.IsPlayerHome)
                    {
                        QuestNode_Theseus.tmpMaps.Add(map);
                    }
                }
                colonyMap = QuestNode_Theseus.tmpMaps.RandomElementWithFallback(null);
                if (colonyMap == null)
                {
                    population = -1;
                    requiredPawnCount = -1;
                }
                else
                {
                    population = (slate.Exists("population", false) ? slate.Get<int>("population", 0, false) : colonyMap.mapPawns.FreeColonists.Count);
                    requiredPawnCount = Math.Max(this.GetRequiredPawnCount(population, (float)slate.Get<int>("points", 0, false)), 1);
                }
            }
            finally
            {
                QuestNode_Theseus.tmpMaps.Clear();
            }
        }
        protected int GetRequiredPawnCount(int population, float threatPoints)
        {
            if (population == 0)
            {
                return -1;
            }
            int num = -1;
            for (int i = 1; i <= population; i++)
            {
                if (this.GetSiteThreatPoints(threatPoints, population, i) >= 200f)
                {
                    num = i;
                    break;
                }
            }
            if (num == -1)
            {
                return -1;
            }
            return Math.Max(0, Rand.Range(num, population));
        }
        private float GetSiteThreatPoints(float threatPoints, int population, int pawnCount)
        {
            return threatPoints * (HVMP_Utility.MutatorEnabled(HVMP_Mod.settings.theseus1, HVMP_Mod.settings.theseusX) ?this.AQAIO_pointFactor.RandomInRange : 1f);
        }
        protected RimWorld.Planet.Site GenerateSite(Pawn asker, float threatPoints, int pawnCount, int population, int tile)
        {
            this.TryGetSiteFaction(out Faction faction);
            RimWorld.Planet.Site site = QuestGen_Sites.GenerateSite(new SitePartDefWithParams[]
            {
                new SitePartDefWithParams(SitePartDefOf.BanditCamp, new SitePartParams
                {
                    threatPoints = Math.Max(this.GetSiteThreatPoints(Math.Max(threatPoints,200), population, pawnCount),500)
                })
            }, tile, faction, false, null);
            site.factionMustRemainHostile = true;
            site.desiredThreatPoints = site.ActualThreatPoints;
            return site;
        }
        private bool TryGetSiteFaction(out Faction faction)
        {
            faction = HVMP_Utility.GetAnEnemyFaction();
            return faction != null;
        }
        private static readonly SimpleCurve PawnCountToSitePointsFactorCurve = new SimpleCurve
        {
            {
                new CurvePoint(1f, 0.33f),
                true
            },
            {
                new CurvePoint(3f, 0.5f),
                true
            },
            {
                new CurvePoint(5f, 0.75f),
                true
            },
            {
                new CurvePoint(10f, 1f),
                true
            }
        };
        [NoTranslate]
        public SlateRef<string> inSignal;
        private static List<Map> tmpMaps = new List<Map>();
        public SlateRef<string> customLetterLabel;
        public SlateRef<string> customLetterText;
        public QuestNode nodeIfChosenPawnSignalUsed;
        public RewardsGeneratorParams parms;
        public SlateRef<int?> variants;
        public bool canBeSpace;
        public FloatRange timeLimitDays = new FloatRange(2f, 5f);
        public FloatRange AQAIO_pointFactor = new FloatRange(2f,3f);
        [MustTranslate]
        public string GG_description;
        [MustTranslate]
        public string SD_description;
    }
    public class WorldObjectCompProperties_Mutator_GG_SD : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_Mutator_GG_SD()
        {
            this.compClass = typeof(Mutator_GG_SD);
        }
        public int GG_maxBanditsBuffed;
        public List<HediffDef> GG_buffs;
        public List<ThingDef> SD_turrets;
        public float SD_turretsPerPawn;
    }
    public class Mutator_GG_SD : WorldObjectComp
    {
        public override void Initialize(WorldObjectCompProperties props)
        {
            base.Initialize(props);
            WorldObjectCompProperties_Mutator_GG_SD proper = props as WorldObjectCompProperties_Mutator_GG_SD;
            if (proper != null)
            {
                this.GG_buffs = proper.GG_buffs;
                this.GG_maxBanditsBuffed = proper.GG_maxBanditsBuffed;
                this.SD_turretsPerPawn = proper.SD_turretsPerPawn;
                this.SD_turrets = proper.SD_turrets;
            }
        }
        public override void PostMapGenerate()
        {
            if (this.parent is MapParent mp)
            {
                Map m = mp.Map;
                if (this.GG_on && !this.GG_buffs.NullOrEmpty())
                {
                    int GG_count = this.GG_maxBanditsBuffed;
                    List<Pawn> pawns = m.mapPawns.PawnsInFaction(this.parent.Faction).InRandomOrder().ToList();
                    foreach (Pawn p in pawns)
                    {
                        if (p.Spawned)
                        {
                            GG_count--;
                            if (GG_count >= 0)
                            {
                                if (p.RaceProps.Humanlike)
                                {
                                    p.health.AddHediff(this.GG_buffs.RandomElement());
                                }
                            }
                        }
                    }
                }
                if (this.SD_on && !this.SD_turrets.NullOrEmpty())
                {
                    List<Pawn> pawns = m.mapPawns.PawnsInFaction(this.parent.Faction).Where((Pawn paw)=>paw.RaceProps.Humanlike).InRandomOrder().ToList();
                    int SD_count = Math.Max(1,(int)(pawns.Count*this.SD_turretsPerPawn));
                    foreach (Pawn p in pawns)
                    {
                        if (p.Spawned)
                        {
                            if (SD_count > 0)
                            {
                                Thing t = ThingMaker.MakeThing(this.SD_turrets.RandomElement());
                                GenPlace.TryPlaceThing(t, p.Position, m, ThingPlaceMode.Near, null, null, null, 1);
                                t.SetFactionDirect(p.Faction);
                                SD_count--;
                            }
                        }
                    }
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.GG_on, "GG_on", false, false);
            Scribe_Values.Look<int>(ref this.GG_maxBanditsBuffed, "GG_maxBanditsBuffed", 10, false);
            Scribe_Collections.Look<HediffDef>(ref this.GG_buffs, "GG_buffs", LookMode.Undefined, LookMode.Undefined);
            Scribe_Values.Look<bool>(ref this.SD_on, "SD_on", false, false);
            Scribe_Values.Look<float>(ref this.SD_turretsPerPawn, "SD_turretsPerPawn", 0.5f, false);
            Scribe_Collections.Look<ThingDef>(ref this.SD_turrets, "SD_turrets", LookMode.Undefined, LookMode.Undefined);
        }
        public bool GG_on;
        public int GG_maxBanditsBuffed;
        public List<HediffDef> GG_buffs;
        public bool SD_on;
        public List<ThingDef> SD_turrets;
        public float SD_turretsPerPawn;
    }
    //technically for Biotech's environmental control quest
    public class WorldObjectCompProperties_Mutator_DNLU : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_Mutator_DNLU()
        {
            this.compClass = typeof(Mutator_DNLU);
        }
        public float DNLU_pointFactor;
    }
    public class Mutator_DNLU : WorldObjectComp
    {
        public override void Initialize(WorldObjectCompProperties props)
        {
            base.Initialize(props);
            WorldObjectCompProperties_Mutator_DNLU proper = props as WorldObjectCompProperties_Mutator_DNLU;
            if (proper != null)
            {
                this.DNLU_pointFactor = proper.DNLU_pointFactor;
            }
        }
        public override void PostMapGenerate()
        {
            if (this.DNLU_on && this.parent is Site site)
            {
                Thing problemCauser = null;
                foreach (SitePart sp in site.parts)
                {
                    if (sp.conditionCauser != null && sp.conditionCauser.SpawnedOrAnyParentSpawned)
                    {
                        problemCauser = sp.conditionCauser;
                        break;
                    }
                }
                if (problemCauser != null)
                {
                    Faction faction = site.Faction ?? Faction.OfMechanoids;
                    PawnGroupMaker pgm = faction.def.pawnGroupMakers.Where((PawnGroupMaker pgm0) => pgm0.kindDef == PawnGroupKindDefOf.Settlement || pgm0.kindDef == PawnGroupKindDefOf.Combat).RandomElement();
                    if (pgm != null)
                    {
                        PawnGroupMakerParms pawnGroupMakerParms = new PawnGroupMakerParms
                        {
                            groupKind = pgm.kindDef,
                            tile = site.Tile,
                            faction = faction,
                            inhabitants = true,
                            generateFightersOnly = true,
                            seed = new int?(Rand.Int),
                            points = Math.Max(StorytellerUtility.DefaultThreatPointsNow(site.Map) * this.DNLU_pointFactor, faction.def.MinPointsToGeneratePawnGroup(pgm.kindDef, null))
                        };
                        IntVec3 iv3 = problemCauser.PositionHeld;
                        Map map = problemCauser.MapHeld;
                        List<Pawn> pawns = new List<Pawn>();
                        foreach (Pawn pawn in PawnGroupMakerUtility.GeneratePawns(pawnGroupMakerParms, true))
                        {
                            GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(iv3, map, 10, null), map);
                            pawns.Add(pawn);
                        }
                        if (pawns.Count > 0)
                        {
                            LordMaker.MakeNewLord(faction, new LordJob_DefendPoint(iv3), map, pawns);
                        }
                    }
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.DNLU_on, "DNLU_on", false, false);
            Scribe_Values.Look<float>(ref this.DNLU_pointFactor, "DNLU_pointFactor", 0.5f, false);
        }
        public bool DNLU_on;
        public float DNLU_pointFactor;
    }
    //utility
    public static class HVMP_Utility
    {
        public static bool TryFindCommerceFaction(out Faction commerceFaction)
        {
            commerceFaction = Find.FactionManager.FirstFactionOfDef(HVMPDefOf.HVMP_CommerceBranch);
            return commerceFaction != null;
        }
        public static bool TryFindPaxFaction(out Faction paxFaction)
        {
            paxFaction = Find.FactionManager.FirstFactionOfDef(HVMPDefOf.HVMP_PaxBranch);
            return paxFaction != null;
        }
        public static bool TryFindRoverFaction(out Faction roverFaction)
        {
            roverFaction = Find.FactionManager.FirstFactionOfDef(HVMPDefOf.HVMP_RoverBranch);
            return roverFaction != null;
        }
        public static bool TryFindArchiveFaction(out Faction archiveFaction)
        {
            archiveFaction = null;
            if (ModsConfig.IdeologyActive)
            {
                archiveFaction = Find.FactionManager.FirstFactionOfDef(HVMPDefOf.HVMP_ArchiveBranch);
            }
            return archiveFaction != null;
        }
        public static bool TryFindEcosphereFaction(out Faction ecosphereBranch)
        {
            ecosphereBranch = null;
            if (ModsConfig.BiotechActive)
            {
                ecosphereBranch = Find.FactionManager.FirstFactionOfDef(HVMPDefOf.HVMP_EcosphereBranch);
            }
            return ecosphereBranch != null;
        }
        public static bool TryFindOccultFaction(out Faction occultBranch)
        {
            occultBranch = null;
            if (ModsConfig.AnomalyActive)
            {
                occultBranch = Find.FactionManager.FirstFactionOfDef(HVMPDefOf.HVMP_OccultBranch);
            }
            return occultBranch != null;
        }
        public static Map TryGetMap()
        {
            List<Map> mapCandidates = new List<Map>();
            foreach (Map map in Find.Maps)
            {
                if (map.IsPlayerHome && !map.generatorDef.isUnderground && map.Tile.Layer.IsRootSurface)
                {
                    mapCandidates.Add(map);
                }
            }
            if (mapCandidates.Count > 0)
            {
                return mapCandidates.RandomElementWithFallback(null);
            }
            mapCandidates.Clear();
            foreach (Map map in Find.Maps)
            {
                if (map.IsPlayerHome && !map.generatorDef.isUnderground)
                {
                    mapCandidates.Add(map);
                }
            }
            if (mapCandidates.Count > 0)
            {
                return mapCandidates.RandomElementWithFallback(null);
            }
            mapCandidates.Clear();
            foreach (Map map in Find.Maps)
            {
                if (!map.generatorDef.isUnderground && map.mapPawns.FreeColonists.Count > 0)
                {
                    mapCandidates.Add(map);
                }
            }
            if (mapCandidates.Count > 0)
            {
                return mapCandidates.RandomElementWithFallback(null);
            }
            return null;
        }
        public static PlanetTile TryGetPlanetTile()
        {
            Map m = HVMP_Utility.TryGetMap();
            if (m != null)
            {
                return m.Tile;
            }
            return TileFinder.RandomStartingTile();
        }
        public static float TryGetPoints(Pawn caller)
        {
            if (caller != null && caller.Map != null && caller.Map.IsPlayerHome)
            {
                return StorytellerUtility.DefaultThreatPointsNow(caller.Map);
            } else if (Find.AnyPlayerHomeMap != null) {
                return StorytellerUtility.DefaultThreatPointsNow(Find.RandomPlayerHomeMap);
            }
            return 1000f;
        }
        public static int ExpectationBasedGoodwillLoss(Map map, bool loss, bool refusalNotFailure, Faction faction)
        {
            int value = 0;
            if (loss)
            {
                bool lossFromExpectation = false;
                if (HVMP_Mod.settings.bratBehaviorMinExpectationLvl < 999)
                {
                    int highestExpectationOrder = -1;
                    foreach (Map m in Find.Maps)
                    {
                        if (m.IsPlayerHome)
                        {
                            ExpectationDef ed = ExpectationsUtility.CurrentExpectationFor(m);
                            highestExpectationOrder = Math.Max(highestExpectationOrder,ed.order);
                        }
                    }
                    if (highestExpectationOrder < 0)
                    {
                        if (map == null)
                        {
                            map = Find.CurrentMap;
                        }
                        if (map != null)
                        {
                            ExpectationDef ed = ExpectationsUtility.CurrentExpectationFor(map);
                            highestExpectationOrder = Math.Max(highestExpectationOrder, ed.order);
                        }
                    }
                    lossFromExpectation = highestExpectationOrder >= HVMP_Mod.settings.bratBehaviorMinExpectationLvl;
                    if (HVMP_Mod.settings.bratBehaviorMinSeniorityLvl >= 9999)
                    {
                        value = lossFromExpectation ? (refusalNotFailure ? HVMP_Mod.settings.goodwillQuestRefusalLoss : HVMP_Mod.settings.goodwillQuestFailureLoss) : 0;
                        return -value;
                    }
                }
                bool lossFromSeniority = false;
                if (HVMP_Mod.settings.bratBehaviorMinSeniorityLvl < 9999)
                {
                    List<Pawn> colonists = new List<Pawn>();
                    foreach (Map m in Find.Maps)
                    {
                        colonists.AddRange(m.mapPawns.AllPawns.Where((Pawn p)=>!p.Dead && p.IsColonist));
                    }
                    foreach (Caravan c in Find.WorldObjects.Caravans)
                    {
                        colonists.AddRange(c.PawnsListForReading.Where((Pawn p)=> p.IsColonist));
                    }
                    foreach (Pawn col in colonists)
                    {
                        if (col.royalty != null)
                        {
                            RoyalTitle rt = col.royalty.GetCurrentTitleInFaction(faction);
                            if (rt != null && rt.def.seniority >= HVMP_Mod.settings.bratBehaviorMinSeniorityLvl)
                            {
                                lossFromSeniority = true;
                                break;
                            }
                        }
                    }
                    if (HVMP_Mod.settings.bratBehaviorMinExpectationLvl >= 999)
                    {
                        value = lossFromSeniority ? (refusalNotFailure ? HVMP_Mod.settings.goodwillQuestRefusalLoss : HVMP_Mod.settings.goodwillQuestFailureLoss) : 0;
                        return -value;
                    }
                }
                if (lossFromSeniority && lossFromExpectation)
                {
                    value = refusalNotFailure ? HVMP_Mod.settings.goodwillQuestRefusalLoss : HVMP_Mod.settings.goodwillQuestFailureLoss;
                }
            }
            return -value;
        }
        public static bool NegotiatorIsCosmopolitan(Pawn negotiator)
        {
            return ModsConfig.IdeologyActive && negotiator.ideo != null && negotiator.Ideo.HasPrecept(HVMPDefOf.HVMP_InterfactionAidImproved);
        }
        public static bool FactionIsCosmopolitan(Faction faction)
        {
            return ModsConfig.IdeologyActive && faction.ideos != null && faction.ideos.PrimaryIdeo != null && faction.ideos.PrimaryIdeo.HasPrecept(HVMPDefOf.HVMP_InterfactionAidImproved);
        }
        public static void SetSettingScalingRewardValue(Slate slate, float factor = 1f)
        {
            slate.Set<float>("rewardValue", factor * Rand.RangeInclusive(350, 700) * HVMP_Mod.settings.questRewardFactor);
        }
        public static bool CanReadAtlas(Book book, Pawn reader, List<SkillDef> skillDefs, out string reason)
        {
            reason = "";
            CompStudiableQuestItem cda = book.GetComp<CompStudiableQuestItem>();
            if (cda != null)
            {
                if (!cda.pawns.Contains(reader))
                {
                    reason = "HVMP_NeverBeenToAtlasPoint".Translate();
                    return false;
                }
                if (reader.skills != null)
                {
                    foreach (SkillDef sd in skillDefs)
                    {
                        if (!reader.skills.GetSkill(sd).TotallyDisabled)
                        {
                            return true;
                        }
                    }
                }
                reason = "HVMP_WrongSkillsToStudy".Translate();
                return false;
            }
            return true;
        }
        public static void PaxMundiInner(int goodwillChange)
        {
            WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
            if (wcbs != null)
            {
                QuestPart_PaxMundi qppm = null;
                for (int i = wcbs.qppms.Count - 1; i >= 0; i--)
                {
                    if (wcbs.qppms[i].quest.Historical || wcbs.qppms[i].denominator <= wcbs.qppms[i].goodwillChangesInt)
                    {
                        wcbs.qppms.Remove(wcbs.qppms[i]);
                    } else if (wcbs.qppms[i].quest.State == QuestState.Ongoing) {
                        if (qppm == null || qppm.quest.TicksSinceAccepted <= wcbs.qppms[i].quest.TicksSinceAccepted)
                        {
                            qppm = wcbs.qppms[i];
                        }
                    }
                }
                if (qppm != null)
                {
                    int functionalChange = Math.Min(goodwillChange, qppm.denominator);
                    int leftovers = goodwillChange - functionalChange;
                    if (qppm.TWOS_on && functionalChange < 0f)
                    {
                        functionalChange *= 2;
                    }
                    qppm.goodwillChangesInt += functionalChange;
                    if (leftovers >= 0)
                    {
                        bool goodwillMaxedOut = true;
                        foreach (Faction f in Find.FactionManager.AllFactions)
                        {
                            if (f != Faction.OfPlayerSilentFail && !f.def.HasModExtension<EBranchQuests>() && !f.def.PermanentlyHostileTo(Faction.OfPlayerSilentFail.def) && f.HasGoodwill && f.GoodwillWith(Faction.OfPlayerSilentFail) < 100)
                            {
                                goodwillMaxedOut = false;
                                break;
                            }
                        }
                        if (goodwillMaxedOut)
                        {
                            qppm.goodwillChangesInt = qppm.denominator;
                            return;
                        }
                        if (leftovers > 0)
                        {
                            HVMP_Utility.PaxMundiInner(leftovers);
                        }
                    }
                }
            }
        }
        public static void ThrowBribeGlow(Vector3 loc, Map map, float size)
        {
            if (!loc.ShouldSpawnMotesAt(map, true))
            {
                return;
            }
            FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc + new Vector3(0.5f, 0f, 0.5f), map, HVMPDefOf.HVMP_BribeGlow, size);
            dataStatic.rotationRate = Rand.Range(-3f, 3f);
            dataStatic.velocityAngle = 330f + Rand.Range(0f, 50f);
            dataStatic.velocitySpeed = 0.6f;
            map.flecks.CreateFleck(dataStatic);
        }
        public static void ThrowScalpelScope(Thing target, Map map, float size)
        {
            if (target.SpawnedOrAnyParentSpawned)
            {
                FleckMaker.AttachedOverlay(target, HVMPDefOf.HVMP_ScalpelBLAST, Vector3.zero, size, -1f);
                return;
            }
        }
        public static void ThrowRepairGlow(Vector3 loc, Map map, float size)
        {
            if (!loc.ShouldSpawnMotesAt(map, true))
            {
                return;
            }
            FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc + new Vector3(0.5f,0f,0.5f), map, HVMPDefOf.HVMP_RepairGlow, size);
            dataStatic.rotationRate = Rand.Range(-3f, 3f);
            dataStatic.velocityAngle = 340f + Rand.Range(0f,40f);
            dataStatic.velocitySpeed = 0.6f;
            map.flecks.CreateFleck(dataStatic);
        }
        public static void ThrowDecryptionGlow(Vector3 loc, Map map, float size)
        {
            if (!loc.ShouldSpawnMotesAt(map, true))
            {
                return;
            }
            FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc + new Vector3(0.5f, 0f, 0.5f), map, HVMPDefOf.HVMP_DecryptGlow, size);
            dataStatic.rotationRate = Rand.Range(-3f, 3f);
            dataStatic.velocityAngle = 340f + Rand.Range(0f, 40f);
            dataStatic.velocitySpeed = 0.6f;
            map.flecks.CreateFleck(dataStatic);
        }
        public static void ThrowQualityUpgradeGlow(Vector3 loc, Map map, float size)
        {
            if (!loc.ShouldSpawnMotesAt(map, true))
            {
                return;
            }
            FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc + new Vector3(0.5f, 0f, 0.5f), map, HVMPDefOf.HVMP_QualityGlow, size);
            dataStatic.rotationRate = Rand.Range(-3f, 3f);
            dataStatic.velocityAngle = 340f + Rand.Range(0f, 40f);
            dataStatic.velocitySpeed = 0.6f;
            map.flecks.CreateFleck(dataStatic);
        }
        public static void ThrowQualityDestroyGlow(Vector3 loc, Map map, float size)
        {
            if (!loc.ShouldSpawnMotesAt(map, true))
            {
                return;
            }
            FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc + new Vector3(0.5f, 0f, 0.5f), map, HVMPDefOf.HVMP_VaalOrNoBaals, size);
            dataStatic.rotationRate = Rand.Range(-3f, 3f);
            dataStatic.velocityAngle = 340f + Rand.Range(0f, 40f);
            dataStatic.velocitySpeed = 0.6f;
            map.flecks.CreateFleck(dataStatic);
        }
        public static void OpenPallet(Pawn pawn, Thing pallet, ThingDefCountClass chosenTDCC)
        {
            Thing thing = ThingMaker.MakeThing(chosenTDCC.thingDef, chosenTDCC.stuff);
            thing.stackCount = chosenTDCC.count;
            if (thing.TryGetComp(out CompQuality compQuality))
            {
                compQuality.SetQuality(chosenTDCC.quality, new ArtGenerationContext?(ArtGenerationContext.Outsider));
            }
            if (chosenTDCC.color != null && thing.TryGetComp(out CompColorable compColorable))
            {
                compColorable.SetColor(chosenTDCC.color.Value);
            }
            if (thing is Building b)
            {
                b.MakeMinified();
            }
            GenDrop.TryDropSpawn(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near, out Thing theThing, null, null, true);
            pallet.SplitOff(1).Destroy();
        }
        public static Faction AssignFallbackFactionToPermitTargeter()
        {
            List<Faction> factions = new List<Faction>();
            foreach (Faction f in Find.FactionManager.AllFactionsVisible)
            {
                if (f.def.HasModExtension<EBranchQuests>())
                {
                    factions.Add(f);
                }
            }
            if (factions.Count > 0)
            {
                return factions.RandomElement();
            }
            factions.Clear();
            foreach (Faction f in Find.FactionManager.AllFactionsVisible)
            {
                if (f.def.HasRoyalTitles)
                {
                    factions.Add(f);
                }
            }
            if (factions.Count > 0)
            {
                return factions.RandomElement();
            }
            return null;
        }
        public static Hediff_PTargeter GetPawnPTargeter(Pawn pawn, Faction faction)
        {
            foreach (Hediff h in pawn.health.hediffSet.hediffs)
            {
                if (h is Hediff_PTargeter hpt && hpt.faction == faction && hpt.Severity >= 1f)
                {
                    return hpt;
                }
            }
            return null;
        }
        public static void DoPTargeterCooldown(Faction faction, Pawn caller, RoyalTitlePermitWorker rptw)
        {
            if (faction.HostileTo(Faction.OfPlayer))
            {
                Hediff_PTargeter hpt = HVMP_Utility.GetPawnPTargeter(caller, faction);
                if (hpt != null)
                {
                    hpt.Severity = 0.001f;
                    hpt.cooldownTicks = (int)(HVMP_Mod.settings.authorizerCooldownDays * 60000 * rptw.def.royalAid.favorCost);
                }
            }
        }
        public static bool ProprietaryFillAidOption(RoyalTitlePermitWorker rptw, Pawn pawn, Faction faction, ref string description, out bool free)
        {
            if (faction.HostileTo(Faction.OfPlayer))
            {
                Hediff_PTargeter hpt = HVMP_Utility.GetPawnPTargeter(pawn, faction);
                if (hpt != null)
                {
                    description += "CommandCallRoyalAidFreeOption".Translate();
                    free = true;
                    return true;
                }
            }
            int lastUsedTick = pawn.royalty.GetPermit(rptw.def, faction).LastUsedTick;
            int num = Math.Max(GenTicks.TicksGame - lastUsedTick, 0);
            if (lastUsedTick < 0 || num >= rptw.def.CooldownTicks)
            {
                description += "CommandCallRoyalAidFreeOption".Translate();
                free = true;
                return true;
            }
            int num2 = (lastUsedTick > 0) ? Math.Max(rptw.def.CooldownTicks - num, 0) : 0;
            description += "CommandCallRoyalAidFavorOption".Translate(num2.TicksToDays().ToString("0.0"), rptw.def.royalAid.favorCost, faction.Named("FACTION"));
            if (pawn.royalty.GetFavor(faction) >= rptw.def.royalAid.favorCost)
            {
                free = false;
                return true;
            }
            free = false;
            return false;
        }
        public static bool MutatorEnabled(bool flag, bool mayhemMode)
        {
            return flag || (mayhemMode && Rand.Chance(0.35f));
        }
        public static Map GetMap_QuestNodeGameCondition(Slate slate)
        {
            Map randomPlayerHomeMap;
            if (!slate.TryGet<Map>("map", out randomPlayerHomeMap, false))
            {
                randomPlayerHomeMap = Find.RandomPlayerHomeMap;
            }
            return randomPlayerHomeMap;
        }
        public static void DoAmbush(IIncidentTarget target, float points)
        {
            IncidentDef id = DefDatabase<IncidentDef>.GetNamed("Ambush");
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(id.category, target);
            parms.points = points;
            id.Worker.TryExecute(parms);
        }
        public static Faction GetAnEnemyFaction()
        {
            Faction faction = Find.FactionManager.RandomEnemyFaction(false, false, false);
            if (faction == null)
            {
                //to build the throne of madness
                faction = Find.FactionManager.RandomEnemyFaction(true, false, false);
                if (faction == null)
                {
                    faction = Find.FactionManager.RandomEnemyFaction(true, true, false);
                    if (faction == null)
                    {
                        faction = Find.FactionManager.RandomEnemyFaction(true, true, true);
                    }
                }
            }
            return faction;
        }
    }
    //settings
    public class HVMP_Settings : ModSettings
    {
        public bool permitsScaleBySeniority = false;
        public bool maximumChaosMode = false;
        public bool occultTiedToAnomalyActivityLevel = false;
        public float minBranchQuestInterval = 12f;
        public float maxBranchQuestInterval = 18f;
        public float questRewardFactor = 1f;
        public int bratBehaviorMinExpectationLvl = 999;
        public int bratBehaviorMinSeniorityLvl = 99999;
        public int goodwillQuestRefusalLoss = 0;
        public int goodwillQuestFailureLoss = 4;
        public float makeNewBranchPlatformInterval = 15f;
        public float maxPlatformsPerBranch = 1f;
        public float platformDefenderScale = 3f;
        public float authorizerCooldownDays = 3f;
        public float authorizerStandingGain = 6f;
        public bool fort1, fort2, fort3, interv1, interv2, interv3, mm1, mm2, mm3, research1, research2, research3, transport1, transport2, transport3;
        public bool caelum1, caelum2, caelum3, machina1, machina2, machina3, mundi1, mundi2, mundi3, prox1, prox2, prox3, vox1, vox2, vox3;
        public bool atlas1, atlas2, atlas3, ares1, ares2, ares3, laelaps1, laelaps2, laelaps3, odyssey1, odyssey2, odyssey3, theseus1, theseus2, theseus3;
        public bool ethnog1, ethnog2, ethnog3, excav1, excav2, excav3, remnant1, remnant2, remnant3, sat1, sat2, sat3, shrine1, shrine2, shrine3;
        public bool cs1, cs2, cs3, ec1, ec2, ec3, fw1, fw2, fw3, hd1, hd2, hd3, ra1, ra2, ra3;
        public bool barker1, barker2, barker3, fuller1, fuller2, fuller3, lc1, lc2, lc3, natali1, natali2, natali3, romero1, romero2, romero3;
        public bool fortX, intervX, mmX, researchX, transportX, caelumX, machinaX, mundiX, proxX, voxX, atlasX, aresX, laelapsX, odysseyX, theseusX, ethnogX, excavX, remnantX, satX, shrineX, csX, ecX, fwX, hdX, raX, barkerX, fullerX, lcX, nataliX, romeroX;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref permitsScaleBySeniority, "permitsScaleBySeniority", false);
            Scribe_Values.Look(ref maximumChaosMode, "maximumChaosMode", false);
            Scribe_Values.Look(ref occultTiedToAnomalyActivityLevel, "occultTiedToAnomalyActivityLevel", false);
            Scribe_Values.Look(ref minBranchQuestInterval, "minBranchQuestInterval", 11f);
            if (maxBranchQuestInterval < minBranchQuestInterval)
            {
                maxBranchQuestInterval = minBranchQuestInterval;
            }
            Scribe_Values.Look(ref maxBranchQuestInterval, "maxBranchQuestInterval", 15f);
            Scribe_Values.Look(ref questRewardFactor, "questRewardFactor", 1f);
            Scribe_Values.Look(ref bratBehaviorMinExpectationLvl, "bratBehaviorMinExpectationLvl", 999);
            Scribe_Values.Look(ref bratBehaviorMinSeniorityLvl, "bratBehaviorMinSeniorityLvl", 99999);
            Scribe_Values.Look(ref goodwillQuestRefusalLoss, "goodwillQuestRefusalLoss", 0);
            Scribe_Values.Look(ref goodwillQuestFailureLoss, "goodwillQuestFailureLoss", 4);
            Scribe_Values.Look(ref makeNewBranchPlatformInterval, "makeNewBranchPlatformInterval", 15f);
            Scribe_Values.Look(ref maxPlatformsPerBranch, "maxPlatformsPerBranch", 1f);
            Scribe_Values.Look(ref platformDefenderScale, "platformDefenderScale", 3f);
            Scribe_Values.Look(ref authorizerCooldownDays, "authorizerCooldownDays", 3f);
            Scribe_Values.Look(ref authorizerStandingGain, "authorizerStandingGain", 6f);
            Scribe_Values.Look(ref fort1, "fort1", false);
            Scribe_Values.Look(ref fort2, "fort2", false);
            Scribe_Values.Look(ref fort3, "fort3", false);
            Scribe_Values.Look(ref fortX, "fortX", false);
            Scribe_Values.Look(ref interv1, "interv1", false);
            Scribe_Values.Look(ref interv2, "interv2", false);
            Scribe_Values.Look(ref interv3, "interv3", false);
            Scribe_Values.Look(ref intervX, "intervX", false);
            Scribe_Values.Look(ref mm1, "mm1", false);
            Scribe_Values.Look(ref mm2, "mm2", false);
            Scribe_Values.Look(ref mm3, "mm3", false);
            Scribe_Values.Look(ref mmX, "mmX", false);
            Scribe_Values.Look(ref research1, "research1", false);
            Scribe_Values.Look(ref research2, "research2", false);
            Scribe_Values.Look(ref research3, "research3", false);
            Scribe_Values.Look(ref researchX, "researchX", false);
            Scribe_Values.Look(ref transport1, "transport1", false);
            Scribe_Values.Look(ref transport2, "transport2", false);
            Scribe_Values.Look(ref transport3, "transport3", false);
            Scribe_Values.Look(ref transportX, "transportX", false);
            Scribe_Values.Look(ref caelum1, "caelum1", false);
            Scribe_Values.Look(ref caelum2, "caelum2", false);
            Scribe_Values.Look(ref caelum3, "caelum3", false);
            Scribe_Values.Look(ref caelumX, "caelumX", false);
            Scribe_Values.Look(ref machina1, "machina1", false);
            Scribe_Values.Look(ref machina2, "machina2", false);
            Scribe_Values.Look(ref machina3, "machina3", false);
            Scribe_Values.Look(ref machinaX, "machinaX", false);
            Scribe_Values.Look(ref mundi1, "mundi1", false);
            Scribe_Values.Look(ref mundi2, "mundi2", false);
            Scribe_Values.Look(ref mundi3, "mundi3", false);
            Scribe_Values.Look(ref mundiX, "mundiX", false);
            Scribe_Values.Look(ref prox1, "prox1", false);
            Scribe_Values.Look(ref prox2, "prox2", false);
            Scribe_Values.Look(ref prox3, "prox3", false);
            Scribe_Values.Look(ref proxX, "proxX", false);
            Scribe_Values.Look(ref vox1, "vox1", false);
            Scribe_Values.Look(ref vox2, "vox2", false);
            Scribe_Values.Look(ref vox3, "vox3", false);
            Scribe_Values.Look(ref voxX, "voxX", false);
            Scribe_Values.Look(ref atlas1, "atlas1", false);
            Scribe_Values.Look(ref atlas2, "atlas2", false);
            Scribe_Values.Look(ref atlas3, "atlas3", false);
            Scribe_Values.Look(ref atlasX, "atlasX", false);
            Scribe_Values.Look(ref ares1, "ares1", false);
            Scribe_Values.Look(ref ares2, "ares2", false);
            Scribe_Values.Look(ref ares3, "ares3", false);
            Scribe_Values.Look(ref aresX, "aresX", false);
            Scribe_Values.Look(ref laelaps1, "laelaps1", false);
            Scribe_Values.Look(ref laelaps2, "laelaps2", false);
            Scribe_Values.Look(ref laelaps3, "laelaps3", false);
            Scribe_Values.Look(ref laelapsX, "laelapsX", false);
            Scribe_Values.Look(ref odyssey1, "odyssey1", false);
            Scribe_Values.Look(ref odyssey2, "odyssey2", false);
            Scribe_Values.Look(ref odyssey3, "odyssey3", false);
            Scribe_Values.Look(ref odysseyX, "odysseyX", false);
            Scribe_Values.Look(ref theseus1, "theseus1", false);
            Scribe_Values.Look(ref theseus2, "theseus2", false);
            Scribe_Values.Look(ref theseus3, "theseus3", false);
            Scribe_Values.Look(ref theseusX, "theseusX", false);
            Scribe_Values.Look(ref ethnog1, "ethnog1", false);
            Scribe_Values.Look(ref ethnog2, "ethnog2", false);
            Scribe_Values.Look(ref ethnog3, "ethnog3", false);
            Scribe_Values.Look(ref ethnogX, "ethnogX", false);
            Scribe_Values.Look(ref excav1, "excav1", false);
            Scribe_Values.Look(ref excav2, "excav2", false);
            Scribe_Values.Look(ref excav3, "excav3", false);
            Scribe_Values.Look(ref excavX, "excavX", false);
            Scribe_Values.Look(ref remnant1, "remnant1", false);
            Scribe_Values.Look(ref remnant2, "remnant2", false);
            Scribe_Values.Look(ref remnant3, "remnant3", false);
            Scribe_Values.Look(ref remnantX, "remnantX", false);
            Scribe_Values.Look(ref sat1, "sat1", false);
            Scribe_Values.Look(ref sat2, "sat2", false);
            Scribe_Values.Look(ref sat3, "sat3", false);
            Scribe_Values.Look(ref satX, "satX", false);
            Scribe_Values.Look(ref shrine1, "shrine1", false);
            Scribe_Values.Look(ref shrine2, "shrine2", false);
            Scribe_Values.Look(ref shrine3, "shrine3", false);
            Scribe_Values.Look(ref shrineX, "shrineX", false);
            Scribe_Values.Look(ref cs1, "cs1", false);
            Scribe_Values.Look(ref cs2, "cs2", false);
            Scribe_Values.Look(ref cs3, "cs3", false);
            Scribe_Values.Look(ref csX, "csX", false);
            Scribe_Values.Look(ref ec1, "ec1", false);
            Scribe_Values.Look(ref ec2, "ec2", false);
            Scribe_Values.Look(ref ec3, "ec3", false);
            Scribe_Values.Look(ref ecX, "ecX", false);
            Scribe_Values.Look(ref fw1, "fw1", false);
            Scribe_Values.Look(ref fw2, "fw2", false);
            Scribe_Values.Look(ref fw3, "fw3", false);
            Scribe_Values.Look(ref fwX, "fwX", false);
            Scribe_Values.Look(ref hd1, "hd1", false);
            Scribe_Values.Look(ref hd2, "hd2", false);
            Scribe_Values.Look(ref hd3, "hd3", false);
            Scribe_Values.Look(ref hdX, "hdX", false);
            Scribe_Values.Look(ref ra1, "ra1", false);
            Scribe_Values.Look(ref ra2, "ra2", false);
            Scribe_Values.Look(ref ra3, "ra3", false);
            Scribe_Values.Look(ref raX, "raX", false);
            Scribe_Values.Look(ref barker1, "barker1", false);
            Scribe_Values.Look(ref barker2, "barker2", false);
            Scribe_Values.Look(ref barker3, "barker3", false);
            Scribe_Values.Look(ref barkerX, "barkerX", false);
            Scribe_Values.Look(ref fuller1, "fuller1", false);
            Scribe_Values.Look(ref fuller2, "fuller2", false);
            Scribe_Values.Look(ref fuller3, "fuller3", false);
            Scribe_Values.Look(ref fullerX, "fullerX", false);
            Scribe_Values.Look(ref lc1, "lc1", false);
            Scribe_Values.Look(ref lc2, "lc2", false);
            Scribe_Values.Look(ref lc3, "lc3", false);
            Scribe_Values.Look(ref lcX, "lcX", false);
            Scribe_Values.Look(ref natali1, "natali1", false);
            Scribe_Values.Look(ref natali2, "natali2", false);
            Scribe_Values.Look(ref natali3, "natali3", false);
            Scribe_Values.Look(ref nataliX, "nataliX", false);
            Scribe_Values.Look(ref romero1, "romero1", false);
            Scribe_Values.Look(ref romero2, "romero2", false);
            Scribe_Values.Look(ref romero3, "romero3", false);
            Scribe_Values.Look(ref romeroX, "romeroX", false);
            base.ExposeData();
        }
    }
    public class HVMP_Mod : Mod
    {
        public HVMP_Mod(ModContentPack content) : base(content)
        {
            HVMP_Mod.settings = GetSettings<HVMP_Settings>();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect rect = new Rect(inRect.xMin, inRect.yMin, inRect.width, inRect.height);
            Rect rect2 = new Rect(0f, 0f, inRect.width*0.96f, 4500f);
            Widgets.BeginScrollView(rect, ref this.scrollPosition, rect2, true);
            //toggles chaos mode on or off: off by default, but if on every branch will offer quests on their own timer, leading to A LOT more quests
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(rect2);
            listingStandard.CheckboxLabeled("HVMP_SettingSeniorityScalingPermits".Translate(), ref settings.permitsScaleBySeniority, "HVMP_TooltipSeniorityScalingPermits".Translate());
            listingStandard.CheckboxLabeled("HVMP_SettingChaosMode".Translate(), ref settings.maximumChaosMode, "HVMP_TooltipChaosMode".Translate());
            if (ModsConfig.AnomalyActive)
            {
                listingStandard.CheckboxLabeled("HVMP_SettingAnomalyActivityLevel".Translate(), ref settings.occultTiedToAnomalyActivityLevel, "HVMP_TooltipAnomalyActivityLevel".Translate());
            }
            listingStandard.End();
            //time in between branch quests
            displayMin = ((int)settings.minBranchQuestInterval).ToString();
            displayMax = ((int)settings.maxBranchQuestInterval).ToString();
            float x = rect.xMin, y = rect.yMin + 70, halfWidth = rect2.width * 0.5f;
            float orig = settings.minBranchQuestInterval;
            Rect questDaysMinRect = new Rect(x + 10, y, halfWidth - 15, 32);
            settings.minBranchQuestInterval = Widgets.HorizontalSlider(questDaysMinRect, settings.minBranchQuestInterval, 1f, 60f, true, "HVMP_SettingMinDays".Translate(), "1", "60", 1f);
            TooltipHandler.TipRegion(questDaysMinRect.LeftPart(1f), "HVMP_TooltipMinDays".Translate());
            if (orig != settings.minBranchQuestInterval)
            {
                displayMin = ((int)settings.minBranchQuestInterval).ToString();
            }
            y += 32;
            string origString = displayMin;
            displayMin = Widgets.TextField(new Rect(x + 10, y, 50, 32), displayMin);
            if (!displayMin.Equals(origString))
            {
                this.ParseInput(displayMin, settings.minBranchQuestInterval, out settings.minBranchQuestInterval, 1f, 60f);
            }
            if (settings.minBranchQuestInterval > settings.maxBranchQuestInterval)
            {
                settings.maxBranchQuestInterval = settings.minBranchQuestInterval;
                displayMax = ((int)settings.maxBranchQuestInterval).ToString();
            }
            y -= 32;
            orig = settings.maxBranchQuestInterval;
            Rect questDaysMaxRect = new Rect(x + 5 + halfWidth, y, halfWidth - 15, 32);
            settings.maxBranchQuestInterval = Widgets.HorizontalSlider(questDaysMaxRect, settings.maxBranchQuestInterval, 1f, 60f, true, "HVMP_SettingMaxDays".Translate(), "1", "60", 1f);
            TooltipHandler.TipRegion(questDaysMaxRect.LeftPart(1f), "HVMP_TooltipMaxDays".Translate());
            if (orig != settings.maxBranchQuestInterval)
            {
                displayMax = ((int)settings.maxBranchQuestInterval).ToString();
            }
            y += 32;
            origString = displayMax;
            displayMax = Widgets.TextField(new Rect(x + 5 + halfWidth, y, 50, 32), displayMax);
            if (!displayMax.Equals(origString))
            {
                this.ParseInput(displayMax, settings.maxBranchQuestInterval, out settings.maxBranchQuestInterval, 1f, 60f);
            }
            if (settings.maxBranchQuestInterval < settings.minBranchQuestInterval)
            {
                settings.minBranchQuestInterval = settings.maxBranchQuestInterval;
                displayMin = ((int)settings.minBranchQuestInterval).ToString();
            }
            //set how rewarding you want quests to be
            y += 50;
            displayQuestRewardFactor = (settings.questRewardFactor).ToStringByStyle(ToStringStyle.FloatOne);
            float origR = settings.questRewardFactor;
            Rect rewardFactorRect = new Rect(x + 10, y, halfWidth - 15, 32);
            settings.questRewardFactor = Widgets.HorizontalSlider(rewardFactorRect, settings.questRewardFactor, 0.2f, 2f, true, "HVMP_SettingRewardFactor".Translate(), "low", "high", 0.1f);
            TooltipHandler.TipRegion(rewardFactorRect.LeftPart(1f), "HVMP_TooltipRewardFactor".Translate());
            if (origR != settings.questRewardFactor)
            {
                displayQuestRewardFactor = ((int)settings.questRewardFactor).ToString() + "x";
            }
            y += 32;
            string origStringR = displayQuestRewardFactor;
            displayQuestRewardFactor = Widgets.TextField(new Rect(x + 10, y, 50, 32), displayQuestRewardFactor);
            if (!displayQuestRewardFactor.Equals(origStringR))
            {
                this.ParseInput(displayQuestRewardFactor, settings.questRewardFactor, out settings.questRewardFactor, 0.2f, 2f);
            }
            //set whether you should be punished for failing or foregoing a quest, and if so, when that should start happening
            y += 50;
            Rect bratBehaviorRect = new Rect(halfWidth*0.05f,y, halfWidth * 0.8f, 32);
            this.ExpectationLevelSelector(bratBehaviorRect);
            bb1 = this.ExpectationLabel;
            TooltipHandler.TipRegion(bratBehaviorRect.LeftPart(1f), "HVMP_TooltipExpectationLevel".Translate());
            Rect bratBehaviorRect2 = new Rect(halfWidth*0.95f, y, halfWidth*0.8f, 32);
            bb2 = this.SeniorityLabel;
            TooltipHandler.TipRegion(bratBehaviorRect2.LeftPart(1f), "HVMP_TooltipSeniorityLevel".Translate());
            this.SeniorityLevelSelector(bratBehaviorRect2);
            //set how punishing failing or foregoing a quest should be
            y += 50;
            if (settings.bratBehaviorMinSeniorityLvl < 999 || settings.bratBehaviorMinExpectationLvl < 999)
            {
                refusalLoss = ((int)settings.goodwillQuestRefusalLoss).ToString();
                failureLoss = ((int)settings.goodwillQuestFailureLoss).ToString();
                float orig2 = settings.goodwillQuestRefusalLoss;
                Rect refusalGwRect = new Rect(x + 10, y, halfWidth - 15, 32);
                settings.goodwillQuestRefusalLoss = (int)Widgets.HorizontalSlider(refusalGwRect, settings.goodwillQuestRefusalLoss, 0, 100, true, "HVMP_SettingGoodwillRefusal".Translate(), "0", "100", 1);
                TooltipHandler.TipRegion(refusalGwRect.LeftPart(1f), "HVMP_TooltipGoodwillRefusal".Translate());
                if (orig2 != settings.goodwillQuestRefusalLoss)
                {
                    refusalLoss = ((int)settings.goodwillQuestRefusalLoss).ToString();
                }
                y += 32;
                string origString2 = refusalLoss;
                refusalLoss = Widgets.TextField(new Rect(x + 10, y, 50, 32), refusalLoss);
                if (!refusalLoss.Equals(origString2))
                {
                    this.ParseInput(refusalLoss, settings.goodwillQuestRefusalLoss, out settings.goodwillQuestRefusalLoss, 0, 120);
                }
                y -= 32;
                orig = settings.goodwillQuestFailureLoss;
                Rect failureGwRect = new Rect(x + 5 + halfWidth, y, halfWidth - 15, 32);
                settings.goodwillQuestFailureLoss = (int)Widgets.HorizontalSlider(failureGwRect, settings.goodwillQuestFailureLoss, 0, 100, true, "HVMP_SettingGoodwillFailure".Translate(), "0", "100", 1);
                TooltipHandler.TipRegion(failureGwRect.LeftPart(1f), "HVMP_TooltipGoodwillFailure".Translate());
                if (orig != settings.goodwillQuestFailureLoss)
                {
                    failureLoss = ((int)settings.goodwillQuestFailureLoss).ToString();
                }
                y += 32;
                origString2 = failureLoss;
                failureLoss = Widgets.TextField(new Rect(x + 5 + halfWidth, y, 50, 32), failureLoss);
                if (!failureLoss.Equals(origString2))
                {
                    this.ParseInput(failureLoss, settings.goodwillQuestFailureLoss, out settings.goodwillQuestFailureLoss, 0, 120);
                }
            }
            if (ModsConfig.OdysseyActive)
            {
                y += 70;
                //branch platform spawn rate
                displayBPI = ((int)settings.makeNewBranchPlatformInterval).ToString();
                displayAuthCD = ((int)settings.authorizerCooldownDays).ToString();
                displayBPLimit = ((int)settings.maxPlatformsPerBranch).ToString();
                displayPASG = ((int)settings.authorizerStandingGain).ToString();
                displayBPD = ((int)settings.platformDefenderScale).ToString();
                float origBPI = settings.makeNewBranchPlatformInterval;
                Rect bpiRect = new Rect(x + 10, y, halfWidth - 15, 32);
                settings.makeNewBranchPlatformInterval = Widgets.HorizontalSlider(bpiRect, settings.makeNewBranchPlatformInterval, 5f, 60f, true, "HVMP_SettingBranchPlatformInterval".Translate(), "5 days", "60 days", 1f);
                TooltipHandler.TipRegion(bpiRect.LeftPart(1f), "HVMP_TooltipBranchPlatformInterval".Translate());
                if (origBPI != settings.makeNewBranchPlatformInterval)
                {
                    displayBPI = ((int)settings.makeNewBranchPlatformInterval).ToString();
                }
                y += 32;
                string origStringBPI = displayBPI;
                displayBPI = Widgets.TextField(new Rect(x + 10, y, 50, 32), displayBPI);
                if (!displayBPI.Equals(origStringBPI))
                {
                    this.ParseInput(displayBPI, settings.makeNewBranchPlatformInterval, out settings.makeNewBranchPlatformInterval, 5f, 60f);
                }
                y -= 32;
                //authorizer cooldown per standing cost
                float origPACooldown = settings.authorizerCooldownDays;
                Rect paCooldownRect = new Rect(x + 5 + halfWidth, y, halfWidth - 15, 32);
                settings.authorizerCooldownDays = Widgets.HorizontalSlider(paCooldownRect, settings.authorizerCooldownDays, 1f, 6f, true, "HVMP_SettingAuthorizerCooldown".Translate(), "1d", "6d", 0.1f);
                TooltipHandler.TipRegion(paCooldownRect.LeftPart(1f), "HVMP_TooltipAuthorizerCooldown".Translate());
                if (origPACooldown != settings.authorizerCooldownDays)
                {
                    displayAuthCD = ((int)settings.authorizerCooldownDays).ToString();
                }
                y += 32;
                string origStringPACooldown = displayAuthCD;
                displayAuthCD = Widgets.TextField(new Rect(x + 5 + halfWidth, y, 50, 32), displayAuthCD);
                if (!displayAuthCD.Equals(origStringPACooldown))
                {
                    this.ParseInput(displayAuthCD, settings.authorizerCooldownDays, out settings.authorizerCooldownDays, 1f, 6f);
                }
                y += 32;
                //max platforms per branch
                float origBPLimit = settings.maxPlatformsPerBranch;
                Rect bplimitRect = new Rect(x + 10, y, halfWidth - 15, 32);
                settings.maxPlatformsPerBranch = Widgets.HorizontalSlider(bplimitRect, settings.maxPlatformsPerBranch, 1f, 4f, true, "HVMP_SettingBranchPlatformLimit".Translate(), "1", "4", 1f);
                TooltipHandler.TipRegion(bplimitRect.LeftPart(1f), "HVMP_TooltipBranchPlatformLimit".Translate());
                if (origBPLimit != settings.maxPlatformsPerBranch)
                {
                    displayBPLimit = ((int)settings.maxPlatformsPerBranch).ToString();
                }
                y += 32;
                string origStringBPLimit = displayBPLimit;
                displayBPLimit = Widgets.TextField(new Rect(x + 10, y, 50, 32), displayBPLimit);
                if (!displayBPLimit.Equals(origStringBPLimit))
                {
                    this.ParseInput(displayBPLimit, settings.maxPlatformsPerBranch, out settings.maxPlatformsPerBranch, 1f, 4f);
                }
                y -= 32;
                //authorizer standing gain
                float origPASG = settings.authorizerStandingGain;
                Rect pasgRect = new Rect(x + 5 + halfWidth, y, halfWidth - 15, 32);
                settings.authorizerStandingGain = Widgets.HorizontalSlider(pasgRect, settings.authorizerStandingGain, 1f, 10f, true, "HVMP_SettingAuthorizerStandingGain".Translate(), "1", "10", 1f);
                TooltipHandler.TipRegion(pasgRect.LeftPart(1f), "HVMP_TooltipAuthorizerStandingGain".Translate());
                if (origPASG != settings.authorizerStandingGain)
                {
                    displayPASG = ((int)settings.authorizerStandingGain).ToString();
                }
                y += 32;
                string origStringPASG = displayPASG;
                displayPASG = Widgets.TextField(new Rect(x + 5 + halfWidth, y, 50, 32), displayPASG);
                if (!displayPASG.Equals(origStringPASG))
                {
                    this.ParseInput(displayPASG, settings.authorizerStandingGain, out settings.authorizerStandingGain, 1f, 10f);
                }
                y += 32;
                //branch platform defender scale
                float origBPD = settings.platformDefenderScale;
                Rect bpDefenderRect = new Rect(x + 10, y, halfWidth - 15, 32);
                settings.platformDefenderScale = Widgets.HorizontalSlider(bpDefenderRect, settings.platformDefenderScale, 1f, 7f, true, "HVMP_SettingBranchPlatformDefenders".Translate(), "1x", "7x", 1f);
                TooltipHandler.TipRegion(bpDefenderRect.LeftPart(1f), "HVMP_TooltipBranchPlatformDefenders".Translate());
                if (origBPD != settings.platformDefenderScale)
                {
                    displayBPD = ((int)settings.platformDefenderScale).ToString();
                }
                y += 32;
                string origStringBPD = displayBPD;
                displayBPD = Widgets.TextField(new Rect(x + 10, y, 50, 32), displayBPD);
                if (!displayBPD.Equals(origStringBPD))
                {
                    this.ParseInput(displayBPD, settings.platformDefenderScale, out settings.platformDefenderScale, 1f, 7f);
                }
            }
            y += 70;
            Rect rect3 = new Rect(0f, y, (inRect.width - 30f) * 0.8f, 4000f);
            Listing_Standard listing_Standard2 = new Listing_Standard();
            listing_Standard2.Begin(rect3);
            Text.Font = GameFont.Medium;
            listing_Standard2.Label("HVMP_Label_QuestMutators".Translate(),30f);
            //listing_Standard2.Gap(30f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Tooltip_QuestMutators".Translate());
            listing_Standard2.Gap(20f);
            listing_Standard2.Label("HVMP_Label_Commerce".Translate());
            listing_Standard2.Label("HVMP_Label_Fortification".Translate(),-1,"HVMP_Tooltip_Fortification".Translate());
            Text.Font = GameFont.Tiny;
            bool flagCA1 = settings.fort1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Fortification".Translate() + ": " + "HVMP_HardMode_Label_Fortification_1".Translate(), ref flagCA1, "HVMP_HardMode_Tooltip_Fortification_1".Translate());
            settings.fort1 = flagCA1;
            bool flagCA2 = settings.fort2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Fortification".Translate() + ": " + "HVMP_HardMode_Label_Fortification_2".Translate(), ref flagCA2, "HVMP_HardMode_Tooltip_Fortification_2".Translate());
            settings.fort2 = flagCA2;
            bool flagCA3 = settings.fort3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Fortification".Translate() + ": " + "HVMP_HardMode_Label_Fortification_3".Translate(), ref flagCA3, "HVMP_HardMode_Tooltip_Fortification_3".Translate());
            settings.fort3 = flagCA3;
            bool flagCAX = settings.fortX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagCAX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.fortX = flagCAX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Intervention".Translate(), -1, "HVMP_Tooltip_Intervention".Translate());
            Text.Font = GameFont.Tiny;
            bool flagCB1 = settings.interv1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Intervention".Translate() + ": " + "HVMP_HardMode_Label_Intervention_1".Translate(), ref flagCB1, "HVMP_HardMode_Tooltip_Intervention_1".Translate());
            settings.interv1 = flagCB1;
            bool flagCB2 = settings.interv2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Intervention".Translate() + ": " + "HVMP_HardMode_Label_Intervention_2".Translate(), ref flagCB2, "HVMP_HardMode_Tooltip_Intervention_2".Translate());
            settings.interv2 = flagCB2;
            bool flagCB3 = settings.interv3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Intervention".Translate() + ": " + "HVMP_HardMode_Label_Intervention_3".Translate(), ref flagCB3, "HVMP_HardMode_Tooltip_Intervention_3".Translate());
            settings.interv3 = flagCB3;
            bool flagCBX = settings.intervX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagCBX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.intervX = flagCBX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Mastermind".Translate(), -1, "HVMP_Tooltip_Mastermind".Translate());
            Text.Font = GameFont.Tiny;
            bool flagCC1 = settings.mm1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Mastermind".Translate() + ": " + "HVMP_HardMode_Label_Mastermind_1".Translate(), ref flagCC1, "HVMP_HardMode_Tooltip_Mastermind_1".Translate());
            settings.mm1 = flagCC1;
            bool flagCC2 = settings.mm2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Mastermind".Translate() + ": " + "HVMP_HardMode_Label_Mastermind_2".Translate(), ref flagCC2, "HVMP_HardMode_Tooltip_Mastermind_2".Translate());
            settings.mm2 = flagCC2;
            bool flagCC3 = settings.mm3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Mastermind".Translate() + ": " + "HVMP_HardMode_Label_Mastermind_3".Translate(), ref flagCC3, "HVMP_HardMode_Tooltip_Mastermind_3".Translate());
            settings.mm3 = flagCC3;
            bool flagCCX = settings.mmX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagCCX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.mmX = flagCCX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Research".Translate(), -1, "HVMP_Tooltip_Research".Translate());
            Text.Font = GameFont.Tiny;
            bool flagCD1 = settings.research1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Research".Translate() + ": " + "HVMP_HardMode_Label_Research_1".Translate(), ref flagCD1, "HVMP_HardMode_Tooltip_Research_1".Translate());
            settings.research1 = flagCD1;
            bool flagCD2 = settings.research2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Research".Translate() + ": " + "HVMP_HardMode_Label_Research_2".Translate(), ref flagCD2, "HVMP_HardMode_Tooltip_Research_2".Translate());
            settings.research2 = flagCD2;
            bool flagCD3 = settings.research3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Research".Translate() + ": " + "HVMP_HardMode_Label_Research_3".Translate(), ref flagCD3, "HVMP_HardMode_Tooltip_Research_3".Translate());
            settings.research3 = flagCD3;
            bool flagCDX = settings.researchX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagCDX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.researchX = flagCDX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Transportation".Translate(), -1, "HVMP_Tooltip_Transportation".Translate());
            Text.Font = GameFont.Tiny;
            bool flagCE1 = settings.transport1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Transportation".Translate() + ": " + "HVMP_HardMode_Label_Transportation_1".Translate(), ref flagCE1, "HVMP_HardMode_Tooltip_Transportation_1".Translate());
            settings.transport1 = flagCE1;
            bool flagCE2 = settings.transport2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Transportation".Translate() + ": " + "HVMP_HardMode_Label_Transportation_2".Translate(), ref flagCE2, "HVMP_HardMode_Tooltip_Transportation_2".Translate());
            settings.transport2= flagCE2;
            bool flagCE3 = settings.transport3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Transportation".Translate() + ": " + "HVMP_HardMode_Label_Transportation_3".Translate(), ref flagCE3, "HVMP_HardMode_Tooltip_Transportation_3".Translate());
            settings.transport3 = flagCE3;
            bool flagCEX = settings.transportX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagCEX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.transportX = flagCEX;
            listing_Standard2.Gap(20f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Pax".Translate());
            listing_Standard2.Label("HVMP_Label_Caelum".Translate(), -1, "HVMP_Tooltip_Caelum".Translate());
            Text.Font = GameFont.Tiny;
            bool flagPA1 = settings.caelum1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Caelum".Translate() + ": " + "HVMP_HardMode_Label_Caelum_1".Translate(), ref flagPA1, "HVMP_HardMode_Tooltip_Caelum_1".Translate());
            settings.caelum1 = flagPA1;
            bool flagPA2 = settings.caelum2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Caelum".Translate() + ": " + "HVMP_HardMode_Label_Caelum_2".Translate(), ref flagPA2, "HVMP_HardMode_Tooltip_Caelum_2".Translate());
            settings.caelum2 = flagPA2;
            bool flagPA3 = settings.caelum3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Caelum".Translate() + ": " + "HVMP_HardMode_Label_Caelum_3".Translate(), ref flagPA3, "HVMP_HardMode_Tooltip_Caelum_3".Translate());
            settings.caelum3 = flagPA3;
            bool flagPAX = settings.caelumX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagPAX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.caelumX = flagPAX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Machina".Translate(), -1, "HVMP_Tooltip_Machina".Translate());
            Text.Font = GameFont.Tiny;
            bool flagPB1 = settings.machina1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Machina".Translate() + ": " + "HVMP_HardMode_Label_Machina_1".Translate(), ref flagPB1, "HVMP_HardMode_Tooltip_Machina_1".Translate());
            settings.machina1 = flagPB1;
            bool flagPB2 = settings.machina2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Machina".Translate() + ": " + "HVMP_HardMode_Label_Machina_2".Translate(), ref flagPB2, "HVMP_HardMode_Tooltip_Machina_2".Translate());
            settings.machina2 = flagPB2;
            bool flagPB3 = settings.machina3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Machina".Translate() + ": " + "HVMP_HardMode_Label_Machina_3".Translate(), ref flagPB3, "HVMP_HardMode_Tooltip_Machina_3".Translate());
            settings.machina3 = flagPB3;
            bool flagPBX = settings.machinaX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagPBX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.machinaX = flagPBX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Mundi".Translate(), -1, "HVMP_Tooltip_Mundi".Translate());
            Text.Font = GameFont.Tiny;
            bool flagPC1 = settings.mundi1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Mundi".Translate() + ": " + "HVMP_HardMode_Label_Mundi_1".Translate(), ref flagPC1, "HVMP_HardMode_Tooltip_Mundi_1".Translate());
            settings.mundi1 = flagPC1;
            bool flagPC2 = settings.mundi2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Mundi".Translate() + ": " + "HVMP_HardMode_Label_Mundi_2".Translate(), ref flagPC2, "HVMP_HardMode_Tooltip_Mundi_2".Translate());
            settings.mundi2 = flagPC2;
            bool flagPC3 = settings.mundi3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Mundi".Translate() + ": " + "HVMP_HardMode_Label_Mundi_3".Translate(), ref flagPC3, "HVMP_HardMode_Tooltip_Mundi_3".Translate());
            settings.mundi3 = flagPC3;
            bool flagPCX = settings.mundiX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagPCX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.mundiX = flagPCX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Populus".Translate(), -1, "HVMP_Tooltip_Populus".Translate());
            Text.Font = GameFont.Tiny;
            bool flagPD1 = settings.prox1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Populus".Translate() + ": " + "HVMP_HardMode_Label_Populus_1".Translate(), ref flagPD1, "HVMP_HardMode_Tooltip_Populus_1".Translate());
            settings.prox1 = flagPD1;
            bool flagPD2 = settings.prox2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Populus".Translate() + ": " + "HVMP_HardMode_Label_Populus_2".Translate(), ref flagPD2, "HVMP_HardMode_Tooltip_Populus_2".Translate());
            settings.prox2 = flagPD2;
            bool flagPD3 = settings.prox3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Populus".Translate() + ": " + "HVMP_HardMode_Label_Populus_3".Translate(), ref flagPD3, "HVMP_HardMode_Tooltip_Populus_3".Translate());
            settings.prox3 = flagPD3;
            bool flagPDX = settings.proxX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagPDX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.proxX = flagPDX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Vox".Translate(), -1, "HVMP_Tooltip_Vox".Translate());
            Text.Font = GameFont.Tiny;
            bool flagPE1 = settings.vox1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Vox".Translate() + ": " + "HVMP_HardMode_Label_Vox_1".Translate(), ref flagPE1, "HVMP_HardMode_Tooltip_Vox_1".Translate());
            settings.vox1 = flagPE1;
            bool flagPE2 = settings.vox2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Vox".Translate() + ": " + "HVMP_HardMode_Label_Vox_2".Translate(), ref flagPE2, "HVMP_HardMode_Tooltip_Vox_2".Translate());
            settings.vox2 = flagPE2;
            bool flagPE3 = settings.vox3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Vox".Translate() + ": " + "HVMP_HardMode_Label_Vox_3".Translate(), ref flagPE3, "HVMP_HardMode_Tooltip_Vox_3".Translate());
            settings.vox3 = flagPE3;
            bool flagPEX = settings.voxX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagPEX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.voxX = flagPEX;
            listing_Standard2.Gap(20f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Rover".Translate());
            listing_Standard2.Label("HVMP_Label_Atlas".Translate(), -1, "HVMP_Tooltip_Atlas".Translate());
            Text.Font = GameFont.Tiny;
            bool flagRA1 = settings.atlas1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Atlas".Translate() + ": " + "HVMP_HardMode_Label_Atlas_1".Translate(), ref flagRA1, "HVMP_HardMode_Tooltip_Atlas_1".Translate());
            settings.atlas1 = flagRA1;
            bool flagRA2 = settings.atlas2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Atlas".Translate() + ": " + "HVMP_HardMode_Label_Atlas_2".Translate(), ref flagRA2, "HVMP_HardMode_Tooltip_Atlas_2".Translate());
            settings.atlas2 = flagRA2;
            bool flagRA3 = settings.atlas3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Atlas".Translate() + ": " + "HVMP_HardMode_Label_Atlas_3".Translate(), ref flagRA3, "HVMP_HardMode_Tooltip_Atlas_3".Translate());
            settings.atlas3 = flagRA3;
            bool flagRAX = settings.atlasX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagRAX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.atlasX = flagRAX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Ares".Translate(), -1, "HVMP_Tooltip_Ares".Translate());
            Text.Font = GameFont.Tiny;
            bool flagRB1 = settings.ares1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Ares".Translate() + ": " + "HVMP_HardMode_Label_Ares_1".Translate(), ref flagRB1, "HVMP_HardMode_Tooltip_Ares_1".Translate());
            settings.ares1 = flagRB1;
            bool flagRB2 = settings.ares2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Ares".Translate() + ": " + "HVMP_HardMode_Label_Ares_2".Translate(), ref flagRB2, "HVMP_HardMode_Tooltip_Ares_2".Translate());
            settings.ares2 = flagRB2;
            bool flagRB3 = settings.ares3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Ares".Translate() + ": " + "HVMP_HardMode_Label_Ares_3".Translate(), ref flagRB3, "HVMP_HardMode_Tooltip_Ares_3".Translate());
            settings.ares3 = flagRB3;
            bool flagRBX = settings.aresX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagRBX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.aresX = flagRBX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Laelaps".Translate(), -1, "HVMP_Tooltip_Laelaps".Translate());
            Text.Font = GameFont.Tiny;
            bool flagRC1 = settings.laelaps1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Laelaps".Translate() + ": " + "HVMP_HardMode_Label_Laelaps_1".Translate(), ref flagRC1, "HVMP_HardMode_Tooltip_Laelaps_1".Translate());
            settings.laelaps1 = flagRC1;
            bool flagRC2 = settings.laelaps2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Laelaps".Translate() + ": " + "HVMP_HardMode_Label_Laelaps_2".Translate(), ref flagRC2, "HVMP_HardMode_Tooltip_Laelaps_2".Translate());
            settings.laelaps2 = flagRC2;
            bool flagRC3 = settings.laelaps3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Laelaps".Translate() + ": " + "HVMP_HardMode_Label_Laelaps_3".Translate(), ref flagRC3, "HVMP_HardMode_Tooltip_Laelaps_3".Translate());
            settings.laelaps3 = flagRC3;
            bool flagRCX = settings.laelapsX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagRCX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.laelapsX = flagRCX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Odyssey".Translate(), -1, "HVMP_Tooltip_Odyssey".Translate());
            Text.Font = GameFont.Tiny;
            bool flagRD1 = settings.odyssey1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Odyssey".Translate() + ": " + "HVMP_HardMode_Label_Odyssey_1".Translate(), ref flagRD1, "HVMP_HardMode_Tooltip_Odyssey_1".Translate());
            settings.odyssey1 = flagRD1;
            bool flagRD2 = settings.odyssey2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Odyssey".Translate() + ": " + "HVMP_HardMode_Label_Odyssey_2".Translate(), ref flagRD2, "HVMP_HardMode_Tooltip_Odyssey_2".Translate());
            settings.odyssey2 = flagRD2;
            bool flagRD3 = settings.odyssey3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Odyssey".Translate() + ": " + "HVMP_HardMode_Label_Odyssey_3".Translate(), ref flagRD3, "HVMP_HardMode_Tooltip_Odyssey_3".Translate());
            settings.odyssey3 = flagRD3;
            bool flagRDX = settings.odysseyX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagRDX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.odysseyX = flagRDX;
            listing_Standard2.Gap(10f);
            Text.Font = GameFont.Small;
            listing_Standard2.Label("HVMP_Label_Theseus".Translate(), -1, "HVMP_Tooltip_Theseus".Translate());
            Text.Font = GameFont.Tiny;
            bool flagRE1 = settings.theseus1;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Theseus".Translate() + ": " + "HVMP_HardMode_Label_Theseus_1".Translate(), ref flagRE1, "HVMP_HardMode_Tooltip_Theseus_1".Translate());
            settings.theseus1 = flagRE1;
            bool flagRE2 = settings.theseus2;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Theseus".Translate() + ": " + "HVMP_HardMode_Label_Theseus_2".Translate(), ref flagRE2, "HVMP_HardMode_Tooltip_Theseus_2".Translate());
            settings.theseus2 = flagRE2;
            bool flagRE3 = settings.theseus3;
            listing_Standard2.CheckboxLabeled("HVMP_Label_Theseus".Translate() + ": " + "HVMP_HardMode_Label_Theseus_3".Translate(), ref flagRE3, "HVMP_HardMode_Tooltip_Theseus_3".Translate());
            settings.theseus3 = flagRE3;
            bool flagREX = settings.theseusX;
            listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagREX, "HVMP_Tooltip_MayhemMode".Translate());
            settings.theseusX = flagREX;
            if (ModsConfig.IdeologyActive)
            {
                listing_Standard2.Gap(20f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Archive".Translate());
                listing_Standard2.Label("HVMP_Label_Ethnography".Translate(), -1, "HVMP_Tooltip_Ethnography".Translate());
                Text.Font = GameFont.Tiny;
                bool flagAA1 = settings.ethnog1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Ethnography".Translate() + ": " + "HVMP_HardMode_Label_Ethnography_1".Translate(), ref flagAA1, "HVMP_HardMode_Tooltip_Ethnography_1".Translate());
                settings.ethnog1 = flagAA1;
                bool flagAA2 = settings.ethnog2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Ethnography".Translate() + ": " + "HVMP_HardMode_Label_Ethnography_2".Translate(), ref flagAA2, "HVMP_HardMode_Tooltip_Ethnography_2".Translate());
                settings.ethnog2 = flagAA2;
                bool flagAA3 = settings.ethnog3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Ethnography".Translate() + ": " + "HVMP_HardMode_Label_Ethnography_3".Translate(), ref flagAA3, "HVMP_HardMode_Tooltip_Ethnography_3".Translate());
                settings.ethnog3 = flagAA3;
                bool flagAAX = settings.ethnogX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagAAX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.ethnogX = flagAAX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Excavation".Translate(), -1, "HVMP_Tooltip_Excavation".Translate());
                Text.Font = GameFont.Tiny;
                bool flagAB1 = settings.excav1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Excavation".Translate() + ": " + "HVMP_HardMode_Label_Excavation_1".Translate(), ref flagAB1, "HVMP_HardMode_Tooltip_Excavation_1".Translate());
                settings.excav1 = flagAB1;
                bool flagAB2 = settings.excav2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Excavation".Translate() + ": " + "HVMP_HardMode_Label_Excavation_2".Translate(), ref flagAB2, "HVMP_HardMode_Tooltip_Excavation_2".Translate());
                settings.excav2 = flagAB2;
                bool flagAB3 = settings.excav3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Excavation".Translate() + ": " + "HVMP_HardMode_Label_Excavation_3".Translate(), ref flagAB3, "HVMP_HardMode_Tooltip_Excavation_3".Translate());
                settings.excav3 = flagAB3;
                bool flagABX = settings.excavX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagABX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.excavX = flagABX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Remnant".Translate(), -1, "HVMP_Tooltip_Remnant".Translate());
                Text.Font = GameFont.Tiny;
                bool flagAC1 = settings.remnant1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Remnant".Translate() + ": " + "HVMP_HardMode_Label_Remnant_1".Translate(), ref flagAC1, "HVMP_HardMode_Tooltip_Remnant_1".Translate());
                settings.remnant1 = flagAC1;
                bool flagAC2 = settings.remnant2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Remnant".Translate() + ": " + "HVMP_HardMode_Label_Remnant_2".Translate(), ref flagAC2, "HVMP_HardMode_Tooltip_Remnant_2".Translate());
                settings.remnant2 = flagAC2;
                bool flagAC3 = settings.remnant3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Remnant".Translate() + ": " + "HVMP_HardMode_Label_Remnant_3".Translate(), ref flagAC3, "HVMP_HardMode_Tooltip_Remnant_3".Translate());
                settings.remnant3 = flagAC3;
                bool flagACX = settings.remnantX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagACX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.remnantX = flagACX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Satellite".Translate(), -1, "HVMP_Tooltip_Satellite".Translate());
                Text.Font = GameFont.Tiny;
                bool flagAD1 = settings.sat1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Satellite".Translate() + ": " + "HVMP_HardMode_Label_Satellite_1".Translate(), ref flagAD1, "HVMP_HardMode_Tooltip_Satellite_1".Translate());
                settings.sat1 = flagAD1;
                bool flagAD2 = settings.sat2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Satellite".Translate() + ": " + "HVMP_HardMode_Label_Satellite_2".Translate(), ref flagAD2, "HVMP_HardMode_Tooltip_Satellite_2".Translate());
                settings.sat2 = flagAD2;
                bool flagAD3 = settings.sat3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Satellite".Translate() + ": " + "HVMP_HardMode_Label_Satellite_3".Translate(), ref flagAD3, "HVMP_HardMode_Tooltip_Satellite_3".Translate());
                settings.sat3 = flagAD3;
                bool flagADX = settings.satX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagADX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.satX = flagADX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Shrine".Translate(), -1, "HVMP_Tooltip_Shrine".Translate());
                Text.Font = GameFont.Tiny;
                bool flagAE1 = settings.shrine1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Shrine".Translate() + ": " + "HVMP_HardMode_Label_Shrine_1".Translate(), ref flagAE1, "HVMP_HardMode_Tooltip_Shrine_1".Translate());
                settings.shrine1 = flagAE1;
                bool flagAE2 = settings.shrine2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Shrine".Translate() + ": " + "HVMP_HardMode_Label_Shrine_2".Translate(), ref flagAE2, "HVMP_HardMode_Tooltip_Shrine_2".Translate());
                settings.shrine2 = flagAE2;
                bool flagAE3 = settings.shrine3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Shrine".Translate() + ": " + "HVMP_HardMode_Label_Shrine_3".Translate(), ref flagAE3, "HVMP_HardMode_Tooltip_Shrine_3".Translate());
                settings.shrine3 = flagAE3;
                bool flagAEX = settings.shrineX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagAEX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.shrineX = flagAEX;
            }
            if (ModsConfig.BiotechActive)
            {
                listing_Standard2.Gap(20f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Ecosphere".Translate());
                listing_Standard2.Label("HVMP_Label_CaseStudy".Translate(), -1, "HVMP_Tooltip_CaseStudy".Translate());
                Text.Font = GameFont.Tiny;
                bool flagEA1 = settings.cs1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_CaseStudy".Translate() + ": " + "HVMP_HardMode_Label_CaseStudy_1".Translate(), ref flagEA1, "HVMP_HardMode_Tooltip_CaseStudy_1".Translate());
                settings.cs1 = flagEA1;
                bool flagEA2 = settings.cs2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_CaseStudy".Translate() + ": " + "HVMP_HardMode_Label_CaseStudy_2".Translate(), ref flagEA2, "HVMP_HardMode_Tooltip_CaseStudy_2".Translate());
                settings.cs2 = flagEA2;
                bool flagEA3 = settings.cs3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_CaseStudy".Translate() + ": " + "HVMP_HardMode_Label_CaseStudy_3".Translate(), ref flagEA3, "HVMP_HardMode_Tooltip_CaseStudy_3".Translate());
                settings.cs3 = flagEA3;
                bool flagEAX = settings.csX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagEAX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.csX = flagEAX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_ECquest".Translate(), -1, "HVMP_Tooltip_ECquest".Translate());
                Text.Font = GameFont.Tiny;
                bool flagEB1 = settings.ec1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_ECquest".Translate() + ": " + "HVMP_HardMode_Label_ECquest_1".Translate(), ref flagEB1, "HVMP_HardMode_Tooltip_ECquest_1".Translate());
                settings.ec1 = flagEB1;
                bool flagEB2 = settings.ec2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_ECquest".Translate() + ": " + "HVMP_HardMode_Label_ECquest_2".Translate(), ref flagEB2, "HVMP_HardMode_Tooltip_ECquest_2".Translate());
                settings.ec2 = flagEB2;
                bool flagEB3 = settings.ec3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_ECquest".Translate() + ": " + "HVMP_HardMode_Label_ECquest_3".Translate(), ref flagEB3, "HVMP_HardMode_Tooltip_ECquest_3".Translate());
                settings.ec3 = flagEB3;
                bool flagEBX = settings.ecX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagEBX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.ecX = flagEBX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_FieldWork".Translate(), -1, "HVMP_Tooltip_FieldWork".Translate());
                Text.Font = GameFont.Tiny;
                bool flagEC1 = settings.fw1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_FieldWork".Translate() + ": " + "HVMP_HardMode_Label_FieldWork_1".Translate(), ref flagEC1, "HVMP_HardMode_Tooltip_FieldWork_1".Translate());
                settings.fw1 = flagEC1;
                bool flagEC2 = settings.fw2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_FieldWork".Translate() + ": " + "HVMP_HardMode_Label_FieldWork_2".Translate(), ref flagEC2, "HVMP_HardMode_Tooltip_FieldWork_2".Translate());
                settings.fw2 = flagEC2;
                bool flagEC3 = settings.fw3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_FieldWork".Translate() + ": " + "HVMP_HardMode_Label_FieldWork_3".Translate(), ref flagEC3, "HVMP_HardMode_Tooltip_FieldWork_3".Translate());
                settings.fw3 = flagEC3;
                bool flagECX = settings.fwX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagECX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.fwX = flagECX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_HazardDisposal".Translate(), -1, "HVMP_Tooltip_HazardDisposal".Translate());
                Text.Font = GameFont.Tiny;
                bool flagED1 = settings.hd1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_HazardDisposal".Translate() + ": " + "HVMP_HardMode_Label_HazardDisposal_1".Translate(), ref flagED1, "HVMP_HardMode_Tooltip_HazardDisposal_1".Translate());
                settings.hd1 = flagED1;
                bool flagED2 = settings.hd2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_HazardDisposal".Translate() + ": " + "HVMP_HardMode_Label_HazardDisposal_2".Translate(), ref flagED2, "HVMP_HardMode_Tooltip_HazardDisposal_2".Translate());
                settings.hd2 = flagED2;
                bool flagED3 = settings.hd3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_HazardDisposal".Translate() + ": " + "HVMP_HardMode_Label_HazardDisposal_3".Translate(), ref flagED3, "HVMP_HardMode_Tooltip_HazardDisposal_3".Translate());
                settings.hd3 = flagED3;
                bool flagEDX = settings.hdX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagEDX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.hdX = flagEDX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_RAquest".Translate(), -1, "HVMP_Tooltip_RAquest".Translate());
                Text.Font = GameFont.Tiny;
                bool flagEE1 = settings.ra1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_RAquest".Translate() + ": " + "HVMP_HardMode_Label_RAquest_1".Translate(), ref flagEE1, "HVMP_HardMode_Tooltip_RAquest_1".Translate());
                settings.ra1 = flagEE1;
                bool flagEE2 = settings.ra2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_RAquest".Translate() + ": " + "HVMP_HardMode_Label_RAquest_2".Translate(), ref flagEE2, "HVMP_HardMode_Tooltip_RAquest_2".Translate());
                settings.ra2 = flagEE2;
                bool flagEE3 = settings.ra3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_RAquest".Translate() + ": " + "HVMP_HardMode_Label_RAquest_3".Translate(), ref flagEE3, "HVMP_HardMode_Tooltip_RAquest_3".Translate());
                settings.ra3 = flagEE3;
                bool flagEEX = settings.raX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagEEX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.raX = flagEEX;
            }
            if (ModsConfig.AnomalyActive)
            {
                listing_Standard2.Gap(20f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Occult".Translate());
                listing_Standard2.Label("HVMP_Label_Barker".Translate(), -1, "HVMP_Tooltip_Barker".Translate());
                Text.Font = GameFont.Tiny;
                bool flagOA1 = settings.barker1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Barker".Translate() + ": " + "HVMP_HardMode_Label_Barker_1".Translate(), ref flagOA1, "HVMP_HardMode_Tooltip_Barker_1".Translate());
                settings.barker1 = flagOA1;
                bool flagOA2 = settings.barker2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Barker".Translate() + ": " + "HVMP_HardMode_Label_Barker_2".Translate(), ref flagOA2, "HVMP_HardMode_Tooltip_Barker_2".Translate());
                settings.barker2 = flagOA2;
                bool flagOA3 = settings.barker3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Barker".Translate() + ": " + "HVMP_HardMode_Label_Barker_3".Translate(), ref flagOA3, "HVMP_HardMode_Tooltip_Barker_3".Translate());
                settings.barker3 = flagOA3;
                bool flagOAX = settings.barkerX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagOAX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.barkerX = flagOAX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Fuller".Translate(), -1, "HVMP_Tooltip_Fuller".Translate());
                Text.Font = GameFont.Tiny;
                bool flagOB1 = settings.fuller1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Fuller".Translate() + ": " + "HVMP_HardMode_Label_Fuller_1".Translate(), ref flagOB1, "HVMP_HardMode_Tooltip_Fuller_1".Translate());
                settings.fuller1 = flagOB1;
                bool flagOB2 = settings.fuller2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Fuller".Translate() + ": " + "HVMP_HardMode_Label_Fuller_2".Translate(), ref flagOB2, "HVMP_HardMode_Tooltip_Fuller_2".Translate());
                settings.fuller2 = flagOB2;
                bool flagOB3 = settings.fuller3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Fuller".Translate() + ": " + "HVMP_HardMode_Label_Fuller_3".Translate(), ref flagOB3, "HVMP_HardMode_Tooltip_Fuller_3".Translate());
                settings.fuller3 = flagOB3;
                bool flagOBX = settings.fullerX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagOBX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.fullerX = flagOBX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Lovecraft".Translate(), -1, "HVMP_Tooltip_Lovecraft".Translate());
                Text.Font = GameFont.Tiny;
                bool flagOC1 = settings.lc1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Lovecraft".Translate() + ": " + "HVMP_HardMode_Label_Lovecraft_1".Translate(), ref flagOC1, "HVMP_HardMode_Tooltip_Lovecraft_1".Translate());
                settings.lc1 = flagOC1;
                bool flagOC2 = settings.lc2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Lovecraft".Translate() + ": " + "HVMP_HardMode_Label_Lovecraft_2".Translate(), ref flagOC2, "HVMP_HardMode_Tooltip_Lovecraft_2".Translate());
                settings.lc2 = flagOC2;
                bool flagOC3 = settings.lc3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Lovecraft".Translate() + ": " + "HVMP_HardMode_Label_Lovecraft_3".Translate(), ref flagOC3, "HVMP_HardMode_Tooltip_Lovecraft_3".Translate());
                settings.lc3 = flagOC3;
                bool flagOCX = settings.lcX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagOCX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.lcX = flagOCX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Natali".Translate(), -1, "HVMP_Tooltip_Natali".Translate());
                Text.Font = GameFont.Tiny;
                bool flagOD1 = settings.natali1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Natali".Translate() + ": " + "HVMP_HardMode_Label_Natali_1".Translate(), ref flagOD1, "HVMP_HardMode_Tooltip_Natali_1".Translate());
                settings.natali1 = flagOD1;
                bool flagOD2 = settings.natali2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Natali".Translate() + ": " + "HVMP_HardMode_Label_Natali_2".Translate(), ref flagOD2, "HVMP_HardMode_Tooltip_Natali_2".Translate());
                settings.natali2 = flagOD2;
                bool flagOD3 = settings.natali3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Natali".Translate() + ": " + "HVMP_HardMode_Label_Natali_3".Translate(), ref flagOD3, "HVMP_HardMode_Tooltip_Natali_3".Translate());
                settings.natali3 = flagOD3;
                bool flagODX = settings.nataliX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagODX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.nataliX = flagODX;
                listing_Standard2.Gap(10f);
                Text.Font = GameFont.Small;
                listing_Standard2.Label("HVMP_Label_Romero".Translate(), -1, "HVMP_Tooltip_Romero".Translate());
                Text.Font = GameFont.Tiny;
                bool flagOE1 = settings.romero1;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Romero".Translate() + ": " + "HVMP_HardMode_Label_Romero_1".Translate(), ref flagOE1, "HVMP_HardMode_Tooltip_Romero_1".Translate());
                settings.romero1 = flagOE1;
                bool flagOE2 = settings.romero2;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Romero".Translate() + ": " + "HVMP_HardMode_Label_Romero_2".Translate(), ref flagOE2, "HVMP_HardMode_Tooltip_Romero_2".Translate());
                settings.romero2 = flagOE2;
                bool flagOE3 = settings.romero3;
                listing_Standard2.CheckboxLabeled("HVMP_Label_Romero".Translate() + ": " + "HVMP_HardMode_Label_Romero_3".Translate(), ref flagOE3, "HVMP_HardMode_Tooltip_Romero_3".Translate());
                settings.romero3 = flagOE3;
                bool flagOEX = settings.romeroX;
                listing_Standard2.CheckboxLabeled("HVMP_Label_MayhemMode".Translate(), ref flagOEX, "HVMP_Tooltip_MayhemMode".Translate());
                settings.romeroX = flagOEX;
            }
            listing_Standard2.End();
            Widgets.EndScrollView();
            base.DoSettingsWindowContents(inRect);
        }
        private void ExpectationLevelSelector(Rect rect)
        {
            if (Widgets.ButtonText(rect, "HVMP_TooltipMinExpectationLevel".Translate(bb1), true, true, true, null))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>
                {
                    new FloatMenuOption("HVMP_Inactive".Translate(), delegate
                    {
                        settings.bratBehaviorMinExpectationLvl = 999;
                        bb1 = this.ExpectationLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption(ExpectationsUtility.ExpectationForOrder(0).LabelCap, delegate
                    {
                        settings.bratBehaviorMinExpectationLvl = 0;
                        bb1 = this.ExpectationLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption(ExpectationsUtility.ExpectationForOrder(1).LabelCap, delegate
                    {
                        settings.bratBehaviorMinExpectationLvl = 1;
                        bb1 = this.ExpectationLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption(ExpectationsUtility.ExpectationForOrder(2).LabelCap, delegate
                    {
                        settings.bratBehaviorMinExpectationLvl = 2;
                        bb1 = this.ExpectationLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption(ExpectationsUtility.ExpectationForOrder(3).LabelCap, delegate
                    {
                        settings.bratBehaviorMinExpectationLvl = 3;
                        bb1 = this.ExpectationLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption(ExpectationsUtility.ExpectationForOrder(4).LabelCap, delegate
                    {
                        settings.bratBehaviorMinExpectationLvl = 4;
                        bb1 = this.ExpectationLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption(ExpectationsUtility.ExpectationForOrder(5).LabelCap, delegate
                    {
                        settings.bratBehaviorMinExpectationLvl = 5;
                        bb1 = this.ExpectationLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0)
                };
                Find.WindowStack.Add(new FloatMenu(list));
            }
        }
        private string ExpectationLabel
        {
            get
            {
                return settings.bratBehaviorMinExpectationLvl > 5 ? "HVMP_Inactive".Translate() : ExpectationsUtility.ExpectationForOrder(settings.bratBehaviorMinExpectationLvl).LabelCap;
            }
        }
        private void SeniorityLevelSelector(Rect rect)
        {
            if (Widgets.ButtonText(rect, "HVMP_TooltipMinSeniorityLevel".Translate(bb2), true, true, true, null))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>
                {
                    new FloatMenuOption("HVMP_Inactive".Translate(), delegate
                    {
                        settings.bratBehaviorMinSeniorityLvl = 99999;
                        bb2 = this.SeniorityLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption("HVMP_LevelFriend".Translate(), delegate
                    {
                        settings.bratBehaviorMinSeniorityLvl = 0;
                        bb2 = this.SeniorityLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption("HVMP_Level".Translate(1), delegate
                    {
                        settings.bratBehaviorMinSeniorityLvl = 100;
                        bb2 = this.SeniorityLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption("HVMP_Level".Translate(2), delegate
                    {
                        settings.bratBehaviorMinSeniorityLvl = 200;
                        bb2 = this.SeniorityLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption("HVMP_Level".Translate(3), delegate
                    {
                        settings.bratBehaviorMinSeniorityLvl = 300;
                        bb2 = this.SeniorityLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption("HVMP_Level".Translate(4), delegate
                    {
                        settings.bratBehaviorMinSeniorityLvl = 400;
                        bb2 = this.SeniorityLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption("HVMP_Level".Translate(5), delegate
                    {
                        settings.bratBehaviorMinSeniorityLvl = 500;
                        bb2 = this.SeniorityLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    new FloatMenuOption("HVMP_Level".Translate(6), delegate
                    {
                        settings.bratBehaviorMinSeniorityLvl = 600;
                        bb2 = this.SeniorityLabel;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0)
                };
                Find.WindowStack.Add(new FloatMenu(list));
            }
        }
        private string SeniorityLabel
        {
            get
            {
                return settings.bratBehaviorMinSeniorityLvl > 800 ? "HVMP_Inactive".Translate() : "HVMP_Level".Translate(settings.bratBehaviorMinSeniorityLvl/100);
            }
        }
        private void ParseInput(string buffer, float origValue, out float newValue, float min, float max)
        {
            if (!float.TryParse(buffer, out newValue))
                newValue = origValue;
            if (newValue < min)
                newValue = min;
            if (newValue > max)
                newValue = max;
        }
        private void ParseInput(string buffer, int origValue, out int newValue, int min, int max)
        {
            if (!int.TryParse(buffer, out newValue))
                newValue = origValue;
            if (newValue < min)
                newValue = min;
            if (newValue > max)
                newValue = max;
        }
        public override string SettingsCategory()
        {
            return "Hauts' Enterprise: More Permits";
        }
        public static HVMP_Settings settings;
        public string displayMin, displayMax, displayQuestRewardFactor, refusalLoss, failureLoss, bb1, bb2, displayBPI, displayBPLimit, displayBPD, displayAuthCD, displayPASG;
        public Vector2 scrollPosition = Vector2.zero;
    }
}
