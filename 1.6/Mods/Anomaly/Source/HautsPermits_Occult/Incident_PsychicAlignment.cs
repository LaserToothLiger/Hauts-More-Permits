using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HautsPermits_Occult
{
    //create this incident and play a psychic sound
    public class IncidentWorker_Align : IncidentWorker_MakeGameCondition
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            this.DoConditionAndLetter(parms, map, Mathf.RoundToInt(this.def.durationDays.RandomInRange * 60000f), parms.points);
            SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera((Map)parms.target);
            return true;
        }
        protected void DoConditionAndLetter(IncidentParms parms, Map map, int duration, float points)
        {
            GameCondition gc = GameConditionMaker.MakeCondition(this.def.gameCondition, duration);
            map.gameConditionManager.RegisterCondition(gc);
            base.SendStandardLetter(gc.LabelCap, gc.LetterText, gc.def.letterDef, parms, LookTargets.Invalid, Array.Empty<NamedArgument>());
        }
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return true;
        }
    }
    public class GameCondition_Alignment : GameCondition
    {
        public override int TransitionTicks
        {
            get
            {
                return 60000;
            }
        }
    }
    //xpathed into Psychic Ritual Quality Offset. apply this offset and this factor while Psychic Alignment is active on the pawn's map
    public class StatPart_PsychicAlignment : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing && req.Thing is Pawn p && p.MapHeld != null && p.MapHeld.gameConditionManager.ConditionIsActive(HVMPDefOf_A.HVMP_LovecraftAlignmentCond))
            {
                val += this.offset;
                val *= this.factor;
            }
        }
        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing && req.Thing is Pawn p && p.MapHeld != null && p.MapHeld.gameConditionManager.ConditionIsActive(HVMPDefOf_A.HVMP_LovecraftAlignmentCond))
            {
                string explanation = HVMPDefOf_A.HVMP_LovecraftAlignmentCond.LabelCap + ": +" + this.offset.ToStringPercent() + ", x" + this.factor.ToStringPercent();
                return explanation;
            }
            return null;
        }
        public float offset;
        public float factor = 1f;
    }
}
