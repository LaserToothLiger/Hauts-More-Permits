using HautsFramework;
using HautsPermits;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace HautsPermits_Biotech
{
    /*targets an animal, creating a new member of its species that is then deployed to its position via drop pod. Uses PermitMoreEffects
     * -bodySizeCapRange: if specified, target animal's species' base body size must be in this range
     * -hediffs: adds all hediffs in this field to the newly generated pawn
     * -startsTamed: makes the newly generated pawn a member of the permit-user's faction*/
    public class RoyalTitlePermitWorker_DupeAnimal : RoyalTitlePermitWorker_TargetPawn
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            if (pawn.IsAnimal && !pawn.RaceProps.Dryad)
            {
                PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
                if (pme != null && (pme.bodySizeCapRange == null || pme.bodySizeCapRange.Includes(pawn.RaceProps.baseBodySize)))
                {
                    return true;
                }
            }
            return false;
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
            base.AffectPawnInner(pme, pawn, faction);
            List<Pawn> list = new List<Pawn>();
            Pawn p = PawnGenerator.GeneratePawn(pawn.kindDef, pme.startsTamed ? this.caller.Faction : null);
            if (pme.hediffs != null)
            {
                foreach (HediffDef hd in pme.hediffs)
                {
                    p.health.AddHediff(hd);
                }
            }
            list.Add(p);
            if (list.Any<Pawn>())
            {
                ActiveTransporterInfo activeTransporterInfo = new ActiveTransporterInfo();
                activeTransporterInfo.innerContainer.TryAddRangeOrTransfer(list, true, false);
                DropPodUtility.MakeDropPodAt(pawn.Position, this.map, activeTransporterInfo, null);
                Messages.Message("MessagePermitTransportDrop".Translate(faction.Named("FACTION")), new LookTargets(pawn.Position, this.map), MessageTypeDefOf.NeutralEvent, true);
                this.caller.royalty.GetPermit(this.def, faction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
                }
                this.DoOtherEffect(this.caller, faction);
            }
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            PermitAuthorizerUtility.DoPTargeterCooldown(faction, caller, this);
        }
    }
}
