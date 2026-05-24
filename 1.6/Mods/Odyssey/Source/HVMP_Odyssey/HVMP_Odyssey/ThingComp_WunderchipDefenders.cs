using HautsPermits;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace HVMP_Odyssey
{
    //spawn in defenders for a wunderchip quest structure, based on the difficulty mod setting. No need to save the group maker since this should only fire once
    public class CompProperties_WunderchipDefenders : CompProperties
    {
        public CompProperties_WunderchipDefenders()
        {
            this.compClass = typeof(CompWunderchipDefenders);
        }
        public float pointsPer1xDifficulty;
        public List<PawnGroupMaker> pawnGroupMakers;
        public List<ThingDef> spawnByTheseInsteadOfThis;
    }
    public class CompWunderchipDefenders : ThingComp
    {
        public CompProperties_WunderchipDefenders Props
        {
            get
            {
                return (CompProperties_WunderchipDefenders)this.props;
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                this.SpawnPawns();
            }
        }
        public void SpawnPawns()
        {
            List<Pawn> pawns = this.GetPawnsForPoints(this.parent.Map);
            List<Thing> possibleSpawns = this.Props.spawnByTheseInsteadOfThis.NullOrEmpty() ? null : this.parent.Map.listerThings.AllThings.Where((Thing t) => this.Props.spawnByTheseInsteadOfThis.Contains(t.def)).ToList();
            if (pawns.Count > 0 && this.parent.Spawned)
            {
                foreach (Pawn pawn in pawns)
                {
                    Thing toSpawnAt = this.parent;
                    if (!possibleSpawns.NullOrEmpty())
                    {
                        toSpawnAt = possibleSpawns.RandomElement();
                    }
                    IntVec3 randomCell = CellFinder.StandableCellNear(toSpawnAt.Position, this.parent.Map, 8f, null);
                    GenSpawn.Spawn(pawn, randomCell, this.parent.Map, WipeMode.Vanish);
                }
                Lord lord = LordMaker.MakeNewLord(this.parent.Faction??Faction.OfAncientsHostile, new LordJob_DefendPoint(this.parent.Position, 12f, 65f, false, false), this.parent.Map, null);
                lord.AddPawns(pawns);
            }
        }
        public List<Pawn> GetPawnsForPoints(Map map)
        {
            List<Pawn> pawns = new List<Pawn>();
            if (this.groupMaker == null)
            {
                this.groupMaker = this.Props.pawnGroupMakers.RandomElement();
            }
            float points = this.Props.pointsPer1xDifficulty * HVMP_Mod.settings.wunderQuestDifficulty;
            while (points > 0)
            {
                PawnGenOption pgo = this.groupMaker.options.RandomElementByWeight((PawnGenOption option) => option.selectionWeight);
                pawns.Add(this.GeneratePawn(pgo.kind));
                points -= pgo.Cost;
            }
            return pawns;
        }
        public Pawn GeneratePawn(PawnKindDef kind)
        {
            PawnGenerationRequest pawnGenerationRequest = new PawnGenerationRequest(kind, this.parent.Faction??Faction.OfAncientsHostile, PawnGenerationContext.NonPlayer, this.parent.Tile, false, false, false, true, true, 1f, false, true, true, false, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false);
            return PawnGenerator.GeneratePawn(pawnGenerationRequest);
        }
        public PawnGroupMaker groupMaker;
    }
}
