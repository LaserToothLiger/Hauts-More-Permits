using RimWorld;
using RimWorld.Planet;
using System;
using Verse;
using Verse.Sound;

namespace HautsPermits_Occult
{
    //spawns a ghoul that has a bunch of powerful buffs in its startingHediffs, a big budget for up to 15 artificial body parts, and... well, it's not necesarily hostile to you.
    public class IncidentWorker_SuperGhoul : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 spawnCenter = parms.spawnCenter;
            if (!spawnCenter.IsValid && !RCellFinder.TryFindRandomSpotJustOutsideColony(parms.spawnCenter, map, out spawnCenter))
            {
                return false;
            }
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(HVMPDefOf_A.HVMP_GhoulSuperEvil, Faction.OfEntities, PawnGenerationContext.NonPlayer, new PlanetTile?(map.Tile), false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
            pawn.health.overrideDeathOnDownedChance = 0f;
            if (AnomalyIncidentUtility.IncidentShardChance(parms.points))
            {
                AnomalyIncidentUtility.PawnShardOnDeath(pawn);
            }
            GenSpawn.Spawn(pawn, spawnCenter, map);
            FleckMaker.Static(pawn.Position, map, FleckDefOf.PsycastSkipInnerExit, 1f);
            FleckMaker.Static(pawn.Position, map, FleckDefOf.PsycastSkipOuterRingExit, 1f);
            SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(pawn.Position, map, false));
            //LordMaker.MakeNewLord(parms.faction, new LordJob_AssaultColony(Faction.OfEntities, false, false, false, false, false, false, false), map, Gen.YieldSingle<Pawn>(pawn));
            base.SendStandardLetter(parms, pawn, Array.Empty<NamedArgument>());
            return true;
        }
    }
}
