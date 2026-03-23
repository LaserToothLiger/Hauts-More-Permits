using HautsPermits;
using RimWorld;
using Verse;

namespace HautsPermits_Occult
{
    [DefOf]
    public static class HVMPDefOf_A
    {
        static HVMPDefOf_A()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HVMPDefOf));
        }
        public static GameConditionDef HVMP_LovecraftAlignmentCond;
        public static GameConditionDef HVMP_LovecraftHypnoLight;

        public static HediffDef HVMP_LightHypnotized;
        public static HediffDef HVMP_LGTH;

        public static IncidentDef HVMP_LovecraftAlignment;

        public static LayoutDef HVMP_HypercubeLayout;
        public static LayoutRoomDef HVMP_HypercubeObelisk;
        public static MapGeneratorDef HVMP_Hypercube;
        public static ThingDef HVMP_WarpedObelisk_Hypercube;

        public static PawnsArrivalModeDef HVMP_UsherArrival;

        public static PawnKindDef HVMP_HoraxianUsher;
        public static PawnKindDef HVMP_PrimedNociosphere;
        public static PawnKindDef HVMP_GhoulSuperEvil;
        public static PawnKindDef HVMP_GrandspliceChimera;

        public static ThingDef HVMP_LivingCloudkill;

        public static TerrainDef HVMP_BurntSurface;
    }
}
