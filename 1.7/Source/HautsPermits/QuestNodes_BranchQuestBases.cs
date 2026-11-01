using HautsFramework;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace HautsPermits
{
    /*each of these nodes specifies a handful of generally useful slate refs for a Branch quest. You should use them (or derivatives thereof) as the root of any periodically issued branch quest.
     * Sets "asker" as the faction's leader, "faction" as the branch faction in question, "map" as a player map and pTile as its planet tile (see QuestSetupUtility in the Framework).
     * Also puts in a QuestPart_BranchGoodwillFailureHandler to ensure failure imposes a goodwill penalty in accordance with the mod settings.
     * Also also adjusts the standing/goodwill reward magnitudes by the questRewardFactor mod setting.*/
    public class QuestNode_CommerceIntermediary : QuestNode_Sequence
    {
        protected override void RunInt()
        {
            if (BranchQuestSetupUtility.TryFindCommerceFaction(out Faction commerceFaction))
            {
                Slate slate = QuestGen.slate;
                slate.Set<Thing>("asker", commerceFaction.leader, false);
                slate.Set<Faction>("faction", commerceFaction, false);
                Map map = QuestSetupUtility.Quest_TryGetMap();
                slate.Set<Map>("map", map, false);
                PlanetTile tile = QuestSetupUtility.Quest_TryGetPlanetTile();
                slate.Set<PlanetTile>("pTile", tile, false);
                QuestPart_BranchGoodwillFailureHandler qpbgfh = new QuestPart_BranchGoodwillFailureHandler();
                qpbgfh.faction = commerceFaction;
                QuestGen.quest.AddPart(qpbgfh);
                BranchQuestSetupUtility.SetSettingScalingRewardValue(slate);
            }
            base.RunInt();
        }
        protected override bool TestRunInt(Slate slate)
        {
            BranchQuestSetupUtility.SetSettingScalingRewardValue(slate);
            return BranchQuestSetupUtility.TryFindCommerceFaction(out Faction commerceFaction) && base.TestRunInt(slate);
        }
    }
    public class QuestNode_PaxIntermediary : QuestNode_Sequence
    {
        protected override void RunInt()
        {
            if (BranchQuestSetupUtility.TryFindPaxFaction(out Faction paxFaction))
            {
                Slate slate = QuestGen.slate;
                slate.Set<Thing>("asker", paxFaction.leader, false);
                QuestGen.slate.Set<Faction>("faction", paxFaction, false);
                Map map = QuestSetupUtility.Quest_TryGetMap();
                slate.Set<Map>("map", map, false);
                PlanetTile tile = QuestSetupUtility.Quest_TryGetPlanetTile();
                slate.Set<PlanetTile>("pTile", tile, false);
                QuestPart_BranchGoodwillFailureHandler qpbgfh = new QuestPart_BranchGoodwillFailureHandler();
                qpbgfh.faction = paxFaction;
                QuestGen.quest.AddPart(qpbgfh);
                BranchQuestSetupUtility.SetSettingScalingRewardValue(slate);
            }
            base.RunInt();
        }
        protected override bool TestRunInt(Slate slate)
        {
            BranchQuestSetupUtility.SetSettingScalingRewardValue(slate);
            return BranchQuestSetupUtility.TryFindPaxFaction(out Faction paxFaction) && base.TestRunInt(slate);
        }
    }
    public class QuestNode_RoverIntermediary : QuestNode_Sequence
    {
        protected override void RunInt()
        {
            if (BranchQuestSetupUtility.TryFindRoverFaction(out Faction roverFaction))
            {
                Slate slate = QuestGen.slate;
                slate.Set<Thing>("asker", roverFaction.leader, false);
                QuestGen.slate.Set<Faction>("faction", roverFaction, false);
                Map map = QuestSetupUtility.Quest_TryGetMap();
                slate.Set<Map>("map", map, false);
                PlanetTile tile = QuestSetupUtility.Quest_TryGetPlanetTile();
                slate.Set<PlanetTile>("pTile", tile, false);
                QuestPart_BranchGoodwillFailureHandler qpbgfh = new QuestPart_BranchGoodwillFailureHandler();
                qpbgfh.faction = roverFaction;
                QuestGen.quest.AddPart(qpbgfh);
                BranchQuestSetupUtility.SetSettingScalingRewardValue(slate);
            }
            base.RunInt();
        }
        protected override bool TestRunInt(Slate slate)
        {
            BranchQuestSetupUtility.SetSettingScalingRewardValue(slate);
            return BranchQuestSetupUtility.TryFindRoverFaction(out Faction roverFaction) && base.TestRunInt(slate);
        }
    }
}
