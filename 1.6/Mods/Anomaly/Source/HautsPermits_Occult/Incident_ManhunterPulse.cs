using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;

namespace HautsPermits_Occult
{
    //animal pulser activation effect, basically
    public class IncidentWorker_ManhunterPulse : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
            {
                if ((p.Faction == null || p.Faction != Faction.OfPlayer) && p.IsAnimal && p.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon && !p.InAggroMentalState)
                {
                    return true;
                }
            }
            return false;
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            int insaneAnimals = 0;
            SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera((Map)parms.target);
            List<Pawn> pawns = map.mapPawns.AllPawnsSpawned.ToList();
            for (int i = pawns.Count - 1; i >= 0; i--)
            {
                Pawn p = pawns[i];
                if ((p.Faction == null || p.Faction != Faction.OfPlayer) && p.IsAnimal && p.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon && (insaneAnimals <= 2 || Rand.Chance(0.9f)))
                {
                    if (!p.Awake())
                    {
                        RestUtility.WakeUp(p, true);
                    }
                    p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, null, false, false, false, null, false, false, false);
                    insaneAnimals++;
                }
            }
            base.SendStandardLetter("HVMP_LovecraftManhunterLabel".Translate(), "HVMP_LovecraftManhunterText".Translate(), LetterDefOf.ThreatBig, parms, LookTargets.Invalid, Array.Empty<NamedArgument>());
            return true;
        }
    }
}
