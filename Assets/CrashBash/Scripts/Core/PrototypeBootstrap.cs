using System.Collections.Generic;
using UnityEngine;

namespace CrashBashRemake
{
    /// <summary>Builds the existing project's Ballistix slice in an empty scene.</summary>
    public class PrototypeBootstrap : MonoBehaviour
    {
        public ArkenoidVariant variant=ArkenoidVariant.BA;
        public ArkenoidTuning ba=new ArkenoidTuning(), se=new ArkenoidTuning(), ng=new ArkenoidTuning(), pi=new ArkenoidTuning();
        public List<PlayerSlot> players=new List<PlayerSlot> {
            new PlayerSlot{slotId=0,side=ArenaSide.Bottom,character=CharacterId.Crash,isHuman=true,inputIndex=0},
            new PlayerSlot{slotId=1,side=ArenaSide.Right,character=CharacterId.Tiny},
            new PlayerSlot{slotId=2,side=ArenaSide.Top,character=CharacterId.Dingodile},
            new PlayerSlot{slotId=3,side=ArenaSide.Left,character=CharacterId.Cortex},
        };
        MatchManager manager;
        ArkenoidRenderResources resources;
        Transform playRoot;
        public MatchManager Match => manager;
        public ArkenoidTuning SelectedTuning => variant==ArkenoidVariant.BA?ba:variant==ArkenoidVariant.SE?se:variant==ArkenoidVariant.NG?ng:pi;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if(FindFirstObjectByType<PrototypeBootstrap>()==null && FindFirstObjectByType<MatchManager>()==null)
                new GameObject("Crashball — Arkenoid").AddComponent<PrototypeBootstrap>();
        }
        void Start(){ BuildSession(); gameObject.AddComponent<ArkenoidFrontEnd>().Configure(this); }
        public void BuildSession()
        {
            if(playRoot) {playRoot.gameObject.SetActive(false);if(Application.isPlaying)Destroy(playRoot.gameObject);else DestroyImmediate(playRoot.gameObject);}
            var root=new GameObject("Ballistix Session");root.transform.SetParent(transform,false);playRoot=root.transform;
            resources=root.AddComponent<ArkenoidRenderResources>();
            var setup=new List<ArkPlayerSetup>();
            foreach(var slot in players) setup.Add(new ArkPlayerSetup(slot.slotId,slot.side,slot.character,slot.isHuman));
            var tuning=SelectedTuning;
            var simulation=new ArkenoidSimulation(tuning,ArkenoidRulesFactory.Create(variant,tuning.startingScore),setup);
            var managerObject=new GameObject("Arkenoid Level");managerObject.transform.SetParent(playRoot,false);
            manager=managerObject.AddComponent<MatchManager>();
            foreach(var slot in players)
            {
                var g=new GameObject("Slot "+slot.slotId+" — "+ArkenoidPilotVisual.CharacterName(slot.character));g.transform.SetParent(playRoot,false);
                slot.hero=g.AddComponent<ArkenoidHeroController>();slot.paddle=g.AddComponent<ArenaPaddle>();
                slot.paddle.side=slot.side;slot.paddle.isHuman=slot.isHuman;
                ArkenoidCraftVisual.Build(g.transform,slot.hero,slot.side,slot.character,ArkenoidArenaVisual.SideColors[(int)slot.side],resources);
            }
            var environment=ArkenoidEnvironment.AddTo(manager.gameObject,variant);
            manager.Configure(players,simulation,model=>CreateBallView(simulation,model),environment);
            var arena=new GameObject("Crashball Arena Visual");arena.transform.SetParent(playRoot,false);
            arena.AddComponent<ArkenoidArenaVisual>().Build(manager,resources);
            root.AddComponent<ArkenoidVfxDirector>().Configure(manager,resources);
            BuildCameraAndLight(tuning);
        }
        ArenaBall CreateBallView(ArkenoidSimulation simulation,ArkBallModel model)
        {
            var g=new GameObject("Ball "+model.Id);g.transform.SetParent(playRoot,false);
            var view=g.AddComponent<ArenaBall>();
            var visual=ArkenoidBallVisual.Build(g.transform,simulation.Tuning.ballRadius,resources);
            view.Configure(simulation,model,visual,manager);return view;
        }
        void BuildCameraAndLight(ArkenoidTuning tuning)
        {
            Camera camera=Camera.main;
            if(!camera)
            {
                var go=new GameObject("Main Camera");go.transform.SetParent(transform,false);go.tag="MainCamera";
                camera=go.AddComponent<Camera>();go.AddComponent<AudioListener>();
            }
            else if(!FindFirstObjectByType<AudioListener>()) camera.gameObject.AddComponent<AudioListener>();
            camera.transform.position=new Vector3(0,16,-11.2f);camera.transform.rotation=Quaternion.Euler(55,0,0);
            camera.orthographic=true;camera.orthographicSize=(tuning.goalPlane+2.2f)*Mathf.Max(1,1/camera.aspect);
            camera.allowHDR=true;camera.nearClipPlane=.1f;camera.farClipPlane=100;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.014f,.023f,.04f);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.22f,.30f,.40f);RenderSettings.ambientEquatorColor=new Color(.14f,.20f,.28f);
            RenderSettings.ambientGroundColor=new Color(.06f,.08f,.12f);
            var key=new GameObject("Arena key light").AddComponent<Light>();key.transform.SetParent(playRoot,false);
            key.type=LightType.Directional;key.intensity=1.6f;key.color=new Color(1,.91f,.80f);
            key.shadows=LightShadows.Soft;key.transform.rotation=Quaternion.Euler(48,-32,0);
            var fill=new GameObject("Arena fill light").AddComponent<Light>();fill.transform.SetParent(playRoot,false);
            fill.type=LightType.Directional;fill.intensity=.65f;fill.color=new Color(.40f,.65f,1);fill.shadows=LightShadows.None;
            fill.transform.rotation=Quaternion.Euler(30,135,0);
        }
    }
}
