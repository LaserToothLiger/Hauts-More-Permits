using HautsPermits;
using RimWorld.QuestGen;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld.Planet;
using UnityEngine;
using Verse.Grammar;
using Verse.Sound;
using HautsFramework;
using System.Reflection;
using Verse.AI.Group;
using Verse.AI;
using Verse.Noise;
using DelaunatorSharp;
using HarmonyLib;

namespace HautsPermits_Occult
{
    [StaticConstructorOnStartup]
    public class HautsPermits_Occult
    {
        private static readonly Type patchType = typeof(HautsPermits_Occult);
        static HautsPermits_Occult()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautspermits.anomaly");
            harmony.Patch(AccessTools.Method(typeof(CompFloorEtching), nameof(CompFloorEtching.PostSpawnSetup)),
                          postfix: new HarmonyMethod(patchType, nameof(HVMPCompFloorEtching_PostSpawnSetupPostfix)));
            MethodInfo methodInfo = typeof(CompGrayStatueTeleporter).GetMethod("Trigger", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo,
                          prefix: new HarmonyMethod(patchType, nameof(HVMPCompGrayStatueTeleporter_TriggerPrefix)));
            //there's so many graphic things for chimerae and I think some mods also change them up. Grandsplice Chimerae should inherit those alt graphics too
            HVMPDefOf_A.HVMP_GrandspliceChimera.alternateGraphics = PawnKindDefOf.Chimera.alternateGraphics;
            HVMPDefOf_A.HVMP_GrandspliceChimera.alternateGraphicChance = PawnKindDefOf.Chimera.alternateGraphicChance;
        }
        /*because Natali labyrinths are not TECHNICALLY gray labyrinths due to technical issues (see WorldObject_NataliLabyrinth.cs for more ranting)
         * the floor etchings in Natali labyrinths don't tell you the relative cardinal direction of the exit. This fixes that.*/
        public static void HVMPCompFloorEtching_PostSpawnSetupPostfix(CompFloorEtching __instance)
        {
            HVMP_HypercubeMapComponent component = __instance.parent.Map.GetComponent<HVMP_HypercubeMapComponent>();
            if (component == null)
            {
                return;
            }
            float angleFlat = (component.labyrinthObelisk.Position - __instance.parent.Position).AngleFlat;
            Rot4 direction = Rot4.North;
            if (angleFlat >= 315f || angleFlat < 45f)
            {
                direction = Rot4.North;
            } else if (angleFlat >= 45f && angleFlat < 135f) {
                direction = Rot4.East;
            } else if (angleFlat >= 135f && angleFlat < 225f) {
                direction = Rot4.South;
            } else if (angleFlat >= 225f && angleFlat < 315f) {
                direction = Rot4.West;
            }
            if (__instance.GetType().GetField("direction", BindingFlags.NonPublic | BindingFlags.Instance) != null)
            {
                __instance.GetType().GetField("direction", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, direction);
            }
        }
        //ALSO due to the same technical limits, the gray statue teleporters don't work. This makes them work as expected when in a Natali labyrinth
        public static bool HVMPCompGrayStatueTeleporter_TriggerPrefix(CompGrayStatueTeleporter __instance, Pawn target)
        {
            HVMP_HypercubeMapComponent component = __instance.parent.Map.GetComponent<HVMP_HypercubeMapComponent>();
            if (component != null)
            {
                __instance.parent.MapHeld.GetComponent<HVMP_HypercubeMapComponent>().TeleportToLabyrinth(target,true);
                return false;
            }
            return true;
        }
    }
}
