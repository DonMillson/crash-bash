using System.Collections.Generic;
using UnityEngine;

namespace CrashBashRemake
{
    public class PrototypeBootstrap : MonoBehaviour
    {
        static readonly Color[] SideColors = {
            new Color(.10f,.45f,1f), new Color(1f,.72f,.08f),
            new Color(.18f,1f,.38f), new Color(1f,.12f,.18f)
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (FindFirstObjectByType<PrototypeBootstrap>() == null)
                new GameObject("Ballistix Prototype").AddComponent<PrototypeBootstrap>();
        }

        void Start()
        {
            RenderSettings.ambientLight = new Color(.16f,.18f,.24f);
            BuildCameraAndLight();
            BuildArena();

            var manager = new GameObject("Match Manager").AddComponent<MatchManager>();
            var ball = CreateBall();
            var slots = new List<PlayerSlot> {
                NewSlot(0,ArenaSide.Bottom,CharacterId.Crash,true),
                NewSlot(1,ArenaSide.Right,CharacterId.Tiny,false),
                NewSlot(2,ArenaSide.Top,CharacterId.Dingodile,false),
                NewSlot(3,ArenaSide.Left,CharacterId.Cortex,false)
            };
            foreach (var slot in slots) {
                slot.paddle = CreatePaddle(slot, ball.transform);
                CreateGoal(slot.side, manager);
            }
            manager.Configure(slots, ball);
        }

        PlayerSlot NewSlot(int id,ArenaSide side,CharacterId c,bool human) =>
            new PlayerSlot { slotId=id, side=side, character=c, isHuman=human, lives=5 };

        Material Mat(string name, Color c, float metallic=.15f, float smooth=.5f)
        {
            var shader=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m=new Material(shader){name=name,color=c};
            if(m.HasProperty("_Metallic")) m.SetFloat("_Metallic",metallic);
            if(m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness",smooth);
            return m;
        }

        GameObject Cube(string name,Vector3 p,Vector3 s,Material m)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name;
            g.transform.SetPositionAndRotation(p,Quaternion.identity); g.transform.localScale=s;
            g.GetComponent<Renderer>().material=m; return g;
        }

        void BuildCameraAndLight()
        {
            Camera cam=Camera.main;
            if(!cam){var g=new GameObject("Main Camera"); cam=g.AddComponent<Camera>(); g.tag="MainCamera";}
            cam.transform.position=new Vector3(0,14.8f,-10.8f);
            cam.transform.rotation=Quaternion.Euler(55f,0,0); cam.fieldOfView=50; cam.backgroundColor=new Color(.025f,.03f,.06f);
            var l=new GameObject("Key Light").AddComponent<Light>(); l.type=LightType.Directional; l.intensity=1.45f;
            l.transform.rotation=Quaternion.Euler(48,-32,0);
        }

        void BuildArena()
        {
            var stone=Mat("DarkStone",new Color(.11f,.13f,.16f),.05f,.35f);
            var floor=Mat("ArenaFloor",new Color(.18f,.21f,.25f),.25f,.7f);
            Cube("Arena Floor",new Vector3(0,-.25f,0),new Vector3(13,.5f,13),floor);
            Cube("Center Plate",new Vector3(0,.015f,0),new Vector3(4.1f,.06f,4.1f),Mat("Center",new Color(.24f,.27f,.3f),.45f,.8f));
            // Four corner blocks leave a true open goal on every side.
            float c=4.7f;
            Cube("Wall B-L",new Vector3(-c,.55f,-6f),new Vector3(3.6f,1.1f,.38f),stone);
            Cube("Wall B-R",new Vector3(c,.55f,-6f),new Vector3(3.6f,1.1f,.38f),stone);
            Cube("Wall T-L",new Vector3(-c,.55f,6f),new Vector3(3.6f,1.1f,.38f),stone);
            Cube("Wall T-R",new Vector3(c,.55f,6f),new Vector3(3.6f,1.1f,.38f),stone);
            Cube("Wall L-B",new Vector3(-6f,.55f,-c),new Vector3(.38f,1.1f,3.6f),stone);
            Cube("Wall L-T",new Vector3(-6f,.55f,c),new Vector3(.38f,1.1f,3.6f),stone);
            Cube("Wall R-B",new Vector3(6f,.55f,-c),new Vector3(.38f,1.1f,3.6f),stone);
            Cube("Wall R-T",new Vector3(6f,.55f,c),new Vector3(.38f,1.1f,3.6f),stone);
            for(int i=0;i<4;i++) BuildGoalFrame((ArenaSide)i,SideColors[i]);
        }

        void BuildGoalFrame(ArenaSide side,Color color)
        {
            var glow=Mat("Goal_"+side,color,.5f,.85f);
            bool horizontal=side==ArenaSide.Bottom||side==ArenaSide.Top;
            float edge=(side==ArenaSide.Bottom||side==ArenaSide.Left)?-6.05f:6.05f;
            if(horizontal){
                Cube(side+" GoalRail L",new Vector3(-2.7f,.65f,edge),new Vector3(.18f,1.3f,.5f),glow);
                Cube(side+" GoalRail R",new Vector3(2.7f,.65f,edge),new Vector3(.18f,1.3f,.5f),glow);
            } else {
                Cube(side+" GoalRail B",new Vector3(edge,.65f,-2.7f),new Vector3(.5f,1.3f,.18f),glow);
                Cube(side+" GoalRail T",new Vector3(edge,.65f,2.7f),new Vector3(.5f,1.3f,.18f),glow);
            }
        }

        ArenaBall CreateBall()
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Sphere); g.name="Energy Ball";
            g.transform.position=new Vector3(0,.55f,0); g.transform.localScale=Vector3.one*.72f;
            g.GetComponent<Renderer>().material=Mat("BallMetal",new Color(.55f,.63f,.72f),.9f,.9f);
            var rb=g.AddComponent<Rigidbody>(); rb.mass=.8f; rb.linearDamping=0; rb.angularDamping=.05f;
            return g.AddComponent<ArenaBall>();
        }

        ArenaPaddle CreatePaddle(PlayerSlot slot,Transform target)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=$"Defender {slot.slotId} - {slot.character}";
            bool h=slot.side==ArenaSide.Bottom||slot.side==ArenaSide.Top;
            g.transform.position=slot.side switch {
                ArenaSide.Bottom=>new Vector3(0,.55f,-5.45f), ArenaSide.Top=>new Vector3(0,.55f,5.45f),
                ArenaSide.Left=>new Vector3(-5.45f,.55f,0), _=>new Vector3(5.45f,.55f,0)};
            g.transform.localScale=h?new Vector3(1.45f,1.05f,.48f):new Vector3(.48f,1.05f,1.45f);
            g.GetComponent<Renderer>().material=Mat("Defender_"+slot.side,SideColors[(int)slot.side],.35f,.7f);
            // The cube is gameplay collision only; hide it and render a separate hovercraft visual.
            var renderer = g.GetComponent<Renderer>();
            if (renderer) renderer.enabled = false;

            g.AddComponent<Rigidbody>();

            var motor = g.AddComponent<ArkenoidPlayerMotor>();
            motor.side = slot.side;

            var hero = g.AddComponent<ArkenoidHeroController>();
            hero.human = slot.isHuman;
            hero.ballTarget = target;

            ArkenoidCraftVisual.Build(g.transform, slot.side, SideColors[(int)slot.side]);

            // Compatibility adapter for the current match manager; remove after full Arkenoid migration.
            var p=g.AddComponent<ArenaPaddle>();
            p.side=slot.side;
            p.isHuman=false;
            p.enabled=false;
            p.ballTarget=target;
            return p;
        }

        void CreateGoal(ArenaSide side,MatchManager manager)
        {
            var g=new GameObject("Goal Trigger "+side); var box=g.AddComponent<BoxCollider>(); box.isTrigger=true;
            bool h=side==ArenaSide.Bottom||side==ArenaSide.Top;
            float e=(side==ArenaSide.Bottom||side==ArenaSide.Left)?-6.35f:6.35f;
            g.transform.position=h?new Vector3(0,.5f,e):new Vector3(e,.5f,0);
            box.size=h?new Vector3(5.2f,2f,.45f):new Vector3(.45f,2f,5.2f);
            var z=g.AddComponent<GoalZone>(); z.side=side; z.match=manager;
        }
    }
}