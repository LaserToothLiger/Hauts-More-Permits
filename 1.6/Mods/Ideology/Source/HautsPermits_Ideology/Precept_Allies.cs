using RimWorld;
using Verse;

namespace HautsPermits_Ideology
{
    public class ThoughtWorker_Precept_NetAllyToFoeCount : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            int num = ThoughtWorker_Precept_NetAllyToFoeCount.NetAllyCount(p);
            if (num > 0)
            {
                return ThoughtState.ActiveAtStage(0);
            } else if (num < 0) {
                return ThoughtState.ActiveAtStage(1);
            } else {
                return ThoughtState.Inactive;
            }
        }
        public static int NetAllyCount(Pawn p)
        {
            int num = 0;
            if (p.Faction != null)
            {
                Faction pawnFac = p.Faction;
                foreach (Faction f in Find.FactionManager.AllFactionsVisible)
                {
                    if (f != pawnFac)
                    {
                        if (pawnFac.RelationKindWith(f) == FactionRelationKind.Ally)
                        {
                            num++;
                        } else if (pawnFac.RelationKindWith(f) == FactionRelationKind.Hostile) {
                            num--;
                        }
                    }
                }
            }
            return num;
        }
    }
}
