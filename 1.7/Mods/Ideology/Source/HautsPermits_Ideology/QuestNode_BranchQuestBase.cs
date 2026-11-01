using HautsFramework;
using HautsPermits;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace HautsPermits_Ideology
{
    //the Archive equivalent of Commerce|Pax|RoverIntermediary (see QuestNodes_BranchQuestBases.cs in the core directory)
    public class QuestNode_ArchiveIntermediary : QuestNode_Sequence
    {
        protected override void RunInt()
        {
            if (BranchQuestSetupUtility.TryFindArchiveFaction(out Faction faction))
            {
                Slate slate = QuestGen.slate;
                slate.Set<Thing>("asker", faction.leader, false);
                QuestGen.slate.Set<Faction>("faction", faction, false);
                Map map = QuestSetupUtility.Quest_TryGetMap();
                slate.Set<Map>("map", map, false);
                PlanetTile tile = QuestSetupUtility.Quest_TryGetPlanetTile();
                slate.Set<PlanetTile>("pTile", tile, false);
                QuestPart_BranchGoodwillFailureHandler qpbgfh = new QuestPart_BranchGoodwillFailureHandler();
                qpbgfh.faction = faction;
                QuestGen.quest.AddPart(qpbgfh);
                BranchQuestSetupUtility.SetSettingScalingRewardValue(slate);
            }
            base.RunInt();
        }
        protected override bool TestRunInt(Slate slate)
        {
            BranchQuestSetupUtility.SetSettingScalingRewardValue(slate);
            return BranchQuestSetupUtility.TryFindArchiveFaction(out Faction archiveFaction) && base.TestRunInt(slate);
        }
    }
}
