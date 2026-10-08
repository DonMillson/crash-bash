using System.Collections.Generic;
using UnityEngine;

namespace CrashBashRemake
{
    public sealed class ArkenoidCraftVisual : MonoBehaviour
    {
        ArkenoidHeroController hero;
        float yaw;
        readonly List<Transform> jets = new List<Transform>();
        Renderer[] renderers;
        Light underLight;
        Material emission;
        Color accent;
        ArkenoidHeroState previousState;
        float recoil;

        public static ArkenoidCraftVisual Build(Transform parent, ArenaSide side, Color color)
        {
            var resources = parent.GetComponentInParent<ArkenoidRenderResources>();
            if (!resources) resources = parent.gameObject.AddComponent<ArkenoidRenderResources>();
            return Build(parent, parent.GetComponent<ArkenoidHeroController>(), side, CharacterId.Crash, color, resources);
        }
        public static ArkenoidCraftVisual Build(Transform parent, ArkenoidHeroController controller, ArenaSide side,
            CharacterId character, Color color, ArkenoidRenderResources r)
        {
            var root = new GameObject("Hovercraft Visual — replaceable art").AddComponent<ArkenoidCraftVisual>();
            root.transform.SetParent(parent, false); root.hero = controller; root.accent = color;
            root.transform.localPosition = new Vector3(0,.32f,0);
            root.yaw = side == ArenaSide.Top ? 180 : side == ArenaSide.Left ? 90 : side == ArenaSide.Right ? -90 : 0;
            var paint = r.Material("Craft paint " + side, color, .55f, .78f);
            var alloy = r.Material("Craft brushed alloy",new Color(.46f,.53f,.60f),.87f,.66f);
            var rubber = r.Material("Craft black elastomer",new Color(.026f,.032f,.041f),.08f,.27f);
            var seat = r.Material("Craft seat fabric",new Color(.035f,.054f,.073f),.03f,.38f);
            root.emission = r.Material("Craft lamps " + side,color,.45f,.72f,2.5f);
            var exhaust = r.Material("Craft ion exhaust " + side,Color.Lerp(color,Color.white,.6f),.2f,.8f,3);
            foreach (ArkCraftPart part in ArkenoidCraftRecipe.Create())
            {
                Material material = part.Surface == ArkSurface.Paint ? paint : part.Surface == ArkSurface.Alloy ? alloy
                    : part.Surface == ArkSurface.Rubber ? rubber : part.Surface == ArkSurface.Seat ? seat
                    : part.Surface == ArkSurface.Light ? root.emission : exhaust;
                ArkVertex p = part.Position, q = part.Rotation;
                GameObject go = r.Part(root.transform,part.Name,r.Mesh("Craft " + part.Name,part.Mesh),
                    new Vector3(p.X,p.Y,p.Z),material,Quaternion.Euler(q.X,q.Y,q.Z));
                if (part.Name.Contains("hover jet")) { root.jets.Add(go.transform); }
            }
            var lamp = new GameObject("Hover light"); lamp.transform.SetParent(root.transform,false);
            lamp.transform.localPosition = new Vector3(0,-.15f,0);
            root.underLight = lamp.AddComponent<Light>(); root.underLight.type = LightType.Point;
            root.underLight.color = color; root.underLight.intensity = 1.2f; root.underLight.range = 1.7f;
            root.underLight.shadows = LightShadows.None;
            ArkenoidPilotVisual.Build(root.transform,controller,character,r);
            root.renderers = root.GetComponentsInChildren<Renderer>();
            root.transform.localRotation = Quaternion.Euler(0,root.yaw,0);
            return root;
        }
        void LateUpdate()
        {
            if (!hero || hero.Model == null) return;
            var model = hero.Model;
            if (model.State == ArkenoidHeroState.Dead)
            { foreach (Renderer r in renderers) r.enabled=false; underLight.enabled=false; return; }
            foreach (Renderer r in renderers) r.enabled=true;
            underLight.enabled=true;
            if (previousState != model.State && (model.State == ArkenoidHeroState.Kick || model.State == ArkenoidHeroState.RedKick)) recoil=1;
            previousState=model.State; recoil=Mathf.MoveTowards(recoil,0,Time.deltaTime*5);
            float motion=Mathf.Clamp(model.Velocity/hero.Simulation.Tuning.MotionFor(model.Character).speed,-1.6f,1.6f);
            float hover=Mathf.Sin(Time.time*4.5f+model.SlotId)*.027f;
            float sinking=model.State==ArkenoidHeroState.Die ? Mathf.Clamp01(model.StateTime/.65f)*-.24f : 0;
            float lateral = Mathf.Lerp(model.PreviousLateral,model.Lateral,hero.PresentationAlpha)-model.Lateral;
            Vector3 smoothing = ArkenoidPlayerMotor.ToWorld(ArkenoidArenaGeometry.Tangent(model.Side)*lateral,0);
            transform.localPosition=smoothing+new Vector3(0,.32f+hover+sinking,0);
            transform.localRotation=Quaternion.Euler(-recoil*9, yaw, -motion*9+(model.State==ArkenoidHeroState.Die?model.StateTime*80:0));
            float power=.85f+Mathf.Abs(motion)*.45f+recoil*.8f;
            for(int i=0;i<jets.Count;i++) jets[i].localScale=new Vector3(1,power,1);
            underLight.intensity=1+power*.4f;
            emission.SetColor("_EmissionColor",accent*(model.State==ArkenoidHeroState.Grab?3.5f:1.8f+recoil*3));
        }
    }
}
