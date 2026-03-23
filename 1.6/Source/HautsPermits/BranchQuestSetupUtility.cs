using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace HautsPermits
{
    public static class BranchQuestSetupUtility
    {
        //spits out the first faction of the corresponding def
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
        //determine the points that a branch quest should have (used by periodically-assigned branch quests and by those solicited thru the comms console)
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
        //as based on your current mod settings, determines how much goodwill should you lose for letting this quest expire/preventing it from being issued (refusalNotFailure = true) or failing the quest (= false)
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
                            highestExpectationOrder = Math.Max(highestExpectationOrder, ed.order);
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
                        colonists.AddRange(m.mapPawns.AllPawns.Where((Pawn p) => !p.Dead && p.IsColonist));
                    }
                    foreach (Caravan c in Find.WorldObjects.Caravans)
                    {
                        colonists.AddRange(c.PawnsListForReading.Where((Pawn p) => p.IsColonist));
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
        //sets the reward for any given branch quest - this is where the questRewardFactor mod setting takes effect. You can specify a further factor for the output
        public static void SetSettingScalingRewardValue(Slate slate, float factor = 1f)
        {
            slate.Set<float>("rewardValue", factor * Rand.RangeInclusive(350, 700) * HVMP_Mod.settings.questRewardFactor);
        }
        //determines if a specific mutator (whose mod setting is represented here as flag, and whose quest's mayhem mode mod setting is represented here as mayhemMode) should apply to a new instance of the quest
        public static bool MutatorEnabled(bool flag, bool mayhemMode)
        {
            return flag || (mayhemMode && Rand.Chance(0.35f));
        }
        //for quests that hit you with map-targeted game conditions i.e. PAX Caelum, OCCULT Lovecraft... just finds a random player home map if the quest being generated doesn't already have a target map
        public static Map GetMap_QuestNodeGameCondition(Slate slate)
        {
            if (!slate.TryGet<Map>("map", out Map randomPlayerHomeMap, false))
            {
                randomPlayerHomeMap = Find.RandomPlayerHomeMap;
            }
            return randomPlayerHomeMap;
        }
        /*used by ROVER Theseus, as well as a fallback if the mech faction is unable to raid you with ECOSPHERE Environmental Control... preferentially finds a random visible, undefeated, humanlike faction that's hostile to you.
         * If it can't, it begins relaxing those constraints, in that order.*/
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
        //literally just do a caravan Ambush incident at the target with this many points
        public static void DoAmbush(IIncidentTarget target, float points)
        {
            IncidentDef id = DefDatabase<IncidentDef>.GetNamed("Ambush");
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(id.category, target);
            parms.points = points;
            id.Worker.TryExecute(parms);
        }
    }
}
