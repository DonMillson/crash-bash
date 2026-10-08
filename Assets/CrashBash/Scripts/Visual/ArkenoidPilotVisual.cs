using UnityEngine;

namespace CrashBashRemake
{
    /// <summary>Authored modular pilot rigs for all eight characters, awaiting finished character art.</summary>
    public sealed class ArkenoidPilotVisual : MonoBehaviour
    {
        Transform torso, head, leftArm, rightArm;
        ArkenoidHeroController hero;
        public static string CharacterName(CharacterId id)
        {
            switch (id) {
                case CharacterId.NBrio: return "N. Brio";
                case CharacterId.Tiny: return "Tiny Tiger";
                case CharacterId.KoalaKong: return "Koala Kong";
                case CharacterId.RillaRoo: return "Rilla Roo";
                default: return id.ToString();
            }
        }
        public static ArkenoidPilotVisual Build(Transform craft, ArkenoidHeroController controller, CharacterId character, ArkenoidRenderResources resources)
        {
            var go = new GameObject(CharacterName(character) + " pilot rig"); go.transform.SetParent(craft, false);
            go.transform.localPosition = new Vector3(0, .33f, -.05f);
            var pilot = go.AddComponent<ArkenoidPilotVisual>(); pilot.hero = controller;
            pilot.Make(character, resources); return pilot;
        }
        Transform Pivot(string name, Transform parent, Vector3 position)
        { var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = position; return g.transform; }
        void Make(CharacterId character, ArkenoidRenderResources r)
        {
            bool big = character == CharacterId.Tiny || character == CharacterId.KoalaKong || character == CharacterId.RillaRoo;
            bool scientist = character == CharacterId.Cortex || character == CharacterId.NBrio;
            Color skin = character == CharacterId.Crash ? new Color(1,.37f,.07f) : character == CharacterId.Coco ? new Color(1,.67f,.34f)
                : character == CharacterId.Cortex ? new Color(.96f,.78f,.22f) : character == CharacterId.NBrio ? new Color(.63f,.76f,.43f)
                : character == CharacterId.Tiny ? new Color(.88f,.54f,.16f) : character == CharacterId.Dingodile ? new Color(.32f,.57f,.22f)
                : character == CharacterId.KoalaKong ? new Color(.47f,.51f,.54f) : new Color(.50f,.32f,.53f);
            Material body = r.Material(character + " skin", skin, 0, .35f);
            Material muzzle = r.Material(character + " muzzle", skin * 1.16f, 0, .30f);
            Material clothes = r.Material(character + " outfit", scientist ? new Color(.86f,.90f,.89f) : character == CharacterId.KoalaKong ? new Color(.62f,.12f,.12f) : new Color(.08f,.22f,.58f), .05f, .38f);
            Material dark = r.Material("Pilot dark details", new Color(.035f,.025f,.035f), 0, .36f);
            Material eye = r.Material("Pilot eyes", new Color(.96f,.97f,.90f), 0, .55f);
            Material hair = r.Material(character + " hair", character == CharacterId.Coco ? new Color(.98f,.77f,.08f)
                : character == CharacterId.Crash || character == CharacterId.Dingodile ? new Color(.48f,.12f,.04f) : new Color(.06f,.06f,.065f), 0, .3f);
            torso = Pivot("Torso pivot", transform, new Vector3(0,.08f,0));
            head = Pivot("Head pivot", torso, new Vector3(0,.29f,0));
            float width = big ? .43f : .29f;
            r.Part(torso,"Torso",r.Mesh(character+" torso",ArkenoidMeshData.Ellipsoid(width,.37f,.22f)),Vector3.zero,scientist || character==CharacterId.Coco ? clothes : body);
            r.Part(torso,"Trousers",r.Mesh(character+" trousers",ArkenoidMeshData.Ellipsoid(width,.18f,.25f)),new Vector3(0,-.13f,.02f),clothes);
            float headWidth = character == CharacterId.Cortex ? .34f : big ? .32f : .28f;
            r.Part(head,"Face",r.Mesh(character+" face",ArkenoidMeshData.Ellipsoid(headWidth,.30f,.25f)),Vector3.zero,body);
            float snout = character == CharacterId.Dingodile ? .28f : scientist ? .10f : .15f;
            r.Part(head,"Muzzle",r.Mesh(character+" muzzle mesh",ArkenoidMeshData.Ellipsoid(headWidth*.8f,.12f,snout)),new Vector3(0,-.05f,.14f),muzzle);
            foreach (int sign in new[] {-1,1})
            {
                r.Part(head,"Eye white",r.Mesh("Pilot eye white",ArkenoidMeshData.Ellipsoid(.073f,.09f,.037f)),new Vector3(sign*.063f,.035f,.122f),eye);
                r.Part(head,"Pupil",r.Mesh("Pilot pupil",ArkenoidMeshData.Ellipsoid(.029f,.048f,.015f)),new Vector3(sign*.063f,.035f,.144f),dark);
                if (!scientist) r.Part(head,"Ear",r.Mesh(character+" ear",ArkenoidMeshData.Ellipsoid(big?.16f:.105f,.16f,.075f)),new Vector3(sign*headWidth*.49f,.10f,-.01f),body);
                Transform arm = Pivot(sign<0?"Left arm pivot":"Right arm pivot",torso,new Vector3(sign*width*.48f,.10f,.01f));
                r.Part(arm,"Upper arm",r.Mesh(character+" arm",ArkenoidMeshData.Ellipsoid(big?.16f:.09f,.23f,.10f)),new Vector3(sign*.04f,-.075f,.035f),scientist?clothes:body);
                r.Part(arm,"Driving hand",r.Mesh(character+" hand",ArkenoidMeshData.Ellipsoid(.10f,.085f,.11f)),new Vector3(sign*.025f,-.135f,.15f),body);
                if (sign<0) leftArm=arm; else rightArm=arm;
                r.Part(torso,"Boot",r.Mesh(character+" boot",ArkenoidMeshData.Ellipsoid(.13f,.10f,.22f)),new Vector3(sign*.105f,-.24f,.18f),dark);
                if (character == CharacterId.NBrio)
                    r.Part(head,"Temple bolt",r.Mesh("Brio bolt",ArkenoidMeshData.Cylinder(.023f,.07f,12)),new Vector3(sign*.19f,.035f,0),r.Material("Bolt steel",new Color(.52f,.58f,.62f),.9f,.65f),Quaternion.Euler(0,0,90));
            }
            if (character == CharacterId.Coco)
            {
                r.Part(head,"Blonde crown",r.Mesh("Coco hair crown",ArkenoidMeshData.Ellipsoid(.31f,.16f,.26f)),new Vector3(0,.135f,-.02f),hair);
                r.Part(head,"Ponytail",r.Mesh("Coco ponytail",ArkenoidMeshData.Ellipsoid(.17f,.29f,.17f)),new Vector3(.17f,.10f,-.14f),hair,Quaternion.Euler(0,0,-30));
            }
            else
            {
                for (int i=0;i<(scientist?2:3);i++)
                    r.Part(head,"Hair crest",r.Mesh(character+" crest",ArkenoidMeshData.Hull(.10f,.12f,.20f,8)),new Vector3((i-1)*.07f,.16f,-.01f),hair,Quaternion.Euler(0,0,(i-1)*-18));
            }
            r.Part(head,"Nose",r.Mesh("Pilot nose",ArkenoidMeshData.Ellipsoid(.07f,.065f,.045f)),new Vector3(0,-.025f,.14f+snout*.40f),dark);
        }
        void LateUpdate()
        {
            if (!hero || hero.Model == null) return;
            var model = hero.Model;
            float breath = Mathf.Sin(Time.time*3.1f)*.006f;
            float lean = Mathf.Clamp(model.Velocity/hero.Simulation.Tuning.MotionFor(model.Character).speed,-1,1)*-8;
            float pitch = 0, armRaise = 0, nod = 0;
            switch (model.State)
            {
                case ArkenoidHeroState.Move: pitch=8; break;
                case ArkenoidHeroState.Kick: pitch=-16*Mathf.Exp(-model.StateTime*12); break;
                case ArkenoidHeroState.RedKick: pitch=-10; armRaise=45; break;
                case ArkenoidHeroState.Grab: pitch=14; armRaise=-18; break;
                case ArkenoidHeroState.Taunt: nod=Mathf.Sin(model.StateTime*13)*18; armRaise=55; break;
                case ArkenoidHeroState.Winner: armRaise=135; nod=-12; break;
                case ArkenoidHeroState.Lose: pitch=22; nod=30; break;
                case ArkenoidHeroState.Die: pitch=45; nod=25; break;
            }
            torso.localPosition = new Vector3(0,.08f+breath,0);
            torso.localRotation = Quaternion.Euler(pitch,0,lean);
            head.localRotation = Quaternion.Euler(nod,0,0);
            leftArm.localRotation = Quaternion.Euler(0,0,-armRaise);
            rightArm.localRotation = Quaternion.Euler(0,0,armRaise);
        }
    }
}
