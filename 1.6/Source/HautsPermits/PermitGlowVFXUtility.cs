using RimWorld;
using UnityEngine;
using Verse;

namespace HautsPermits
{
    //make certain flecks at specified locations with specified sizes
    public static class PermitGlowVFXUtility
    {
        public static void ThrowBribeGlow(Vector3 loc, Map map, float size)
        {
            if (!loc.ShouldSpawnMotesAt(map, true))
            {
                return;
            }
            FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc + new Vector3(0.5f, 0f, 0.5f), map, HVMPDefOf.HVMP_BribeGlow, size);
            dataStatic.rotationRate = Rand.Range(-3f, 3f);
            dataStatic.velocityAngle = 330f + Rand.Range(0f, 50f);
            dataStatic.velocitySpeed = 0.6f;
            map.flecks.CreateFleck(dataStatic);
        }
        public static void ThrowScalpelScope(Thing target, Map map, float size)
        {
            if (target.SpawnedOrAnyParentSpawned)
            {
                FleckMaker.AttachedOverlay(target, HVMPDefOf.HVMP_ScalpelBLAST, Vector3.zero, size, -1f);
                return;
            }
        }
        public static void ThrowRepairGlow(Vector3 loc, Map map, float size)
        {
            if (!loc.ShouldSpawnMotesAt(map, true))
            {
                return;
            }
            FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc + new Vector3(0.5f, 0f, 0.5f), map, HVMPDefOf.HVMP_RepairGlow, size);
            dataStatic.rotationRate = Rand.Range(-3f, 3f);
            dataStatic.velocityAngle = 340f + Rand.Range(0f, 40f);
            dataStatic.velocitySpeed = 0.6f;
            map.flecks.CreateFleck(dataStatic);
        }
        public static void ThrowDecryptionGlow(Vector3 loc, Map map, float size)
        {
            if (!loc.ShouldSpawnMotesAt(map, true))
            {
                return;
            }
            FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc + new Vector3(0.5f, 0f, 0.5f), map, HVMPDefOf.HVMP_DecryptGlow, size);
            dataStatic.rotationRate = Rand.Range(-3f, 3f);
            dataStatic.velocityAngle = 340f + Rand.Range(0f, 40f);
            dataStatic.velocitySpeed = 0.6f;
            map.flecks.CreateFleck(dataStatic);
        }
        public static void ThrowQualityUpgradeGlow(Vector3 loc, Map map, float size)
        {
            if (!loc.ShouldSpawnMotesAt(map, true))
            {
                return;
            }
            FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc + new Vector3(0.5f, 0f, 0.5f), map, HVMPDefOf.HVMP_QualityGlow, size);
            dataStatic.rotationRate = Rand.Range(-3f, 3f);
            dataStatic.velocityAngle = 340f + Rand.Range(0f, 40f);
            dataStatic.velocitySpeed = 0.6f;
            map.flecks.CreateFleck(dataStatic);
        }
        public static void ThrowQualityDestroyGlow(Vector3 loc, Map map, float size)
        {
            if (!loc.ShouldSpawnMotesAt(map, true))
            {
                return;
            }
            FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc + new Vector3(0.5f, 0f, 0.5f), map, HVMPDefOf.HVMP_VaalOrNoBaals, size);
            dataStatic.rotationRate = Rand.Range(-3f, 3f);
            dataStatic.velocityAngle = 340f + Rand.Range(0f, 40f);
            dataStatic.velocitySpeed = 0.6f;
            map.flecks.CreateFleck(dataStatic);
        }
    }
}
