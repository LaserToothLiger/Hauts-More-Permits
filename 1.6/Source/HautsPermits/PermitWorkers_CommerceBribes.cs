using HautsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HautsPermits
{
    //Targets a conscious, awake hostile humanlike (or member of a humanlike faction) and makes them flee the map
    public class RoyalTitlePermitWorker_Retreat : RoyalTitlePermitWorker_TargetPawn
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return (pawn.HostileTo(this.CasterPawn.Faction) || pawn.HostileTo(this.CasterPawn)) && !pawn.InMentalState && pawn.Awake() && !pawn.DeadOrDowned && ((pawn.Faction != null && pawn.Faction.def.humanlikeFaction) || pawn.RaceProps.Humanlike);
        }
        public override void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
            base.AffectPawnInner(pme, pawn, faction);
            if (!pawn.InMentalState && pawn.Awake() && !pawn.DeadOrDowned)
            {
                PermitGlowVFXUtility.ThrowBribeGlow(pawn.Position.ToVector3() + new Vector3(0.5f, 0f, 0.5f), this.map, 1.5f);
                pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, null, false, false, false, null, false, false, false);
            }
        }
    }
    //targets a conscious, awake, prisoner not in a mental state with non-zero resistance, offsetting its resistance
    public class RoyalTitlePermitWorker_Recruit : RoyalTitlePermitWorker_TargetPawn
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return pawn.IsPrisonerOfColony && !pawn.InMentalState && pawn.Awake() && !pawn.DeadOrDowned && pawn.guest.resistance >= float.Epsilon;
        }
        public override void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
            base.AffectPawnInner(pme, pawn, faction);
            if (!pawn.InMentalState && pawn.Awake() && !pawn.DeadOrDowned && pawn.guest != null && pawn.guest.resistance >= float.Epsilon)
            {
                PermitGlowVFXUtility.ThrowBribeGlow(pawn.Position.ToVector3() + new Vector3(0.5f, 0f, 0.5f), this.map, 1.5f);
                pawn.guest.resistance += pme.extraNumber.RandomInRange;
            }
        }
    }
    //targets a conscious, awake pawn of a non-Branch faction with a numerical goodwill score, raising goodwil with that faction
    public class RoyalTitlePermitWorker_Ingratiate : RoyalTitlePermitWorker_TargetPawn
    {
        public override bool IsGoodPawn(Pawn pawn)
        {
            return pawn.Faction != null && this.CasterPawn.Faction != null && pawn.Faction != this.CasterPawn.Faction && !pawn.Faction.def.HasModExtension<EBranchQuests>() && !pawn.Faction.def.PermanentlyHostileTo(this.CasterPawn.Faction.def) && !pawn.InMentalState && pawn.Awake() && !pawn.DeadOrDowned && (pawn.Faction.def.humanlikeFaction || pawn.RaceProps.Humanlike);
        }
        public override void AffectPawnInner(PermitMoreEffects pme, Pawn pawn, Faction faction)
        {
            base.AffectPawnInner(pme, pawn, faction);
            if (!pawn.InMentalState && pawn.Awake() && !pawn.DeadOrDowned && pawn.Faction != null)
            {
                PermitGlowVFXUtility.ThrowBribeGlow(pawn.Position.ToVector3() + new Vector3(0.5f, 0f, 0.5f), this.map, 1.5f);
                this.CasterPawn.Faction.TryAffectGoodwillWith(pawn.Faction, (int)pme.extraNumber.RandomInRange, true, true, HVMPDefOf.HVMP_IngratiationAccepted, null);
            }
        }
    }
    //unlike the other bribes, this just forces all conscious, awake hostile humanlikes (or of humanlike faction) who aren't in a mental state to flee for their lives. Skips over prisoners, obviously, to avoid stupidity.
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_Peacemaking : RoyalTitlePermitWorker_Targeted
    {
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (faction.HostileTo(Faction.OfPlayer))
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            if (base.FillAidOption(pawn, faction, ref text, out bool free))
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
        protected virtual void MakeCondition(Pawn caller, Faction faction, IncidentParms parms, bool free)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null && caller.MapHeld != null)
            {
                foreach (Pawn p in caller.MapHeld.mapPawns.AllPawnsSpawned)
                {
                    if ((p.HostileTo(caller.Faction) || p.HostileTo(caller)) && !p.InMentalState && p.Awake() && !p.DeadOrDowned && ((p.Faction != null && p.Faction.def.humanlikeFaction) || p.RaceProps.Humanlike) && !p.IsPrisoner)
                    {
                        PermitGlowVFXUtility.ThrowBribeGlow(p.Position.ToVector3() + new Vector3(0.5f, 0f, 0.5f), caller.MapHeld, 1.5f);
                        p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, null, false, false, false, null, false, false, false);
                    }
                }
                Messages.Message(pme.onUseMessage.Translate(faction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
                if (pme.screenShake && caller.MapHeld == Find.CurrentMap)
                {
                    Find.CameraDriver.shaker.DoShake(1f);
                }
                if (pme.soundDef != null)
                {
                    pme.soundDef.PlayOneShot(new TargetInfo(caller.PositionHeld, caller.MapHeld, false));
                }
                caller.royalty.GetPermit(this.def, faction).Notify_Used();
                if (!free)
                {
                    caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
                }
            }
        }
    }
}
