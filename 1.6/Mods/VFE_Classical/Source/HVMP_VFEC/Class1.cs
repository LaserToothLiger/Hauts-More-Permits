using HautsFramework;
using HautsPermits;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Sound;
using VFEC.Senators;

namespace HVMP_VFEC
{
    [StaticConstructorOnStartup]
    public class HVMP_VFEC
    {
        static HVMP_VFEC()
        {
        }
    }
    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_BribeSenator : RoyalTitlePermitWorker_Targeted
    {
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (map.generatorDef.isUnderground)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (faction.HostileTo(Faction.OfPlayer) && HVMP_Utility.GetPawnPTargeter(pawn, faction) == null)
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (this.BribeableSenators.NullOrEmpty())
            {
                yield return new FloatMenuOption("HVMP_NoSenatorsLeftToBribe".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string text = this.def.LabelCap + ": ";
            bool free;
            if (HVMP_Utility.ProprietaryFillAidOption(this, pawn, faction, ref text, out free))
            {
                action = delegate
                {
                    this.DoBribe(pawn, faction, new IncidentParms(), this.free);
                };
            }
            yield return new FloatMenuOption(text, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }
        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            yield break;
        }
        public List<SenatorInfo> BribeableSenators
        {
            get
            {
                List<SenatorInfo> list = new List<SenatorInfo>();
                foreach (KeyValuePair<Faction, List<SenatorInfo>> kvp in WorldComponent_Senators.Instance.SenatorInfo)
                {
                    if (!kvp.Value.NullOrEmpty())
                    {
                        list.AddRange(kvp.Value.Where((SenatorInfo si)=> !si.Favored && si.Pawn != null));
                    }
                }
                return list;
            }
        }
        protected virtual void DoBribe(Pawn caller, Faction faction, IncidentParms parms, bool free)
        {
            PermitMoreEffects pme = this.def.GetModExtension<PermitMoreEffects>();
            if (pme != null)
            {
                List<SenatorInfo> bribeableSenators = this.BribeableSenators;
                if (!bribeableSenators.NullOrEmpty())
                {
                    SenatorInfo si = bribeableSenators.RandomElement();
                    List<Faction> siFactions = WorldComponent_Senators.Instance.SenatorInfo.Keys.Where((Faction f) => WorldComponent_Senators.Instance.SenatorInfo[f].Contains(si)).ToList();
                    if (!siFactions.NullOrEmpty())
                    {
                        Faction siFaction = siFactions.RandomElement();
                        WorldComponent_Senators.Instance.GainFavorOf(si.Pawn, siFaction);
                    }
                }
                Messages.Message(pme.onUseMessage.Translate(faction.Named("FACTION")), null, MessageTypeDefOf.NeutralEvent, true);
                if (caller.MapHeld != null)
                {
                    if (pme.screenShake && caller.MapHeld == Find.CurrentMap)
                    {
                        Find.CameraDriver.shaker.DoShake(1f);
                    }
                    pme.soundDef?.PlayOneShot(new TargetInfo(caller.PositionHeld, caller.MapHeld, false));
                }
                caller.royalty.GetPermit(this.def, faction).Notify_Used();
                if (!free)
                {
                    caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
                }
                HVMP_Utility.DoPTargeterCooldown(faction,caller,this);
            }
        }
    }
}
