using RimWorld;
using UnityEngine;
using Verse;

namespace HautsPermits_Occult
{
    //graphical floaty orb for Voidcairn. This is a thing comp instead of being a building class for backward compatibility reasons; it's a derivative of CompFacility to keep the num of comps the Voidcairn has low, making its ticks less spensive.
    public class CompVoidcairn : CompFacility
    {
        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            if (base.parent.Spawned)
            {
                drawLoc.z += 0.5f + ((1f + Mathf.Sin(3.2831855f * (float)GenTicks.TicksGame / 500f)) * 0.3f);
                drawLoc.y += 0.03658537f;
                Vector3 vector = new Vector3(this.parent.def.graphicData.drawSize.x*0.75f, 1f, this.parent.def.graphicData.drawSize.y * 0.75f);
                Graphics.DrawMesh(MeshPool.plane10Back, Matrix4x4.TRS(drawLoc, (0f).ToQuat(), vector), CompVoidcairn.OrbMat.Material, 0, null, 0);
            }
        }
        private static readonly CachedMaterial OrbMat = new CachedMaterial("Things/Building/HVMP_Voidcairn_Top", ShaderDatabase.Cutout);
    }
}
