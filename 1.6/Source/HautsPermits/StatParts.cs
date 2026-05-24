using RimWorld;
using Verse;

namespace HautsPermits
{
    /*to avoid conflicts with other mods that impose their own favor values on certain items, we use a different stat which overrides favor value ONLY while trading with an e-branch.
     * This is partly to avoid issues with non-conditional patches from other mods, which would demand a particular load order for a trivially fixable issue,
     * but more to the point it's to avoid me overriding their VisionTM of their mod's permit acquisition, and vice versa.*/
    public class StatPart_BranchStanding : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (this.ShouldWork(req))
            {
                val = req.Thing.GetStatValue(HVMPDefOf.HVMP_StandingValue);
            }
        }
        public override string ExplanationPart(StatRequest req)
        {
            return null;
        }
        public bool ShouldWork(StatRequest req)
        {
            if (TradeSession.trader != null)
            {
                Faction f = TradeSession.trader.Faction;
                if (f != null && f.def.HasModExtension<EBranchQuests>())
                {
                    return req.Thing != null;
                }
            }
            return false;
        }
    }
    /*if requiredCondition is active in the world game condition manager, apply offset and factor.
     * Non-flesh pawns are immune if !worksOnMechs. Similar flags exist for anomaly mutants and unnatural entities.*/
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
                if (this.offset > 0)
                {
                    explanation += this.requiredCondition.LabelCap + ": +" + this.offset.ToStringPercent() + "\n";
                } else if (this.offset < 0) {
                    explanation += this.requiredCondition.LabelCap + ": " + this.offset.ToStringPercent() + "\n";
                }
                if (this.factor != 1)
                {
                    explanation += this.requiredCondition.LabelCap + ": x" + this.factor.ToStringPercent();
                }
                return explanation;
            }
            return null;
        }
        public bool ShouldWork(StatRequest req)
        {
            return req.HasThing && req.Thing is Pawn p && Find.World.gameConditionManager.ConditionIsActive(this.requiredCondition);
        }
        public float offset;
        public float factor = 1f;
        public GameConditionDef requiredCondition;
    }
}
