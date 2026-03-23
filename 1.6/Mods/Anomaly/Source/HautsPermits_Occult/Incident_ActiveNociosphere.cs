using RimWorld;
using RimWorld.Planet;
using System;
using Verse;

namespace HautsPermits_Occult
{
    //generates a nociosphere, using a custom pawn kind def that activates after a few seconds due to one of its startingHediffs
    public class IncidentWorker_PrimedNociosphere : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 spawnCenter = parms.spawnCenter;
            if (!spawnCenter.IsValid && !RCellFinder.TryFindRandomSpotJustOutsideColony(parms.spawnCenter, map, out spawnCenter))
            {
                return false;
            }
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(HVMPDefOf_A.HVMP_PrimedNociosphere, Faction.OfEntities, PawnGenerationContext.NonPlayer, new PlanetTile?(map.Tile), false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
            NociosphereUtility.SkipTo((Pawn)GenSpawn.Spawn(pawn, spawnCenter, map, WipeMode.Vanish), spawnCenter);
            base.SendStandardLetter(parms, pawn, Array.Empty<NamedArgument>());
            return true;
        }
    }
}
