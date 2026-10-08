using UnityEngine;

namespace CrashBashRemake
{
    public static class ArkenoidBallVisual
    {
        public static Transform Build(Transform parent,float radius,ArkenoidRenderResources r)
        {
            var visual=new GameObject("Ball visual — replaceable mesh");visual.transform.SetParent(parent,false);
            var alloy=r.Material("Ball polished alloy",new Color(.62f,.69f,.77f),.93f,.87f);
            r.Part(visual.transform,"Sphere shell",r.Mesh("Ball sphere",ArkenoidMeshData.Ellipsoid(radius*2,radius*2,radius*2,32,20)),Vector3.zero,alloy);
            var light=r.Material("Ball seam glow",new Color(.15f,.75f,1),.4f,.8f,1.8f);
            r.Part(visual.transform,"Equatorial seam",r.Mesh("Ball seam",ArkenoidMeshData.Torus(radius+.001f,.009f,40)),Vector3.zero,light);
            r.Part(visual.transform,"Meridian seam",r.Mesh("Ball seam",ArkenoidMeshData.Torus(radius+.001f,.009f,40)),Vector3.zero,light,Quaternion.Euler(90,0,0));
            var trail=parent.gameObject.AddComponent<TrailRenderer>();trail.sharedMaterial=r.Glow();trail.time=.12f;
            trail.startWidth=radius*.55f;trail.endWidth=0;trail.minVertexDistance=.08f;
            trail.startColor=new Color(.15f,.65f,1,.6f);trail.endColor=new Color(.1f,.5f,1,0);
            trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;trail.receiveShadows=false;
            trail.emitting=false;return visual.transform;
        }
    }
}
