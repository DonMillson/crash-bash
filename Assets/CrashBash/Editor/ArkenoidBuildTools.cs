using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrashBashRemake.Editor
{
    public static class ArkenoidBuildTools
    {
        public const string ScenePath="Assets/CrashBash/Scenes/Crashball.unity";
        [MenuItem("CrashBash/Create Crashball scene")]
        public static void CreateScene()
        {
            if(File.Exists(ScenePath))return;
            Directory.CreateDirectory("Assets/CrashBash/Scenes");
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            var bootstrap=new GameObject("Crashball — Arkenoid").AddComponent<PrototypeBootstrap>();
            SceneManager.MoveGameObjectToScene(bootstrap.gameObject,scene);
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorSceneManager.CloseScene(scene,true);
            AssetDatabase.Refresh();
        }
        [MenuItem("CrashBash/Build Windows player")]
        public static void BuildWindows()
        {
            CreateScene();IncludeRuntimeShaders();
            string directory=Path.GetFullPath("Builds/Windows");Directory.CreateDirectory(directory);
            BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{ScenePath},locationPathName=Path.Combine(directory,"Crashball.exe"),
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.None });
            if(report.summary.result!=BuildResult.Succeeded)
                throw new InvalidOperationException("Unity Windows build failed: "+report.summary.result);
            Debug.Log("Unity Windows build succeeded: "+report.summary.outputPath);
        }
        static void IncludeRuntimeShaders()
        {
            // Runtime-authored materials still need their shaders retained in a player build.
            UnityEngine.Object graphics=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0];
            var settings=new SerializedObject(graphics);
            var shaders=settings.FindProperty("m_AlwaysIncludedShaders");
            foreach(string name in new[]{"Standard","CrashBash/Glow","Unlit/Color"})
            {
                Shader shader=Shader.Find(name);if(!shader)throw new InvalidOperationException("Missing shader "+name);
                bool found=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;
                if(!found){int i=shaders.arraySize;shaders.InsertArrayElementAtIndex(i);shaders.GetArrayElementAtIndex(i).objectReferenceValue=shader;}
            }
            settings.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
        }
    }
}
