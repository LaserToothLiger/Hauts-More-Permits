using HarmonyLib;
using HautsFramework;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace HautsPermits
{
    [StaticConstructorOnStartup]
    public class HautsPermits
    {
        private static readonly Type patchType = typeof(HautsPermits);
        static HautsPermits()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautspermits");
            //regular quests should NOT be given to you be branch factions! No!
            MethodInfo methodInfo = typeof(QuestNode_GetFaction).GetMethod("IsGoodFaction", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo,
                          prefix: new HarmonyMethod(patchType, nameof(HVMPIsGoodFactionPrefix)));
            //No!
            MethodInfo methodInfo1 = typeof(QuestNode_GetPawn).GetMethod("IsGoodPawn", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo1,
                          prefix: new HarmonyMethod(patchType, nameof(HVMPIsGoodPawnPrefix)));
            //NO!
            MethodInfo methodInfo2 = typeof(QuestNode_GetPawn).GetMethod("TryFindFactionForPawnGeneration", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo2,
                          postfix: new HarmonyMethod(patchType, nameof(HVMPTryFindFactionForPawnGenerationPostfix)));
            //No means NO!
            if (ModsConfig.IdeologyActive)
            {
                MethodInfo methodInfo3a = typeof(QuestNode_Root_Mission_AncientComplex).GetMethod("AskerFactionValid", BindingFlags.NonPublic | BindingFlags.Instance);
                harmony.Patch(methodInfo3a,
                              postfix: new HarmonyMethod(patchType, nameof(HVMPAskerFactionValidPostfix)));
            }
            //It's Spanish for NO!!
            if (ModsConfig.BiotechActive)
            {
                MethodInfo methodInfo3b = typeof(QuestNode_Root_PollutionDump).GetMethod("FindAsker", BindingFlags.NonPublic | BindingFlags.Instance);
                harmony.Patch(methodInfo3b,
                              postfix: new HarmonyMethod(patchType, nameof(HVMPFindAskerPostfix)));
            }
            //Why don't these all have a shared basis!?
            if (ModsConfig.AnomalyActive)
            {
                MethodInfo methodInfo3c = typeof(QuestNode_Root_MysteriousCargo).GetMethod("FindAsker", BindingFlags.NonPublic | BindingFlags.Instance);
                harmony.Patch(methodInfo3c,
                              postfix: new HarmonyMethod(patchType, nameof(HVMPFindAskerPostfix)));
            }
            //Absolutely, freakingge,, notte!!!
            MethodInfo methodInfo4 = typeof(QuestNode_Root_Mission_BanditCamp).GetMethod("GetAsker", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo4,
                          postfix: new HarmonyMethod(patchType, nameof(HVMPFindAskerPostfix)));
            /*instead of normal comms console dialog options, ringing up a branch faction gives you three options
             * -solicit a random quest from their random quest pool (shares 3d cooldown between all branch factions)
             * -open a trade window with their donationTraderKind, allowing you to donate them items for standing (requires a title with that branch)
             * -refresh the caller's cooldowns for their permits with that branch (requires having any such permits, and you must be allies with that branch)*/
            harmony.Patch(AccessTools.Method(typeof(FactionDialogMaker), nameof(FactionDialogMaker.FactionDialogFor)),
                          prefix: new HarmonyMethod(patchType, nameof(HVMPFactionDialogForPrefix)));
            //makes donations offices always capable of trading with you. Also makes you unable to trade with any trader pawn if the Communications Blackout condition is in effect*/
            harmony.Patch(AccessTools.Property(typeof(Pawn_TraderTracker), nameof(Pawn_TraderTracker.CanTradeNow)).GetGetMethod(),
                           prefix: new HarmonyMethod(patchType, nameof(HVMPCanTradeNowPrefix)));
            //Communications Blackout also prevents you from trading with settlements
            harmony.Patch(AccessTools.Property(typeof(Settlement), nameof(Settlement.CanTradeNow)).GetGetMethod(),
                           prefix: new HarmonyMethod(patchType, nameof(HVMP_Settlement_CanTradeNowPrefix)));
            //enables donations offices to be able to receive donations from your stockpiles/storage
            MethodInfo methodInfo5 = typeof(TradeDeal).GetMethod("AddAllTradeables", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo5,
                          prefix: new HarmonyMethod(patchType, nameof(HVMPAddAllTradeablesPrefix)));
            //reduces the cooldown of requesting military aid form an allied faction if the negotiator or the aiding faction believe in the Cosmopolitan meme's Interfaction Aid precept
            if (ModsConfig.IdeologyActive)
            {
                harmony.Patch(AccessTools.Method(typeof(IncidentWorker_RaidFriendly), nameof(IncidentWorker_RaidFriendly.ResolveRaidStrategy)),
                              postfix: new HarmonyMethod(patchType, nameof(HVMPResolveRaidStrategyPostfix)));
            }
            //when a trade caravan or orbital trader would arrive, a stack of the Mastermind quest effect is consumed to block it instead
            harmony.Patch(AccessTools.Method(typeof(IncidentWorker), nameof(IncidentWorker.TryExecute)),
                           prefix: new HarmonyMethod(patchType, nameof(HVMPTryExecutePrefix)));
            //you don't lose goodwill for damaging another faction's building or pawn if its def (or kinddef, for pawns) has the HVMP_ItsOkToHarmThisDME
            harmony.Patch(AccessTools.Method(typeof(Faction), nameof(Faction.Notify_BuildingTookDamage)),
                          prefix: new HarmonyMethod(patchType, nameof(HVMPNotify_BuildingTookDamagePrefix)));
            harmony.Patch(AccessTools.Method(typeof(Faction), nameof(Faction.Notify_MemberTookDamage)),
                          prefix: new HarmonyMethod(patchType, nameof(HVMPNotify_MemberTookDamagePrefix)));
            /*obtains the goodwill difference before and after a TryAffectGoodwillWith;
             *   if it's got a reason other than "movement towards natural goodwill", and it's not goodwill with a branch faction, then feed that into the oldest Mundi quest's net goodwill tracker.
             * Also, reduces the cooldown of requesting a trader from an allied faction if the negotiator or the aiding faction have the Interfaction Aid precept*/
            harmony.Patch(AccessTools.Method(typeof(Faction), nameof(Faction.TryAffectGoodwillWith)),
                          prefix: new HarmonyMethod(patchType, nameof(HVMPTryAffectGoodwillWithPrefix)));
            harmony.Patch(AccessTools.Method(typeof(Faction), nameof(Faction.TryAffectGoodwillWith)),
                          postfix: new HarmonyMethod(patchType, nameof(HVMPTryAffectGoodwillWithPostfix)));
            /*if you attack the settlement (or rather, branch platform) of one branch faction, they all become hostile.
             * I don't know if I've mentioned this, but the only reason they're different factions in the first place is because royal titles can only be organized in a strictly linear hierarchy per faction;
             * they're truly supposed to be "branches" of one overarching faction, just with different permit "skill trees"*/
            harmony.Patch(AccessTools.Method(typeof(SettlementUtility), nameof(SettlementUtility.AffectRelationsOnAttacked)),
                          postfix: new HarmonyMethod(patchType, nameof(HVMPAffectRelationsOnAttackedPostfix)));
            //branch factions are immune to being defeated by the destruction of their last settlement
            harmony.Patch(AccessTools.Method(typeof(SettlementDefeatUtility), nameof(SettlementDefeatUtility.CheckDefeated)),
                          prefix: new HarmonyMethod(patchType, nameof(HVMPCheckDefeatedPrefix)));
            //the usage of biofilm medicine to tend tends all tendable hediffs on the pawn
            if (ModsConfig.OdysseyActive)
            {
                harmony.Patch(AccessTools.Method(typeof(TendUtility), nameof(TendUtility.DoTend)),
                              postfix: new HarmonyMethod(patchType, nameof(HVMPDoTendPostfix)));
            }
            /*right after the game starts, destroy all initially-loaded non-branch platform world objects belonging to branch factions (i.e. the settlements the game plops down for them)
             * and, in case something prevented them from being generated, add them into the game*/
            harmony.Patch(AccessTools.Method(typeof(WorldComponent_HautsFactionComps), nameof(WorldComponent_HautsFactionComps.ThirdTickEffects)),
                          postfix: new HarmonyMethod(patchType, nameof(HVMPThirdTickEffectsPostfix)));
            //if you turned off permit rank-scaling, subs out the descriptions of all permits whose effects scale with the permit-user's seniority within that faction for more appropriate descriptions
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
                bool isCosmopolitan = CosmopolitanMemeUtility.NegotiatorIsCosmopolitan(negotiator);
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
                                    slate0.Set<float>("points", BranchQuestSetupUtility.TryGetPoints(negotiator), false);
                                    Map maplike = QuestSetupUtility.Quest_TryGetMap();
                                    slate0.Set<Map>("map", maplike, false);
                                    PlanetTile tile = QuestSetupUtility.Quest_TryGetPlanetTile();
                                    if (qsd.CanRun(slate0, map))
                                    {
                                        qsds.Add(qsd);
                                    }
                                }
                                if (qsds.Count > 0)
                                {
                                    QuestScriptDef questDef = qsds.RandomElement();
                                    Slate slate = new Slate();
                                    slate.Set<Faction>("branchFaction", faction, false);
                                    slate.Set<float>("points", 1000f, false);
                                    Map maplike = QuestSetupUtility.Quest_TryGetMap();
                                    slate.Set<Map>("map", maplike, false);
                                    PlanetTile tile = QuestSetupUtility.Quest_TryGetPlanetTile();
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
                    TaggedString taggedString = "HVMP_Donate".Translate(gq.donationString);
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
        public static bool HVMP_Settlement_CanTradeNowPrefix(ref bool __result)
        {
            if (Find.World.gameConditionManager.ConditionIsActive(HVMPDefOf.HVMP_CommsBlackout))
            {
                __result = false;
                return false;
            }
            return true;
        }
        public static bool HVMPAddAllTradeablesPrefix(TradeDeal __instance)
        {
            if (TradeSession.trader is Pawn pawn && pawn.Faction != null)
            {
                EBranchQuests gq = pawn.Faction.def.GetModExtension<EBranchQuests>();
                if (gq != null && gq.donationTraderKind == TradeSession.trader.TraderKind)
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
                        if (CosmopolitanMemeUtility.NegotiatorIsCosmopolitan(pgq.tmpNegotiatorForInterfactionAid) || CosmopolitanMemeUtility.FactionIsCosmopolitan(parms.faction))
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
        public static void HVMPTryAffectGoodwillWithPrefix(Faction __instance, Faction other, out int __state)
        {
            if (__instance == Faction.OfPlayerSilentFail && other != Faction.OfPlayerSilentFail)
            {
                __state = other.GoodwillWith(Faction.OfPlayerSilentFail);
            } else if (__instance != Faction.OfPlayerSilentFail && other == Faction.OfPlayerSilentFail) {
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
            if (other == Faction.OfPlayerSilentFail || __instance == Faction.OfPlayerSilentFail)
            {
                Faction nonPlayerFaction = other != Faction.OfPlayerSilentFail ? other : (__instance != Faction.OfPlayerSilentFail ? __instance : null);
                if (nonPlayerFaction != null && !nonPlayerFaction.def.HasModExtension<EBranchQuests>() && !nonPlayerFaction.def.permanentEnemy && nonPlayerFaction.HasGoodwill && (reason == null || reason != HistoryEventDefOf.ReachNaturalGoodwill))
                {
                    int goodwillChange = nonPlayerFaction.GoodwillWith(Faction.OfPlayerSilentFail) - __state;
                    HautsPermits.PaxMundiInner(goodwillChange);
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
                            if (CosmopolitanMemeUtility.NegotiatorIsCosmopolitan(pgq.tmpNegotiatorForInterfactionAid) || CosmopolitanMemeUtility.FactionIsCosmopolitan(other))
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
            foreach (FactionDef fd in DefDatabase<FactionDef>.AllDefsListForReading)
            {
                if (fd.HasModExtension<EBranchQuests>() && !Find.FactionManager.AllFactionsListForReading.Any((Faction f)=>f.def == fd))
                {
                    FactionGenerator.CreateFactionAndAddToManager(fd);
                }
            }
        }

        //NOT A HARMONY PATCH - handles the assignation of goodwill to the oldest QuestPart_PaxMundi currently active, and calls itself in case of excess goodwill to hand the excess to the next oldest
        public static void PaxMundiInner(int goodwillChange)
        {
            WorldComponent_BranchStuff wcbs = (WorldComponent_BranchStuff)Find.World.GetComponent(typeof(WorldComponent_BranchStuff));
            if (wcbs != null)
            {
                QuestPart_PaxMundi qppm = null;
                if (wcbs.qppms == null)
                {
                    wcbs.qppms = new List<QuestPart_PaxMundi>();
                }
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
                            HautsPermits.PaxMundiInner(leftovers);
                        }
                    }
                }
            }
        }
    }
}
