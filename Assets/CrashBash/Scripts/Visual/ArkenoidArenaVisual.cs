using UnityEngine;

namespace CrashBashRemake
{
    /// <summary>Rendering of the selected gameplay geometry; it never writes tuning.</summary>
    public sealed class ArkenoidArenaVisual : MonoBehaviour
    {
        MatchManager match;
        ArkenoidRenderResources resources;
        readonly GameObject[] deadWalls = new GameObject[4];
        readonly Light[] launcherLamps = new Light[4];
        readonly Material[] launcherGlow = new Material[4];
        readonly TextMesh[] scoreDisplays = new TextMesh[4];
        public static readonly Color[] SideColors = {
            new Color(.1f,.53f,1), new Color(1,.70f,.08f), new Color(.16f,.9f,.40f), new Color(1,.18f,.22f)
        };
        public void Build(MatchManager manager, ArkenoidRenderResources r)
        {
            match = manager; resources = r;
            var geometry = match.Simulation.Geometry; var t = geometry.Tuning;
            var baseMetal = r.Material("Arena structural steel",new Color(.055f,.078f,.11f),.72f,.47f);
            var floor = r.Material("Crashball brushed playing surface",new Color(.24f,.30f,.36f),.53f,.56f);
            floor.mainTexture = r.FloorTexture(); floor.mainTextureScale = Vector2.one * 5;
            var trim = r.Material("Arena pale alloy",new Color(.55f,.65f,.71f),.82f,.65f);
            var dark = r.Material("Recessed launcher interior",new Color(.025f,.030f,.038f),.16f,.22f);
            float e = t.wallHalfExtent;
            r.Part(transform,"Floating foundation",r.Mesh("Arena foundation",ArkenoidMeshData.Prism((e+1.0f)*2,.65f,(e+1.0f)*2,.65f)),new Vector3(0,-.42f,0),baseMetal);
            r.Part(transform,"Playing surface",r.Mesh("Arena playing floor",ArkenoidMeshData.Prism((e+.55f)*2,.10f,(e+.55f)*2,.45f)),new Vector3(0,-.045f,0),floor);
            r.Part(transform,"Central machined plate",r.Mesh("Central plate",ArkenoidMeshData.Cylinder(1.55f,.07f,64)),new Vector3(0,.025f,0),baseMetal);
            r.Part(transform,"Central trim inlay",r.Mesh("Central ring",ArkenoidMeshData.Torus(1.44f,.018f,64)),new Vector3(0,.07f,0),trim);
            foreach (ArkWallSegment wall in geometry.Walls(match.Simulation.Heroes))
            {
                Vector3 a = ArkenoidPlayerMotor.ToWorld(wall.A, .34f), b = ArkenoidPlayerMotor.ToWorld(wall.B, .34f);
                Vector3 delta = b-a;
                var wallRoot = new GameObject(wall.Side+" corner wall"); wallRoot.transform.SetParent(transform,false);
                wallRoot.transform.position=(a+b)*.5f;
                // Decoration lies behind the exact collision surface, never inside the playable area.
                wallRoot.transform.position -= ArkenoidPlayerMotor.ToWorld(ArkenoidArenaGeometry.Inward(wall.Side),0)*t.wallThickness*.5f;
                wallRoot.transform.rotation=Quaternion.LookRotation(ArkenoidPlayerMotor.ToWorld(ArkenoidArenaGeometry.Inward(wall.Side),0));
                r.Part(wallRoot.transform,"Armoured wall",r.Mesh("Wall segment",ArkenoidMeshData.Prism(delta.magnitude,.68f,t.wallThickness,.07f)),Vector3.zero,baseMetal);
                r.Part(wallRoot.transform,"Polished top lip",r.Mesh("Wall top lip",ArkenoidMeshData.Prism(delta.magnitude,.05f,t.wallThickness+.08f,.05f)),new Vector3(0,.36f,0),trim);
                var sensor = new GameObject("Invisible wall surface"); sensor.transform.SetParent(wallRoot.transform,false);
                var box=sensor.AddComponent<BoxCollider>(); box.size=new Vector3(delta.magnitude,1.5f,t.wallThickness); box.isTrigger=true;
            }
            for(int i=0;i<4;i++)
            {
                ArenaSide side=(ArenaSide)i;
                var goalRoot=new GameObject(side+" goal assembly"); goalRoot.transform.SetParent(transform,false);
                goalRoot.transform.position=ArkenoidPlayerMotor.ToWorld(geometry.SidePoint(side,0,t.wallHalfExtent),0);
                goalRoot.transform.rotation=Quaternion.LookRotation(ArkenoidPlayerMotor.ToWorld(ArkenoidArenaGeometry.Inward(side),0));
                var paint=r.Material(side+" goal paint",SideColors[i],.55f,.67f);
                var light=r.Material(side+" guide lamps",SideColors[i],.22f,.8f,2.7f);
                r.Part(goalRoot.transform,"Threshold",r.Mesh("Goal threshold",ArkenoidMeshData.Prism(t.goalHalfWidth*2,.07f,.45f,.10f)),new Vector3(0,.01f,-.12f),paint);
                r.Part(goalRoot.transform,"Guide strip",r.Mesh("Goal guide strip",ArkenoidMeshData.Prism(t.goalHalfWidth*2,.022f,.055f,.018f)),new Vector3(0,.055f,.11f),light);
                for(int sign=-1;sign<=1;sign+=2)
                {
                    r.Part(goalRoot.transform,"Goal post housing",r.Mesh("Goal post",ArkenoidMeshData.Prism(.18f,.85f,.48f,.055f)),new Vector3(sign*(t.goalHalfWidth+.09f),.38f,0),trim);
                    r.Part(goalRoot.transform,"Post lamp",r.Mesh("Goal post lamp",ArkenoidMeshData.Ellipsoid(.085f,.24f,.04f)),new Vector3(sign*(t.goalHalfWidth+.09f),.61f,.251f),light);
                }
                var barrier=new GameObject(side+" eliminated goal"); barrier.transform.SetParent(goalRoot.transform,false);
                r.Part(barrier.transform,"DeadWall armoured gate",r.Mesh("Elimination gate",ArkenoidMeshData.Prism(t.goalHalfWidth*2,.62f,t.wallThickness,.075f)),new Vector3(0,.30f,-t.wallThickness*.5f),baseMetal);
                r.Part(barrier.transform,"DeadWall light",r.Mesh("Elimination glow rail",ArkenoidMeshData.Prism(t.goalHalfWidth*2,.055f,.035f,.01f)),new Vector3(0,.60f,.02f),light);
                deadWalls[i]=barrier; barrier.SetActive(false);
                var sensor=new GameObject("Goal plane sensor "+side);sensor.transform.SetParent(transform,false);
                sensor.transform.position=ArkenoidPlayerMotor.ToWorld(geometry.SidePoint(side,0,t.goalPlane),t.ballHeight);
                sensor.transform.rotation=goalRoot.transform.rotation;
                var collider=sensor.AddComponent<BoxCollider>();collider.isTrigger=true;collider.size=new Vector3(t.goalHalfWidth*2,2,.12f);
                var zone=sensor.AddComponent<GoalZone>();zone.side=side;zone.match=manager;
                var display=new GameObject(side+" score readout");display.transform.SetParent(goalRoot.transform,false);
                display.transform.localPosition=new Vector3(0,.12f,-.72f);display.transform.localRotation=Quaternion.Euler(90,0,0);
                var text=display.AddComponent<TextMesh>();text.text="15";text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;
                text.fontSize=48;text.characterSize=.13f;text.color=SideColors[i];scoreDisplays[i]=text;
            }
            for(int corner=0;corner<4;corner++)
            {
                ArkVector launch=match.Simulation.Rules.LaunchPosition(corner,geometry);
                var p=ArkenoidPlayerMotor.ToWorld(launch,0);
                // Launchers surround the playable corner; their nozzle matches the actual spawn point.
                var root=new GameObject("Corner launcher "+corner);root.transform.SetParent(transform,false);root.transform.position=p;
                var toward=new Vector3(-p.x,0,-p.z).normalized;root.transform.rotation=Quaternion.LookRotation(toward);
                r.Part(root.transform,"Launcher pedestal",r.Mesh("Corner pedestal",ArkenoidMeshData.Cylinder(.42f,.10f,32)),new Vector3(0,.02f,-.48f),baseMetal);
                r.Part(root.transform,"Launcher collar",r.Mesh("Corner launcher collar",ArkenoidMeshData.Torus(.21f,.065f)),new Vector3(0,t.ballHeight,-.31f),trim,Quaternion.Euler(90,0,0));
                r.Part(root.transform,"Launcher dark bore",r.Mesh("Corner bore",ArkenoidMeshData.Cylinder(.15f,.12f,24)),new Vector3(0,t.ballHeight,-.32f),dark,Quaternion.Euler(90,0,0));
                launcherGlow[corner]=r.Material("Corner warning "+corner,new Color(.08f,.75f,.83f),.3f,.8f,1);
                r.Part(root.transform,"Warning lens",r.Mesh("Corner warning lens",ArkenoidMeshData.Ellipsoid(.13f,.08f,.13f)),new Vector3(0,.2f,-.49f),launcherGlow[corner]);
                var lamp=new GameObject("Launch warning lamp");lamp.transform.SetParent(root.transform,false);lamp.transform.localPosition=new Vector3(0,.40f,-.49f);
                launcherLamps[corner]=lamp.AddComponent<Light>();launcherLamps[corner].type=LightType.Point;launcherLamps[corner].range=1.5f;launcherLamps[corner].shadows=LightShadows.None;
            }
            // Recessed lane markings consume the same movement extent as the motor.
            for(int i=0;i<4;i++)
            {
                ArenaSide side=(ArenaSide)i;
                var marker=new GameObject(side+" defender lane");marker.transform.SetParent(transform,false);
                marker.transform.position=ArkenoidPlayerMotor.ToWorld(geometry.HeroPosition(side,0),.027f);
                marker.transform.rotation=Quaternion.LookRotation(ArkenoidPlayerMotor.ToWorld(ArkenoidArenaGeometry.Inward(side),0));
                r.Part(marker.transform,"Travel lane",r.Mesh("Travel lane",ArkenoidMeshData.Prism(t.defenderTravel*2,.018f,.035f,.01f)),Vector3.zero,r.Material("Lane glow "+side,SideColors[i]*.6f,.4f,.6f,.5f));
            }
        }
        void LateUpdate()
        {
            if (!match || match.Simulation==null) return;
            foreach(var hero in match.Simulation.Heroes)
            {
                deadWalls[(int)hero.Side].SetActive(hero.IsEliminated);
                scoreDisplays[(int)hero.Side].text=hero.IsEliminated?"OUT":hero.Lives.ToString("00");
            }
            for(int i=0;i<4;i++)
            {
                bool warning=match.Simulation.LaunchWarningCorner==i;
                Color color=warning?new Color(1,.29f,.05f):new Color(.06f,.69f,.80f);
                float power=warning?2+Mathf.Sin(Time.time*22)*.9f:.5f;
                launcherLamps[i].color=color;launcherLamps[i].intensity=power;
                launcherGlow[i].SetColor("_EmissionColor",color*power);
            }
        }
    }
}
