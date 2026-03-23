using HautsPermits;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HautsPermits_Ideology
{
    /*these workers are permit authorizer friendly as usual. If you don't know what this means, go read up.
     * unfogs the target cell (plus adjacent cells, which means if you hit a wall of a room, it'll still unfog adjacent rooms*/
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_ScannerSweep : RoyalTitlePermitWorker_Targeted
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.Reveal(target.Cell, this.caller.Map);
        }
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            bool free;
            if (PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free))
            {
                action = delegate
                {
                    this.BeginReveal(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            yield break;
        }
        private void BeginReveal(Pawn caller, Faction faction, Map map, bool free)
        {
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetPawns = false;
            this.caller = caller;
            this.map = map;
            this.faction = faction;
            this.free = free;
            this.targetingParameters.validator = (TargetInfo target) => (this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(caller.Position) <= this.def.royalAid.targetingRange);
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }
        private void Reveal(IntVec3 cell, Map map)
        {
            FloodFillerFog.FloodUnfog(cell, map);
            FogGrid fg = map.fogGrid;
            foreach (IntVec3 iv3 in GenRadial.RadialCellsAround(cell, 1.9f, true))
            {
                fg.Unfog(iv3);
            }
            Messages.Message("HVMP_ScannerSweep".Translate(this.faction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
            this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
            }
            PermitAuthorizerUtility.DoPTargeterCooldown(this.faction, this.caller, this);
        }
        private Faction faction;
    }
    //generates a deep drillable lump
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_ScannerDeep : RoyalTitlePermitWorker_Targeted
    {
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            bool free;
            if (PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free))
            {
                action = delegate
                {
                    this.MakeCondition(pawn, faction, new IncidentParms(), this.free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            yield break;
        }
        protected void MakeCondition(Pawn caller, Faction faction, IncidentParms parms, bool free)
        {
            if (caller.Spawned)
            {
                Map map = caller.Map;
                IntVec3 intVec;
                if (!CellFinderLoose.TryFindRandomNotEdgeCellWith(10, (IntVec3 x) => this.CanScatterAt(x, map), map, out intVec))
                {
                    Log.Error("Could not find a center cell for deep scanning lump generation!");
                }
                ThingDef thingDef = DefDatabase<ThingDef>.AllDefs.RandomElementByWeight((ThingDef def) => def.deepCommonality);
                int num = Mathf.CeilToInt((float)thingDef.deepLumpSizeRange.RandomInRange);
                foreach (IntVec3 intVec2 in GridShapeMaker.IrregularLump(intVec, map, num, null))
                {
                    if (this.CanScatterAt(intVec2, map) && !intVec2.InNoBuildEdgeArea(map))
                    {
                        map.deepResourceGrid.SetAt(intVec2, thingDef, thingDef.deepCountPerCell);
                    }
                }
                Find.LetterStack.ReceiveLetter("LetterLabelDeepScannerFoundLump".Translate() + ": " + thingDef.LabelCap, "HVMP_ScannerDeep".Translate(thingDef.label, faction.Named("FACTION")), LetterDefOf.PositiveEvent, new LookTargets(intVec, map), null, null, null, null, 0, true);
                caller.royalty.GetPermit(this.def, faction).Notify_Used();
                if (!this.free)
                {
                    caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
                }
                PermitAuthorizerUtility.DoPTargeterCooldown(faction, caller, this);
            }
        }
        private bool CanScatterAt(IntVec3 pos, Map map)
        {
            int num = CellIndicesUtility.CellToIndex(pos, map.Size.x);
            TerrainDef terrainDef = map.terrainGrid.TerrainAt(num);
            return (terrainDef == null || !terrainDef.IsWater || terrainDef.passability != Traversability.Impassable) && terrainDef.affordances.Contains(ThingDefOf.DeepDrill.terrainAffordanceNeeded) && !map.deepResourceGrid.GetCellBool(num);
        }
    }
    //Revealing Scan instantiates this condition, which disrupts all invisibility (and reduces HediffComp_Disappears timer to 0, if applicable) every 60s
    public class GameCondition_RevealingScan : GameCondition
    {
        public override void Init()
        {
            base.Init();
            this.RevealAll();
            this.ticks = 60;
        }
        public override void GameConditionTick()
        {
            this.ticks--;
            if (this.ticks <= 0)
            {
                this.RevealAll();
                this.ticks = 60;
            }
        }
        public void RevealAll()
        {
            foreach (Map m in base.AffectedMaps)
            {
                foreach (Pawn p in m.mapPawns.AllPawnsSpawned)
                {
                    foreach (Hediff h in p.health.hediffSet.hediffs)
                    {
                        HediffComp_Invisibility hci = h.TryGetComp<HediffComp_Invisibility>();
                        if (hci != null)
                        {
                            hci.DisruptInvisibility();
                            HediffComp_Disappears hcd = h.TryGetComp<HediffComp_Disappears>();
                            if (hcd != null)
                            {
                                hcd.ticksToDisappear = 0;
                            }
                        }
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.ticks, "ticks", 0, false);
        }
        public int ticks;
    }
}
