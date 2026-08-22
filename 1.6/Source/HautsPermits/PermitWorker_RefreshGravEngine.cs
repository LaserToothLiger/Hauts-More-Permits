using HautsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace HautsPermits
{
    public class RoyalTitlePermitWorker_RefreshGravEngine : RoyalTitlePermitWorker_Targeted
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
                    this.RefreshGravEngine(pawn, faction, new IncidentParms(), this.free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            yield break;
        }
        protected virtual void RefreshGravEngine(Pawn caller, Faction faction, IncidentParms parms, bool free)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null && caller.MapHeld != null)
            {
                Building_GravEngine bg = GravshipUtility.GetPlayerGravEngine_NewTemp(caller.MapHeld);
                if (bg != null && Find.TickManager.TicksGame < bg.cooldownCompleteTick)
                {
                    bg.cooldownCompleteTick = -1;
                    Messages.Message(pme.onUseMessage.Translate(faction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
                    if (pme.screenShake && caller.MapHeld == Find.CurrentMap)
                    {
                        Find.CameraDriver.shaker.DoShake(1f);
                    }
                    if (pme.soundDef != null)
                    {
                        pme.soundDef.PlayOneShot(new TargetInfo(bg.PositionHeld, caller.MapHeld, false));
                    }
                    caller.royalty.GetPermit(this.def, faction).Notify_Used();
                    if (!free)
                    {
                        caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
                    }
                } else {
                    Messages.Message("HVMP_NoRefreshableGravEngineFound".Translate(faction.Named("FACTION")), null, MessageTypeDefOf.RejectInput, true);
                }
            }
        }
    }
}
