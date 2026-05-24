using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HautsPermits
{
    //like Farskip, but it only affects the caster. Needs some serious testing to ensure I haven't absolutely screwed the pooch on handling teleporting out of a caravan; that function is disabled until I give enough shits to conduct this testing
    public class CompAbilityEffect_FarNetskip : CompAbilityEffect
    {
        private new CompProperties_AbilityFarskip Props
        {
            get
            {
                return (CompProperties_AbilityFarskip)this.props;
            }
        }
        public override void Apply(GlobalTargetInfo target)
        {
            Caravan caravan = this.parent.pawn.GetCaravan();
            MapParent mapParent = target.WorldObject as MapParent;
            Map targetMap = ((mapParent != null) ? mapParent.Map : null);
            IntVec3 targetCell = IntVec3.Invalid;
            Pawn pawn = this.parent.pawn;
            if (pawn.Spawned)
            {
                this.parent.AddEffecterToMaintain(EffecterDefOf.Skip_Entry.Spawn(pawn, pawn.Map, 1f), pawn.Position, 60, null);
                SoundDefOf.Psycast_Skip_Pulse.PlayOneShot(new TargetInfo(target.Cell, pawn.Map, false));
            }
            if (this.ShouldEnterMap(target))
            {
                Pawn pawn2 = this.AlliedPawnOnMap(targetMap);
                if (pawn2 != null)
                {
                    targetCell = pawn2.Position;
                } else {
                    targetCell = pawn.Position;
                }
            }
            if (targetCell.IsValid)
            {
                if (pawn.Spawned)
                {
                    pawn.teleporting = true;
                    pawn.ExitMap(false, Rot4.Invalid);
                    AbilityUtility.DoClamor(pawn.Position, (float)this.Props.clamorRadius, pawn, this.Props.clamorType);
                    pawn.teleporting = false;
                }
                int num = 4;
                CellFinder.TryFindRandomSpawnCellForPawnNear(targetCell, targetMap, out IntVec3 intVec, num, (IntVec3 cell) => cell != targetCell && cell.GetRoom(targetMap) == targetCell.GetRoom(targetMap));
                GenSpawn.Spawn(pawn, intVec, targetMap, WipeMode.Vanish);
                if (pawn.drafter != null && pawn.IsColonistPlayerControlled && !pawn.Downed)
                {
                    pawn.drafter.Drafted = true;
                }
                pawn.stances.stunner.StunFor(this.Props.stunTicks.RandomInRange, pawn, false, true, false);
                pawn.Notify_Teleported(true, true);
                CompAbilityEffect_Teleport.SendSkipUsedSignal(pawn, pawn);
                if (pawn.IsPrisoner)
                {
                    pawn.guest.WaitInsteadOfEscapingForDefaultTicks();
                }
                this.parent.AddEffecterToMaintain(EffecterDefOf.Skip_ExitNoDelay.Spawn(pawn, pawn.Map, 1f), pawn.Position, 60, targetMap);
                SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(intVec, pawn.Map, false));
                if ((pawn.IsColonist || pawn.RaceProps.packAnimal || pawn.IsColonyMech) && pawn.Map.IsPlayerHome)
                {
                    pawn.inventory.UnloadEverything = true;
                }
                if (caravan != null && caravan.pawns.Count == 0)
                {
                    caravan.Destroy();
                    return;
                }
            } else {
                Caravan caravan2 = target.WorldObject as Caravan;
                if (caravan2 != null && caravan2.Faction == this.parent.pawn.Faction)
                {
                    if (caravan != null && caravan.pawns.Count == 0)
                    {
                        caravan.pawns.TryTransferToContainer(pawn, caravan2.pawns, true);
                        caravan2.Notify_Merged(new List<Caravan> { caravan });
                        caravan.Destroy();
                        return;
                    }
                    caravan2.AddPawn(pawn, true);
                    pawn.ExitMap(false, Rot4.Invalid);
                    AbilityUtility.DoClamor(pawn.Position, (float)this.Props.clamorRadius, this.parent.pawn, this.Props.clamorType);
                    return;
                }
                if (caravan != null && caravan.pawns.Count == 0)
                {
                    caravan.Tile = target.Tile;
                    caravan.pather.StopDead();
                    return;
                }
                CaravanMaker.MakeCaravan(new List<Pawn>() { pawn }, pawn.Faction, target.Tile, false);
                pawn.ExitMap(false, Rot4.Invalid);
            }
        }
        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            yield return new PreCastAction
            {
                action = delegate (LocalTargetInfo t, LocalTargetInfo d)
                {
                    FleckCreationData dataAttachedOverlay = FleckMaker.GetDataAttachedOverlay(this.parent.pawn, FleckDefOf.PsycastSkipFlashEntry, new Vector3(-0.5f, 0f, -0.5f), 1f, -1f);
                    dataAttachedOverlay.link.detachAfterTicks = 5;
                    this.parent.pawn.Map.flecks.CreateFleck(dataAttachedOverlay);
                },
                ticksAwayFromCast = 5
            };
            yield break;
        }
        private Pawn AlliedPawnOnMap(Map targetMap)
        {
            return targetMap.mapPawns.AllPawnsSpawned.FirstOrDefault((Pawn p) => !p.NonHumanlikeOrWildMan() && p.IsColonist && p.HomeFaction == Faction.OfPlayer && p != this.parent.pawn);
        }
        private bool ShouldEnterMap(GlobalTargetInfo target)
        {
            Caravan caravan = target.WorldObject as Caravan;
            if (caravan != null && caravan.Faction == this.parent.pawn.Faction)
            {
                return false;
            }
            MapParent mapParent = target.WorldObject as MapParent;
            return mapParent != null && mapParent.HasMap && (this.AlliedPawnOnMap(mapParent.Map) != null || mapParent.Map == this.parent.pawn.Map);
        }
        private bool ShouldJoinCaravan(GlobalTargetInfo target)
        {
            Caravan caravan = target.WorldObject as Caravan;
            return caravan != null && caravan.Faction == this.parent.pawn.Faction;
        }
        public override bool Valid(GlobalTargetInfo target, bool throwMessages = false)
        {
            Caravan caravan = this.parent.pawn.GetCaravan();
            if (caravan != null && caravan.ImmobilizedByMass)
            {
                return false;
            }
            Caravan caravan2 = target.WorldObject as Caravan;
            return (caravan == null || caravan != caravan2) && (this.ShouldEnterMap(target) || this.ShouldJoinCaravan(target)) && base.Valid(target, throwMessages);
        }
        public override bool CanApplyOn(GlobalTargetInfo target)
        {
            MapParent mapParent = target.WorldObject as MapParent;
            return (mapParent == null || mapParent.Map == null || this.AlliedPawnOnMap(mapParent.Map) != null) && base.CanApplyOn(target);
        }
        public override string WorldMapExtraLabel(GlobalTargetInfo target)
        {
            Caravan caravan = this.parent.pawn.GetCaravan();
            if (caravan != null && caravan.ImmobilizedByMass)
            {
                return "CaravanImmobilizedByMass".Translate();
            }
            if (!this.Valid(target, false))
            {
                return "AbilityNeedAllyToSkip".Translate();
            }
            if (this.ShouldJoinCaravan(target))
            {
                return "AbilitySkipToJoinCaravan".Translate();
            }
            return "AbilitySkipToRandomAlly".Translate();
        }
    }
}
