using HautsFramework;
using HautsPermits;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace HautsPermits_Occult
{
    //the Occult equivalent of Commerce|Pax|RoverIntermediary (see QuestNodes_BranchQuestBases.cs in the core directory)
    public class QuestNode_OccultIntermediary : QuestNode_Sequence
    {
        protected override void RunInt()
        {
            if (BranchQuestSetupUtility.TryFindOccultFaction(out Faction occultFaction))
            {
                Slate slate = QuestGen.slate;
                slate.Set<Thing>("asker", occultFaction.leader, false);
                slate.Set<Faction>("faction", occultFaction, false);
                Map map = QuestSetupUtility.Quest_TryGetMap();
                slate.Set<Map>("map", map, false);
                PlanetTile tile = QuestSetupUtility.Quest_TryGetPlanetTile();
                slate.Set<PlanetTile>("pTile", tile, false);
                QuestPart_BranchGoodwillFailureHandler qpbgfh = new QuestPart_BranchGoodwillFailureHandler();
                qpbgfh.faction = occultFaction;
                QuestGen.quest.AddPart(qpbgfh);
                BranchQuestSetupUtility.SetSettingScalingRewardValue(slate);
            }
            base.RunInt();
        }
        protected override bool TestRunInt(Slate slate)
        {
            BranchQuestSetupUtility.SetSettingScalingRewardValue(slate);
            return BranchQuestSetupUtility.TryFindOccultFaction(out Faction occultFaction) && base.TestRunInt(slate);
        }
    }
}
