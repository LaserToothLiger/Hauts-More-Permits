using HautsFramework;
using HautsPermits;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HautsPermits_Biotech
{
    /*permit auth friendly. Read up iydk
     * copy all the pawn's endogenes into a xenogerm, then drop pod it directly onto them and give them the genes regrowing condition. Does not work if target's genes are regrowing.*/
    public class RoyalTitlePermitWorker_MakeXenogermOfEndo : RoyalTitlePermitWorker_TargetPawn
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return pawn.genes != null && pawn.genes.Endogenes.Count > 0 && !pawn.Map.generatorDef.isUnderground && DropCellFinder.CanPhysicallyDropInto(pawn.Position, pawn.Map, true, true) && !pawn.health.hediffSet.HasHediff(HediffDefOf.XenogermReplicating);
        }
        public override void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
            base.AffectPawnInner(pme, pawn, faction);
            Xenogerm xg = this.MakeXenogerm(pawn);
            List<Thing> list = new List<Thing> {
                xg
            };
            ActiveTransporterInfo activeDropPodInfo = new ActiveTransporterInfo();
            activeDropPodInfo.innerContainer.TryAddRangeOrTransfer(list, true, false);
            DropPodUtility.MakeDropPodAt(pawn.Position, pawn.Map, activeDropPodInfo, null);
            Messages.Message("MessagePermitTransportDrop".Translate(faction.Named("FACTION")), new LookTargets(pawn.Position, pawn.Map), MessageTypeDefOf.NeutralEvent, true);
            this.caller.royalty.GetPermit(this.def, faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
            }
        }
        public Xenogerm MakeXenogerm(Pawn target)
        {
            Xenogerm xenogerm = (Xenogerm)ThingMaker.MakeThing(ThingDefOf.Xenogerm, null);
            List<Genepack> noGenepacks = new List<Genepack>();
            string name = "HVB_ProgenoidXenogerm".Translate(target.Name.ToStringShort);
            xenogerm.Initialize(noGenepacks, (target.genes.XenotypeLabel != null) ? target.genes.XenotypeLabel.Trim() : name, (target.genes.iconDef != null) ? target.genes.iconDef : XenotypeIconDefOf.Basic);
            foreach (Gene g in target.genes.Endogenes)
            {
                if (g.def.biostatArc <= 0)
                {
                    xenogerm.GeneSet.AddGene(g.def);
                }
            }
            Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.XenogermReplicating, target, null);
            target.health.AddHediff(hediff, null, null, null);
            return xenogerm;
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            PermitAuthorizerUtility.DoPTargeterCooldown(faction, caller, this);
        }
    }
    //as MakeXenogermOfXeno, but does it for xenogenes
    public class RoyalTitlePermitWorker_MakeXenogermOfXeno : RoyalTitlePermitWorker_TargetPawn
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return pawn.genes != null && pawn.genes.Xenogenes.Count > 0 && !pawn.Map.generatorDef.isUnderground && DropCellFinder.CanPhysicallyDropInto(pawn.Position, pawn.Map, true, true) && !pawn.health.hediffSet.HasHediff(HediffDefOf.XenogermReplicating);
        }
        public override void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
            base.AffectPawnInner(pme, pawn, faction);
            Xenogerm xg = this.MakeXenogerm(pawn);
            List<Thing> list = new List<Thing> {
                xg
            };
            ActiveTransporterInfo activeDropPodInfo = new ActiveTransporterInfo();
            activeDropPodInfo.innerContainer.TryAddRangeOrTransfer(list, true, false);
            DropPodUtility.MakeDropPodAt(pawn.Position, pawn.Map, activeDropPodInfo, null);
            Messages.Message("MessagePermitTransportDrop".Translate(faction.Named("FACTION")), new LookTargets(pawn.Position, pawn.Map), MessageTypeDefOf.NeutralEvent, true);
            this.caller.royalty.GetPermit(this.def, faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
            }
        }
        public Xenogerm MakeXenogerm(Pawn target)
        {
            Xenogerm xenogerm = (Xenogerm)ThingMaker.MakeThing(ThingDefOf.Xenogerm, null);
            List<Genepack> noGenepacks = new List<Genepack>();
            string name = "HVB_ProgenoidXenogerm".Translate(target.Name.ToStringShort);
            xenogerm.Initialize(noGenepacks, (target.genes.XenotypeLabel != null) ? target.genes.XenotypeLabel.Trim() : name, (target.genes.iconDef != null) ? target.genes.iconDef : XenotypeIconDefOf.Basic);
            foreach (Gene g in target.genes.Xenogenes)
            {
                if (g.def.biostatArc <= 0)
                {
                    xenogerm.GeneSet.AddGene(g.def);
                }
            }
            Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.XenogermReplicating, target, null);
            target.health.AddHediff(hediff, null, null, null);
            return xenogerm;
        }
        public override bool IsFactionHostileToPlayer(Faction faction, Pawn pawn)
        {
            return faction.HostileTo(Faction.OfPlayer) && PermitAuthorizerUtility.GetPawnPTargeter(pawn, faction) == null;
        }
        public override bool OverridableFillAidOption(Pawn pawn, Faction faction, ref string text, out bool free)
        {
            return PermitAuthorizerUtility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free);
        }
        public override void DoOtherEffect(Pawn caller, Faction faction)
        {
            PermitAuthorizerUtility.DoPTargeterCooldown(faction, caller, this);
        }
    }
}
