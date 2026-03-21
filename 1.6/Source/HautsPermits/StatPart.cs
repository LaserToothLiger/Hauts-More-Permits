using RimWorld;
using Verse;

namespace HautsPermits
{
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
}
