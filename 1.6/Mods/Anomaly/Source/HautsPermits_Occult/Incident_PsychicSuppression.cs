using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HautsPermits_Occult
{
    //instantiates a Psychic Suppression (the completely normal, non-modded game condition) and assigns a gender to it. Just using a normal incident worker to do this does not assign a gender
    public class IncidentWorker_Suppression : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return !map.gameConditionManager.ConditionIsActive(GameConditionDefOf.PsychicSuppression) && map.mapPawns.FreeColonistsCount != 0;
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            this.DoConditionAndLetter(parms, map, Mathf.RoundToInt(this.def.durationDays.RandomInRange * 60000f), map.mapPawns.FreeColonists.RandomElement<Pawn>().gender, parms.points);
            SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera((Map)parms.target);
            return true;
        }
        protected void DoConditionAndLetter(IncidentParms parms, Map map, int duration, Gender gender, float points)
        {
            if (points < 0f)
            {
                points = StorytellerUtility.DefaultThreatPointsNow(map);
            }
            GameCondition_PsychicSuppression gameCondition = (GameCondition_PsychicSuppression)GameConditionMaker.MakeCondition(GameConditionDefOf.PsychicSuppression, duration);
            gameCondition.gender = gender;
            map.gameConditionManager.RegisterCondition(gameCondition);
            base.SendStandardLetter(gameCondition.LabelCap, gameCondition.LetterText, gameCondition.def.letterDef, parms, LookTargets.Invalid, Array.Empty<NamedArgument>());
        }
    }
}
