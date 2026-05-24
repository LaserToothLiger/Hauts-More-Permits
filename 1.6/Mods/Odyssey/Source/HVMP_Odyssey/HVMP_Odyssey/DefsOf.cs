using RimWorld;
using Verse;

namespace HVMP_Odyssey
{
    [DefOf]
    public static class HVMPDefOf_Odyssey
    {
        static HVMPDefOf_Odyssey()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HVMPDefOf_Odyssey));
        }
        public static LayoutDef HVMP_AncientRuins_KorhalSite;

        public static SitePartDef HVMP_KorhalSite;
        public static SitePartDef HVMP_MoriaSite;

        public static ThingDef HVMP_MineralExtractor;
        public static ThingDef HVMP_OrbitalTargetingBeacon;
    } 
}
