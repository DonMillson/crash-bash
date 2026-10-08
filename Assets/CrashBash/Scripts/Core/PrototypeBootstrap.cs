using System.Collections.Generic;
using UnityEngine;

namespace CrashBashRemake
{
    public class PrototypeBootstrap : MonoBehaviour
    {
        public ArkenoidVariant variant = ArkenoidVariant.BA;
        public ArkenoidTuning ba = new ArkenoidTuning();
        public ArkenoidTuning se = new ArkenoidTuning();
        public ArkenoidTuning ng = new ArkenoidTuning();
        public ArkenoidTuning pi = new ArkenoidTuning();
        public List<PlayerSlot> players = new List<PlayerSlot> {
            new PlayerSlot { slotId = 0, side = ArenaSide.Bottom, character = CharacterId.Crash, isHuman = true, inputIndex = 0 },
            new PlayerSlot { slotId = 1, side = ArenaSide.Right, character = CharacterId.Tiny },
            new PlayerSlot { slotId = 2, side = ArenaSide.Top, character = CharacterId.Dingodile },
            new PlayerSlot { slotId = 3, side = ArenaSide.Left, character = CharacterId.Cortex },
        };
        ArkenoidSimulation simulation;
        MatchManager manager;
        static readonly Color[] SideColors = {
            new Color(.10f,.45f,1f), new Color(1f,.72f,.08f),
            new Color(.18f,1f,.38f), new Color(1f,.12f,.18f)
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (FindFirstObjectByType<PrototypeBootstrap>() == null && FindFirstObjectByType<MatchManager>() == null)
                new GameObject("Ballistix Prototype").AddComponent<PrototypeBootstrap>();
        }

        void Start()
        {
            RenderSettings.ambientLight = new Color(.16f,.18f,.24f);
            BuildCameraAndLight();
            BuildArena();

            var tuning = variant == ArkenoidVariant.BA ? ba : variant == ArkenoidVariant.SE ? se : variant == ArkenoidVariant.NG ? ng : pi;
            var setup = new List<ArkPlayerSetup>();
            foreach (var slot in players) setup.Add(new ArkPlayerSetup(slot.slotId, slot.side, slot.character, slot.isHuman));
            simulation = new ArkenoidSimulation(tuning, ArkenoidRulesFactory.Create(variant, tuning.startingScore), setup);
            manager = new GameObject("Match Manager").AddComponent<MatchManager>();
            manager.transform.SetParent(transform, false);
            foreach (var slot in players) {
                slot.paddle = CreatePaddle(slot, null);
                slot.hero = slot.paddle.GetComponent<ArkenoidHeroController>();
                CreateGoal(slot.side, manager);
            }
            var environment = ArkenoidEnvironment.AddTo(manager.gameObject, variant);
            manager.Configure(players, simulation, CreateBallView, environment);
        }

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

        ArenaBall CreateBallView(ArkBallModel model)
        {
            var g = new GameObject("Ball " + model.Id);
            g.transform.SetParent(transform, false);
            g.AddComponent<Rigidbody>(); g.AddComponent<SphereCollider>();
            var view = g.AddComponent<ArenaBall>();
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Ball Visual"; visual.transform.SetParent(g.transform, false);
            visual.transform.localScale = Vector3.one * simulation.Tuning.ballRadius * 2;
            Destroy(visual.GetComponent<Collider>());
            visual.GetComponent<Renderer>().sharedMaterial = Mat("BallMetal", new Color(.55f,.63f,.72f), .9f,.9f);
            view.Configure(simulation, model, visual.transform);
            return view;
        }

        ArenaPaddle CreatePaddle(PlayerSlot slot, Transform target)
        {
            var g = new GameObject($"Defender {slot.slotId} - {slot.character}");
            g.transform.SetParent(transform, false);
            var hero = g.AddComponent<ArkenoidHeroController>();
            hero.human = slot.isHuman; hero.inputIndex = slot.inputIndex;
            ArkenoidCraftVisual.Build(g.transform, slot.side, SideColors[(int)slot.side]);
            var adapter = g.AddComponent<ArenaPaddle>();
            adapter.side = slot.side; adapter.isHuman = slot.isHuman;
            return adapter;
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