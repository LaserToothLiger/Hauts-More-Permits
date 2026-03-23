using RimWorld;
using Verse;

namespace HautsPermits
{
    public static class CosmopolitanMemeUtility
    {
        /*the effects of the Interfaction Aid precept are gated behind these checks, as shown in HamronyPatches.cs
         * negotiators with this precept get a discount on refreshing their branch permit cooldowns;
         * when a negotiator with this precept requests military aid or trade caravans thru the comms console, or when the faction solicited for these services believes in that precept, the cooldown is reduced*/
        public static bool NegotiatorIsCosmopolitan(Pawn negotiator)
        {
            return ModsConfig.IdeologyActive && negotiator.ideo != null && negotiator.Ideo.HasPrecept(HVMPDefOf.HVMP_InterfactionAidImproved);
        }
        public static bool FactionIsCosmopolitan(Faction faction)
        {
            return ModsConfig.IdeologyActive && faction.ideos != null && faction.ideos.PrimaryIdeo != null && faction.ideos.PrimaryIdeo.HasPrecept(HVMPDefOf.HVMP_InterfactionAidImproved);
        }
    }
}
