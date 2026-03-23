using RimWorld;
using Verse;

namespace HautsPermits_Biotech
{
    [DefOf]
    public static class HVMP_BDefOf
    {
        static HVMP_BDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HVMP_BDefOf));
        }
        public static HistoryEventDef HVMP_PerformedHarmfulInjection;

        public static JobDef HVMP_InjectRetroviralPackage;

        public static ThingDef HVMP_PreserveMarker;
    }
}
