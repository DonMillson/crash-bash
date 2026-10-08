using NUnit.Framework;
using UnityEngine;

namespace CrashBashRemake.Tests
{
    public sealed class ArkenoidSceneTests
    {
        [Test]
        public void EightCharactersDoNotChangeCollisionEnvelope()
        {
            var root=new GameObject("Arkenoid scene test");
            try
            {
                var resources=root.AddComponent<ArkenoidRenderResources>();
                var tuning=new ArkenoidTuning();
                foreach(CharacterId character in System.Enum.GetValues(typeof(CharacterId)))
                {
                    var players=new[]{new ArkPlayerSetup(0,ArenaSide.Bottom,character,true),
                        new ArkPlayerSetup(1,ArenaSide.Right,character,false),new ArkPlayerSetup(2,ArenaSide.Top,character,false),
                        new ArkPlayerSetup(3,ArenaSide.Left,character,false)};
                    var simulation=new ArkenoidSimulation(tuning,new BAArkenoidRules(),players);
                    foreach(var model in simulation.Heroes)
                    {
                        var go=new GameObject("Defender test");go.transform.SetParent(root.transform,false);
                        var hero=go.AddComponent<ArkenoidHeroController>();hero.Configure(simulation,model,0);
                        ArkenoidCraftVisual.Build(go.transform,hero,model.Side,character,Color.blue,resources);
                        Assert.AreEqual(Vector3.one,go.transform.localScale);
                        Assert.IsNull(go.GetComponent<Renderer>(),"Gameplay root must not render a placeholder.");
                        Assert.AreEqual(1,go.GetComponentsInChildren<Collider>().Length,"Visual art must not add colliders.");
                        Assert.IsNotNull(go.GetComponentInChildren<ArkenoidPilotVisual>());
                        var box=go.GetComponent<BoxCollider>();
                        bool horizontal=model.Side==ArenaSide.Top || model.Side==ArenaSide.Bottom;
                        Assert.AreEqual(tuning.defenderHalfWidth*2,horizontal?box.size.x:box.size.z,.0001f);
                        Assert.AreEqual(tuning.defenderHalfDepth*2,horizontal?box.size.z:box.size.x,.0001f);
                        Object.DestroyImmediate(go);
                    }
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void ProfilesAndVariantHandlersAreSeparate()
        {
            foreach(ArkenoidVariant variant in System.Enum.GetValues(typeof(ArkenoidVariant)))
            {
                var root=new GameObject("Environment test");
                try
                {
                    var environment=ArkenoidEnvironment.AddTo(root,variant);
                    Assert.AreEqual(variant,environment.Variant);
                    Assert.AreEqual(variant,ArkenoidRulesFactory.Create(variant,15).Variant);
                }
                finally{Object.DestroyImmediate(root);}
            }
        }
    }
}
