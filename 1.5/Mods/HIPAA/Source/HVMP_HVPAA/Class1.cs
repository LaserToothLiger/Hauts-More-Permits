using HarmonyLib;
using HVPAA;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Sound;

namespace HVMP_HVPAA
{
    [StaticConstructorOnStartup]
    public class HVMP_HVPAA
    {
        private static readonly Type patchType = typeof(HVMP_HVPAA);
        static HVMP_HVPAA()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsmorepermits.hvpaa");
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.Kill)),
                           prefix: new HarmonyMethod(patchType, nameof(HVMP_HVPAA_KillPrefix)));
        }
        public static bool HVMP_HVPAA_KillPrefix(Pawn __instance)
        {
            if (__instance.health.hediffSet.HasHediff(HVMP_HVPAADefOf.HVPAA_EmergencySkipBeacon))
            {
                HVPAAUtility.SkipOutPawnInner(__instance);
                HealthUtility.HealNonPermanentInjuriesAndRestoreLegs(__instance);
                return false;
            }
            return true;
        }
    }
    [DefOf]
    public static class HVMP_HVPAADefOf
    {
        static HVMP_HVPAADefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HVPAADefOf));
        }
        public static HediffDef HVPAA_EmergencySkipBeacon;
    }
    public class Hediff_EmergencySkipOut : HediffWithComps
    {
        public override void Tick()
        {
            base.Tick();
            if (this.pawn.Downed)
            {
                HVPAAUtility.SkipOutPawnInner(this.pawn);
            }
        }
    }
}
